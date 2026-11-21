using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace GoogleDesktopRemake.Controls;

public static class ScrollAnimator
{
    public static readonly DependencyProperty OffsetProperty = DependencyProperty.RegisterAttached(
        "Offset", typeof(double), typeof(ScrollAnimator),
        new PropertyMetadata(0.0, (d, e) => { if (d is ScrollViewer viewer) viewer.ScrollToVerticalOffset((double)e.NewValue); }));

    public static double GetOffset(DependencyObject target) => (double)target.GetValue(OffsetProperty);
    public static void SetOffset(DependencyObject target, double value) => target.SetValue(OffsetProperty, value);

    public static void ScrollBy(ScrollViewer viewer, double delta)
    {
        double from = viewer.VerticalOffset;
        double to = Math.Clamp(from + delta, 0, viewer.ScrollableHeight);
        if (Math.Abs(to - from) < 0.5) return;

        var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(220))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        animation.Completed += (_, _) =>
        {
            viewer.BeginAnimation(OffsetProperty, null);
            viewer.ScrollToVerticalOffset(to);
        };
        viewer.BeginAnimation(OffsetProperty, animation);
    }
}
