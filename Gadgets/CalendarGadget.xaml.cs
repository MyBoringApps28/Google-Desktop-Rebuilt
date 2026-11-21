using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GoogleDesktopRemake.Gadgets;

public partial class CalendarGadget : UserControl
{
    static readonly Brush Gold = Frozen(Color.FromRgb(0xFF, 0xD5, 0x4A));
    static readonly Brush Cyan = Frozen(Color.FromRgb(0x7F, 0xE6, 0xFF));
    static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

    DateTime _month;
    DateTime _selected = DateTime.MinValue;
    DateTime _builtFor;

    public CalendarGadget()
    {
        InitializeComponent();
        GoToToday();

        EventStore.Changed += () => { Build(); RefreshUpcoming(); };
        Settings.Changed += () => { Build(); RefreshUpcoming(); };

        // keeps the "in 5 days / in 2 hr / in 12 min" lines fresh and rolls the date at midnight
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        t.Tick += (_, _) =>
        {
            if (TimeService.Now.Date != _builtFor) Build();
            RefreshUpcoming();
        };
        t.Start();
    }

    void GoToToday() { var n = TimeService.Now; _month = new DateTime(n.Year, n.Month, 1); Build(); RefreshUpcoming(); }
    void Prev_Click(object s, RoutedEventArgs e) { _month = _month.AddMonths(-1); Build(); }
    void Next_Click(object s, RoutedEventArgs e) { _month = _month.AddMonths(1); Build(); }
    void Month_Click(object s, MouseButtonEventArgs e) => GoToToday();

    // ---- "XYZ in 5 days" strip above the month grid ----
    void RefreshUpcoming()
    {
        var now = TimeService.Now;
        Upcoming.Children.Clear();
        foreach (var ev in EventStore.Upcoming(now, 2))
        {
            var dock = new DockPanel { Margin = new Thickness(0, 0, 0, 2), Background = Brushes.Transparent, Cursor = Cursors.Hand };
            var when = new TextBlock
            {
                Text = EventStore.Countdown(ev, now), Foreground = Gold, FontSize = 10, FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(when, Dock.Right);
            dock.Children.Add(when);
            dock.Children.Add(new TextBlock
            {
                Text = ev.Title, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"), TextTrimming = TextTrimming.CharacterEllipsis
            });
            dock.ToolTip = $"{ev.Date:dddd, MMM d} - {ev.WhenText}";
            var date = ev.Date;
            dock.MouseLeftButtonUp += (_, _) => EventPanelWindow.Open(date, this);
            Upcoming.Children.Add(dock);
        }
        Upcoming.Visibility = Upcoming.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    // ---- month grid ----
    void Build()
    {
        var today = TimeService.Now.Date;
        _builtFor = today;
        var culture = CultureInfo.CurrentCulture;
        var fdow = culture.DateTimeFormat.FirstDayOfWeek;

        MonthText.Text = _month.ToString("MMMM yyyy", culture);
        Days.Children.Clear();

        for (int i = 0; i < 7; i++)
        {
            var d = (DayOfWeek)(((int)fdow + i) % 7);
            Days.Children.Add(new TextBlock
            {
                Text = culture.DateTimeFormat.GetShortestDayName(d), Foreground = Cyan,
                FontSize = 9, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            });
        }

        int offset = ((int)_month.DayOfWeek - (int)fdow + 7) % 7;
        var start = _month.AddDays(-offset);
        for (int i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            bool inMonth = date.Month == _month.Month;
            bool isToday = date == today;
            bool isSelected = date == _selected;

            var grid = new Grid();
            grid.Children.Add(new TextBlock
            {
                Text = date.Day.ToString(), FontSize = 10, FontFamily = new FontFamily("Segoe UI"),
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isToday ? Brushes.White : new SolidColorBrush(inMonth ? Colors.White : Color.FromArgb(0x66, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            });

            var dayEvents = EventStore.On(date);
            if (dayEvents.Count > 0)   // event marker
                grid.Children.Add(new Ellipse
                {
                    Width = 4, Height = 4, Fill = Gold, VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 1), HorizontalAlignment = HorizontalAlignment.Center
                });

            var cell = new Border
            {
                Margin = new Thickness(1), CornerRadius = new CornerRadius(3), Child = grid,
                Background = Brushes.Transparent, Cursor = Cursors.Hand
            };
            if (isToday)
            {
                cell.BorderThickness = new Thickness(1);
                cell.BorderBrush = new SolidColorBrush(Color.FromArgb(0xCC, 255, 255, 255));
                cell.Background = new LinearGradientBrush(Color.FromRgb(0x7C, 0xD8, 0xFF), Color.FromRgb(0x1E, 0x7A, 0xD0), 90);
            }
            else if (isSelected)
            {
                cell.BorderThickness = new Thickness(1);
                cell.BorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 255, 255, 255));
            }
            if (dayEvents.Count > 0)
                cell.ToolTip = string.Join("\n", dayEvents.Select(ev => $"{ev.WhenText}  {ev.Title}"));

            var clicked = date;
            cell.MouseLeftButtonUp += (_, _) =>
            {
                _selected = clicked;
                Build();
                EventPanelWindow.Open(clicked, this);
            };
            Days.Children.Add(cell);
        }
    }
}
