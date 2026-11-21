using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace GoogleDesktopRemake;

public static class ThemeManager
{
    static Color BaseColor(Settings settings) => settings.Theme switch
    {
        "Dark" => Color.FromRgb(0x26, 0x26, 0x26),
        "Custom" => TryParse(settings.CustomColor, out var color) ? color : Color.FromRgb(0x15, 0x65, 0xB8),
        _ => Color.FromRgb(0x15, 0x65, 0xB8)
    };

    public static void Apply()
    {
        var settings = Settings.Current;
        var (hue, saturation, value) = ToHsv(BaseColor(settings));

        Set("SidebarTint", BuildSidebarBrush(settings));
        var (bloom, haze) = BuildGlowBrushes(settings);
        Set("AeroBloom", bloom);
        Set("AeroHaze", haze);

        Set("ButtonGlass", Grad(new Point(0, 0), new Point(0, 1),
            (Hsv(hue, saturation * 0.60, Math.Min(1, value * 1.45)), 0),
            (Hsv(hue, saturation, value * 0.82), 0.5),
            (Hsv(hue, saturation * 0.85, Math.Min(1, value * 1.2)), 1)));

        Set("PanelGlass", Grad(new Point(0, 0), new Point(0, 1),
            (Hsv(hue, saturation * 0.82, Math.Min(1, value * 1.1), 0xD0), 0),
            (Hsv(hue, saturation, value * 0.60, 0xD0), 1)));

        Set("WindowGlass", Grad(new Point(0, 0), new Point(0, 1),
            (Hsv(hue, saturation * 0.35, Math.Min(1, value * 1.30), 0xF0), 0),
            (Hsv(hue, saturation * 0.65, value * 1.08, 0xF0), 0.5),
            (Hsv(hue, saturation * 0.80, value * 0.95, 0xF0), 1)));
    }

    public static Brush BuildSidebarBrush(Settings settings)
    {
        byte alpha(double factor) => (byte)Math.Clamp(255 * factor, 8, 255);

        if (settings.BackgroundStyle == "Gradient")
        {
            var stops = ReadStops(settings);
            if (stops.Count >= 2)
            {
                double radians = settings.GradientAngle * Math.PI / 180.0;
                double dx = Math.Cos(radians), dy = Math.Sin(radians);
                double shift = Math.Clamp(settings.GradientShift, -0.5, 0.5);
                double cx = 0.5 + shift * dx, cy = 0.5 + shift * dy;
                var brush = new LinearGradientBrush
                {
                    StartPoint = new Point(cx - dx / 2, cy - dy / 2),
                    EndPoint = new Point(cx + dx / 2, cy + dy / 2)
                };
                foreach (var (color, offset) in stops)
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha(1.0), color.R, color.G, color.B), offset));
                return brush;
            }
        }

        var (hue, saturation, value) = ToHsv(BaseColor(settings));
        return Grad(new Point(0, 0), new Point(1, 0),
            (Hsv(hue, saturation, value * 0.49, alpha(1.1)), 0),
            (Hsv(hue, saturation, value * 0.75, alpha(1.0)), 0.55),
            (Hsv(hue, saturation * 0.80, Math.Min(1, value * 1.15), alpha(0.9)), 1));
    }

    public static (Color streak, Brush bloom, Brush haze) BuildStreakColors(Settings settings)
    {
        Color bloomSource, hazeSource, streakSource;
        var stops = ReadStops(settings);
        if (settings.BackgroundStyle == "Gradient" && stops.Count >= 2)
        {
            hazeSource = stops[0].color;
            bloomSource = stops[^1].color;
            streakSource = stops[^1].color;
        }
        else
        {
            var (hue, saturation, value) = ToHsv(BaseColor(settings));
            bloomSource = Hsv(hue - 12, saturation, Math.Min(1, value * 1.1));
            hazeSource = Hsv(hue + 40, saturation, value);
            streakSource = Hsv(hue, saturation, Math.Min(1, value * 1.2));
        }

        var (streakHue, streakSaturation, _) = ToHsv(streakSource);
        Color streak = Hsv(streakHue, streakSaturation * 0.38, 1.0);
        return (streak, Radial(Lift(bloomSource, 0x99)), Radial(Lift(hazeSource, 0x70)));
    }

    public static (Brush bloom, Brush haze) BuildGlowBrushes(Settings settings)
    {
        var (_, bloom, haze) = BuildStreakColors(settings);
        return (bloom, haze);
    }

    static Color Lift(Color color, byte alpha)
    {
        var (hue, saturation, value) = ToHsv(color);
        return Hsv(hue, saturation * 0.6, Math.Min(1, value * 1.3 + 0.15), alpha);
    }

    static Brush Radial(Color center)
    {
        var brush = new RadialGradientBrush(center, Color.FromArgb(0, center.R, center.G, center.B));
        brush.Freeze();
        return brush;
    }

    public static List<(Color color, double offset)> ReadStops(Settings settings)
    {
        var list = new List<(Color, double)>();
        foreach (var stop in settings.GradientStops ?? new())
            if (TryParse(stop.Color, out var color)) list.Add((color, Math.Clamp(stop.Offset, 0, 1)));
        return list.OrderBy(item => item.Item2).ToList();
    }

    static void Set(string key, Brush brush)
    {
        if (brush.CanFreeze) brush.Freeze();
        Application.Current.Resources[key] = brush;
    }

    static LinearGradientBrush Grad(Point start, Point end, params (Color color, double offset)[] stops)
    {
        var brush = new LinearGradientBrush { StartPoint = start, EndPoint = end };
        foreach (var (color, offset) in stops) brush.GradientStops.Add(new GradientStop(color, offset));
        return brush;
    }

    public static bool TryParse(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text) || !Regex.IsMatch(text.Trim(), "^#?[0-9a-fA-F]{6}$")) return false;
        string value = text.Trim().TrimStart('#');
        color = Color.FromRgb(Convert.ToByte(value[..2], 16), Convert.ToByte(value[2..4], 16), Convert.ToByte(value[4..], 16));
        return true;
    }

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    static (double h, double s, double v) ToHsv(Color color)
    {
        double red = color.R / 255.0, green = color.G / 255.0, blue = color.B / 255.0;
        double max = Math.Max(red, Math.Max(green, blue)), min = Math.Min(red, Math.Min(green, blue)), delta = max - min;
        double hue = 0;
        if (delta > 0)
        {
            if (max == red) hue = ((green - blue) / delta) % 6;
            else if (max == green) hue = (blue - red) / delta + 2;
            else hue = (red - green) / delta + 4;
            hue *= 60;
            if (hue < 0) hue += 360;
        }
        return (hue, max == 0 ? 0 : delta / max, max);
    }

    static Color Hsv(double hue, double saturation, double value, byte alpha = 255)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);
        double chroma = value * saturation;
        double x = chroma * (1 - Math.Abs((hue / 60) % 2 - 1));
        double m = value - chroma;
        (double red, double green, double blue) = ((int)(hue / 60) % 6) switch
        {
            0 => (chroma, x, 0.0), 1 => (x, chroma, 0.0), 2 => (0.0, chroma, x),
            3 => (0.0, x, chroma), 4 => (x, 0.0, chroma), _ => (chroma, 0.0, x)
        };
        return Color.FromArgb(alpha, (byte)Math.Round((red + m) * 255), (byte)Math.Round((green + m) * 255), (byte)Math.Round((blue + m) * 255));
    }
}
