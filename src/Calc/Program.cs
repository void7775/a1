using System.Text.Json;

namespace Calc;

static class Program
{
    internal static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Calculator");
    internal static void Log(string m)
    {
        try { File.AppendAllText(Path.Combine(Dir, "calc.log"), $"{DateTime.Now:s} {m}{Environment.NewLine}"); } catch { }
    }

    [STAThread]
    static void Main()
    {
        Directory.CreateDirectory(Dir);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("UNHANDLED: " + e.ExceptionObject);
        Application.ThreadException += (_, e) => Log("UI EXCEPTION: " + e.Exception);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        using var mutex = new Mutex(true, "VoidCalc.SingleInstance", out bool first);
        if (!first) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

sealed record Settings(int X, int Y);

sealed class MainForm : Form
{
    static readonly Color Bg = Color.FromArgb(32, 32, 32), Key = Color.FromArgb(59, 59, 59),
        OpKey = Color.FromArgb(255, 150, 0), Fg = Color.White;
    readonly string _settingsFile = Path.Combine(Program.Dir, "settings.json");
    readonly Engine _e = new();
    readonly Label _screen;

    public MainForm()
    {
        Text = "Calculator";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Bg;
        ClientSize = new Size(4 * 70 + 10, 6 * 60 + 20);
        KeyPreview = true;
        StartPosition = FormStartPosition.Manual;
        Location = LoadPos();

        _screen = new Label
        {
            Bounds = new Rectangle(5, 5, ClientSize.Width - 10, 70),
            ForeColor = Fg, Font = new Font("Segoe UI", 28), TextAlign = ContentAlignment.MiddleRight, Text = "0"
        };
        Controls.Add(_screen);

        string[] keys = ["C", "⌫", "%", "/", "7", "8", "9", "*", "4", "5", "6", "-", "1", "2", "3", "+", "±", "0", ".", "="];
        for (int i = 0; i < keys.Length; i++)
        {
            var k = keys[i];
            var b = new Button
            {
                Text = k == "*" ? "×" : k == "/" ? "÷" : k,
                Bounds = new Rectangle(5 + i % 4 * 70, 80 + i / 4 * 60, 66, 56),
                FlatStyle = FlatStyle.Flat, ForeColor = Fg, Font = new Font("Segoe UI", 16),
                BackColor = "/*-+=".Contains(k) ? OpKey : Key, TabStop = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => Press(k);
            Controls.Add(b);
        }
        FormClosing += (_, _) => SavePos();
    }

    void Press(string k)
    {
        switch (k)
        {
            case "C": _e.Clear(); break;
            case "⌫": _e.Back(); break;
            case "%": _e.Percent(); break;
            case "±": _e.Negate(); break;
            case "=": _e.Equals(); break;
            case "+" or "-" or "*" or "/": _e.Op(k[0]); break;
            default: _e.Digit(k[0]); break;
        }
        _screen.Text = _e.Display;
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        var c = e.KeyChar;
        string? k = c switch
        {
            >= '0' and <= '9' or '.' or '+' or '-' or '*' or '/' or '%' => c.ToString(),
            ',' => ".",
            '=' or '\r' => "=",
            '\b' => "⌫",
            (char)27 => "C",
            _ => null
        };
        if (k != null) { Press(k); e.Handled = true; }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Dark title bar (Windows 10 2004+/11); ignored elsewhere.
        int on = 1;
        try { DwmSetWindowAttribute(Handle, 20, ref on, 4); } catch { }
    }

    Point LoadPos()
    {
        try
        {
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_settingsFile));
            if (s != null && Screen.AllScreens.Any(sc => sc.WorkingArea.Contains(s.X + 20, s.Y + 20))) return new Point(s.X, s.Y);
        }
        catch (FileNotFoundException) { }
        catch (Exception ex) { Program.Log("settings load: " + ex.Message); }
        var wa = Screen.PrimaryScreen!.WorkingArea;
        return new Point(wa.X + (wa.Width - Width) / 2, wa.Y + (wa.Height - Height) / 2);
    }

    void SavePos()
    {
        try { File.WriteAllText(_settingsFile, JsonSerializer.Serialize(new Settings(Location.X, Location.Y))); }
        catch (Exception ex) { Program.Log("settings save: " + ex.Message); }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);
}
