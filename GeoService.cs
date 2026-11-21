using System.Net.Http;
using System.Text.Json;

namespace GoogleDesktopRemake;

public record GeoPlace(string Name, string Admin1, string Country, double Lat, double Lon)
{
    public string Display => string.Join(", ", new[] { Name, Admin1, Country }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

/// <summary>City lookup using Open-Meteo's free geocoding API (no API key).</summary>
public static class GeoService
{
    static readonly HttpClient Http = new();

    public static async Task<List<GeoPlace>> SearchAsync(string query)
    {
        var list = new List<GeoPlace>();
        try
        {
            string url = "https://geocoding-api.open-meteo.com/v1/search?count=6&language=en&name=" + Uri.EscapeDataString(query);
            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
            if (!doc.RootElement.TryGetProperty("results", out var results)) return list;
            foreach (var r in results.EnumerateArray())
            {
                list.Add(new GeoPlace(
                    r.GetProperty("name").GetString() ?? "",
                    r.TryGetProperty("admin1", out var a) ? a.GetString() ?? "" : "",
                    r.TryGetProperty("country", out var c) ? c.GetString() ?? "" : "",
                    r.GetProperty("latitude").GetDouble(),
                    r.GetProperty("longitude").GetDouble()));
            }
        }
        catch { }
        return list;
    }
}
