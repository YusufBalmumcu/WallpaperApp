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

The app runs in the **system tray**. With no argument it opens a small control
window where you can **Browse…** for a source, toggle *Primary monitor only*, and
**Apply / Use sample / Stop**. Closing the window hides it to the tray; the
wallpaper keeps running. Double-click the tray icon for the window, or right-click
it for **Show controls / Use sample / Stop / Exit**.

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
  ControlForm.cs         control window
  WallpaperManager.cs    owns per-monitor wallpaper windows
  WallpaperWindow.cs     borderless WebView2 host window
  DesktopWorker.cs       Progman/WorkerW desktop placement
  NativeMethods.cs       Win32 P/Invoke
wallpapers/sample/     bundled animated HTML wallpaper
```

## Roadmap

- [x] Tray icon + simple control UI (pick wallpaper, stop)
- [ ] Remember last wallpaper / restore on launch
- [ ] Wallpaper library with thumbnails
- [ ] Pause rendering when a fullscreen app is focused (save GPU)
- [ ] Run at startup
