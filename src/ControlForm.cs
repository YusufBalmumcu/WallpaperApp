using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>Minimal control panel: pick a source, apply it, or stop. Closing it
/// hides to the tray; real exit is via the tray menu.</summary>
internal sealed class ControlForm : Form
{
    private readonly WallpaperManager _mgr;
    private readonly TextBox _source = new();
    private readonly CheckBox _primaryOnly = new();
    private readonly Label _status = new();

    public bool AllowExit { get; set; }

    public ControlForm(WallpaperManager mgr)
    {
        _mgr = mgr;

        Text = "WallpaperApp";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(540, 210);

        var lbl = new Label { Text = "Wallpaper source (.html, video, image, or URL):", AutoSize = true, Location = new Point(14, 14) };

        _source.SetBounds(16, 38, 410, 24);
        _source.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var browse = new Button { Text = "Browse…", Location = new Point(434, 37), Size = new Size(90, 26) };
        browse.Click += (_, _) => Browse();

        _primaryOnly.Text = "Primary monitor only";
        _primaryOnly.AutoSize = true;
        _primaryOnly.Location = new Point(16, 74);
        _primaryOnly.Checked = _mgr.PrimaryOnly;
        _primaryOnly.CheckedChanged += (_, _) =>
        {
            _mgr.PrimaryOnly = _primaryOnly.Checked;
            if (_mgr.IsRunning && _mgr.CurrentSource is { } s) _mgr.SetWallpaper(s); // re-apply
        };

        var apply = new Button { Text = "Apply", Location = new Point(16, 108), Size = new Size(110, 32) };
        apply.Click += (_, _) => Apply();

        var sample = new Button { Text = "Use sample", Location = new Point(134, 108), Size = new Size(110, 32) };
        sample.Click += (_, _) => { _source.Text = Program.DefaultSample(); Apply(); };

        var stop = new Button { Text = "Stop", Location = new Point(252, 108), Size = new Size(110, 32) };
        stop.Click += (_, _) => { _mgr.Stop(); RefreshStatus(); };

        _status.AutoSize = false;
        _status.SetBounds(16, 156, 508, 40);
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange(new Control[] { lbl, _source, browse, _primaryOnly, apply, sample, stop, _status });
        RefreshStatus();
    }

    private void Browse()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Choose a wallpaper",
            Filter = "Wallpapers|*.html;*.htm;*.mp4;*.webm;*.ogg;*.mov;*.m4v;*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp|" +
                     "Web pages|*.html;*.htm|Video|*.mp4;*.webm;*.ogg;*.mov;*.m4v|Images|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp|All files|*.*"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) _source.Text = dlg.FileName;
    }

    private void Apply()
    {
        string src = _source.Text.Trim();
        if (string.IsNullOrEmpty(src)) { _status.Text = "Enter a file path or URL first."; return; }
        try
        {
            _mgr.SetWallpaper(src);
            RefreshStatus();
        }
        catch (Exception ex)
        {
            _status.Text = "Failed: " + ex.Message;
        }
    }

    private void RefreshStatus()
        => _status.Text = _mgr.IsRunning ? "Running: " + _mgr.CurrentSource : "Stopped.";

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // X just hides to tray; the wallpaper keeps running.
        if (!AllowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }
}
