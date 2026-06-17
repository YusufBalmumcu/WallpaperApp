# WallpaperApp

A from-scratch live-wallpaper engine for Windows (Wallpaper-Engine / Lively style).
Renders **HTML/CSS/JS** animations and **video** as your desktop wallpaper, behind
the icons, using WebView2.

## Requirements

- Windows 10/11 (tested on Windows 11 25H2, build 26200)
- WebView2 Runtime (pre-installed on current Windows 11)
- .NET 8 SDK (to build)

## Build & run

```powershell
dotnet build src\WallpaperApp.csproj -c Release
src\bin\Release\net8.0-windows\WallpaperApp.exe                 # opens the control window
src\bin\Release\net8.0-windows\WallpaperApp.exe "C:\clip.mp4"   # apply a wallpaper on startup
src\bin\Release\net8.0-windows\WallpaperApp.exe "C:\art\index.html" --primary
```

The app runs in the **system tray** with a sleek dark control window:

- **Browse / drag-and-drop** a source, then **Apply** (or **Use sample**).
- **Pause / Resume** hides and restores the wallpaper without tearing it down.
- **Recent** list — click a previous wallpaper to re-apply it.
- **Primary monitor only**, **Run when Windows starts**, and **Re-apply last
  wallpaper on launch** toggles.

Settings (last wallpaper, recents, options) persist to
`%APPDATA%\WallpaperApp\settings.json`. Closing the window hides it to the tray;
the wallpaper keeps running. Double-click the tray icon for the window, or
right-click it for **Show controls / Use sample / Pause / Stop / Exit**.

The `--silent` flag (used by the run-at-startup entry) starts in the tray and
re-applies the last wallpaper without opening the window.

Sources: a local `.html`, video (`.mp4/.webm/.ogg/.mov`), image, or an http(s) URL.

## How it renders behind the icons

`src/DesktopWorker.cs` sends the undocumented `0x052C` message to `Progman` to
spawn the desktop `WorkerW`, then `src/WallpaperWindow.cs` reparents a borderless
WebView2 window into the desktop layer (preferring a real `WorkerW`, falling back
to `Progman` directly — the path that works on Windows 11 24H2/25H2, the same one
Lively uses). One window is created per monitor, sized to that monitor.

> Note: behind-icon rendering was broken on early Windows 11 24H2/25H2 builds
> (DWM composited the wallpaper above the WorkerW layer). A Windows update fixed
> it. If live wallpaper ever stops rendering, toggling *Show desktop icons* off/on
> is the known kick (`DesktopWorker.KickDesktop`).

## Project layout

```
src/                   C#/.NET 8 WinForms + WebView2 engine
  Program.cs             entry point (tray app)
  TrayContext.cs         tray icon + menu
  ControlForm.cs         control window (dark UI)
  Theme.cs               dark palette + rounded button
  AppSettings.cs         JSON settings (last/recents/options)
  StartupManager.cs      run-at-startup registry toggle
  WallpaperManager.cs    owns per-monitor wallpaper windows; pause/resume
  WallpaperWindow.cs     borderless WebView2 host window
  DesktopWorker.cs       Progman/WorkerW desktop placement
  NativeMethods.cs       Win32 P/Invoke
wallpapers/sample/     bundled animated HTML wallpaper
```

## Roadmap

- [x] Tray icon + control UI (sleek dark theme)
- [x] Remember last wallpaper + recents, re-apply on launch
- [x] Run when Windows starts
- [x] Pause / resume
- [ ] Wallpaper library with thumbnails
- [ ] Auto-pause when a fullscreen app is focused (save GPU)
- [ ] Per-monitor different wallpapers
- [ ] Custom tray icon
