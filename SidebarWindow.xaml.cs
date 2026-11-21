using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;

namespace GoogleDesktopRemake;

/// <summary>
/// The sidebar lives on the desktop: just above the wallpaper/icons and below every other window,
/// so games, browsers, etc. cover it. (Settings > "Keep on top" switches that off.)
/// It reserves no screen space.
/// </summary>
public partial class SidebarWindow : Window
{
    [StructLayout(LayoutKind.Sequential)]
    struct WINDOWPOS { public IntPtr hwnd, hwndInsertAfter; public int x, y, cx, cy; public uint flags; }
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);

    const int WM_WINDOWPOSCHANGING = 0x0046;
    const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;
    static readonly IntPtr HWND_NOTOPMOST = new(-2);

    IntPtr _hwnd;

    public SidebarWindow()
    {
        InitializeComponent();
        Width = Settings.SidebarWidth;
        Topmost = Settings.Current.KeepOnTop;

        Settings.Changed += OnSettingsChanged;
        ContentRendered += (_, _) => { ApplyLayout(); ApplyZOrder(); ArrangeDesktop(); };

        SystemEvents.DisplaySettingsChanged += async (_, _) =>
        {
            await Task.Delay(1500);                    // let Explorer finish re-laying out the desktop
            Dispatcher.Invoke(() => { Place(); ApplyLayout(); ApplyZOrder(); ArrangeDesktop(); });
        };
        SystemParameters.StaticPropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SystemParameters.WorkArea))
                Dispatcher.Invoke(() => { Place(); ApplyLayout(); ArrangeDesktop(); });
        };

        // Show-Desktop / minimise-all must never make the sidebar disappear.
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized) Dispatcher.BeginInvoke(() => WindowState = WindowState.Normal);
        };

        // Re-assert the "just above the desktop" slot now and then (cheap; no-op when already correct).
        var keepLow = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        keepLow.Tick += (_, _) => ApplyZOrder();
        keepLow.Start();
    }

    void OnSettingsChanged()
    {
        Topmost = Settings.Current.KeepOnTop;
        ApplyLayout();
        ApplyZOrder();
        ArrangeDesktop();
    }

    // ---- placement ----
    void Place()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width;
        Top = wa.Top;
        Height = wa.Height;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(_hwnd).AddHook(WndProc);
        Place();
        WindowStyles.HideFromAltTab(this);
    }

    // ---- desktop layer: rewrite every z-order change to "directly above the desktop" ----
    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_WINDOWPOSCHANGING && !Settings.Current.KeepOnTop)
        {
            var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            if ((pos.flags & SWP_NOZORDER) == 0)
            {
                var after = DesktopIcons.InsertAfterForDesktop(hwnd);
                if (after == IntPtr.Zero || after == hwnd) pos.flags |= SWP_NOZORDER;
                else pos.hwndInsertAfter = after;
                Marshal.StructureToPtr(pos, lParam, false);
            }
        }
        return IntPtr.Zero;
    }

    void ApplyZOrder()
    {
        if (_hwnd == IntPtr.Zero || Settings.Current.KeepOnTop) return;
        // The hook turns this into "directly above the desktop".
        SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    // ---- optional gadgets + keeping the news feed usable ----
    public bool CanFitGadgets(bool showPhotos, bool showNews, bool enableCalendar, bool enableNotes, bool enableCalculator, out string error)
    {
        error = "";
        if (ContentGrid.ActualHeight <= 0)
        {
            error = "The sidebar layout is not ready yet. Try again in a moment.";
            return false;
        }

        var previousVisibility = (PhotoBox.Visibility, NewsBox.Visibility, CalendarBox.Visibility, NotesBox.Visibility, CalcBox.Visibility);
        double previousNewsMinHeight = NewsRow.MinHeight;
        GridLength previousNewsHeight = NewsRow.Height;
        try
        {
            bool compact = Height < 820 && (enableCalendar || enableNotes || enableCalculator);
            PhotoBox.Visibility = showPhotos && !compact ? Visibility.Visible : Visibility.Collapsed;
            NewsBox.Visibility = showNews ? Visibility.Visible : Visibility.Collapsed;
            CalendarBox.Visibility = enableCalendar ? Visibility.Visible : Visibility.Collapsed;
            NotesBox.Visibility = enableNotes ? Visibility.Visible : Visibility.Collapsed;
            CalcBox.Visibility = enableCalculator ? Visibility.Visible : Visibility.Collapsed;
            NewsRow.MinHeight = showNews ? 70 : 0;
            NewsRow.Height = showNews ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;

            ContentGrid.UpdateLayout();
            double requiredHeight = ContentGrid.RowDefinitions.Sum(row => row.ActualHeight);
            if (requiredHeight <= ContentGrid.ActualHeight + 1) return true;

            error = $"These widgets need about {Math.Ceiling(requiredHeight)} px, but this screen has {Math.Floor(ContentGrid.ActualHeight)} px available. Turn off a widget, photos, or the news feed to make room.";
            return false;
        }
        finally
        {
            (PhotoBox.Visibility, NewsBox.Visibility, CalendarBox.Visibility, NotesBox.Visibility, CalcBox.Visibility) = previousVisibility;
            NewsRow.MinHeight = previousNewsMinHeight;
            NewsRow.Height = previousNewsHeight;
            ContentGrid.UpdateLayout();
        }
    }

    void ApplyLayout()
    {
        var s = Settings.Current;
        CalendarBox.Visibility = s.EnableCalendar ? Visibility.Visible : Visibility.Collapsed;
        NotesBox.Visibility = s.EnableNotes ? Visibility.Visible : Visibility.Collapsed;
        CalcBox.Visibility = s.EnableCalculator ? Visibility.Visible : Visibility.Collapsed;
        BackgroundLayer.Opacity = Math.Clamp(s.SidebarOpacity, 0.05, 1.0);
        StreaksBox.Visibility = s.Streaks ? Visibility.Visible : Visibility.Collapsed;
        StreaksBox.Opacity = Math.Clamp(s.StreakStrength, 0.05, 1.0);
        var (streak, bloom, haze) = ThemeManager.BuildStreakColors(s);
        StreaksBox.SetColors(streak, bloom, haze);

        // On short screens, drop the photo frame when extra gadgets are on so the news feed stays readable.
        bool extras = s.EnableCalendar || s.EnableNotes || s.EnableCalculator;
        bool compact = Height < 820 && extras;
        PhotoBox.Visibility = s.ShowPhotos && !compact ? Visibility.Visible : Visibility.Collapsed;
        NewsBox.Visibility = s.ShowNews ? Visibility.Visible : Visibility.Collapsed;
        NewsRow.MinHeight = s.ShowNews ? 70 : 0;
        NewsRow.Height = s.ShowNews ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
    }

    // Slide any desktop files/folders that sit under the sidebar over to its left (or put them back).
    void ArrangeDesktop()
    {
        if (!Settings.Current.MoveDesktopIcons) { DesktopIcons.Restore(); return; }
        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget == null) return;
        var m = src.CompositionTarget.TransformToDevice;
        DesktopIcons.MakeRoom((int)(Left * m.M11), (int)(Top * m.M22), (int)((Top + Height) * m.M22));
    }

    protected override void OnClosed(EventArgs e)
    {
        if (Settings.Current.RestoreIconsOnExit) DesktopIcons.Restore();
        base.OnClosed(e);
    }

    void Settings_Click(object sender, RoutedEventArgs e) => new SettingsWindow().ShowDialog();
    void Close_Click(object sender, RoutedEventArgs e) => Close();
    void OpenSearch_Click(object sender, RoutedEventArgs e) => App.QuickSearch?.Toggle();

    void NotesBox_FloatRequested(object sender, EventArgs e)
    {
        // A detached note is one card, rather than the entire sidebar stack.
        var origin = NotesBox.PointToScreen(new Point(0, 0));
        string text = NotesBox.DetachCurrentNote();
        if (!NotesBox.HasNotes) NotesBox.Visibility = Visibility.Collapsed;
        var note = new FloatingNoteWindow(text)
        {
            Left = origin.X,
            Top = origin.Y,
            Owner = this
        };
        note.ReturnedToSidebar += returnedText =>
        {
            NotesBox.ReturnDetachedNote(returnedText);
            if (Settings.Current.EnableNotes) NotesBox.Visibility = Visibility.Visible;
        };
        note.Deleted += () =>
        {
            NotesBox.EnsureNoteAvailable();
            if (Settings.Current.EnableNotes) NotesBox.Visibility = Visibility.Visible;
        };
        note.Show();
        note.BeginPinDrag();
    }

    void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(SearchBox.Text)) return;
        Process.Start(new ProcessStartInfo(
            "https://www.google.com/search?q=" + Uri.EscapeDataString(SearchBox.Text)) { UseShellExecute = true });
        SearchBox.Clear();
    }
}
