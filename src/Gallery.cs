using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>An embeddable dark thumbnail grid of the wallpaper library, with a
/// small header (Refresh / Open folder). Raises <see cref="Selected"/> when a tile is clicked.</summary>
internal sealed class GalleryPanel : UserControl
{
    private readonly WallpaperLibrary _lib;
    private readonly FlowLayoutPanel _flow = new();

    public event Action<string>? Selected;

    public GalleryPanel(WallpaperLibrary lib)
    {
        _lib = lib;
        BackColor = Theme.Bg;

        var header = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Theme.Bg };
        var label = new Label { Text = "LIBRARY", AutoSize = true, Location = new Point(0, 9),
                                Font = new Font("Segoe UI Semibold", 8f), ForeColor = Theme.SubText };
        var open = new RoundedButton { Text = "Open folder", Size = new Size(108, 26) };
        var refresh = new RoundedButton { Text = "Refresh", Size = new Size(82, 26) };
        open.Click += (_, _) => _lib.OpenInExplorer();
        refresh.Click += (_, _) => Reload();
        header.Controls.AddRange(new Control[] { label, open, refresh });
        header.Resize += (_, _) =>
        {
            open.Top = refresh.Top = 4;
            open.Left = header.Width - open.Width;
            refresh.Left = open.Left - refresh.Width - 8;
        };

        _flow.Dock = DockStyle.Fill;
        _flow.AutoScroll = true;
        _flow.BackColor = Theme.Bg;
        _flow.Padding = new Padding(0, 4, 0, 4);

        Controls.Add(_flow);
        Controls.Add(header);
        Reload();
    }

    public void Reload()
    {
        _flow.SuspendLayout();
        foreach (Control c in _flow.Controls) c.Dispose();
        _flow.Controls.Clear();

        var items = _lib.Scan();
        foreach (var item in items)
        {
            var tile = new WallpaperTile(item);
            tile.Click += (_, _) => Selected?.Invoke(item.EntryPath);
            _flow.Controls.Add(tile);
        }
        if (items.Count == 0)
            _flow.Controls.Add(new Label
            {
                Text = "No wallpapers yet. Click \"Open folder\" to add some, then Refresh.",
                AutoSize = true, ForeColor = Theme.SubText, Margin = new Padding(6)
            });
        _flow.ResumeLayout();
    }
}

/// <summary>A single wallpaper thumbnail: preview image (or generated gradient) + name.</summary>
internal sealed class WallpaperTile : Panel
{
    private readonly string _title;
    private readonly Image? _img;
    private bool _hover;

    public WallpaperTile(WallpaperItem item)
    {
        _title = item.Name;
        _img = LoadUnlocked(item.PreviewPath);
        Size = new Size(232, 150);
        Margin = new Padding(8);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Theme.Bg);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Rounded(r, 10);
        g.SetClip(path);

        if (_img != null) DrawCover(g, _img, r);
        else DrawGradient(g, r, _title);

        using (var bar = new LinearGradientBrush(new Rectangle(0, Height - 44, Width, 44),
                   Color.FromArgb(0, 0, 0, 0), Color.FromArgb(205, 0, 0, 0), 90f))
            g.FillRectangle(bar, new Rectangle(0, Height - 44, Width, 44));
        using (var f = new Font("Segoe UI Semibold", 9.5f))
            TextRenderer.DrawText(g, _title, f, new Rectangle(10, Height - 28, Width - 20, 20),
                Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        g.ResetClip();
        using var border = new Pen(_hover ? Theme.Accent : Theme.Border, _hover ? 2f : 1f);
        g.DrawPath(border, path);
    }

    private static void DrawCover(Graphics g, Image img, Rectangle r)
    {
        float s = Math.Max((float)r.Width / img.Width, (float)r.Height / img.Height);
        int w = (int)(img.Width * s), h = (int)(img.Height * s);
        g.DrawImage(img, new Rectangle(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h));
    }

    private static void DrawGradient(Graphics g, Rectangle r, string name)
    {
        int hash = 0;
        foreach (char ch in name) hash = ch + ((hash << 5) - hash);
        int h1 = Math.Abs(hash) % 360, h2 = (h1 + 45) % 360;
        using var br = new LinearGradientBrush(r, FromHsl(h1, 0.5, 0.34), FromHsl(h2, 0.55, 0.20), 55f);
        g.FillRectangle(br, r);
    }

    private static Image? LoadUnlocked(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try
        {
            using var fs = File.OpenRead(path);
            using var tmp = Image.FromStream(fs);
            return new Bitmap(tmp); // independent copy; file stays unlocked
        }
        catch { return null; }
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        int d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    private static Color FromHsl(double h, double s, double l)
    {
        h /= 360.0;
        double r, gg, b;
        if (s == 0) { r = gg = b = l; }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            r = Hue(p, q, h + 1.0 / 3); gg = Hue(p, q, h); b = Hue(p, q, h - 1.0 / 3);
        }
        return Color.FromArgb((int)(r * 255), (int)(gg * 255), (int)(b * 255));
    }

    private static double Hue(double p, double q, double t)
    {
        if (t < 0) t += 1; if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
}
