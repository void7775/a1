using System.Globalization;

namespace Calc;

// Basic calculator state machine: left op right, evaluated on next operator or '='.
public sealed class Engine
{
    decimal? _acc;
    char _op;
    bool _fresh = true;
    public string Display { get; private set; } = "0";

    public void Digit(char d)
    {
        if (Display == "Error") Clear();
        if (_fresh) { Display = d == '.' ? "0." : d.ToString(); _fresh = false; return; }
        if (d == '.' && Display.Contains('.')) return;
        if (Display.Length >= 18) return;
        Display = Display == "0" && d != '.' ? d.ToString() : Display + d;
    }

    public void Op(char op)
    {
        if (Display == "Error") return;
        if (!_fresh || _acc is null) Apply();
        _op = op;
        _fresh = true;
    }

    public void Equals() { if (Display == "Error") return; Apply(); _op = '\0'; _fresh = true; }

    public void Clear() { _acc = null; _op = '\0'; _fresh = true; Display = "0"; }

    public void Back()
    {
        if (_fresh || Display == "Error") return;
        Display = Display.Length > 1 && Display != "-0" ? Display[..^1] : "0";
        if (Display == "-") Display = "0";
    }

    public void Negate()
    {
        if (Display == "Error" || Display == "0") return;
        Display = Display.StartsWith('-') ? Display[1..] : "-" + Display;
    }

    public void Percent()
    {
        if (Display == "Error") return;
        var v = Cur / 100m;
        if (_acc is not null && _op is '+' or '-') v = _acc.Value * v;
        Show(v);
        _fresh = true;
    }

    decimal Cur => decimal.Parse(Display, CultureInfo.InvariantCulture);

    void Apply()
    {
        var v = Cur;
        try
        {
            if (_acc is null || _op == '\0') { _acc = v; Show(v); return; }
            _acc = _op switch
            {
                '+' => _acc + v,
                '-' => _acc - v,
                '*' => _acc * v,
                '/' => v == 0 ? throw new DivideByZeroException() : _acc / v,
                _ => v
            };
            Show(_acc.Value);
        }
        catch (Exception e) when (e is DivideByZeroException or OverflowException)
        {
            _acc = null; _op = '\0'; Display = "Error";
        }
    }

    void Show(decimal v) => Display = Math.Round(v, 12).ToString("0.############", CultureInfo.InvariantCulture);
}
