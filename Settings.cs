using System.Globalization;
using System.IO;
using System.Text.Json;

namespace GoogleDesktopRemake;

public class GradientStopConfig
{
    public string Color { get; set; } = "#FFFFFF";
    public double Offset { get; set; }
    public GradientStopConfig() { }
    public GradientStopConfig(string color, double offset) { Color = color; Offset = offset; }
}

public class FeedConfig
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public FeedConfig() { }
    public FeedConfig(string name, string url) { Name = name; Url = url; }
}

/// <summary>User settings, saved to %AppData%\GoogleDesktopRemake\settings.json.</summary>
public class Settings
{
    public const double SidebarWidth = 200;
    const string DefaultNyt = "https://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml";

    public int Version { get; set; } = 0;
    public bool SetupCompleted { get; set; }

    // Sidebar behaviour
    public bool KeepOnTop { get; set; } = false;           // false = lives on the desktop, hidden behind windows
    public bool MoveDesktopIcons { get; set; } = true;
    public bool RestoreIconsOnExit { get; set; } = true;
    public bool EnableCalendar { get; set; } = false;      // optional gadgets (they shrink the news feed)
    public bool EnableNotes { get; set; } = false;
    public bool EnableCalculator { get; set; } = false;
    public bool ShowPhotos { get; set; } = true;
    public bool ShowNews { get; set; } = true;
    public bool EnableDoubleCtrlSearch { get; set; } = true;

    // Background
    public string BackgroundStyle { get; set; } = "Theme";
    public List<GradientStopConfig> GradientStops { get; set; } = BackgroundPresets.Windows7Stops();
    public double GradientAngle { get; set; } = 90;
    public double GradientShift { get; set; }
    public bool Streaks { get; set; }
    public double StreakStrength { get; set; } = 0.6;

    // Look
    public string Theme { get; set; } = "Aero";           // Aero | Dark | Custom
    public string CustomColor { get; set; } = "#1565B8";
    public double SidebarOpacity { get; set; } = 0.5;     // 0.05 (see-through) .. 1 (solid)

    // Weather
    public string City { get; set; } = "San Francisco";
    public double Latitude { get; set; } = 37.7749;
    public double Longitude { get; set; } = -122.4194;
    public bool UseFahrenheit { get; set; } = true;

    // News (any number of RSS/Atom feeds)
    public List<FeedConfig> Feeds { get; set; } = new();
    public string NewsFeedUrl { get; set; } = DefaultNyt;     // legacy (migrated into Feeds)
    public string NewsSourceName { get; set; } = "New York Times";

    // Photos
    public string PhotoFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

    // Clock
    public string ClockStyle { get; set; } = "Analog";     // Analog | AnalogBlack | Flip | Segment
    public bool Use24Hour { get; set; } = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains('H');
    public string ClockLabel { get; set; } = Environment.UserName;
    public string TimeSource { get; set; } = "System";      // "System" or "NTP"
    public string NtpServer { get; set; } = "time.windows.com";
    public string TimeZoneId { get; set; } = "";            // "" = this PC's zone

    // ---- persistence ----
    static readonly string AppDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoogleDesktopRemake");
    static readonly string FilePath = Path.Combine(AppDataDirectory, "settings.json");

    public static bool IsResettingAppData { get; private set; }
    public static Settings Current { get; private set; } = Load();
    public static event Action? Changed;

    static Settings Load()
    {
        Settings s;
        bool keepOnTopWasSet = false;
        try
        {
            if (!File.Exists(FilePath)) return Migrate(new(), false);

            var json = File.ReadAllText(FilePath);
            s = JsonSerializer.Deserialize<Settings>(json) ?? new();
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(SetupCompleted), out var setupCompleted) ||
                setupCompleted.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                s.SetupCompleted = true;
            }
            if (document.RootElement.TryGetProperty(nameof(KeepOnTop), out var keepOnTop) &&
                keepOnTop.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                keepOnTopWasSet = true;
            }
            else if (document.RootElement.TryGetProperty("AlwaysOnTop", out var alwaysOnTop) &&
                     alwaysOnTop.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                s.KeepOnTop = alwaysOnTop.GetBoolean();
                keepOnTopWasSet = true;
            }
        }
        catch { s = new(); }
        return Migrate(s, keepOnTopWasSet);
    }

    static Settings Migrate(Settings s, bool keepOnTopWasSet)
    {
        if (s.Version < 2)
        {
            if (!keepOnTopWasSet) s.KeepOnTop = false;
            s.Version = 2;
        }   // "on top" is OFF by default, while an existing preference is preserved
        if (s.ClockStyle == "Nixie") s.ClockStyle = "Analog";
        if (s.Feeds.Count == 0)
        {
            bool legacyCustom = !string.IsNullOrWhiteSpace(s.NewsFeedUrl) && s.NewsFeedUrl != DefaultNyt;
            if (legacyCustom) s.Feeds.Add(new FeedConfig(s.NewsSourceName, s.NewsFeedUrl));
            else
            {
                s.Feeds.Add(new FeedConfig("New York Times", DefaultNyt));
                s.Feeds.Add(new FeedConfig("BBC News", "https://feeds.bbci.co.uk/news/rss.xml"));
            }
        }
        return s;
    }

    public static void Save(Settings s)
    {
        if (IsResettingAppData) return;
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        Current = s;
        Changed?.Invoke();
    }

    public static void ResetAppData()
    {
        IsResettingAppData = true;
        try
        {
            if (Directory.Exists(AppDataDirectory)) Directory.Delete(AppDataDirectory, recursive: true);
        }
        catch
        {
            IsResettingAppData = false;
            throw;
        }
    }

    public Settings Clone() => JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(this))!;
}
