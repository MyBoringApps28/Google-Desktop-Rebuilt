namespace GoogleDesktopRemake.Clocks;

public interface IClockFace { void Update(DateTime now); }

public static class ClockText
{
    /// <summary>Four hour/minute characters ("0905"; leading blank in 12-hour mode) plus "AM"/"PM" (empty in 24-hour mode).</summary>
    public static (string digits, string ampm) Parts(DateTime n, bool h24)
    {
        int h = n.Hour; string ap = "";
        if (!h24) { ap = h < 12 ? "AM" : "PM"; h %= 12; if (h == 0) h = 12; }
        string hh = h.ToString("00");
        if (!h24 && hh[0] == '0') hh = " " + hh[1];
        return (hh + n.Minute.ToString("00"), ap);
    }
}
