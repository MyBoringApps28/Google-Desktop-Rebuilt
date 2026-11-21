using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GoogleDesktopRemake.Gadgets;

public partial class PhotoGadget : UserControl
{
    readonly List<string> _files = new();
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(8) };
    int _index;

    public PhotoGadget()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Next();
        Settings.Changed += ReloadFolder;
        ReloadFolder();
    }

    void ReloadFolder()
    {
        _timer.Stop();
        _files.Clear();
        _index = 0;
        Front.BeginAnimation(OpacityProperty, null);
        Front.Opacity = 1;
        Front.Source = null;
        Back.Source = null;

        try
        {
            string folder = Settings.Current.PhotoFolder;
            if (Directory.Exists(folder))
            {
                var opt = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MaxRecursionDepth = 2 };
                var rnd = new Random();
                _files.AddRange(Directory.EnumerateFiles(folder, "*.*", opt)
                    .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                    .Take(500)
                    .OrderBy(_ => rnd.Next()));
            }
        }
        catch { }

        if (_files.Count == 0) return;
        Front.Source = Load(_files[0]);
        if (_files.Count > 1) _timer.Start();
    }

    static BitmapImage? Load(string path)
    {
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.DecodePixelWidth = 400;
            bi.UriSource = new Uri(path);
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        catch { return null; }
    }

    void Next()
    {
        if (_files.Count == 0) return;
        _index = (_index + 1) % _files.Count;
        var img = Load(_files[_index]);
        if (img == null) return;
        Back.Source = img;
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1));
        fade.Completed += (_, _) =>
        {
            Front.BeginAnimation(OpacityProperty, null);
            Front.Source = img;
            Front.Opacity = 1;
        };
        Front.BeginAnimation(OpacityProperty, fade);
    }
}
