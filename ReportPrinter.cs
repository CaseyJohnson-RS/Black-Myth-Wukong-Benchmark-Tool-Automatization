using System.Text;

namespace WukongBench;

public static class ReportPrinter
{
    public static string Build(SystemInfo system, IReadOnlyList<(BenchmarkProfile Profile, BenchmarkResult Result)> runs)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Система ===");
        sb.AppendLine($"ОС:  {system.Os}");
        sb.AppendLine($"CPU: {system.Cpu} ({system.LogicalCores} логических ядер)");
        sb.AppendLine($"RAM: {system.RamGb:0.#} ГБ");
        foreach (var gpu in system.Gpus) sb.AppendLine($"GPU: {gpu}");
        if (runs.Count > 0)
        {
            var r = runs[0].Result;
            sb.AppendLine($"Бенчмарк видит: {r.GPUModel} ({r.VideoMemSize}), версия игры {r.GameVer}");
        }

        sb.AppendLine();
        sb.AppendLine("=== Результаты ===");
        Row(sb, "Метрика", runs.Select(x => x.Profile.Name + "-тест"));
        Row(sb, "FPS средний", runs.Select(x => $"{x.Result.FPSAvg:0}"));
        Row(sb, "FPS минимум", runs.Select(x => $"{x.Result.FPSMin:0}"));
        Row(sb, "FPS максимум", runs.Select(x => $"{x.Result.FPSMax:0}"));
        Row(sb, "FPS95", runs.Select(x => $"{x.Result.FPS95:0}"));
        Row(sb, "Загрузка CPU, %", runs.Select(x => $"{x.Result.CPUAvg:0}"));
        Row(sb, "Загрузка GPU, %", runs.Select(x => $"{x.Result.GPUAvg:0}"));
        Row(sb, "Время кадра CPU, мс", runs.Select(x => $"{x.Result.AvgCpuFrameTime:0.0}"));
        Row(sb, "Время кадра GPU, мс", runs.Select(x => $"{x.Result.AvgGpuFrameTime:0.0}"));
        Row(sb, "Кадров, где CPU дольше GPU", runs.Select(x => $"{x.Result.CpuBoundShare:0}%"));
        Row(sb, "Видеопамять, ГБ", runs.Select(x => $"{x.Result.VideoMem:0.0}"));

        sb.AppendLine();
        sb.AppendLine("=== Настройки (по данным бенчмарка) ===");
        Row(sb, "Параметр", runs.Select(x => x.Profile.Name + "-тест"));
        Row(sb, "Разрешение", runs.Select(x => x.Result.ScreenResolution));
        Row(sb, "Масштаб рендеринга, %", runs.Select(x => x.Result.ImageQuality.ToString()));
        Row(sb, "Общий уровень качества", runs.Select(x => x.Result.QualityLevel.ToString()));
        Row(sb, "Дальность прорисовки", runs.Select(x => x.Result.ViewDistance.ToString()));
        Row(sb, "Сглаживание", runs.Select(x => x.Result.AntiAliasing.ToString()));
        Row(sb, "Постобработка", runs.Select(x => x.Result.PostProcessing.ToString()));
        Row(sb, "Тени", runs.Select(x => x.Result.ShadowQuality.ToString()));
        Row(sb, "Текстуры", runs.Select(x => x.Result.TextureQuality.ToString()));
        Row(sb, "Материалы", runs.Select(x => x.Result.MaterialQuality.ToString()));
        Row(sb, "Растительность", runs.Select(x => x.Result.VegetationQuality.ToString()));
        Row(sb, "Апскейлер (код)", runs.Select(x => x.Result.Dlss.ToString()));
        Row(sb, "Генерация кадров", runs.Select(x => OnOff(x.Result.InsertFrame)));
        Row(sb, "Трассировка лучей", runs.Select(x => OnOff(x.Result.Rtx)));
        Row(sb, "Размытие в движении", runs.Select(x => OnOff(x.Result.MotionBlur)));

        // Игра может молча подставить другие значения — проверяем, что применилось то, что задумано.
        foreach (var (profile, result) in runs)
        {
            if (result.ScreenResolution != profile.ExpectedResolution)
                sb.AppendLine($"ВНИМАНИЕ [{profile.Name}]: разрешение {result.ScreenResolution}, ожидалось {profile.ExpectedResolution}.");
            if (result.QualityLevel != profile.ExpectedQualityLevel)
                sb.AppendLine($"ВНИМАНИЕ [{profile.Name}]: уровень качества {result.QualityLevel}, ожидался {profile.ExpectedQualityLevel}.");
        }

        return sb.ToString();
    }

    private static string OnOff(int value) => value == 0 ? "выкл" : $"вкл ({value})";

    private static void Row(StringBuilder sb, string name, IEnumerable<string> values) =>
        sb.AppendLine($"{name,-28}" + string.Concat(values.Select(v => $"{v,14}")));
}
