namespace WallpaperApp;

/// <summary>
/// Locates the desktop layer to host wallpaper windows on.
///
/// On Windows 11 24H2/25H2 the actively-maintained engines (e.g. Lively) parent
/// their render window to <c>Progman</c> directly; the wallpaper then composites
/// behind the desktop icons. We replicate that: spawn the WorkerW, prefer it if
/// a usable one exists, otherwise fall back to Progman (the path verified to work
/// on this build).
/// </summary>
internal static class DesktopWorker
{
    public static IntPtr GetProgman() => NativeMethods.FindWindow("Progman", null);

    /// <summary>Find the SHELLDLL_DefView host (the desktop icon view).</summary>
    private static IntPtr FindDefView()
    {
        IntPtr progman = GetProgman();
        IntPtr def = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (def != IntPtr.Zero) return def;

        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindows((h, _) =>
        {
            if (NativeMethods.FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                found = NativeMethods.FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
            return true;
        }, IntPtr.Zero);
        return found;
    }

    /// <summary>
    /// Toggle "Show desktop icons" off then on. On some 24H2/25H2 configs this is
    /// the documented "kick" that makes the shell (re)build the wallpaper layer so
    /// hosted windows render.
    /// </summary>
    public static void KickDesktop()
    {
        IntPtr def = FindDefView();
        if (def == IntPtr.Zero) return;
        NativeMethods.SendMessage(def, NativeMethods.WM_COMMAND, (IntPtr)NativeMethods.CMD_TOGGLE_ICONS, IntPtr.Zero);
        Thread.Sleep(400);
        NativeMethods.SendMessage(def, NativeMethods.WM_COMMAND, (IntPtr)NativeMethods.CMD_TOGGLE_ICONS, IntPtr.Zero);
        Thread.Sleep(400);
    }

    /// <summary>
    /// Return the window to parent wallpaper surfaces to. Sends the WorkerW spawn
    /// message first, then prefers a real WorkerW, falling back to Progman.
    /// </summary>
    public static IntPtr GetWallpaperHost()
    {
        IntPtr progman = GetProgman();
        NativeMethods.SendMessageTimeout(progman, NativeMethods.WM_SPAWN_WORKER, IntPtr.Zero, IntPtr.Zero,
            0 /*SMTO_NORMAL*/, 1000, out _);

        // Win10-style: top-level WorkerW sibling of the DefView host.
        IntPtr worker = IntPtr.Zero;
        NativeMethods.EnumWindows((h, _) =>
        {
            if (NativeMethods.FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
            {
                IntPtr w = NativeMethods.FindWindowEx(IntPtr.Zero, h, "WorkerW", null);
                if (w != IntPtr.Zero) worker = w;
            }
            return true;
        }, IntPtr.Zero);
        if (worker != IntPtr.Zero) return worker;

        // Win11 24H2/25H2: Progman hosts SHELLDLL_DefView (icons) AND a child
        // WorkerW that sits BEHIND the icons. Target that WorkerW so our window
        // renders behind the icons and does not eat desktop clicks. Parenting to
        // Progman itself would place us ON TOP of the icons.
        IntPtr childWorker = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        if (childWorker != IntPtr.Zero) return childWorker;

        // Last resort.
        return progman;
    }
}
