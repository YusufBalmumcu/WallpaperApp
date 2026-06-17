using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WallpaperApp;

/// <summary>
/// A borderless window that hosts a WebView2 browser and is parented into the
/// desktop layer so its content renders as the wallpaper for one monitor.
/// WebView2 renders HTML/CSS/JS animations and, via the &lt;video&gt; tag, video.
/// </summary>
internal sealed class WallpaperWindow : Form
{
    private readonly WebView2 _web = new();
    private readonly string _source;
    private readonly Rectangle _virtualBounds; // this monitor's bounds in virtual-screen coords

    public WallpaperWindow(Screen screen, string source)
    {
        _source = source;
        _virtualBounds = screen.Bounds;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Bounds = screen.Bounds;
        BackColor = Color.Black;

        _web.Dock = DockStyle.Fill;
        _web.DefaultBackgroundColor = Color.Black;
        Controls.Add(_web);
    }

    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    protected override CreateParams CreateParams
    {
        get
        {
            // Never take focus / never appear in Alt-Tab: it's a wallpaper.
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        AttachToDesktop();
        await InitWebViewAsync();
    }

    /// <summary>Reparent this window into the desktop host and size it to its monitor.</summary>
    private void AttachToDesktop()
    {
        IntPtr host = DesktopWorker.GetWallpaperHost();
        NativeMethods.SetParent(Handle, host);

        // Position relative to the host window's own origin so multi-monitor /
        // negative-coordinate layouts line up exactly with this screen.
        NativeMethods.GetWindowRect(host, out var hr);
        int x = _virtualBounds.X - hr.left;
        int y = _virtualBounds.Y - hr.top;
        NativeMethods.SetWindowPos(Handle, IntPtr.Zero, x, y, _virtualBounds.Width, _virtualBounds.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOZORDER);
    }

    private async Task InitWebViewAsync()
    {
        // Keep WebView2's user-data out of Program Files; place it next to the exe.
        string dataDir = Path.Combine(AppContext.BaseDirectory, "webview2-data");
        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: dataDir);
        await _web.EnsureCoreWebView2Async(env);

        var c = _web.CoreWebView2;
        // Lock it down: it's a wallpaper, not a browser.
        c.Settings.AreDefaultContextMenusEnabled = false;
        c.Settings.IsStatusBarEnabled = false;
        c.Settings.AreDevToolsEnabled = false;
        c.Settings.IsZoomControlEnabled = false;

        Navigate(_source);
    }

    /// <summary>Point the wallpaper at a local file (html/video/image) or a URL.</summary>
    public void Navigate(string source)
    {
        if (_web.CoreWebView2 is null) return;

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            _web.CoreWebView2.Navigate(source);
            return;
        }

        string full = Path.GetFullPath(source);
        string ext = Path.GetExtension(full).ToLowerInvariant();
        if (ext is ".mp4" or ".webm" or ".ogg" or ".mov" or ".m4v")
        {
            // Wrap a video file in a full-bleed looping HTML5 player.
            _web.CoreWebView2.NavigateToString(VideoPage(full));
        }
        else if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp")
        {
            _web.CoreWebView2.NavigateToString(ImagePage(full));
        }
        else // .html and anything else: load directly
        {
            _web.CoreWebView2.Navigate(new Uri(full).AbsoluteUri);
        }
    }

    private static string VideoPage(string file)
    {
        string url = new Uri(file).AbsoluteUri;
        return $@"<!doctype html><html><head><meta charset='utf-8'>
<style>html,body{{margin:0;height:100%;background:#000;overflow:hidden}}
video{{width:100%;height:100%;object-fit:cover}}</style></head>
<body><video autoplay loop muted playsinline src='{url}'></video></body></html>";
    }

    private static string ImagePage(string file)
    {
        string url = new Uri(file).AbsoluteUri;
        return $@"<!doctype html><html><head><meta charset='utf-8'>
<style>html,body{{margin:0;height:100%;background:#000;overflow:hidden}}
img{{width:100%;height:100%;object-fit:cover}}</style></head>
<body><img src='{url}'></body></html>";
    }
}
