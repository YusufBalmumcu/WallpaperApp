using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>The application icon, loaded once (from app.ico next to the exe, with fallbacks).</summary>
internal static class AppArt
{
    private static Icon? _icon;

    public static Icon Icon => _icon ??= Load();

    private static Icon Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "app.ico");
        try { if (File.Exists(path)) return new Icon(path); } catch { /* fall through */ }
        try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application; }
        catch { return SystemIcons.Application; }
    }
}
