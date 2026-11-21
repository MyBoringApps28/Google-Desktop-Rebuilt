using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoogleDesktopRemake;

public class CalendarEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public DateTime Date { get; set; }
    public bool AllDay { get; set; } = true;
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }

    [JsonIgnore] public DateTime StartAt => Date.Date + (AllDay ? TimeSpan.Zero : Start);
    [JsonIgnore] public DateTime EndAt => AllDay ? Date.Date.AddDays(1) : Date.Date + End;
    [JsonIgnore] public string WhenText => AllDay ? "All day" : $"{Fmt(Start)} - {Fmt(End)}";

    static string Fmt(TimeSpan t) => (DateTime.Today + t).ToString("t");
}

/// <summary>Calendar events, saved to %AppData%\GoogleDesktopRemake\events.json.</summary>
public static class EventStore
{
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoogleDesktopRemake", "events.json");

    static readonly List<CalendarEvent> _all = Load();
    public static event Action? Changed;

    static List<CalendarEvent> Load()
    {
        try { if (File.Exists(FilePath)) return JsonSerializer.Deserialize<List<CalendarEvent>>(File.ReadAllText(FilePath)) ?? new(); }
        catch { }
        return new();
    }

    static void Save()
    {
        if (Settings.IsResettingAppData) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_all));
        }
        catch { }
    }

    public static void Add(CalendarEvent e) { _all.Add(e); Save(); Changed?.Invoke(); }
    public static void Remove(string id) { _all.RemoveAll(e => e.Id == id); Save(); Changed?.Invoke(); }

    public static List<CalendarEvent> On(DateTime day) =>
        _all.Where(e => e.Date.Date == day.Date).OrderBy(e => e.StartAt).ToList();

    public static bool HasEvents(DateTime day) => _all.Any(e => e.Date.Date == day.Date);

    public static List<CalendarEvent> Upcoming(DateTime now, int count) =>
        _all.Where(e => e.EndAt > now).OrderBy(e => e.StartAt).Take(count).ToList();

    /// <summary>"in 5 days" ... "in 1 day 3 hr" ... "in 2 hr 10 min" ... "in 12 min" ... "happening now".</summary>
    public static string Countdown(CalendarEvent e, DateTime now)
    {
        if (e.AllDay)
        {
            int days = (e.Date.Date - now.Date).Days;
            return days <= 0 ? "today" : days == 1 ? "tomorrow" : $"in {days} days";
        }
        var diff = e.StartAt - now;
        if (diff <= TimeSpan.Zero) return "happening now";
        if (diff.TotalHours >= 48) return $"in {(e.Date.Date - now.Date).Days} days";
        if (diff.TotalHours >= 24) return diff.Hours == 0 ? "in 1 day" : $"in 1 day {diff.Hours} hr";
        if (diff.TotalHours >= 1) return diff.Minutes == 0 ? $"in {(int)diff.TotalHours} hr" : $"in {(int)diff.TotalHours} hr {diff.Minutes} min";
        int m = (int)Math.Ceiling(diff.TotalMinutes);
        return m <= 1 ? "in 1 min" : $"in {m} min";
    }
}
