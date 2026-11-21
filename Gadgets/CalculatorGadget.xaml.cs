using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleDesktopRemake.Gadgets;

/// <summary>Small glossy calculator: + − × ÷, exponent, ± and fraction conversion.</summary>
public partial class CalculatorGadget : UserControl
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    string _entry = "0";
    double? _left;
    string? _op;
    bool _fresh;
    bool _showFrac;
    bool _error;

    public CalculatorGadget()
    {
        InitializeComponent();
        string[] keys =
        {
            "C", "⌫", "xʸ", "÷",
            "7", "8", "9", "×",
            "4", "5", "6", "−",
            "1", "2", "3", "+",
            "±", "0", ".", "="
        };
        foreach (var key in keys) Keys.Children.Add(MakeKey(key));

        FractionKey.Background = Gradient(0xFF9AE2D8, 0xFF1F8F86);
        FractionKey.Click += (_, _) => Fraction();
        Refresh();
    }

    static Brush Gradient(uint top, uint bottom)
    {
        Color C(uint value) => Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
        var brush = new LinearGradientBrush(C(top), C(bottom), 90);
        brush.Freeze();
        return brush;
    }

    static readonly Brush DigitBrush = Gradient(0xFFA9B3BF, 0xFF4B535D);
    static readonly Brush EqualsBrush = Gradient(0xFFFFE08A, 0xFFD38A12);
    static readonly Brush ClearBrush = Gradient(0xFFFF9A8A, 0xFFB01E12);

    Button MakeKey(string label)
    {
        var button = new Button { Content = label, Tag = label, Style = (Style)FindResource("CalcButton") };
        if (label is "+" or "−" or "×" or "÷" or "xʸ")
            button.SetResourceReference(BackgroundProperty, "ButtonGlass");
        else if (label == "=") button.Background = EqualsBrush;
        else if (label == "C") button.Background = ClearBrush;
        else button.Background = DigitBrush;
        button.Click += Key_Click;
        return button;
    }

    void Key_Click(object sender, RoutedEventArgs e)
    {
        string key = (string)((Button)sender).Tag;
        switch (key)
        {
            case "C": Clear(); break;
            case "⌫": Back(); break;
            case "±": Negate(); break;
            case ".": Dot(); break;
            case "=": Equal(); break;
            case "+": case "−": case "×": case "÷": Operator(key); break;
            case "xʸ": Operator("^"); break;
            default: Digit(key); break;
        }
    }

    double Cur => double.TryParse(_entry, NumberStyles.Float, Inv, out var value) ? value : 0;

    void StartNumberIfNeeded()
    {
        if (_error) Clear();
        if (_fresh || _showFrac) { _entry = "0"; _fresh = false; }
        _showFrac = false;
    }

    void Digit(string digit)
    {
        StartNumberIfNeeded();
        if (_entry == "0") _entry = digit;
        else if (_entry == "-0") _entry = "-" + digit;
        else if (_entry.Count(char.IsDigit) < 12) _entry += digit;
        Refresh();
    }

    void Dot()
    {
        StartNumberIfNeeded();
        if (!_entry.Contains('.')) _entry += ".";
        Refresh();
    }

    void Back()
    {
        if (_error) { Clear(); return; }
        if (_fresh) return;
        _showFrac = false;
        _entry = _entry.Length <= 1 || (_entry.Length == 2 && _entry[0] == '-') ? "0" : _entry[..^1];
        Refresh();
    }

    void Negate()
    {
        if (_error) return;
        _showFrac = false;
        if (_entry.StartsWith('-')) _entry = _entry[1..];
        else if (_entry != "0") _entry = "-" + _entry;
        Refresh();
    }

    void Clear()
    {
        _entry = "0"; _left = null; _op = null; _fresh = false; _showFrac = false; _error = false;
        Refresh();
    }

    void Operator(string op)
    {
        if (_error) return;
        if (_op != null && !_fresh) { if (!Evaluate()) return; }
        else _left = Cur;
        _op = op;
        _fresh = true;
        _showFrac = false;
        Refresh();
    }

    void Equal()
    {
        if (_op == null || _error) return;
        if (!Evaluate()) return;
        _op = null; _left = null; _fresh = true; _showFrac = false;
        Refresh();
    }

    bool Evaluate()
    {
        double a = _left ?? 0, b = Cur;
        double result = _op switch
        {
            "+" => a + b,
            "−" => a - b,
            "×" => a * b,
            "÷" => b == 0 ? double.NaN : a / b,
            "^" => Math.Pow(a, b),
            _ => b
        };
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            _error = true; _entry = "0"; _left = null; _op = null; _fresh = false; _showFrac = false;
            Refresh();
            return false;
        }
        _entry = Format(result);
        _left = result;
        return true;
    }

    static string Format(double value)
    {
        if (value == 0) return "0";
        if (value == Math.Round(value) && Math.Abs(value) < 1e12) return ((long)value).ToString(Inv);
        return value.ToString("G10", Inv);
    }

    void Fraction()
    {
        if (_error) return;
        if (_showFrac) { _showFrac = false; Refresh(); return; }

        var fraction = ToFraction(Cur);
        if (fraction == null) { ExprText.Text = "no simple fraction"; return; }
        if (fraction.Value.d == 1) { ExprText.Text = "already a whole number"; return; }
        _showFrac = true;
        Refresh();
    }

    static (long n, long d)? ToFraction(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 1e9) return null;
        bool negative = value < 0;
        double target = Math.Abs(value), remainder = target;
        long h1 = 1, h0 = 0, k1 = 0, k0 = 1;
        for (int i = 0; i < 24; i++)
        {
            long a = (long)Math.Floor(remainder);
            long h2 = a * h1 + h0, k2 = a * k1 + k0;
            h0 = h1; h1 = h2; k0 = k1; k1 = k2;
            if (Math.Abs(target - (double)h1 / k1) < 1e-9) break;
            double fraction = remainder - a;
            if (fraction < 1e-12) break;
            remainder = 1 / fraction;
            if (k1 > 100000) return null;
        }
        if (k1 > 100000 || Math.Abs(target - (double)h1 / k1) > 1e-7) return null;
        return (negative ? -h1 : h1, k1);
    }

    void Refresh()
    {
        if (_error) { DisplayText.Text = "Error"; ExprText.Text = ""; return; }

        if (_showFrac && ToFraction(Cur) is { } fraction)
        {
            DisplayText.Text = $"{fraction.n}/{fraction.d}";
            long whole = Math.Abs(fraction.n) / fraction.d, remainder = Math.Abs(fraction.n) % fraction.d;
            ExprText.Text = whole > 0 && remainder > 0 ? $"= {(fraction.n < 0 ? "-" : "")}{whole} {remainder}/{fraction.d}" : "";
            return;
        }

        DisplayText.Text = _entry;
        ExprText.Text = _op != null && _left != null ? $"{Format(_left.Value)} {_op}" : "";
    }
}
