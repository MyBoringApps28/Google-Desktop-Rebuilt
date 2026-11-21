using System.Runtime.InteropServices;
using System.Windows;

namespace GoogleDesktopRemake;

/// <summary>
/// Moves desktop icons (files, folders, shortcuts) that would sit under the sidebar into free grid
/// cells to its left, and can put them back. Talks to Explorer's desktop list view directly.
/// </summary>
public static class DesktopIcons
{
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int left, top, right, bottom; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string? title);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string? title);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint cmd);
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] static extern IntPtr VirtualAllocEx(IntPtr p, IntPtr addr, UIntPtr size, uint type, uint prot);
    [DllImport("kernel32.dll")] static extern bool VirtualFreeEx(IntPtr p, IntPtr addr, UIntPtr size, uint type);
    [DllImport("kernel32.dll")] static extern bool ReadProcessMemory(IntPtr p, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const int LVM_GETITEMCOUNT = 0x1004, LVM_SETITEMPOSITION = 0x100F, LVM_GETITEMPOSITION = 0x1010, LVM_GETITEMSPACING = 0x1033;
    const int GWL_STYLE = -16;
    const long LVS_AUTOARRANGE = 0x100;

    static readonly Dictionary<int, (int x, int y)> _original = new();
    static int _savedCount = -1;

    /// <summary>Human-readable reason when nothing could be moved (shown nowhere by default, handy for debugging).</summary>
    public static string LastNote { get; private set; } = "";

    static IntPtr FindDesktopListView()
    {
        IntPtr defView = FindWindowEx(FindWindow("Progman", null), IntPtr.Zero, "SHELLDLL_DefView", null);
        if (defView == IntPtr.Zero)
        {
            IntPtr w = IntPtr.Zero;
            while ((w = FindWindowEx(IntPtr.Zero, w, "WorkerW", null)) != IntPtr.Zero)
            {
                defView = FindWindowEx(w, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero) break;
            }
        }
        return defView == IntPtr.Zero ? IntPtr.Zero : FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
    }

    /// <summary>The window that hosts the desktop icons (Progman, or a WorkerW on some setups).</summary>
    public static IntPtr FindDesktopHost()
    {
        IntPtr progman = FindWindow("Progman", null);
        if (FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero) return progman;
        IntPtr w = IntPtr.Zero;
        while ((w = FindWindowEx(IntPtr.Zero, w, "WorkerW", null)) != IntPtr.Zero)
            if (FindWindowEx(w, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero) return w;
        return IntPtr.Zero;
    }

    /// <summary>
    /// Z-order slot "just above the desktop, below every normal window".
    /// Returns the hwndInsertAfter value to use, or Zero to leave the order alone.
    /// </summary>
    public static IntPtr InsertAfterForDesktop(IntPtr self)
    {
        var host = FindDesktopHost();
        if (host == IntPtr.Zero) return IntPtr.Zero;
        var prev = GetWindow(host, 3);                       // GW_HWNDPREV
        if (prev == self) return self;                       // already in the right slot
        bool prevIsTopmost = prev != IntPtr.Zero && (GetWindowLongPtr(prev, -20).ToInt64() & 0x8) != 0;
        if (prev == IntPtr.Zero || prevIsTopmost) return new IntPtr(-2);   // HWND_NOTOPMOST
        return prev;
    }

    static List<(int x, int y)>? ReadPositions(IntPtr lv, int count)
    {
        GetWindowThreadProcessId(lv, out uint pid);
        IntPtr proc = OpenProcess(0x0008 | 0x0010 | 0x0020, false, pid);   // VM_OPERATION | VM_READ | VM_WRITE
        if (proc == IntPtr.Zero) { LastNote = "Couldn't open Explorer."; return null; }
        IntPtr remote = VirtualAllocEx(proc, IntPtr.Zero, (UIntPtr)8, 0x3000, 0x04);
        if (remote == IntPtr.Zero) { CloseHandle(proc); return null; }
        try
        {
            var list = new List<(int, int)>(count);
            var buf = new byte[8];
            for (int i = 0; i < count; i++)
            {
                SendMessage(lv, LVM_GETITEMPOSITION, (IntPtr)i, remote);
                ReadProcessMemory(proc, remote, buf, 8, out _);
                list.Add((BitConverter.ToInt32(buf, 0), BitConverter.ToInt32(buf, 4)));
            }
            return list;
        }
        finally { VirtualFreeEx(proc, remote, UIntPtr.Zero, 0x8000); CloseHandle(proc); }
    }

    static void SetPos(IntPtr lv, int index, int x, int y) =>
        SendMessage(lv, LVM_SETITEMPOSITION, (IntPtr)index, new IntPtr(((y & 0xFFFF) << 16) | (x & 0xFFFF)));

    /// <param name="sidebarLeft">Sidebar's left edge in screen pixels. workTop/workBottom: usable height in pixels.</param>
    /// <returns>How many icons were moved.</returns>
    public static int MakeRoom(int sidebarLeft, int workTop, int workBottom)
    {
        var lv = FindDesktopListView();
        if (lv == IntPtr.Zero) { LastNote = "Desktop not found."; return 0; }
        if ((GetWindowLongPtr(lv, GWL_STYLE).ToInt64() & LVS_AUTOARRANGE) != 0)
        { LastNote = "'Auto arrange icons' is on - turn it off to let icons move."; return 0; }

        int count = (int)SendMessage(lv, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
        if (count <= 0) return 0;
        if (count != _savedCount) { _original.Clear(); _savedCount = count; }

        var pos = ReadPositions(lv, count);
        if (pos == null) return 0;

        long sp = SendMessage(lv, LVM_GETITEMSPACING, IntPtr.Zero, IntPtr.Zero).ToInt64();
        int cx = (int)(sp & 0xFFFF), cy = (int)((sp >> 16) & 0xFFFF);
        if (cx < 20 || cy < 20) return 0;

        GetWindowRect(lv, out var wr);
        int monLeft = -wr.left;
        int monRight = GetSystemMetrics(0) - wr.left;          // primary monitor only
        int top = workTop - wr.top, bottom = workBottom - wr.top;
        int barLeft = sidebarLeft - wr.left;

        Rect Cell(int x, int y) => new(x + 6, y + 6, cx - 12, cy - 12);

        var occupied = new List<Rect>();
        var toMove = new List<int>();
        for (int i = 0; i < pos.Count; i++)
        {
            var (x, y) = pos[i];
            bool onPrimary = x + cx > monLeft && x < monRight;
            if (onPrimary && x + cx > barLeft + 4) toMove.Add(i);
            else occupied.Add(Cell(x, y));
        }
        if (toMove.Count == 0) return 0;

        // candidate cells follow the desktop's own grid
        int minX = pos.Min(p => p.x), minY = pos.Min(p => p.y);
        int gx = ((minX % cx) + cx) % cx, gy = ((minY % cy) + cy) % cy;
        int colMax = (int)Math.Floor((barLeft - 8 - cx - gx) / (double)cx);
        int colMin = (int)Math.Ceiling((monLeft - gx) / (double)cx);
        int rowMin = (int)Math.Ceiling((top - gy) / (double)cy);

        int moved = 0;
        foreach (int i in toMove.OrderBy(i => pos[i].y).ThenBy(i => pos[i].x))
        {
            bool placed = false;
            for (int col = colMax; col >= colMin && !placed; col--)
            {
                int x = gx + col * cx;
                for (int row = rowMin; ; row++)
                {
                    int y = gy + row * cy;
                    if (y + cy > bottom) break;
                    var cell = Cell(x, y);
                    if (occupied.Any(r => r.IntersectsWith(cell))) continue;

                    if (!_original.ContainsKey(i)) _original[i] = pos[i];
                    SetPos(lv, i, x, y);
                    occupied.Add(cell);
                    placed = true; moved++;
                    break;
                }
            }
        }
        return moved;
    }

    /// <summary>Puts moved icons back where they were (only if the desktop item count is unchanged).</summary>
    public static void Restore()
    {
        if (_original.Count == 0) return;
        var lv = FindDesktopListView();
        if (lv != IntPtr.Zero)
        {
            int count = (int)SendMessage(lv, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
            if (count == _savedCount)
                foreach (var (i, p) in _original) SetPos(lv, i, p.x, p.y);
        }
        _original.Clear();
    }
}
