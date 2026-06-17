using System.Text.Json;

namespace WallpaperApp;

/// <summary>Persisted user settings, stored as JSON in %APPDATA%\WallpaperApp.</summary>
internal sealed class AppSettings
{
    public string? LastSource { get; set; }
    public bool PrimaryOnly { get; set; }
    public bool ReapplyOnLaunch { get; set; } = true;

    private static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WallpaperApp");
    private static string FilePath => Path.Combine(Dir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch { /* corrupt/missing -> defaults */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best effort */ }
    }
}
