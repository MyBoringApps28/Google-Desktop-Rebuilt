using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace GoogleDesktopRemake;

/// <summary>Keeps the sidebar out of Alt+Tab (it behaves like a desktop gadget).</summary>
public static class WindowStyles
{
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr h, int index, IntPtr value);
    const int GWL_EXSTYLE = -20;
    const long WS_EX_TOOLWINDOW = 0x80;

    public static void HideFromAltTab(Window w)
    {
        var hwnd = new WindowInteropHelper(w).Handle;
        long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex | WS_EX_TOOLWINDOW));
    }
}

/// <summary>Global double-tap Ctrl detector (the original Quick Search Box hotkey).</summary>
public sealed class DoubleCtrlHook : IDisposable
{
    delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int id, HookProc fn, IntPtr mod, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr w, IntPtr l);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string name);

    const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;

    readonly HookProc _proc;   // keep a reference so the GC doesn't collect it
    readonly IntPtr _hook;
    bool _ctrlDown, _otherKey;
    long _lastTap;

    public event Action? Triggered;

    public DoubleCtrlHook()
    {
        _proc = Callback;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
            GetModuleHandle(Process.GetCurrentProcess().MainModule!.ModuleName), 0);
    }

    IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            int vk = Marshal.ReadInt32(lParam);
            bool isCtrl = vk == 0xA2 || vk == 0xA3;
            int msg = (int)wParam;
            bool down = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            bool up = msg == WM_KEYUP || msg == WM_SYSKEYUP;

            if (isCtrl && down && !_ctrlDown) { _ctrlDown = true; _otherKey = false; }
            else if (!isCtrl && down) { _otherKey = true; _lastTap = 0; }
            else if (isCtrl && up)
            {
                _ctrlDown = false;
                if (!_otherKey)
                {
                    long now = Environment.TickCount64;
                    if (_lastTap != 0 && now - _lastTap < 400)
                    {
                        _lastTap = 0;
                        Application.Current.Dispatcher.BeginInvoke(() => Triggered?.Invoke());
                    }
                    else _lastTap = now;
                }
            }
        }
        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    public void Dispose() => UnhookWindowsHookEx(_hook);
}
