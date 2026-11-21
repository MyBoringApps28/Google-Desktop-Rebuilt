using System.Net.Sockets;

namespace GoogleDesktopRemake;

/// <summary>Clock source for the sidebar: PC clock, or internet time via NTP (e.g. time.windows.com).</summary>
public static class TimeService
{
    static TimeSpan _offset = TimeSpan.Zero;
    static string _zoneId = "\0";
    static TimeZoneInfo _zone = TimeZoneInfo.Local;

    public static DateTime Now
    {
        get
        {
            var id = Settings.Current.TimeZoneId ?? "";
            if (id != _zoneId)
            {
                _zoneId = id;
                try { _zone = id == "" ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch { _zone = TimeZoneInfo.Local; }
            }
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow + _offset, _zone);
        }
    }

    /// <summary>Re-syncs and returns a human-readable status line.</summary>
    public static async Task<string> SyncAsync()
    {
        var s = Settings.Current;
        if (s.TimeSource != "NTP") { _offset = TimeSpan.Zero; return "Using this PC's clock."; }
        var off = await Task.Run(() => QueryOffsetAsync(s.NtpServer));
        if (off == null) return $"Couldn't reach {s.NtpServer} - using this PC's clock.";
        _offset = off.Value;
        return $"Synced with {s.NtpServer} (PC clock is off by {-off.Value.TotalSeconds:+0.00;-0.00} s).";
    }

    public static async Task RunPeriodicSyncAsync()
    {
        while (true)
        {
            await SyncAsync();
            await Task.Delay(TimeSpan.FromMinutes(30));
        }
    }

    static async Task<TimeSpan?> QueryOffsetAsync(string server)
    {
        try
        {
            var packet = new byte[48];
            packet[0] = 0x1B; // LI=0, VN=3, Mode=3 (client)
            using var udp = new UdpClient();
            udp.Connect(server, 123);
            var t1 = DateTime.UtcNow;
            await udp.SendAsync(packet, packet.Length);
            var recv = udp.ReceiveAsync();
            if (await Task.WhenAny(recv, Task.Delay(3000)) != recv) return null;
            var t4 = DateTime.UtcNow;
            var buf = recv.Result.Buffer;
            if (buf.Length < 48) return null;

            DateTime Stamp(int o)
            {
                ulong sec = ((ulong)buf[o] << 24) | ((ulong)buf[o + 1] << 16) | ((ulong)buf[o + 2] << 8) | buf[o + 3];
                ulong frac = ((ulong)buf[o + 4] << 24) | ((ulong)buf[o + 5] << 16) | ((ulong)buf[o + 6] << 8) | buf[o + 7];
                double ms = sec * 1000.0 + frac * 1000.0 / 4294967296.0;
                return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms);
            }
            var t2 = Stamp(32); // server receive
            var t3 = Stamp(40); // server transmit
            return ((t2 - t1) + (t3 - t4)) / 2;
        }
        catch { return null; }
    }
}
