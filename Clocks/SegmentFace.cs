using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace GoogleDesktopRemake.Clocks;

/// <summary>Red seven-segment display in a black plastic case with a glossy rim. (No seconds.)</summary>
public class SegmentFace : UserControl, IClockFace
{
    const double W = 22, H = 40, T = 4.4;
    static readonly string[] Masks = { "ABCDEF", "BC", "ABDEG", "ABCDG", "BCFG", "ACDFG", "ACDEFG", "ABC", "ABCDEFG", "ABCDFG" };
    static readonly Brush On = Frozen(Color.FromRgb(0xFF, 0x3A, 0x2E));
    static readonly Brush Off = Frozen(Color.FromRgb(0x2E, 0x08, 0x08));

    readonly bool _h24;
    readonly Polygon[][] _seg = new Polygon[4][];
    readonly TextBlock _am, _pm;
    string _last = "";

    static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

    public SegmentFace(bool h24)
    {
        _h24 = h24;
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < 4; i++)
        {
            if (i == 2) row.Children.Add(Colon());
            _seg[i] = new Polygon[7];
            row.Children.Add(Digit(_seg[i]));
        }

        // AM / PM lamps in their own column beside the digits
        _am = Lamp("AM"); _pm = Lamp("PM");
        var ind = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0), Visibility = h24 ? Visibility.Collapsed : Visibility.Visible };
        ind.Children.Add(_am);
        ind.Children.Add(_pm);
        row.Children.Add(ind);

        var shineRim = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        shineRim.GradientStops.Add(new GradientStop(Color.FromRgb(0xC8, 0xC8, 0xC8), 0));
        shineRim.GradientStops.Add(new GradientStop(Color.FromRgb(0x4A, 0x4A, 0x4A), 0.45));
        shineRim.GradientStops.Add(new GradientStop(Color.FromRgb(0x95, 0x95, 0x95), 1));

        var plastic = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        plastic.GradientStops.Add(new GradientStop(Color.FromRgb(0x34, 0x34, 0x34), 0));
        plastic.GradientStops.Add(new GradientStop(Color.FromRgb(0x0B, 0x0B, 0x0B), 0.5));
        plastic.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x1A, 0x1A), 1));

        var case_ = new Border
        {
            CornerRadius = new CornerRadius(7), BorderThickness = new Thickness(2), BorderBrush = shineRim,
            Background = plastic, Padding = new Thickness(10, 10, 10, 10), Child = row,
            Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 3, Opacity = 0.6 }
        };
        var gloss = new Border
        {
            Height = 30, VerticalAlignment = VerticalAlignment.Top, CornerRadius = new CornerRadius(6, 6, 0, 0),
            Margin = new Thickness(2, 2, 2, 0), IsHitTestVisible = false,
            Background = new LinearGradientBrush(Color.FromArgb(0x2E, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90)
        };
        var g = new Grid();
        g.Children.Add(case_);
        g.Children.Add(gloss);
        Content = g;
    }

    static TextBlock Lamp(string t) => new()
    {
        Text = t, FontSize = 9, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Segoe UI"),
        Foreground = Off, Margin = new Thickness(0, 3, 0, 3)
    };

    UIElement Colon()
    {
        var c = new Canvas { Width = 10, Height = H, Margin = new Thickness(2, 0, 2, 0), Effect = Glow() };
        foreach (double y in new[] { 12.0, 25.0 })
        {
            var r = new Rectangle { Width = 4, Height = 4, Fill = On };
            Canvas.SetLeft(r, 3); Canvas.SetTop(r, y);
            c.Children.Add(r);
        }
        return c;
    }

    static DropShadowEffect Glow() =>
        new() { Color = Color.FromRgb(0xFF, 0x20, 0x20), BlurRadius = 7, ShadowDepth = 0, Opacity = 0.8 };

    UIElement Digit(Polygon[] segs)
    {
        Polygon P(bool horiz, double x, double y, double len)
        {
            double h = T / 2;
            var pts = horiz
                ? new PointCollection { new(x + h, y), new(x + len - h, y), new(x + len, y + h), new(x + len - h, y + T), new(x + h, y + T), new(x, y + h) }
                : new PointCollection { new(x + h, y), new(x + T, y + h), new(x + T, y + len - h), new(x + h, y + len), new(x, y + len - h), new(x, y + h) };
            return new Polygon { Points = pts, Fill = Off };
        }
        segs[0] = P(true, 2, 0, 18);                 // A top
        segs[1] = P(false, W - T, 1.5, 17.5);        // B upper right
        segs[2] = P(false, W - T, 21, 17.5);         // C lower right
        segs[3] = P(true, 2, H - T, 18);             // D bottom
        segs[4] = P(false, 0, 21, 17.5);             // E lower left
        segs[5] = P(false, 0, 1.5, 17.5);            // F upper left
        segs[6] = P(true, 2, (H - T) / 2, 18);       // G middle

        var c = new Canvas { Width = W, Height = H, Margin = new Thickness(2, 0, 2, 0), Effect = Glow() };
        foreach (var p in segs) c.Children.Add(p);
        return c;
    }

    public void Update(DateTime now)
    {
        var (d, ap) = ClockText.Parts(now, _h24);
        if (d == _last) return;
        _last = d;
        for (int i = 0; i < 4; i++)
        {
            string mask = d[i] == ' ' ? "" : Masks[d[i] - '0'];
            for (int k = 0; k < 7; k++)
                _seg[i][k].Fill = mask.Contains((char)('A' + k)) ? On : Off;
        }
        SetLamp(_am, ap == "AM");
        SetLamp(_pm, ap == "PM");
    }

    static void SetLamp(TextBlock t, bool lit)
    {
        t.Foreground = lit ? On : Off;
        t.Effect = lit ? new DropShadowEffect { Color = Color.FromRgb(0xFF, 0x20, 0x20), BlurRadius = 6, ShadowDepth = 0, Opacity = 0.9 } : null;
    }
}
