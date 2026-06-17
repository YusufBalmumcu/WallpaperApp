using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>Runs the app in the tray. Owns the manager and the control window.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly WallpaperManager _mgr = new();
    private readonly ControlForm _form;
    private readonly NotifyIcon _tray;

    public TrayContext(string? startupSource, bool primaryOnly)
    {
        _mgr.PrimaryOnly = primaryOnly;
        _form = new ControlForm(_mgr);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show controls", null, (_, _) => ShowForm());
        menu.Items.Add("Use sample wallpaper", null, (_, _) => _mgr.SetWallpaper(Program.DefaultSample()));
        menu.Items.Add("Stop wallpaper", null, (_, _) => _mgr.Stop());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "WallpaperApp",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowForm();

        if (!string.IsNullOrWhiteSpace(startupSource))
            _mgr.SetWallpaper(startupSource!);
        else
            ShowForm();
    }

    private void ShowForm()
    {
        _form.Show();
        _form.WindowState = FormWindowState.Normal;
        _form.BringToFront();
        _form.Activate();
    }

    private void ExitApp()
    {
        _mgr.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        _form.AllowExit = true;
        _form.Close();
        ExitThread();
    }
}
