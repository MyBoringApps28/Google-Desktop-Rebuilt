using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace GoogleDesktopRemake.Gadgets;

public partial class WeatherGadget : UserControl
{
    static readonly HttpClient Http = new();
    bool _started;

    public WeatherGadget()
    {
        InitializeComponent();
        CityText.Text = Settings.Current.City;
        Settings.Changed += () => { CityText.Text = Settings.Current.City; _ = RefreshAsync(); };
        Loaded += async (_, _) =>
        {
            if (_started) return;
            _started = true;
            while (true)
            {
                await RefreshAsync();
                await Task.Delay(TimeSpan.FromMinutes(20));
            }
        };
    }

    async Task RefreshAsync()
    {
        try
        {
            var inv = CultureInfo.InvariantCulture;
            var cfg = Settings.Current;
            string unit = cfg.UseFahrenheit ? "fahrenheit" : "celsius";
            string url = $"https://api.open-meteo.com/v1/forecast?latitude={cfg.Latitude.ToString(inv)}" +
                         $"&longitude={cfg.Longitude.ToString(inv)}&current=temperature_2m,weather_code,is_day" +
                         $"&daily=temperature_2m_max,temperature_2m_min&temperature_unit={unit}&timezone=auto&forecast_days=1";
            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
            var root = doc.RootElement;
            var cur = root.GetProperty("current");
            double t = cur.GetProperty("temperature_2m").GetDouble();
            int code = cur.GetProperty("weather_code").GetInt32();
            bool isDay = cur.GetProperty("is_day").GetInt32() == 1;
            var daily = root.GetProperty("daily");
            double hi = daily.GetProperty("temperature_2m_max")[0].GetDouble();
            double lo = daily.GetProperty("temperature_2m_min")[0].GetDouble();

            TempText.Text = $"{Math.Round(t)}°";
            HiLoText.Text = $"H:{Math.Round(hi)}° | L:{Math.Round(lo)}°";
            ApplyIcon(code, isDay);
        }
        catch { HiLoText.Text = "offline"; }
    }

    // WMO weather codes: 0-1 clear, 2 partly cloudy, 3+ cloudy/fog/rain/snow
    void ApplyIcon(int code, bool isDay)
    {
        Sun.Visibility = code <= 2 && isDay ? Visibility.Visible : Visibility.Collapsed;
        Moon.Visibility = code <= 2 && !isDay ? Visibility.Visible : Visibility.Collapsed;
        Cloud.Visibility = code >= 2 ? Visibility.Visible : Visibility.Collapsed;
        bool rain = (code is >= 51 and <= 67) || (code is >= 80 and <= 82) || code >= 95;
        Rain.Visibility = rain ? Visibility.Visible : Visibility.Collapsed;
        Thunder.Visibility = code >= 95 ? Visibility.Visible : Visibility.Collapsed;
    }
}
