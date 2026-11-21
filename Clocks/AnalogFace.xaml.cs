using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GoogleDesktopRemake.Clocks;

public partial class AnalogFace : UserControl, IClockFace
{
    public AnalogFace(bool black, string label)
    {
        InitializeComponent();
        Label.Text = label;
        if (black) ApplyBlack();
        BuildTicks(black);
    }

    void ApplyBlack()
    {
        Bezel.Fill = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(0.6, 1),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(0x9A, 0x9A, 0x9A), 0),
                new GradientStop(Color.FromRgb(0x22, 0x22, 0x22), 0.5),
                new GradientStop(Color.FromRgb(0x66, 0x66, 0x66), 1)
            }
        };
        Dial.Fill = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.4, 0.3),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(0x42, 0x42, 0x42), 0),
                new GradientStop(Color.FromRgb(0x14, 0x14, 0x14), 0.7),
                new GradientStop(Colors.Black, 1)
            }
        };
        Dial.Stroke = new SolidColorBrush(Color.FromArgb(0x55, 255, 255, 255));
        HourHand.Stroke = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
        MinHand.Stroke = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        Label.Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A));

        // Replace the oval highlight with a glass sheen that follows the dial's own circle and fades out
        // smoothly, so there is no visible oval edge on the black face.
        Gloss.Width = 104; Gloss.Height = 104;
        Gloss.Margin = new Thickness(0);
        Gloss.VerticalAlignment = VerticalAlignment.Center;
        Gloss.Fill = new LinearGradientBrush
        {
            StartPoint = new Point(0.25, 0), EndPoint = new Point(0.6, 1),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0x5A, 255, 255, 255), 0),
                new GradientStop(Color.FromArgb(0x1E, 255, 255, 255), 0.28),
                new GradientStop(Color.FromArgb(0x00, 255, 255, 255), 0.52)
            }
        };
    }

    void BuildTicks(bool black)
    {
        const double c = 52;
        var major = new SolidColorBrush(black ? Color.FromRgb(235, 235, 235) : Color.FromRgb(40, 40, 40));
        var minor = new SolidColorBrush(black ? Color.FromRgb(120, 120, 120) : Color.FromRgb(130, 130, 130));
        for (int i = 0; i < 60; i++)
        {
            bool m = i % 5 == 0;
            double a = i * 6 * Math.PI / 180, r1 = 48, r2 = m ? 40 : 45;
            Ticks.Children.Add(new Line
            {
                X1 = c + r1 * Math.Sin(a), Y1 = c - r1 * Math.Cos(a),
                X2 = c + r2 * Math.Sin(a), Y2 = c - r2 * Math.Cos(a),
                Stroke = m ? major : minor, StrokeThickness = m ? 2 : 1
            });
        }
    }

    public void Update(DateTime n)
    {
        double s = n.Second + n.Millisecond / 1000.0;
        double m = n.Minute + s / 60;
        double h = n.Hour % 12 + m / 60;
        SecRot.Angle = s * 6;
        MinRot.Angle = m * 6;
        HourRot.Angle = h * 30;
    }
}
