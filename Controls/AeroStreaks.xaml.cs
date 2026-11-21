using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleDesktopRemake.Controls;

public partial class AeroStreaks : UserControl
{
    static readonly (string Data, bool Rising, double Width)[] Curves =
    {
        ("M -40,812 C 20,640 150,520 232,236", true, 1.3),
        ("M -40,834 C 40,672 160,566 238,298", true, 1.0),
        ("M -30,860 C 60,706 172,616 244,404", true, 0.8),
        ("M -50,790 C 10,690 120,560 215,430", true, 0.7),
        ("M 236,70 C 150,96 64,160 -34,268", false, 1.0),
        ("M 244,116 C 158,150 72,238 -34,338", false, 0.8),
        ("M 230,10 C 160,50 90,100 -30,190", false, 0.7),
    };

    static readonly (double Mult, double Opacity)[] Layers = { (7.0, 0.07), (3.2, 0.16), (1.0, 0.85) };

    public AeroStreaks()
    {
        InitializeComponent();
        var defaultStreak = Color.FromRgb(255, 255, 255);
        var up = StrokeBrush(defaultStreak, true);
        var down = StrokeBrush(defaultStreak, false);
        foreach (var (data, rising, width) in Curves)
        {
            var geometry = Geometry.Parse(data);
            geometry.Freeze();
            foreach (var (multiplier, opacity) in Layers)
                Art.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = geometry,
                    Stroke = rising ? up : down,
                    StrokeThickness = width * multiplier,
                    Opacity = opacity,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                });
        }
    }

    static Brush StrokeBrush(Color color, bool rising)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = rising ? new Point(0, 1) : new Point(1, 0),
            EndPoint = rising ? new Point(1, 0) : new Point(0, 1)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, color.R, color.G, color.B), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xE0, color.R, color.G, color.B), 0.4));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xC8, color.R, color.G, color.B), 0.65));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, color.R, color.G, color.B), 1));
        brush.Freeze();
        return brush;
    }

    public void SetColors(Color streak, Brush bloom, Brush haze)
    {
        Resources["AeroBloom"] = bloom;
        Resources["AeroHaze"] = haze;

        var risingBrush = StrokeBrush(streak, true);
        var fallingBrush = StrokeBrush(streak, false);
        int index = 0;
        foreach (var child in Art.Children)
        {
            if (child is System.Windows.Shapes.Path path)
            {
                path.Stroke = Curves[index / Layers.Length].Rising ? risingBrush : fallingBrush;
                index++;
            }
        }
    }
}
