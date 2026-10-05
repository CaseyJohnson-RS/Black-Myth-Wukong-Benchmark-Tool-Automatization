namespace WukongBench;

/// <summary>Набор настроек одного прохода бенчмарка.</summary>
/// <param name="UiSettings">Значения внутри UISettingData (то, что показывает меню игры).</param>
/// <param name="IniValues">Обычные ключи ini: (секция, ключ) → значение.</param>
public sealed record BenchmarkProfile(
    string Name,
    IReadOnlyDictionary<string, string> UiSettings,
    IReadOnlyDictionary<(string Section, string Key), string> IniValues,
    string ExpectedResolution,
    int ExpectedQualityLevel)
{
    private const string MainSection = "/Script/GSGameSettings.GSGameUserSettings";
    private const string SgSection = "ScalabilityGroups";

    private static readonly string[] UiQualityKeys =
    [
        "ViewDistance", "AntiAliasing", "PostProcessing", "ShadowQuality", "TextureQuality",
        "FxQuality", "MaterialQuality", "VegetationQuality", "GlobalIllumination", "ReflectionQuality",
    ];

    private static readonly string[] SgQualityKeys =
    [
        "sg.ViewDistanceQuality", "sg.AntiAliasingQuality", "sg.ShadowQuality", "sg.GlobalIlluminationQuality",
        "sg.ReflectionQuality", "sg.PostProcessQuality", "sg.TextureQuality", "sg.EffectsQuality",
        "sg.FoliageQuality", "sg.ShadingQuality",
    ];

    public static BenchmarkProfile Cpu { get; } = Create(
        "CPU", width: 1280, height: 720, resolutionIndex: "2",
        renderHeight: "356", renderPercent: "50", uiQuality: 1);

    public static BenchmarkProfile Gpu { get; } = Create(
        "GPU", width: 1920, height: 1080, resolutionIndex: "0",
        renderHeight: "1080", renderPercent: "100", uiQuality: 5);

    private static BenchmarkProfile Create(
        string name, int width, int height, string resolutionIndex,
        string renderHeight, string renderPercent, int uiQuality)
    {
        var ui = new Dictionary<string, string>
        {
            ["ScreenMode"] = "1",               // полноэкранное окно
            ["ScreenResolution"] = resolutionIndex,
            ["ImageQuality"] = renderHeight,    // высота рендера в пикселях
            ["WindowFullImageQuality"] = "0",
            ["SuperResolutionSampling"] = "3",  // апскейлер, как в рабочем конфиге
            ["Vsync"] = "0",
            ["LockFrameRate"] = "0",
            ["MotionBlur"] = "0",
            ["InsertFrame"] = "0",              // без генерации кадров
            ["Rtx"] = "0",
            ["QualityLevel"] = uiQuality.ToString(),
        };
        foreach (var key in UiQualityKeys) ui[key] = uiQuality.ToString();

        var ini = new Dictionary<(string, string), string>
        {
            [(MainSection, "FullscreenMode")] = "1",
            [(MainSection, "LastConfirmedFullscreenMode")] = "1",
            [(MainSection, "PreferredFullscreenMode")] = "1",
            [(MainSection, "ResolutionSizeX")] = width.ToString(),
            [(MainSection, "ResolutionSizeY")] = height.ToString(),
            [(MainSection, "LastUserConfirmedResolutionSizeX")] = width.ToString(),
            [(MainSection, "LastUserConfirmedResolutionSizeY")] = height.ToString(),
            [(MainSection, "bUseVSync")] = "False",
            [(MainSection, "FrameRateLimit")] = "0.000000",
            [(SgSection, "sg.ResolutionQuality")] = renderPercent,
        };
        foreach (var key in SgQualityKeys) ini[(SgSection, key)] = (uiQuality - 1).ToString();

        return new BenchmarkProfile(name, ui, ini, $"{width} \u00D7 {height}", uiQuality);
    }
}
