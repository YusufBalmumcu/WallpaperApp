using System.Windows.Forms;

namespace WallpaperApp;

internal static class Program
{
    /// <summary>
    /// Usage: WallpaperApp.exe [source] [--primary] [--silent]
    ///   source    path to .html / video / image, or an http(s) URL. If given, it
    ///             is applied on startup (overrides the remembered wallpaper).
    ///   --primary force primary-monitor-only for this run.
    ///   --silent  start in the tray without opening the control window
    ///             (used by the "run at startup" entry).
    /// The app lives in the system tray; double-click the tray icon for controls.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var settings = AppSettings.Load();
        if (args.Contains("--primary")) settings.PrimaryOnly = true;
        bool silent = args.Contains("--silent");
        string? source = args.FirstOrDefault(a => !a.StartsWith("--"));

        Application.Run(new TrayContext(settings, source, silent));
    }

    internal static string DefaultSample()
        => Path.Combine(AppContext.BaseDirectory, "wallpapers", "sample", "index.html");
}
