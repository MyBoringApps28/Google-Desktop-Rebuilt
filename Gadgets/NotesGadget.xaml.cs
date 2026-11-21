using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GoogleDesktopRemake.Gadgets;

public class NotesFile
{
    public List<string> Notes { get; set; } = new() { "" };
    public int Index { get; set; }
}

public partial class NotesGadget : UserControl
{
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoogleDesktopRemake", "notes.json");

    NotesFile _data = new();
    readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    bool _loading;
    Point _pinStart;
    bool _pinHeld;

    public event EventHandler? FloatRequested;

    public NotesGadget()
    {
        InitializeComponent();
        try { if (File.Exists(FilePath)) _data = JsonSerializer.Deserialize<NotesFile>(File.ReadAllText(FilePath)) ?? new(); }
        catch { }
        if (_data.Notes.Count == 0) _data.Notes.Add("");
        _data.Index = Math.Clamp(_data.Index, 0, _data.Notes.Count - 1);

        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); Save(); };
        NoteBox.TextChanged += (_, _) =>
        {
            if (_loading) return;
            _data.Notes[_data.Index] = NoteBox.Text;
            _saveTimer.Stop(); _saveTimer.Start();      // save shortly after you stop typing
        };
        Dispatcher.ShutdownStarted += (_, _) => Save();
        Show();
    }

    void Show()
    {
        _loading = true;
        NoteBox.Text = _data.Notes[_data.Index];
        _loading = false;
        PageText.Text = $"{_data.Index + 1}/{_data.Notes.Count}";
    }

    void Save()
    {
        if (Settings.IsResettingAppData) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data));
        }
        catch { }
    }

    public void SaveNow() => Save();

    public bool HasNotes => _data.Notes.Count > 0;

    public void EnsureNoteAvailable()
    {
        if (HasNotes) return;
        _data.Notes.Add("");
        _data.Index = 0;
        Show();
        Save();
    }

    // A detached sticky is removed from this stack; the floating window owns that one card.
    public string DetachCurrentNote()
    {
        if (_data.Notes.Count == 0) return "";
        string text = _data.Notes[_data.Index];
        _data.Notes.RemoveAt(_data.Index);
        if (_data.Notes.Count > 0)
        {
            _data.Index = Math.Clamp(_data.Index, 0, _data.Notes.Count - 1);
            Show();
        }
        Save();
        return text;
    }

    public void ReturnDetachedNote(string text)
    {
        _data.Notes.Add(text);
        _data.Index = _data.Notes.Count - 1;
        Show();
        Save();
    }

    void Pin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pinStart = e.GetPosition(this);
        _pinHeld = true;
        Pin.CaptureMouse();
        e.Handled = true;
    }

    void Pin_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_pinHeld || e.LeftButton != MouseButtonState.Pressed) return;
        var p = e.GetPosition(this);
        if (Math.Abs(p.X - _pinStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(p.Y - _pinStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        _pinHeld = false;
        Pin.ReleaseMouseCapture();
        SaveNow();
        FloatRequested?.Invoke(this, EventArgs.Empty);
    }

    void Pin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _pinHeld = false;
        Pin.ReleaseMouseCapture();
    }

    void Go(int delta)
    {
        _data.Index = (_data.Index + delta + _data.Notes.Count) % _data.Notes.Count;
        Show(); Save();
    }

    void Prev_Click(object s, RoutedEventArgs e) => Go(-1);
    void Next_Click(object s, RoutedEventArgs e) => Go(1);

    void Add_Click(object s, RoutedEventArgs e)
    {
        _data.Notes.Add("");
        _data.Index = _data.Notes.Count - 1;
        Show(); Save();
        NoteBox.Focus();
    }

    void Delete_Click(object s, RoutedEventArgs e)
    {
        if (_data.Notes.Count == 1) _data.Notes[0] = "";
        else { _data.Notes.RemoveAt(_data.Index); _data.Index = Math.Max(0, _data.Index - 1); }
        Show(); Save();
    }
}
