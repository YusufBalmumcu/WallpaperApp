using System.Diagnostics;

namespace WallpaperApp;

/// <summary>One wallpaper in the library: a name, the entry to load, and an optional preview image.</summary>
internal sealed record WallpaperItem(string Name, string EntryPath, string? PreviewPath);

/// <summary>
/// The on-disk wallpaper library. Lives in Documents\WallpaperApp\Wallpapers so
/// it is easy to find and add to. On first run it is seeded with the bundled
/// example wallpapers shipped next to the exe.
/// </summary>
internal sealed class WallpaperLibrary
{
    public string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WallpaperApp", "Wallpapers");

    private static readonly string[] WebEntries = { "index.html", "index.htm" };
    private static readonly string[] MediaExt = { ".mp4", ".webm", ".ogg", ".mov", ".m4v" };
    private static readonly string[] ImageExt = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
    private static readonly string[] PreviewNames = { "preview.png", "preview.jpg", "preview.jpeg", "thumbnail.png", "thumbnail.jpg" };

    /// <summary>Create the folder and, if empty, copy in the bundled examples.</summary>
    public void EnsureSeeded()
    {
        try
        {
            Directory.CreateDirectory(Root);
            if (Directory.EnumerateFileSystemEntries(Root).Any()) return;
            string bundled = Path.Combine(AppContext.BaseDirectory, "wallpapers");
            if (Directory.Exists(bundled)) CopyDir(bundled, Root);
        }
        catch { /* best effort */ }
    }

    /// <summary>Find every wallpaper under the library root.</summary>
    public List<WallpaperItem> Scan()
    {
        var items = new List<WallpaperItem>();
        if (!Directory.Exists(Root)) return items;

        foreach (var dir in Directory.GetDirectories(Root))
        {
            string? entry = WebEntries.Select(w => Path.Combine(dir, w)).FirstOrDefault(File.Exists)
                            ?? FirstWithExt(dir, MediaExt) ?? FirstWithExt(dir, ImageExt);
            if (entry == null) continue;

            string? preview = PreviewNames.Select(p => Path.Combine(dir, p)).FirstOrDefault(File.Exists);
            if (preview == null && ImageExt.Contains(Ext(entry))) preview = entry;
            items.Add(new WallpaperItem(Path.GetFileName(dir), entry, preview));
        }

        foreach (var f in Directory.GetFiles(Root))
        {
            string ext = Ext(f);
            if (ext is ".html" or ".htm" || MediaExt.Contains(ext) || ImageExt.Contains(ext))
                items.Add(new WallpaperItem(Path.GetFileNameWithoutExtension(f), f,
                    ImageExt.Contains(ext) ? f : null));
        }

        return items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public void OpenInExplorer()
    {
        Directory.CreateDirectory(Root);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{Root}\"") { UseShellExecute = true });
    }

    private static string Ext(string path) => Path.GetExtension(path).ToLowerInvariant();

    private static string? FirstWithExt(string dir, string[] exts) =>
        Directory.GetFiles(dir).FirstOrDefault(f => exts.Contains(Ext(f)));

    private static void CopyDir(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.GetFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
        foreach (var d in Directory.GetDirectories(src))
            CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
    }
}
