using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>Sleek dark control panel: pick/apply a wallpaper, set options, and pick
/// from the embedded gallery. Closing it hides to the tray; real exit is via the tray menu.</summary>
internal sealed class ControlForm : Form
{
    private readonly WallpaperManager _mgr;
    private readonly AppSettings _settings;

    private readonly TextBox _source = new();
    private readonly RoundedButton _pause = new();
    private readonly GalleryPanel _gallery;
    private readonly Label _status = new();
    private readonly CheckBox _primaryOnly;
    private readonly CheckBox _runAtStartup;
    private readonly CheckBox _reapply;

    public bool AllowExit { get; set; }

    public ControlForm(WallpaperManager mgr, AppSettings settings, WallpaperLibrary library)
    {
        _mgr = mgr;
        _settings = settings;

        Text = "WallpaperApp";
        Icon = AppArt.Icon;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimumSize = new Size(540, 560);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(600, 720);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.5f);

        const int pad = 18;
        int contentW = ClientSize.Width - pad * 2;

        var title = new Label { Text = "WallpaperApp", AutoSize = true, Location = new Point(pad, 14),
                                Font = new Font("Segoe UI Semibold", 15.75f), ForeColor = Theme.Text };
        var subtitle = new Label { Text = "Live HTML & video wallpaper", AutoSize = true, Location = new Point(pad + 2, 46),
                                   Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.SubText, UseMnemonic = false };
        var accentLine = new Panel { Location = new Point(pad, 72), Size = new Size(contentW, 2), BackColor = Theme.Accent,
                                     Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        var srcLabel = new Label { Text = "SOURCE", AutoSize = true, Location = new Point(pad, 86),
                                   Font = new Font("Segoe UI Semibold", 8f), ForeColor = Theme.SubText };

        _source.SetBounds(pad, 108, contentW - 108, 28);
        _source.BackColor = Theme.Panel;
        _source.ForeColor = Theme.Text;
        _source.BorderStyle = BorderStyle.FixedSingle;
        _source.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _source.AllowDrop = true;
        _source.DragEnter += (_, e) => e!.Effect = e.Data!.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        _source.DragDrop += (_, e) =>
        {
            var files = (string[]?)e!.Data!.GetData(DataFormats.FileDrop);
            if (files is { Length: > 0 }) { _source.Text = files[0]; Apply(); }
        };

        var browse = new RoundedButton { Text = "Browse", Size = new Size(96, 30),
                                         Location = new Point(pad + contentW - 96, 107),
                                         Anchor = AnchorStyles.Top | AnchorStyles.Right };
        browse.Click += (_, _) => Browse();

        // action row
        int by = 154, bw = (contentW - 3 * 8) / 4, bh = 38;
        var apply = new RoundedButton { Text = "Apply", BaseColor = Theme.Accent, HoverColor = Theme.AccentHi, ForeColor = Color.White,
                                        Location = new Point(pad, by), Size = new Size(bw, bh) };
        apply.Click += (_, _) => Apply();
        var sample = new RoundedButton { Text = "Use sample", Location = new Point(pad + bw + 8, by), Size = new Size(bw, bh) };
        sample.Click += (_, _) => { _source.Text = Program.DefaultSample(); Apply(); };
        _pause.Text = "Pause"; _pause.Location = new Point(pad + 2 * (bw + 8), by); _pause.Size = new Size(bw, bh);
        _pause.Click += (_, _) => TogglePause();
        var stop = new RoundedButton { Text = "Stop", BaseColor = Theme.Panel, HoverColor = Theme.DangerHi, ForeColor = Theme.Danger,
                                       Location = new Point(pad + 3 * (bw + 8), by), Size = new Size(bw, bh) };
        stop.Click += (_, _) => { _mgr.Stop(); RefreshState(); };

        // options
        _primaryOnly = MakeCheck("Primary monitor only", 208, _mgr.PrimaryOnly);
        _primaryOnly.CheckedChanged += (_, _) =>
        {
            _mgr.PrimaryOnly = _settings.PrimaryOnly = _primaryOnly.Checked;
            _settings.Save();
            if (_mgr.IsRunning && _mgr.CurrentSource is { } s) _mgr.SetWallpaper(s);
            RefreshState();
        };
        _runAtStartup = MakeCheck("Run when Windows starts", 236, StartupManager.IsEnabled());
        _runAtStartup.CheckedChanged += (_, _) =>
        {
            try { StartupManager.SetEnabled(_runAtStartup.Checked); }
            catch (Exception ex) { _status.Text = "Startup toggle failed: " + ex.Message; }
        };
        _reapply = MakeCheck("Re-apply last wallpaper on launch", 264, _settings.ReapplyOnLaunch);
        _reapply.CheckedChanged += (_, _) => { _settings.ReapplyOnLaunch = _reapply.Checked; _settings.Save(); };

        // embedded gallery fills the rest
        _gallery = new GalleryPanel(library)
        {
            Location = new Point(pad, 300),
            Size = new Size(contentW, ClientSize.Height - 300 - 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _gallery.Selected += src => { _source.Text = src; Apply(); };

        _status.SetBounds(pad, ClientSize.Height - 26, contentW, 22);
        _status.ForeColor = Theme.SubText;
        _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange(new Control[]
        {
            title, subtitle, accentLine, srcLabel, _source, browse,
            apply, sample, _pause, stop,
            _primaryOnly, _runAtStartup, _reapply,
            _gallery, _status
        });

        if (!string.IsNullOrWhiteSpace(_settings.LastSource)) _source.Text = _settings.LastSource;
        RefreshState();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.DarkTitleBar(Handle);
    }

    private static CheckBox MakeCheck(string text, int y, bool initial) => new()
    {
        Text = text, AutoSize = true, Location = new Point(18, y), Checked = initial,
        ForeColor = Theme.Text, BackColor = Color.Transparent,
        Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Standard
    };

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
            _settings.LastSource = src;
            _settings.Save();
            RefreshState();
        }
        catch (Exception ex)
        {
            _status.Text = "Failed: " + ex.Message;
        }
    }

    private void TogglePause()
    {
        if (!_mgr.IsRunning) return;
        if (_mgr.IsPaused) _mgr.Resume(); else _mgr.Pause();
        RefreshState();
    }

    private void RefreshState()
    {
        _pause.Text = _mgr.IsPaused ? "Resume" : "Pause";
        _pause.Enabled = _mgr.IsRunning;
        _status.Text = !_mgr.IsRunning ? "Stopped."
            : _mgr.IsPaused ? "Paused — " + Short(_mgr.CurrentSource)
            : "Running — " + Short(_mgr.CurrentSource);
    }

    /// <summary>Re-sync the UI with the current state (after tray-driven changes).</summary>
    public void Sync()
    {
        if (!string.IsNullOrWhiteSpace(_mgr.CurrentSource)) _source.Text = _mgr.CurrentSource;
        _gallery.Reload();
        RefreshState();
    }

    private static string Short(string? s) =>
        string.IsNullOrEmpty(s) ? "" : IsUrl(s) ? s : Path.GetFileName(s);

    private static bool IsUrl(string s) =>
        Uri.TryCreate(s, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https");

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!AllowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }
}
