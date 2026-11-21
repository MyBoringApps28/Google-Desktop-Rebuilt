using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleDesktopRemake;

/// <summary>Side panel that slides out to the left of the sidebar to view/add events for one date.</summary>
public partial class EventPanelWindow : Window
{
    static EventPanelWindow? _instance;
    DateTime _date;

    public static void Open(DateTime date, FrameworkElement anchor)
    {
        _instance ??= new EventPanelWindow();
        _instance.SetDate(date);
        _instance.PlaceNextTo(anchor);
        _instance.Show();
        _instance.Activate();
        _instance.TitleBox.Focus();
    }

    EventPanelWindow()
    {
        InitializeComponent();
        var times = new List<string>();
        for (int m = 0; m < 24 * 60; m += 30) times.Add((DateTime.Today + TimeSpan.FromMinutes(m)).ToString("t"));
        FromBox.ItemsSource = times;
        ToBox.ItemsSource = times;

        EventStore.Changed += Refresh;
        Closed += (_, _) => { EventStore.Changed -= Refresh; _instance = null; };
        SizeChanged += (_, _) => ClampToScreen();
    }

    void SetDate(DateTime date)
    {
        _date = date.Date;
        DateText.Text = _date.ToString("dddd, MMMM d");
        ErrorText.Text = "";

        // sensible default times: next full hour (today) or 9:00
        var now = TimeService.Now;
        var start = _date == now.Date ? TimeSpan.FromHours(Math.Min(22, now.Hour + 1)) : TimeSpan.FromHours(9);
        FromBox.Text = (DateTime.Today + start).ToString("t");
        ToBox.Text = (DateTime.Today + start + TimeSpan.FromHours(1)).ToString("t");
        Refresh();
    }

    void Refresh()
    {
        var list = EventStore.On(_date);
        DayList.ItemsSource = list;
        NoEvents.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    void PlaceNextTo(FrameworkElement anchor)
    {
        var sidebar = GetWindow(anchor);
        var src = PresentationSource.FromVisual(anchor);
        if (sidebar == null || src?.CompositionTarget == null) return;
        var topLeft = src.CompositionTarget.TransformFromDevice.Transform(anchor.PointToScreen(new Point(0, 0)));
        Left = sidebar.Left - Width + 8;       // 12px transparent margin for the shadow, small gap to the sidebar
        Top = topLeft.Y - 12;
        ClampToScreen();
    }

    void ClampToScreen()
    {
        var wa = SystemParameters.WorkArea;
        if (ActualHeight > 0) Top = Math.Max(wa.Top, Math.Min(Top, wa.Bottom - ActualHeight));
        Left = Math.Max(wa.Left, Left);
    }

    void AllDay_Changed(object sender, RoutedEventArgs e)
    {
        if (TimeRow == null) return;
        TimeRow.Visibility = AllDayCheck.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
    }

    void Add_Click(object sender, RoutedEventArgs e)
    {
        string title = TitleBox.Text.Trim();
        if (title.Length == 0) { ErrorText.Text = "Type what kind of event it is."; return; }

        var ev = new CalendarEvent { Title = title, Date = _date, AllDay = AllDayCheck.IsChecked == true };
        if (!ev.AllDay)
        {
            if (!DateTime.TryParse(FromBox.Text, out var from) || !DateTime.TryParse(ToBox.Text, out var to))
            { ErrorText.Text = "Couldn't read those times (try 9:00 AM)."; return; }
            if (to.TimeOfDay <= from.TimeOfDay) { ErrorText.Text = "The end time must be after the start time."; return; }
            ev.Start = from.TimeOfDay;
            ev.End = to.TimeOfDay;
        }

        EventStore.Add(ev);
        TitleBox.Clear();
        ErrorText.Text = "";
    }

    void TitleBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Add_Click(sender, e); e.Handled = true; }
    }

    void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is CalendarEvent ev) EventStore.Remove(ev.Id);
    }

    void Title_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
    void Close_Click(object sender, RoutedEventArgs e) => Close();
    void Window_PreviewKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); }
}
