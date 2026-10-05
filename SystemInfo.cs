using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WukongBench;

/// <summary>Характеристики ПК из реестра и WinAPI — без сторонних пакетов.</summary>
public sealed record SystemInfo(string Os, string Cpu, int LogicalCores, double RamGb, IReadOnlyList<string> Gpus)
{
    private const string DisplayAdaptersClass =
        @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    public static SystemInfo Collect() =>
        new(ReadOs(), ReadCpu(), Environment.ProcessorCount, ReadRamGb(), ReadGpus());

    private static string ReadOs()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var name = key?.GetValue("ProductName") as string ?? "Windows";
        var version = key?.GetValue("DisplayVersion") as string ?? "";
        var build = key?.GetValue("CurrentBuild") as string ?? "";

        // На Windows 11 ProductName до сих пор пишет «Windows 10».
        if (int.TryParse(build, out var b) && b >= 22000) name = name.Replace("Windows 10", "Windows 11");
        return $"{name} {version} (сборка {build})".Trim();
    }

    private static string ReadCpu()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        return (key?.GetValue("ProcessorNameString") as string)?.Trim() ?? "неизвестно";
    }

    private static double ReadRamGb()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        return GlobalMemoryStatusEx(ref status) ? status.TotalPhys / 1024.0 / 1024 / 1024 : 0;
    }

    private static List<string> ReadGpus()
    {
        var gpus = new List<string>();
        using var root = Registry.LocalMachine.OpenSubKey(DisplayAdaptersClass);
        if (root is null) return gpus;

        foreach (var name in root.GetSubKeyNames().Where(n => n.All(char.IsDigit)))
        {
            try
            {
                using var key = root.OpenSubKey(name);
                if (key?.GetValue("DriverDesc") is not string desc) continue;

                var driver = key.GetValue("DriverVersion") as string ?? "?";
                var vram = key.GetValue("HardwareInformation.qwMemorySize") is long bytes
                    ? $", {bytes / 1024.0 / 1024 / 1024:0.#} ГБ VRAM"
                    : "";
                gpus.Add($"{desc}{vram}, драйвер {driver}");
            }
            catch (System.Security.SecurityException) { } // часть подключей закрыта для чтения
        }
        return gpus;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length, MemoryLoad;
        public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")] private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);
}
