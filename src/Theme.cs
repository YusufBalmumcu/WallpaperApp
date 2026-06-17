using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WallpaperApp;

/// <summary>Dark theme palette and helpers.</summary>
internal static class Theme
{
    public static readonly Color Bg       = Color.FromArgb(18, 19, 24);
    public static readonly Color Panel    = Color.FromArgb(31, 33, 42);
    public static readonly Color PanelHi  = Color.FromArgb(44, 47, 60);
    public static readonly Color Text     = Color.FromArgb(233, 235, 240);
    public static readonly Color SubText  = Color.FromArgb(146, 152, 166);
    public static readonly Color Accent   = Color.FromArgb(91, 124, 250);
    public static readonly Color AccentHi = Color.FromArgb(118, 146, 252);
    public static readonly Color Border   = Color.FromArgb(58, 62, 76);
    public static readonly Color Danger   = Color.FromArgb(224, 90, 92);
    public static readonly Color DangerHi = Color.FromArgb(80, 42, 46);

    /// <summary>Turn the window's title bar dark (no-op on older Windows).</summary>
    public static void DarkTitleBar(IntPtr hwnd)
    {
        int on = 1;
        NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
    }
}

/// <summary>A flat, rounded, hover-aware button for the dark UI.</summary>
internal sealed class RoundedButton : Button
{
    public Color BaseColor { get; set; } = Theme.Panel;
    public Color HoverColor { get; set; } = Theme.PanelHi;
    public int Radius { get; set; } = 8;
    private bool _hover;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI Semibold", 9.5f);
        Cursor = Cursors.Hand;
        Height = 36;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Bg);
        using var path = RoundRect(ClientRectangle, Radius);
        using var brush = new SolidBrush(Enabled ? (_hover ? HoverColor : BaseColor) : Theme.Panel);
        g.FillPath(brush, path);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, Enabled ? ForeColor : Theme.SubText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static GraphicsPath RoundRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        r.Width -= 1; r.Height -= 1;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
