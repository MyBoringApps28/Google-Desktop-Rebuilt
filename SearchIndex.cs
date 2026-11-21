using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogleDesktopRemake;

public record IndexEntry(string Name, string Path, bool IsApp);

public class SearchResult
{
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string Glyph { get; init; } = "";
    public string Kind { get; init; } = "";
    public ImageSource? Icon { get; init; }
    public bool HasIcon => Icon != null;
    public Action? Launch { get; init; }
}

/// <summary>A tiny "Desktop Search": Start-menu apps plus files in Desktop/Documents/Downloads.</summary>
public static class SearchIndex
{
    static List<IndexEntry> _entries = new();

    public static Task BuildAsync() => Task.Run(() =>
    {
        var list = new List<IndexEntry>();
        var opt = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MaxRecursionDepth = 4 };

        foreach (var root in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
        })
        {
            if (!Directory.Exists(root)) continue;
            foreach (var f in Directory.EnumerateFiles(root, "*.lnk", opt))
                list.Add(new IndexEntry(System.IO.Path.GetFileNameWithoutExtension(f), f, true));
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var root in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            System.IO.Path.Combine(userProfile, "Downloads")
        })
        {
            if (!Directory.Exists(root)) continue;
            var o = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MaxRecursionDepth = 3 };
            foreach (var f in Directory.EnumerateFiles(root, "*.*", o).Take(20000))
                list.Add(new IndexEntry(System.IO.Path.GetFileName(f), f, false));
        }
        _entries = list;
    });

    public static List<IndexEntry> Query(string text, int max)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return new();
        return _entries
            .Where(e => tokens.All(t => e.Name.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(e => e.Name.StartsWith(tokens[0], StringComparison.OrdinalIgnoreCase) ? 0 :
                          e.Name.Contains(" " + tokens[0], StringComparison.OrdinalIgnoreCase) ? 1 : 2)
            .ThenBy(e => e.IsApp ? 0 : 1)
            .ThenBy(e => e.Name.Length)
            .Take(max).ToList();
    }

    public static ImageSource? GetIcon(string path)
    {
        try
        {
            using var ic = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (ic == null) return null;
            var bs = Imaging.CreateBitmapSourceFromHIcon(ic.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bs.Freeze();
            return bs;
        }
        catch { return null; }
    }
}
