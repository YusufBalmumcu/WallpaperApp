using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>Owns the per-monitor wallpaper windows and swaps them at runtime.</summary>
internal sealed class WallpaperManager
{
    private readonly List<WallpaperWindow> _windows = new();

    public string? CurrentSource { get; private set; }
    public bool PrimaryOnly { get; set; }
    public bool IsRunning => _windows.Count > 0;

    /// <summary>Apply a wallpaper source (html / video / image / url) to the monitors.</summary>
    public void SetWallpaper(string source)
    {
        Stop();
        var screens = PrimaryOnly
            ? new[] { Screen.PrimaryScreen! }
            : Screen.AllScreens;

        foreach (var screen in screens)
        {
            var win = new WallpaperWindow(screen, source);
            _windows.Add(win);
            win.Show();
        }
        CurrentSource = source;
    }

    /// <summary>Tear down all wallpaper windows and restore the normal desktop.</summary>
    public void Stop()
    {
        foreach (var win in _windows)
        {
            try { win.Close(); win.Dispose(); }
            catch { /* window may already be gone */ }
        }
        _windows.Clear();
        CurrentSource = null;
    }
}
