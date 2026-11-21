using System.Windows.Controls;
using System.Windows.Threading;
using GoogleDesktopRemake.Clocks;

namespace GoogleDesktopRemake.Gadgets;

/// <summary>Hosts whichever clock face is chosen in Settings. Only the analog faces show seconds.</summary>
public partial class ClockGadget : UserControl
{
    IClockFace? _face;
    string _key = "";

    public ClockGadget()
    {
        InitializeComponent();
        Build();
        Settings.Changed += Build;
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        t.Tick += (_, _) => _face?.Update(TimeService.Now);
        t.Start();
    }

    void Build()
    {
        var s = Settings.Current;
        // Dark theme turns the default analog clock black; "AnalogBlack" is black in any theme.
        bool black = s.ClockStyle == "AnalogBlack" || (s.ClockStyle == "Analog" && s.Theme == "Dark");
        string key = $"{s.ClockStyle}|{black}|{s.Use24Hour}|{s.ClockLabel}";
        if (key == _key) return;
        _key = key;

        UserControl face = s.ClockStyle switch
        {
            "Flip" => new FlipFace(s.Use24Hour),
            "Segment" => new SegmentFace(s.Use24Hour),
            _ => new AnalogFace(black, s.ClockLabel)
        };
        _face = (IClockFace)face;
        Host.Content = face;
        _face.Update(TimeService.Now);
    }
}
