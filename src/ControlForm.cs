using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>Sleek dark control panel: pick/apply a wallpaper, manage recents and
/// options. Closing it hides to the tray; real exit is via the tray menu.</summary>
internal sealed class ControlForm : Form
{
    private readonly WallpaperManager _mgr;
    private readonly AppSettings _settings;

    private readonly TextBox _source = new();
    private readonly RoundedButton _pause = new();
    private readonly ListBox _recent = new();
    private readonly Label _status = new();
    private readonly CheckBox _primaryOnly;
    private readonly CheckBox _runAtStartup;
    private readonly CheckBox _reapply;

    public bool AllowExit { get; set; }

    public ControlForm(WallpaperManager mgr, AppSettings settings)
    {
        _mgr = mgr;
        _settings = settings;

        Text = "WallpaperApp";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(580, 492);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.5f);

        const int pad = 18;
        int contentW = ClientSize.Width - pad * 2;

        var title = new Label
        {
            Text = "WallpaperApp", AutoSize = true, Location = new Point(pad, 14),
            Font = new Font("Segoe UI Semibold", 15.75f), ForeColor = Theme.Text
        };
        var subtitle = new Label
        {
            Text = "Live HTML & video wallpaper", AutoSize = true, Location = new Point(pad + 2, 46),
            Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.SubText, UseMnemonic = false
        };
        var accentLine = new Panel { Location = new Point(pad, 72), Size = new Size(contentW, 2), BackColor = Theme.Accent };

        var srcLabel = new Label { Text = "SOURCE", AutoSize = true, Location = new Point(pad, 86),
                                   Font = new Font("Segoe UI Semibold", 8f), ForeColor = Theme.SubText };

        _source.SetBounds(pad, 108, contentW - 108, 28);
        _source.BackColor = Theme.Panel;
        _source.ForeColor = Theme.Text;
        _source.BorderStyle = BorderStyle.FixedSingle;
        _source.AllowDrop = true;
        _source.DragEnter += (_, e) => e!.Effect = e.Data!.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        _source.DragDrop += (_, e) =>
        {
            var files = (string[]?)e!.Data!.GetData(DataFormats.FileDrop);
            if (files is { Length: > 0 }) { _source.Text = files[0]; Apply(); }
        };

        var browse = new RoundedButton { Text = "Browse", Location = new Point(pad + contentW - 96, 107), Size = new Size(96, 30) };
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

        var recentLabel = new Label { Text = "RECENT", AutoSize = true, Location = new Point(pad, 300),
                                      Font = new Font("Segoe UI Semibold", 8f), ForeColor = Theme.SubText };

        _recent.SetBounds(pad, 322, contentW, 132);
        _recent.BorderStyle = BorderStyle.None;
        _recent.BackColor = Theme.Panel;
        _recent.ForeColor = Theme.Text;
        _recent.IntegralHeight = false;
        _recent.DrawMode = DrawMode.OwnerDrawFixed;
        _recent.ItemHeight = 26;
        _recent.DrawItem += DrawRecentItem;
        _recent.SelectedIndexChanged += (_, _) => { if (_recent.SelectedItem is string s) _source.Text = s; };
        _recent.DoubleClick += (_, _) => { if (_recent.SelectedItem is string) Apply(); };

        _status.SetBounds(pad, 462, contentW, 22);
        _status.ForeColor = Theme.SubText;

        Controls.AddRange(new Control[]
        {
            title, subtitle, accentLine, srcLabel, _source, browse,
            apply, sample, _pause, stop,
            _primaryOnly, _runAtStartup, _reapply,
            recentLabel, _recent, _status
        });

        if (!string.IsNullOrWhiteSpace(_settings.LastSource)) _source.Text = _settings.LastSource;
        LoadRecents();
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

    private void DrawRecentItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        string val = _recent.Items[e.Index].ToString() ?? "";
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using (var bg = new SolidBrush(selected ? Theme.PanelHi : Theme.Panel))
            e.Graphics.FillRectangle(bg, e.Bounds);
        if (selected)
            using (var bar = new SolidBrush(Theme.Accent))
                e.Graphics.FillRectangle(bar, new Rectangle(e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height));

        string label = IsUrl(val) ? val : Path.GetFileName(val);
        var rect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, label, _recent.Font, rect, Theme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
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
            _settings.LastSource = src;
            _settings.AddRecent(src);
            _settings.Save();
            LoadRecents();
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

    /// <summary>Re-sync the UI with the current state (after tray-driven changes).</summary>
    public void Sync()
    {
        LoadRecents();
        if (!string.IsNullOrWhiteSpace(_mgr.CurrentSource)) _source.Text = _mgr.CurrentSource;
        RefreshState();
    }

    private void LoadRecents()
    {
        _recent.BeginUpdate();
        _recent.Items.Clear();
        foreach (var r in _settings.Recents) _recent.Items.Add(r);
        _recent.EndUpdate();
    }

    private void RefreshState()
    {
        _pause.Text = _mgr.IsPaused ? "Resume" : "Pause";
        _pause.Enabled = _mgr.IsRunning;
        _status.Text = !_mgr.IsRunning ? "Stopped."
            : _mgr.IsPaused ? "Paused — " + Short(_mgr.CurrentSource)
            : "Running — " + Short(_mgr.CurrentSource);
    }

    private static string Short(string? s) =>
        string.IsNullOrEmpty(s) ? "" : IsUrl(s) ? s : Path.GetFileName(s);

    private static bool IsUrl(string s) =>
        Uri.TryCreate(s, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https");

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
