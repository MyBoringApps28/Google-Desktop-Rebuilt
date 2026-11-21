namespace GoogleDesktopRemake;

public record BackgroundPreset(string Key, string Name, string Style, (string color, double offset)[] Stops,
                               double Angle, bool Streaks, double Strength);

public static class BackgroundPresets
{
    public static List<GradientStopConfig> Windows7Stops() => new()
    {
        new("#1A1A55", 0.00),
        new("#1646C8", 0.35),
        new("#1E78E6", 0.70),
        new("#10D0F0", 1.00)
    };

    public static readonly BackgroundPreset[] All =
    {
        new("aero", "Aero blue (default)", "Theme", Array.Empty<(string, double)>(), 90, false, 0.6),
        new("win7", "Windows 7", "Gradient", new[] { ("#1A1A55", 0.0), ("#1646C8", 0.35), ("#1E78E6", 0.70), ("#10D0F0", 1.0) }, 90, true, 0.7),
        new("sunset", "Sunset", "Gradient", new[] { ("#2B1055", 0.0), ("#7A2A8C", 0.35), ("#E8467C", 0.70), ("#FFB347", 1.0) }, 90, false, 0.6),
        new("aurora", "Aurora", "Gradient", new[] { ("#04202F", 0.0), ("#0B6E6A", 0.40), ("#2EC27E", 0.70), ("#7B5CE6", 1.0) }, 70, true, 0.5),
        new("ocean", "Deep ocean", "Gradient", new[] { ("#02101F", 0.0), ("#0A3A66", 0.50), ("#0E7FA8", 1.0) }, 90, true, 0.45),
        new("graphite", "Graphite", "Gradient", new[] { ("#101214", 0.0), ("#2C3035", 0.50), ("#5A6068", 1.0) }, 90, true, 0.35),
        new("rose", "Rose gold", "Gradient", new[] { ("#4A2230", 0.0), ("#B5646F", 0.50), ("#F2B8A0", 1.0) }, 110, false, 0.6),
    };

    public static BackgroundPreset? Find(string key) => All.FirstOrDefault(p => p.Key == key);
}
