using System.Threading;
using System.Windows;

namespace GoogleDesktopRemake;

public partial class App : Application
{
    static readonly string InstanceMutexName = @"Local\GoogleDesktopRevived.SingleInstance";
    DoubleCtrlHook? _hook;
    Mutex? _instanceMutex;
    bool _ownsInstanceMutex;
    public static QuickSearchWindow? QuickSearch { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _instanceMutex = new Mutex(initiallyOwned: true, InstanceMutexName, out bool createdNew);
        if (!createdNew)
        {
            _instanceMutex.Dispose();
            _instanceMutex = null;
            MessageBox.Show("Google Desktop Revived is already running.", "Google Desktop Revived",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        _ownsInstanceMutex = true;

        ThemeManager.Apply();
        Settings.Changed += ThemeManager.Apply;
        Settings.Changed += UpdateDoubleCtrlHook;

        if (!Settings.Current.SetupCompleted)
        {
            var setup = new FirstRunSetupWindow();
            if (setup.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }

        var sidebar = new SidebarWindow();
        MainWindow = sidebar;
        sidebar.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        QuickSearch = new QuickSearchWindow();
        _ = SearchIndex.BuildAsync();
        _ = TimeService.RunPeriodicSyncAsync();

        UpdateDoubleCtrlHook();
    }

    void UpdateDoubleCtrlHook()
    {
        _hook?.Dispose();
        _hook = null;
        if (!Settings.Current.EnableDoubleCtrlSearch || QuickSearch == null) return;

        _hook = new DoubleCtrlHook();
        _hook.Triggered += () => QuickSearch?.Toggle();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
        if (_instanceMutex != null)
        {
            if (_ownsInstanceMutex) _instanceMutex.ReleaseMutex();
            _instanceMutex.Dispose();
        }
        base.OnExit(e);
    }
}
