using System.Text.Json;
using System.Windows.Forms;

namespace A1;

static class Program
{
    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "A1");
    static readonly string LogFile = Path.Combine(Dir, "app.log");
    static readonly string SettingsFile = Path.Combine(Dir, "settings.json");

    static void Log(string m)
    {
        try { File.AppendAllText(LogFile, $"{DateTime.Now:s} {m}{Environment.NewLine}"); } catch { }
    }

    [STAThread]
    static void Main()
    {
        Directory.CreateDirectory(Dir);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("UNHANDLED: " + e.ExceptionObject);
        Application.ThreadException += (_, e) => Log("UI EXCEPTION: " + e.Exception);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        using var mutex = new Mutex(true, "A1.SingleInstance", out bool first);
        if (!first) return;

        Application.EnableVisualStyles();
        var settings = Load();
        var tray = new NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            Text = "A1 v" + typeof(Program).Assembly.GetName().Version,
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        tray.ContextMenuStrip.Items.Add("Exit", null, (_, _) => Application.Exit());
        Log("started v" + typeof(Program).Assembly.GetName().Version);
        Application.Run();
        tray.Visible = false;
        Save(settings);
        Log("exited");
    }

    static Dictionary<string, string> Load()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsFile)) ?? new(); }
        catch { return new(); }
    }

    static void Save(Dictionary<string, string> s)
    {
        try { File.WriteAllText(SettingsFile, JsonSerializer.Serialize(s)); } catch (Exception e) { Log("save failed: " + e.Message); }
    }
}
