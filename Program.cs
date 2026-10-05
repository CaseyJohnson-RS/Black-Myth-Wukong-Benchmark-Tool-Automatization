using WukongBench;

const string DefaultBenchmarkDir =
    @"C:\Program Files (x86)\Steam\steamapps\common\Black Myth Wukong Benchmark Tool";

try
{
    NativeInput.EnableDpiAwareness();

    var benchmarkDir = args.Length > 0 ? args[0] : DefaultBenchmarkDir;
    var exePath = FindExe(benchmarkDir);
    var iniPath = Path.Combine(benchmarkDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");
    var historyDir = Path.Combine(Path.GetTempPath(), "b1", "BenchMarkHistory", "Tool");

    var settings = new SettingsFile(iniPath);
    settings.RecoverAfterCrash();

    var outputDir = Path.Combine(AppContext.BaseDirectory, "results", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
    Directory.CreateDirectory(outputDir);

    var system = SystemInfo.Collect();
    var runner = new BenchmarkRunner(historyDir);
    var results = new List<(BenchmarkProfile Profile, BenchmarkResult Result)>();

    settings.Backup();
    Console.CancelKeyPress += (_, _) => settings.Restore();
    try
    {
        foreach (var profile in new[] { BenchmarkProfile.Cpu, BenchmarkProfile.Gpu })
        {
            Console.WriteLine($"[{profile.Name}] Применяю настройки и запускаю бенчмарк...");
            settings.Apply(profile);

            var (rawPath, result) = runner.Run(exePath);
            File.Copy(rawPath, Path.Combine(outputDir, $"{profile.Name.ToLowerInvariant()}_raw.json"));
            results.Add((profile, result));

            Console.WriteLine($"[{profile.Name}] Готово: средний FPS {result.FPSAvg}");
        }
    }
    finally
    {
        settings.Restore();
    }

    var report = ReportPrinter.Build(system, results);
    Console.WriteLine();
    Console.WriteLine(report);
    File.WriteAllText(Path.Combine(outputDir, "report.txt"), report);
    Console.WriteLine($"Отчёт и исходные JSON сохранены в: {outputDir}");
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine($"Ошибка: {e.Message}");
    return 1;
}

static string FindExe(string benchmarkDir)
{
    string[] candidates =
    [
        Path.Combine(benchmarkDir, "b1.exe"),
        Path.Combine(benchmarkDir, "b1", "Binaries", "Win64", "b1-Win64-Shipping.exe"),
    ];
    return candidates.FirstOrDefault(File.Exists)
        ?? throw new FileNotFoundException($"Не найден exe бенчмарка в {benchmarkDir}. Передайте путь первым аргументом.");
}
