using System.Text.Json;
using System.Text.Json.Serialization;

namespace WukongBench;

/// <summary>JSON из %TEMP%\b1\BenchMarkHistory\Tool. Имена свойств = ключи файла.</summary>
public sealed class BenchmarkResult
{
    public double FPSAvg { get; set; }
    public double FPSMax { get; set; }
    public double FPSMin { get; set; }
    public double FPS95 { get; set; }
    public double CPUAvg { get; set; }
    public double GPUAvg { get; set; }
    public double VideoMem { get; set; }

    public string GameVer { get; set; } = "";
    public string SysVer { get; set; } = "";
    public string CPUModel { get; set; } = "";
    public string GPUModel { get; set; } = "";
    public string GpuDriverVer { get; set; } = "";
    public string VideoMemSize { get; set; } = "";
    public string SysMem { get; set; } = "";

    public int ScreenMode { get; set; }
    public string ScreenResolution { get; set; } = "";
    public int QualityLevel { get; set; }
    public int ImageQuality { get; set; }       // масштаб рендеринга, %
    public int ViewDistance { get; set; }
    public int AntiAliasing { get; set; }
    public int PostProcessing { get; set; }
    public int ShadowQuality { get; set; }
    public int TextureQuality { get; set; }
    public int MaterialQuality { get; set; }
    public int VegetationQuality { get; set; }
    public int MotionBlur { get; set; }
    public int Rtx { get; set; }
    public int Dlss { get; set; }               // фактически SuperResolutionSampling (апскейлер)
    public int InsertFrame { get; set; }
    public int Dx12 { get; set; }

    public List<FrameRecord> Records { get; set; } = [];

    [JsonIgnore] public double AvgCpuFrameTime => Records.Count == 0 ? 0 : Records.Average(r => r.CPUFrameTime);
    [JsonIgnore] public double AvgGpuFrameTime => Records.Count == 0 ? 0 : Records.Average(r => r.GPUFrameTime);

    /// <summary>Доля кадров, где процессор считал кадр дольше видеокарты, %.</summary>
    [JsonIgnore]
    public double CpuBoundShare =>
        Records.Count == 0 ? 0 : 100.0 * Records.Count(r => r.CPUFrameTime > r.GPUFrameTime) / Records.Count;

    public static BenchmarkResult? TryLoad(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return JsonSerializer.Deserialize<BenchmarkResult>(stream);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

public sealed class FrameRecord
{
    public double FrameRate { get; set; }
    public double CPUUsage { get; set; }
    public double GPUUsage { get; set; }
    public double CPUFrameTime { get; set; }
    public double GPUFrameTime { get; set; }
    public double VideoMemoryUsage { get; set; }
}
