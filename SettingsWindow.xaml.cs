using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleDesktopRemake;

public record ZoneItem(string Id, string Display);

public partial class SettingsWindow : Window
{
    Settings _s = Settings.Current.Clone();
    readonly ObservableCollection<FeedConfig> _feeds = new();
    bool _busy = true;     // suppresses change events while we fill the controls
    List<GradientStopConfig> _stops = new();

    static readonly string[] PaletteColors =
    {
        "#0B1A4D", "#1A1A55", "#1646C8", "#1E78E6", "#10D0F0", "#7FE6FF", "#FFFFFF", "#C8D0E0",
        "#7C4DFF", "#6A1B9A", "#AD1457", "#E8467C", "#FF8A65", "#FFB347", "#F9A825", "#2EC27E",
        "#0B6E6A", "#2E7D32", "#37474F", "#101214", "#4E342E", "#8D6E63", "#F2B8A0", "#000000"
    };

    static readonly string[] SwatchColors =
    {
        "#1565B8", "#00838F", "#2E7D32", "#F9A825", "#EF6C00", "#C62828",
        "#AD1457", "#6A1B9A", "#4E342E", "#37474F", "#1B1B1B", "#7C4DFF"
    };

    public SettingsWindow()
    {
        InitializeComponent();

        // time zones
        var zones = new List<ZoneItem> { new("", "Local time (this PC)") };
        zones.AddRange(TimeZoneInfo.GetSystemTimeZones().Select(z => new ZoneItem(z.Id, z.DisplayName)));
        ZoneBox.ItemsSource = zones;
        ZoneBox.SelectedItem = zones.FirstOrDefault(z => z.Id == _s.TimeZoneId) ?? zones[0];

        // color swatches
        foreach (var hex in SwatchColors)
        {
            ThemeManager.TryParse(hex, out var col);
            var sw = new Border
            {
                Width = 24, Height = 24, Margin = new Thickness(2), CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(col), BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                BorderThickness = new Thickness(1), Cursor = Cursors.Hand, ToolTip = hex, Tag = col
            };
            sw.MouseLeftButtonUp += (_, _) => SetColor((Color)sw.Tag);
            Swatches.Children.Add(sw);
        }

        // fill controls from settings
        SelectByTag(ThemeBox, _s.Theme);
        ThemeManager.TryParse(_s.CustomColor, out var current);
        SetColor(current, silent: true);
        CustomPanel.Visibility = _s.Theme == "Custom" ? Visibility.Visible : Visibility.Collapsed;
        OpacitySlider.Value = Math.Round(_s.SidebarOpacity * 100);
        OpacityText.Text = $"{OpacitySlider.Value:0}%";

        TopmostCheck.IsChecked = _s.KeepOnTop;
        MoveIconsCheck.IsChecked = _s.MoveDesktopIcons;
        RestoreIconsCheck.IsChecked = _s.RestoreIconsOnExit;
        DoubleCtrlSearchCheck.IsChecked = _s.EnableDoubleCtrlSearch;

        SelectByTag(ClockStyleBox, _s.ClockStyle);
        Hour24Check.IsChecked = _s.Use24Hour;
        SelectByTag(SourceBox, _s.TimeSource);
        NtpBox.Text = _s.NtpServer;
        LabelBox.Text = _s.ClockLabel;

        PhotosCheck.IsChecked = _s.ShowPhotos;
        NewsCheck.IsChecked = _s.ShowNews;
        CalendarCheck.IsChecked = _s.EnableCalendar;
        CalculatorCheck.IsChecked = _s.EnableCalculator;
        NotesCheck.IsChecked = _s.EnableNotes;
        FolderBox.Text = _s.PhotoFolder;

        FahrenheitRadio.IsChecked = _s.UseFahrenheit;
        CelsiusRadio.IsChecked = !_s.UseFahrenheit;
        ShowCurrentPlace();

        foreach (var f in _s.Feeds) _feeds.Add(new FeedConfig(f.Name, f.Url));
        FeedList.ItemsSource = _feeds;

        PresetBox.Items.Add(new ComboBoxItem { Content = "Choose a preset...", Tag = "" });
        foreach (var preset in BackgroundPresets.All)
            PresetBox.Items.Add(new ComboBoxItem { Content = preset.Name, Tag = preset.Key });
        PresetBox.SelectedIndex = 0;
        SelectByTag(BgStyleBox, _s.BackgroundStyle);
        AngleSlider.Value = _s.GradientAngle;
        ShiftSlider.Value = _s.GradientShift * 100;
        _stops = (_s.GradientStops ?? new()).Select(stop => new GradientStopConfig(stop.Color, stop.Offset)).ToList();
        if (_stops.Count < 2) _stops = BackgroundPresets.Windows7Stops();
        BuildStopRows();
        StreaksCheck.IsChecked = _s.Streaks;
        StreakSlider.Value = _s.StreakStrength * 100;

        _busy = false;
        UpdateBackgroundUi();
        RefreshWidgetCapacity();
        Source_Changed(null, null!);
        UpdateStartupStatus();
    }

    void Widget_Changed(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        if (!RefreshWidgetCapacity() && sender is CheckBox checkbox && checkbox.IsChecked == true)
        {
            string error = WidgetSpaceError.Text;
            _busy = true;
            checkbox.IsChecked = false;
            _busy = false;
            WidgetSpaceError.Text = error;
        }
    }

    bool RefreshWidgetCapacity()
    {
        if (Application.Current.MainWindow is not SidebarWindow sidebar) return true;

        bool fits = sidebar.CanFitGadgets(
            PhotosCheck.IsChecked == true,
            NewsCheck.IsChecked == true,
            CalendarCheck.IsChecked == true,
            NotesCheck.IsChecked == true,
            CalculatorCheck.IsChecked == true,
            out string error);
        WidgetSpaceError.Text = fits ? "" : error;
        return fits;
    }

    void UpdateStartupStatus()
    {
        try
        {
            bool enabled = StartupRegistration.IsEnabled();
            StartupButton.Content = enabled ? "Remove from startup programs" : "Add to startup programs";
            StartupStatus.Text = enabled
                ? "This app starts automatically for this Windows account."
                : "Startup launch is off for this Windows account.";
        }
        catch (Exception ex)
        {
            StartupButton.Content = "Add to startup programs";
            StartupStatus.Text = ex.Message;
        }
    }

    void Startup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StartupRegistration.SetEnabled(!StartupRegistration.IsEnabled());
            UpdateStartupStatus();
        }
        catch (Exception ex)
        {
            StartupStatus.Text = $"Could not update startup settings: {ex.Message}";
        }
    }

    void ResetAppData_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(this,
            "This will permanently delete this account's saved Google Desktop settings, calendar events, and sticky notes. The app will close, and setup will appear the next time it is launched. Continue?",
            "Reset Google Desktop data", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            Settings.ResetAppData();
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not remove app data: {ex.Message}", "Google Desktop Settings",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    static void SelectByTag(ComboBox box, string tag)
    {
        foreach (ComboBoxItem item in box.Items)
            if ((item.Tag as string) == tag) { box.SelectedItem = item; return; }
        box.SelectedIndex = 0;
    }

    static string TagOf(ComboBox box, string fallback) => (box.SelectedItem as ComboBoxItem)?.Tag as string ?? fallback;

    void TitleBar_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }

    // ---------------- theme / colors ----------------
    void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_busy || CustomPanel == null) return;
        CustomPanel.Visibility = TagOf(ThemeBox, "Aero") == "Custom" ? Visibility.Visible : Visibility.Collapsed;
        UpdateBackgroundUi();
    }

    void SetColor(Color c, bool silent = false)
    {
        bool was = _busy; _busy = true;
        RSlider.Value = c.R; GSlider.Value = c.G; BSlider.Value = c.B;
        HexBox.Text = ThemeManager.ToHex(c);
        Preview.Background = new SolidColorBrush(c);
        _busy = was;
        if (!silent) _s.CustomColor = ThemeManager.ToHex(c);
        UpdateBackgroundUi();
    }

    void Rgb_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_busy) return;
        SetColor(Color.FromRgb((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value));
    }

    void Hex_Changed(object sender, TextChangedEventArgs e)
    {
        if (_busy) return;
        if (ThemeManager.TryParse(HexBox.Text, out var c))
        {
            bool was = _busy; _busy = true;
            RSlider.Value = c.R; GSlider.Value = c.G; BSlider.Value = c.B;
            Preview.Background = new SolidColorBrush(c);
            _s.CustomColor = ThemeManager.ToHex(c);
            _busy = was;
            UpdateBackgroundUi();
        }
    }

    void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityText != null) OpacityText.Text = $"{OpacitySlider.Value:0}%";
        UpdateBackgroundUi();
    }

    // ---------------- weather ----------------
    void ShowCurrentPlace() =>
        CurrentPlace.Text = $"Current: {_s.City}  ({_s.Latitude:0.00}, {_s.Longitude:0.00})";

    async void PlaceSearch_Click(object sender, RoutedEventArgs e)
    {
        string q = PlaceBox.Text.Trim();
        if (q.Length == 0) return;
        CurrentPlace.Text = "Searching...";
        var results = await GeoService.SearchAsync(q);
        ShowCurrentPlace();
        if (results.Count == 0) { CurrentPlace.Text = "No places found (or you're offline)."; PlaceResults.Visibility = Visibility.Collapsed; return; }
        PlaceResults.ItemsSource = results;
        PlaceResults.Visibility = Visibility.Visible;
    }

    void PlaceBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { PlaceSearch_Click(sender, e); e.Handled = true; }
    }

    void PlaceResults_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (PlaceResults.SelectedItem is not GeoPlace p) return;
        _s.City = p.Name; _s.Latitude = p.Lat; _s.Longitude = p.Lon;
        ShowCurrentPlace();
    }

    // ---------------- photos ----------------
    void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Choose your photo folder", InitialDirectory = FolderBox.Text };
        if (dlg.ShowDialog(this) == true) FolderBox.Text = dlg.FolderName;
    }

    // ---------------- clock ----------------
    void Source_Changed(object? sender, SelectionChangedEventArgs? e)
    {
        if (NtpBox == null || SyncBtn == null) return;
        bool ntp = TagOf(SourceBox, "System") == "NTP";
        NtpBox.IsEnabled = ntp;
        SyncBtn.IsEnabled = ntp;
    }

    async void Sync_Click(object sender, RoutedEventArgs e)
    {
        var backup = Settings.Current;
        var trial = backup.Clone();
        trial.TimeSource = "NTP";
        trial.NtpServer = NtpBox.Text.Trim();
        SyncStatus.Text = "Contacting server...";
        try
        {
            Settings.Save(trial);               // TimeService reads Settings.Current
        }
        catch (Exception ex)
        {
            SyncStatus.Text = $"Could not apply temporary clock settings: {ex.Message}";
            return;
        }

        SyncStatus.Text = await TimeService.SyncAsync();
        try
        {
            Settings.Save(backup);              // revert; Apply makes it permanent
        }
        catch (Exception ex)
        {
            SyncStatus.Text = $"Could not restore the saved clock settings: {ex.Message}";
        }
    }

    // ---------------- news feeds ----------------
    void FeedList_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (FeedList.SelectedItem is FeedConfig f) { FeedNameBox.Text = f.Name; FeedUrlBox.Text = f.Url; }
    }

    bool ReadFeedBoxes(out FeedConfig feed)
    {
        string url = FeedUrlBox.Text.Trim();
        feed = new FeedConfig();
        if (url.Length == 0) return false;
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
        string name = FeedNameBox.Text.Trim();
        if (name.Length == 0) name = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host.Replace("www.", "") : url;
        feed = new FeedConfig(name, url);
        return true;
    }

    void FeedAdd_Click(object sender, RoutedEventArgs e)
    {
        if (ReadFeedBoxes(out var f)) { _feeds.Add(f); FeedList.SelectedItem = f; }
    }

    void FeedUpdate_Click(object sender, RoutedEventArgs e)
    {
        int i = FeedList.SelectedIndex;
        if (i >= 0 && ReadFeedBoxes(out var f)) { _feeds[i] = f; FeedList.SelectedIndex = i; }
    }

    void FeedRemove_Click(object sender, RoutedEventArgs e)
    {
        int i = FeedList.SelectedIndex;
        if (i >= 0) { _feeds.RemoveAt(i); FeedNameBox.Clear(); FeedUrlBox.Clear(); }
    }

    // ---------------- background style ----------------
    void Preset_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_busy || PresetBox.SelectedItem is not ComboBoxItem { Tag: string key } || key.Length == 0) return;
        var preset = BackgroundPresets.Find(key);
        if (preset == null) return;

        _busy = true;
        SelectByTag(BgStyleBox, preset.Style);
        if (preset.Style == "Theme")
        {
            SelectByTag(ThemeBox, "Aero");
            CustomPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            AngleSlider.Value = preset.Angle;
            ShiftSlider.Value = 0;
            _stops = preset.Stops.Select(stop => new GradientStopConfig(stop.color, stop.offset)).ToList();
            BuildStopRows();
        }
        StreaksCheck.IsChecked = preset.Streaks;
        StreakSlider.Value = preset.Strength * 100;
        _busy = false;
        UpdateBackgroundUi();
    }

    void BgStyle_Changed(object sender, SelectionChangedEventArgs e) => UpdateBackgroundUi();
    void Bg_Changed(object sender, RoutedEventArgs e) => UpdateBackgroundUi();

    void UpdateBackgroundUi()
    {
        if (_busy || BgPreview == null) return;

        GradientPanel.Visibility = TagOf(BgStyleBox, "Theme") == "Gradient" ? Visibility.Visible : Visibility.Collapsed;
        AngleText.Text = $"{AngleSlider.Value:0}°";
        ShiftText.Text = $"{ShiftSlider.Value:+0;-0;0}%";
        StreakText.Text = $"{StreakSlider.Value:0}%";

        var preview = _s.Clone();
        preview.Theme = TagOf(ThemeBox, "Aero");
        preview.BackgroundStyle = TagOf(BgStyleBox, "Theme");
        preview.GradientAngle = AngleSlider.Value;
        preview.GradientShift = ShiftSlider.Value / 100.0;
        preview.GradientStops = _stops.Select(stop => new GradientStopConfig(stop.Color, stop.Offset)).ToList();

        BgPreview.Background = ThemeManager.BuildSidebarBrush(preview);
        BackgroundPreviewLayer.Opacity = OpacitySlider.Value / 100.0;
        var (streak, bloom, haze) = ThemeManager.BuildStreakColors(preview);
        PreviewStreaks.SetColors(streak, bloom, haze);
        PreviewStreaks.Visibility = StreaksCheck.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PreviewStreaks.Opacity = StreakSlider.Value / 100.0;
    }

    void BuildStopRows()
    {
        StopsPanel.Children.Clear();
        foreach (var stop in _stops) StopsPanel.Children.Add(MakeStopRow(stop));
        AddStopButton.IsEnabled = _stops.Count < 6;
    }

    UIElement MakeStopRow(GradientStopConfig stop)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

        ThemeManager.TryParse(stop.Color, out var color);
        var swatch = new Border
        {
            Width = 24, Height = 24, CornerRadius = new CornerRadius(4), Cursor = Cursors.Hand, ToolTip = "Pick a color",
            Background = new SolidColorBrush(color), BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
            BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left
        };
        var hex = new TextBox { Text = stop.Color, Height = 24, Width = 66, VerticalContentAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = stop.Offset * 100, VerticalAlignment = VerticalAlignment.Center };
        var percent = new TextBlock { Text = $"{stop.Offset * 100:0}%", TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        var remove = new Button { Content = "×", Width = 20, Height = 20, Padding = new Thickness(0), IsEnabled = _stops.Count > 2, ToolTip = "Remove this color", Cursor = Cursors.Hand };

        hex.TextChanged += (_, _) =>
        {
            if (!ThemeManager.TryParse(hex.Text, out var parsed)) return;
            stop.Color = ThemeManager.ToHex(parsed);
            swatch.Background = new SolidColorBrush(parsed);
            UpdateBackgroundUi();
        };
        slider.ValueChanged += (_, _) =>
        {
            stop.Offset = slider.Value / 100.0;
            percent.Text = $"{slider.Value:0}%";
            UpdateBackgroundUi();
        };
        remove.Click += (_, _) =>
        {
            if (_stops.Count <= 2) return;
            _stops.Remove(stop);
            BuildStopRows();
            UpdateBackgroundUi();
        };

        var palette = new WrapPanel { Width = 168 };
        var popup = new Popup
        {
            PlacementTarget = swatch, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true,
            Child = new Border
            {
                Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(4), Child = palette
            }
        };
        foreach (var paletteHex in PaletteColors)
        {
            ThemeManager.TryParse(paletteHex, out var paletteColor);
            var chip = new Border
            {
                Width = 20, Height = 20, Margin = new Thickness(2), CornerRadius = new CornerRadius(3), Cursor = Cursors.Hand,
                Background = new SolidColorBrush(paletteColor), BorderBrush = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)),
                BorderThickness = new Thickness(1), ToolTip = paletteHex
            };
            chip.MouseLeftButtonUp += (_, _) => { hex.Text = paletteHex; popup.IsOpen = false; };
            palette.Children.Add(chip);
        }
        swatch.MouseLeftButtonUp += (_, _) => popup.IsOpen = true;

        Grid.SetColumn(swatch, 0); Grid.SetColumn(hex, 1); Grid.SetColumn(slider, 2); Grid.SetColumn(percent, 3); Grid.SetColumn(remove, 4);
        row.Children.Add(swatch); row.Children.Add(hex); row.Children.Add(slider); row.Children.Add(percent); row.Children.Add(remove); row.Children.Add(popup);
        return row;
    }

    void AddStop_Click(object sender, RoutedEventArgs e)
    {
        if (_stops.Count >= 6) return;
        var sorted = _stops.OrderBy(stop => stop.Offset).ToList();
        int best = 0;
        double biggestGap = -1;
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            double gap = sorted[i + 1].Offset - sorted[i].Offset;
            if (gap > biggestGap) { biggestGap = gap; best = i; }
        }
        ThemeManager.TryParse(sorted[best].Color, out var first);
        ThemeManager.TryParse(sorted[best + 1].Color, out var second);
        var blend = Color.FromRgb((byte)((first.R + second.R) / 2), (byte)((first.G + second.G) / 2), (byte)((first.B + second.B) / 2));
        _stops.Add(new GradientStopConfig(ThemeManager.ToHex(blend), (sorted[best].Offset + sorted[best + 1].Offset) / 2));
        BuildStopRows();
        UpdateBackgroundUi();
    }

    // ---------------- buttons ----------------
    void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!RefreshWidgetCapacity())
        {
            Tabs.SelectedItem = GadgetsTab;
            return;
        }

        _s.Theme = TagOf(ThemeBox, "Aero");
        _s.BackgroundStyle = TagOf(BgStyleBox, "Theme");
        _s.GradientAngle = AngleSlider.Value;
        _s.GradientShift = ShiftSlider.Value / 100.0;
        _s.GradientStops = _stops.Select(stop => new GradientStopConfig(stop.Color, stop.Offset)).ToList();
        _s.Streaks = StreaksCheck.IsChecked == true;
        _s.StreakStrength = StreakSlider.Value / 100.0;
        _s.SidebarOpacity = OpacitySlider.Value / 100.0;
        _s.KeepOnTop = TopmostCheck.IsChecked == true;
        _s.MoveDesktopIcons = MoveIconsCheck.IsChecked == true;
        _s.RestoreIconsOnExit = RestoreIconsCheck.IsChecked == true;
        _s.EnableDoubleCtrlSearch = DoubleCtrlSearchCheck.IsChecked == true;

        _s.ClockStyle = TagOf(ClockStyleBox, "Analog");
        _s.Use24Hour = Hour24Check.IsChecked == true;
        _s.TimeSource = TagOf(SourceBox, "System");
        _s.NtpServer = string.IsNullOrWhiteSpace(NtpBox.Text) ? "time.windows.com" : NtpBox.Text.Trim();
        _s.TimeZoneId = (ZoneBox.SelectedItem as ZoneItem)?.Id ?? "";
        _s.ClockLabel = LabelBox.Text.Trim();

        _s.ShowPhotos = PhotosCheck.IsChecked == true;
        _s.ShowNews = NewsCheck.IsChecked == true;
        _s.EnableCalculator = CalculatorCheck.IsChecked == true;
        _s.EnableCalendar = CalendarCheck.IsChecked == true;
        _s.EnableNotes = NotesCheck.IsChecked == true;
        _s.PhotoFolder = FolderBox.Text.Trim();
        _s.UseFahrenheit = FahrenheitRadio.IsChecked == true;
        _s.Feeds = _feeds.Select(f => new FeedConfig(f.Name, f.Url)).ToList();

        try
        {
            Settings.Save(_s);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save settings: {ex.Message}", "Google Desktop Settings",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        _s = Settings.Current.Clone();      // keep editing from a fresh copy; the window stays open
        _ = TimeService.SyncAsync();
    }

    void Close_Click(object sender, RoutedEventArgs e) => Close();
}
