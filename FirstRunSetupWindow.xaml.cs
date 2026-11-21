using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleDesktopRemake;

public partial class FirstRunSetupWindow : Window
{
    GeoPlace? _selectedPlace;

    public FirstRunSetupWindow()
    {
        InitializeComponent();
        var settings = Settings.Current;
        SelectTheme(settings.Theme);
        CustomColorBox.Text = settings.CustomColor;
        FolderBox.Text = Directory.Exists(settings.PhotoFolder)
            ? settings.PhotoFolder
            : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        PlaceBox.Text = settings.City;
    }

    void SelectTheme(string theme)
    {
        foreach (ComboBoxItem item in ThemeBox.Items)
        {
            if (string.Equals(item.Tag as string, theme, StringComparison.Ordinal))
            {
                ThemeBox.SelectedItem = item;
                return;
            }
        }
        ThemeBox.SelectedIndex = 0;
    }

    static string SelectedTheme(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Tag as string ?? "Aero";

    void TitleBar_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CustomColorPanel != null)
            CustomColorPanel.Visibility = SelectedTheme(ThemeBox) == "Custom" ? Visibility.Visible : Visibility.Collapsed;
    }

    async void FindPlace_Click(object sender, RoutedEventArgs e)
    {
        string query = PlaceBox.Text.Trim();
        if (query.Length == 0) return;

        PlaceStatus.Text = "Searching...";
        PlaceResults.Visibility = Visibility.Collapsed;
        PlaceResults.ItemsSource = null;
        _selectedPlace = null;
        var results = await GeoService.SearchAsync(query);
        if (results.Count == 0)
        {
            PlaceStatus.Text = "No places found. Check the connection and try again.";
            return;
        }

        PlaceResults.ItemsSource = results;
        PlaceResults.Visibility = Visibility.Visible;
        PlaceStatus.Text = "Select the matching city from the results.";
    }

    void PlaceResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlaceResults.SelectedItem is not GeoPlace place) return;
        _selectedPlace = place;
        PlaceBox.Text = place.Name;
        PlaceStatus.Text = $"Selected: {place.Display}";
    }

    void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose your photo folder",
            InitialDirectory = Directory.Exists(FolderBox.Text)
                ? FolderBox.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };
        if (dialog.ShowDialog(this) == true) FolderBox.Text = dialog.FolderName;
    }

    void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPlace is null)
        {
            ErrorText.Text = "Search for your weather location and select a result.";
            return;
        }
        if (!Directory.Exists(FolderBox.Text.Trim()))
        {
            ErrorText.Text = "Choose an existing photo folder.";
            return;
        }

        string theme = SelectedTheme(ThemeBox);
        string customColor = CustomColorBox.Text.Trim();
        if (theme == "Custom" && !ThemeManager.TryParse(customColor, out _))
        {
            ErrorText.Text = "Enter a valid color, such as #1565B8.";
            return;
        }

        var settings = Settings.Current.Clone();
        settings.Theme = theme;
        settings.CustomColor = customColor;
        settings.City = _selectedPlace.Name;
        settings.Latitude = _selectedPlace.Lat;
        settings.Longitude = _selectedPlace.Lon;
        settings.PhotoFolder = FolderBox.Text.Trim();
        settings.SetupCompleted = true;
        try
        {
            Settings.Save(settings);
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not save your preferences: {ex.Message}";
            return;
        }
        DialogResult = true;
    }

    void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
