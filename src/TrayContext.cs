using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>Runs the app in the tray. Owns the manager, settings and control window.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly WallpaperManager _mgr = new();
    private readonly WallpaperLibrary _library = new();
    private readonly ControlForm _form;
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _pauseItem;

    public TrayContext(AppSettings settings, string? startupSource, bool silent)
    {
        _settings = settings;
        _mgr.PrimaryOnly = settings.PrimaryOnly;
        _library.EnsureSeeded();
        _form = new ControlForm(_mgr, settings, _library);

        _pauseItem = new ToolStripMenuItem("Pause", null, (_, _) => TogglePause());

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show controls", null, (_, _) => ShowForm());
        menu.Items.Add("Use sample wallpaper", null, (_, _) => ApplyAndRemember(Program.DefaultSample()));
        menu.Items.Add(_pauseItem);
        menu.Items.Add("Stop wallpaper", null, (_, _) => { _mgr.Stop(); _form.Sync(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
        menu.Opening += (_, _) =>
        {
            _pauseItem.Text = _mgr.IsPaused ? "Resume" : "Pause";
            _pauseItem.Enabled = _mgr.IsRunning;
        };

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "WallpaperApp",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowForm();

        // Decide what to show on launch.
        string? toApply = !string.IsNullOrWhiteSpace(startupSource) ? startupSource
            : (settings.ReapplyOnLaunch && IsApplicable(settings.LastSource) ? settings.LastSource : null);

        if (toApply != null) ApplyAndRemember(toApply);
        if (!silent) ShowForm();
    }

    private void ApplyAndRemember(string source)
    {
        try
        {
            _mgr.SetWallpaper(source);
            _settings.LastSource = source;
            _settings.Save();
            _form.Sync();
        }
        catch { /* surfaced in the control window on manual applies */ }
    }

    private void TogglePause()
    {
        if (_mgr.IsPaused) _mgr.Resume(); else _mgr.Pause();
        _form.Sync();
    }

    private static bool IsApplicable(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return false;
        if (Uri.TryCreate(source, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https")) return true;
        return File.Exists(source);
    }

    private void ShowForm()
    {
        _form.Show();
        _form.WindowState = FormWindowState.Normal;
        _form.Sync();
        _form.BringToFront();
        _form.Activate();
    }

    private void ExitApp()
    {
        _settings.Save();
        _mgr.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        _form.AllowExit = true;
        _form.Close();
        ExitThread();
    }
}
