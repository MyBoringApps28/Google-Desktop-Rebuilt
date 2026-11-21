using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace GoogleDesktopRemake.Clocks;

/// <summary>Hours + minutes flip clock with a wood-grain frame. (No seconds.)</summary>
public class FlipFace : UserControl, IClockFace
{
    static readonly Brush LitBrush = Frozen(Color.FromRgb(0xF2, 0xF2, 0xF2));
    static readonly Brush DimBrush = Frozen(Color.FromRgb(0x4D, 0x4D, 0x4D));

    readonly bool _h24;
    readonly TextBlock[] _t = new TextBlock[4];
    readonly TextBlock _am, _pm;
    string _last = "";

    static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

    public FlipFace(bool h24)
    {
        _h24 = h24;
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < 4; i++)
        {
            if (i == 2) row.Children.Add(Colon());
            row.Children.Add(Card(i));
        }

        // AM / PM indicator: its own column to the right of the cards, never over the digits
        _am = Label("AM"); _pm = Label("PM");
        var ind = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 1, 0), Visibility = h24 ? Visibility.Collapsed : Visibility.Visible };
        ind.Children.Add(_am);
        ind.Children.Add(_pm);
        row.Children.Add(ind);

        var inner = new Border
        {
            CornerRadius = new CornerRadius(5), Padding = new Thickness(5), Child = row,
            Background = new LinearGradientBrush(Color.FromRgb(0x1A, 0x1A, 0x1A), Color.FromRgb(0x05, 0x05, 0x05), 90)
        };
        Content = new Border
        {
            Background = WoodBrush(), CornerRadius = new CornerRadius(8), Padding = new Thickness(5),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3B, 0x24, 0x10)), BorderThickness = new Thickness(1.5),
            Child = inner,
            Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 3, Opacity = 0.6 }
        };
    }

    static TextBlock Label(string t) => new()
    {
        Text = t, FontSize = 8.5, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Segoe UI"),
        Foreground = DimBrush, Margin = new Thickness(0, 2, 0, 2), HorizontalAlignment = HorizontalAlignment.Center
    };

    static Brush WoodBrush()
    {
        // horizontal grain: many close, slightly different browns
        var rnd = new Random(7);
        var b = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        for (int i = 0; i <= 26; i++)
        {
            int v = rnd.Next(-20, 20);
            b.GradientStops.Add(new GradientStop(
                Color.FromRgb((byte)(152 + v), (byte)(100 + v * 0.7), (byte)(54 + v * 0.5)), i / 26.0));
        }
        b.Freeze();
        return b;
    }

    UIElement Colon()
    {
        var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0) };
        for (int i = 0; i < 2; i++)
            sp.Children.Add(new Ellipse { Width = 5, Height = 5, Margin = new Thickness(0, 3, 0, 3), Fill = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)) });
        return sp;
    }

    UIElement Card(int i)
    {
        var tb = new TextBlock
        {
            FontSize = 28, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Segoe UI"),
            Foreground = LitBrush,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new ScaleTransform(1, 1)
        };
        _t[i] = tb;

        var g = new Grid { Width = 27, Height = 46, Margin = new Thickness(1.5, 0, 1.5, 0) };
        g.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(4),
            Background = new LinearGradientBrush(Color.FromRgb(0x34, 0x34, 0x34), Color.FromRgb(0x0C, 0x0C, 0x0C), 90),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50)), BorderThickness = new Thickness(1)
        });
        g.Children.Add(tb);
        g.Children.Add(new Border   // slight shine on the upper half
        {
            Height = 23, VerticalAlignment = VerticalAlignment.Top, CornerRadius = new CornerRadius(4, 4, 0, 0), IsHitTestVisible = false,
            Background = new LinearGradientBrush(Color.FromArgb(0x30, 255, 255, 255), Color.FromArgb(0x00, 255, 255, 255), 90)
        });
        g.Children.Add(new Rectangle { Height = 2, VerticalAlignment = VerticalAlignment.Center, Fill = Brushes.Black });
        return g;
    }

    public void Update(DateTime now)
    {
        var (d, ap) = ClockText.Parts(now, _h24);
        if (d == _last) return;
        bool first = _last == "";
        _last = d;
        for (int i = 0; i < 4; i++)
        {
            string c = d[i] == ' ' ? "" : d[i].ToString();
            if (first) _t[i].Text = c; else Flip(_t[i], c);
        }
        _am.Foreground = ap == "AM" ? LitBrush : DimBrush;
        _pm.Foreground = ap == "PM" ? LitBrush : DimBrush;
    }

    // quick vertical squash/expand = "card flips"
    static void Flip(TextBlock tb, string text)
    {
        if (tb.Text == text) return;
        var st = (ScaleTransform)tb.RenderTransform;
        var down = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(90));
        down.Completed += (_, _) =>
        {
            tb.Text = text;
            st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(110)));
        };
        st.BeginAnimation(ScaleTransform.ScaleYProperty, down);
    }
}
