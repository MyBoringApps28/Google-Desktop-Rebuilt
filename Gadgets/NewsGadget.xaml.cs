using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using GoogleDesktopRemake.Controls;

namespace GoogleDesktopRemake.Gadgets;

public record NewsItem(string Title, string Link, string Source, string Age, DateTimeOffset When);

/// <summary>Merges any number of RSS 2.0 / Atom feeds, newest first.</summary>
public partial class NewsGadget : UserControl
{
    static readonly HttpClient Http = CreateClient();
    bool _started;

    static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 GoogleDesktopRemake");
        return c;
    }

    public NewsGadget()
    {
        InitializeComponent();
        Scroller.ScrollChanged += (_, _) => UpdateArrows();
        Settings.Changed += () => _ = RefreshAsync();
        Loaded += async (_, _) =>
        {
            if (_started) return;
            _started = true;
            while (true)
            {
                await RefreshAsync();
                await Task.Delay(TimeSpan.FromMinutes(15));
            }
        };
    }

    async Task RefreshAsync()
    {
        try
        {
            var feeds = Settings.Current.Feeds.Where(f => !string.IsNullOrWhiteSpace(f.Url)).ToList();
            var results = await Task.WhenAll(feeds.Select(FetchAsync));
            Items.ItemsSource = results.SelectMany(x => x).OrderByDescending(i => i.When).Take(25).ToList();
        }
        catch { }
    }

    static async Task<List<NewsItem>> FetchAsync(FeedConfig feed)
    {
        var list = new List<NewsItem>();
        try
        {
            var xml = XDocument.Parse(await Http.GetStringAsync(feed.Url));
            var entries = xml.Descendants().Where(e => e.Name.LocalName is "item" or "entry").Take(10).ToList();
            for (int idx = 0; idx < entries.Count; idx++)
            {
                var e = entries[idx];
                string title = Child(e, "title")?.Value.Trim() ?? "";
                if (title.Length == 0) continue;

                string link = Child(e, "link")?.Attribute("href")?.Value        // Atom
                           ?? Child(e, "link")?.Value.Trim()                    // RSS
                           ?? Child(e, "guid")?.Value.Trim() ?? "";

                string? dateText = (Child(e, "pubDate") ?? Child(e, "published") ?? Child(e, "updated") ?? Child(e, "date"))?.Value;
                // undated feeds keep their own order
                var when = DateTimeOffset.TryParse(dateText, out var d) ? d : DateTimeOffset.Now.AddMinutes(-idx);
                list.Add(new NewsItem(title, link, feed.Name, Ago(DateTimeOffset.Now - when), when));
            }
        }
        catch { }
        return list;
    }

    static XElement? Child(XElement e, string localName) => e.Elements().FirstOrDefault(c => c.Name.LocalName == localName);

    static string Ago(TimeSpan t) =>
        t.TotalMinutes < 1 ? "now" :
        t.TotalHours < 1 ? $"{(int)t.TotalMinutes}m ago" :
        t.TotalDays < 1 ? $"{(int)t.TotalHours}h ago" : $"{(int)t.TotalDays}d ago";

    void UpdateArrows()
    {
        UpArrow.Visibility = Scroller.VerticalOffset > 1 ? Visibility.Visible : Visibility.Collapsed;
        DownArrow.Visibility = Scroller.VerticalOffset < Scroller.ScrollableHeight - 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    void Up_Click(object sender, RoutedEventArgs e) => ScrollAnimator.ScrollBy(Scroller, -Scroller.ViewportHeight * 0.6);
    void Down_Click(object sender, RoutedEventArgs e) => ScrollAnimator.ScrollBy(Scroller, Scroller.ViewportHeight * 0.6);

    void Item_Click(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is NewsItem n && n.Link.StartsWith("http"))
            Process.Start(new ProcessStartInfo(n.Link) { UseShellExecute = true });
    }
}
