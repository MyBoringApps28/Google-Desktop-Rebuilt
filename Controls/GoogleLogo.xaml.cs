using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleDesktopRemake.Controls;

/// <summary>The wordmark. Click it to open google.com.</summary>
public partial class GoogleLogo : UserControl
{
    public GoogleLogo()
    {
        InitializeComponent();
        MouseEnter += (_, _) => Opacity = 0.85;
        MouseLeave += (_, _) => Opacity = 1;
        MouseLeftButtonDown += (_, e) => e.Handled = true;      // don't start a window drag
        MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            try { Process.Start(new ProcessStartInfo("https://www.google.com") { UseShellExecute = true }); } catch { }
        };
    }
}
