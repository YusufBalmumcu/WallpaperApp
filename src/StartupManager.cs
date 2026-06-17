using Microsoft.Win32;
using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>Toggles "run when Windows starts" via the per-user Run registry key.</summary>
internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WallpaperApp";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) != null;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null) return;

        if (enabled)
            key.SetValue(ValueName, $"\"{Application.ExecutablePath}\" --silent");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
