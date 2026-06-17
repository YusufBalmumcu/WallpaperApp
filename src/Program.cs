using System.Windows.Forms;

namespace WallpaperApp;

internal static class Program
{
    /// <summary>
    /// Usage: WallpaperApp.exe [source] [--primary]
    ///   source    path to .html / video / image, or an http(s) URL. If given, it
    ///             is applied on startup; otherwise the control window opens.
    ///   --primary only set the wallpaper on the primary monitor.
    /// The app lives in the system tray; double-click the tray icon for controls.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool primaryOnly = args.Contains("--primary");
        string? source = args.FirstOrDefault(a => !a.StartsWith("--"));

        Application.Run(new TrayContext(source, primaryOnly));
    }

    internal static string DefaultSample()
        => Path.Combine(AppContext.BaseDirectory, "wallpapers", "sample", "index.html");
}
