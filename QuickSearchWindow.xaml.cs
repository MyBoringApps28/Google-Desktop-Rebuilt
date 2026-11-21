using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace GoogleDesktopRemake;

public partial class QuickSearchWindow : Window
{
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    readonly ObservableCollection<SearchResult> _results = new();

    public QuickSearchWindow()
    {
        InitializeComponent();
        ResultsList.ItemsSource = _results;
        Deactivated += (_, _) => HideBox();
    }

    public void Toggle()
    {
        if (IsVisible) HideBox(); else ShowBox();
    }

    void ShowBox()
    {
        Input.Text = "";
        _results.Clear();
        ResultsPanel.Visibility = Visibility.Collapsed;
        Hint.Visibility = Visibility.Collapsed;

        double top = SystemParameters.PrimaryScreenHeight * 0.16;
        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = top;
        Opacity = 0;
        Show();

        // gentle fade + slide-in
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
        BeginAnimation(TopProperty, new DoubleAnimation(top - 14, top, TimeSpan.FromMilliseconds(200))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

        // Windows only lets the foreground app steal focus; a synthetic Alt tap lifts that restriction.
        keybd_event(0x12, 0, 0, UIntPtr.Zero);
        keybd_event(0x12, 0, 2, UIntPtr.Zero);
        SetForegroundWindow(new WindowInteropHelper(this).Handle);
        Activate();
        Input.Focus();
    }

    void HideBox() { if (IsVisible) Hide(); }

    void Input_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        string q = Input.Text.Trim();
        _results.Clear();
        if (q.Length == 0)
        {
            ResultsPanel.Visibility = Visibility.Collapsed;
            Hint.Visibility = Visibility.Collapsed;
            return;
        }

        foreach (var hit in SearchIndex.Query(q, 5))
        {
            string path = hit.Path;
            _results.Add(new SearchResult
            {
                Title = hit.Name, Subtitle = path, Icon = SearchIndex.GetIcon(path), Glyph = "?",
                Kind = hit.IsApp ? "Application" : "File",
                Launch = () => Start(path)
            });
        }
        _results.Add(new SearchResult
        {
            Title = $"Searching Desktop for {q}", Subtitle = "Open Windows search", Glyph = "D", Kind = "Search",
            Launch = () => Start("explorer.exe", "search-ms:query=" + Uri.EscapeDataString(q))
        });
        _results.Add(new SearchResult
        {
            Title = $"Search web for {q}", Subtitle = "google.com", Glyph = "G", Kind = "Web",
            Launch = () => Start("https://www.google.com/search?q=" + Uri.EscapeDataString(q))
        });
        ResultsPanel.Visibility = Visibility.Visible;
        Hint.Visibility = Visibility.Visible;
        ResultsList.SelectedIndex = 0;
    }

    void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape: HideBox(); e.Handled = true; break;
            case Key.Down: Move(1); e.Handled = true; break;
            case Key.Up: Move(-1); e.Handled = true; break;
            case Key.Enter: LaunchSelected(); e.Handled = true; break;
        }
    }

    void Move(int delta)
    {
        if (_results.Count == 0) return;
        ResultsList.SelectedIndex = Math.Clamp(ResultsList.SelectedIndex + delta, 0, _results.Count - 1);
        ResultsList.ScrollIntoView(ResultsList.SelectedItem);
    }

    void ResultsList_Click(object sender, MouseButtonEventArgs e)
    {
        var d = e.OriginalSource as DependencyObject;
        while (d != null && d is not System.Windows.Controls.ListBoxItem)
            d = System.Windows.Media.VisualTreeHelper.GetParent(d);
        if (d is System.Windows.Controls.ListBoxItem { DataContext: SearchResult })
        {
            ResultsList.SelectedItem = ((System.Windows.Controls.ListBoxItem)d).DataContext;
            LaunchSelected();
        }
    }

    void LaunchSelected()
    {
        if (ResultsList.SelectedItem is SearchResult r)
        {
            HideBox();
            r.Launch?.Invoke();
        }
    }

    static void Start(string file, string? args = null)
    {
        try { Process.Start(new ProcessStartInfo(file, args ?? "") { UseShellExecute = true }); }
        catch { }
    }
}
