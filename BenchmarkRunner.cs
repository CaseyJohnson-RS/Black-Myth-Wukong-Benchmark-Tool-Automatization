using System.Diagnostics;

namespace WukongBench;

/// <summary>Запуск бенчмарка, прохождение меню и ожидание результата.</summary>
public sealed class BenchmarkRunner(string historyDir)
{
    private static readonly string[] ProcessNames = ["b1", "b1-Win64-Shipping"];
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StepDelay = TimeSpan.FromSeconds(3);

    // Доли клиентской области окна, проверены на 1280×720, 1600×900 и 1920×1080.
    private const double StartButtonX = 0.10, StartButtonY = 0.45;     // «Тест быстродействия»
    private const double ConfirmButtonX = 0.39, ConfirmButtonY = 0.59; // «Подтвердить»

    public (string Path, BenchmarkResult Result) Run(string exePath)
    {
        if (FindBenchmarkProcesses().Any())
            throw new InvalidOperationException("Бенчмарк уже запущен. Закройте его и повторите.");

        Directory.CreateDirectory(historyDir);
        var existing = Directory.GetFiles(historyDir).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exePath),
        })?.Dispose();

        var stopwatch = Stopwatch.StartNew();
        var step = 0;
        try
        {
            while (stopwatch.Elapsed < Timeout)
            {
                // Новый JSON = прогон завершён. Проверяем до каждого действия,
                // чтобы не кликнуть по экрану результатов (там есть «Пройти заново»).
                var finished = FindNewResult(existing);
                if (finished is not null) return finished.Value;

                var window = FindGameWindow();
                if (window != IntPtr.Zero)
                {
                    if (!NativeInput.IsForeground(window)) NativeInput.TryActivate(window);

                    // Кликаем только по своему окну, иначе можно попасть в чужое приложение.
                    if (NativeInput.IsForeground(window)) DoMenuStep(window, step++);
                }

                Thread.Sleep(StepDelay);
            }
            throw new TimeoutException($"Бенчмарк не выдал результат за {Timeout.TotalMinutes} мин.");
        }
        finally
        {
            KillBenchmark();
        }
    }

    // Слепой цикл: заставка → «Тест быстродействия» → «Подтвердить».
    // Клики во время самого теста его не прерывают (проверено вручную).
    private static void DoMenuStep(IntPtr window, int step)
    {
        switch (step % 3)
        {
            case 0: NativeInput.PressKey(NativeInput.VkReturn); break;
            case 1: NativeInput.ClickRelative(window, StartButtonX, StartButtonY); break;
            case 2: NativeInput.ClickRelative(window, ConfirmButtonX, ConfirmButtonY); break;
        }
    }

    private (string, BenchmarkResult)? FindNewResult(HashSet<string> existing)
    {
        if (!Directory.Exists(historyDir)) return null;

        foreach (var file in Directory.GetFiles(historyDir).Where(f => !existing.Contains(f)))
        {
            // Файл, который ещё дописывается, не распарсится — просто проверим на следующем шаге.
            var result = BenchmarkResult.TryLoad(file);
            if (result is not null) return (file, result);
        }
        return null;
    }

    private static IntPtr FindGameWindow() =>
        FindBenchmarkProcesses()
            .Select(p => p.MainWindowHandle)
            .FirstOrDefault(h => h != IntPtr.Zero);

    private static IEnumerable<Process> FindBenchmarkProcesses() =>
        ProcessNames.SelectMany(Process.GetProcessesByName);

    private static void KillBenchmark()
    {
        // Убиваем, а не закрываем штатно: так игра не перезапишет наш ini при выходе.
        foreach (var process in FindBenchmarkProcesses())
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(10_000);
            }
            catch (InvalidOperationException) { } // процесс уже завершился
        }
    }
}
