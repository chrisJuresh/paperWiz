# paperWiz (Windows)

A native Windows app that gives every monitor a wallpaper built from **one image and its
colour** — the classic paperWiz idea, rebuilt for Windows with a real UI.

**Made for people with HiDPI and / or multiple monitors.**

paperWiz pulls the dominant colour from your wallpaper and uses it two ways:

- **Your other monitors** are painted that colour — no more hunting for a matching second
  wallpaper, duplicating one across screens, or spanning one and losing half of it.
- **Smaller / vertical wallpapers are framed** in that colour instead of being stretched or
  cropped, so low-resolution and portrait images look intentional on a big screen.

Unlike the [Linux version](../linux/README.md) (a shell script driving `feh` + `pywal` +
ImageMagick), the Windows build is a self-contained **C# / WPF** app that talks directly to
the Windows shell `IDesktopWallpaper` API, so each monitor really does get its own picture.

## Requirements

- Windows 10 (1903+) or Windows 11
- To **run from source**: the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- To **build / publish**: the [.NET 8 SDK](https://dot.net)
- A published single-file build (see below) needs **nothing installed** — it bundles the runtime.

## Install it as an app (recommended)

**Just double-click [`install.bat`](../install.bat) in the repo root.** (Double-clicking a
`.ps1` only opens it in Notepad — Windows won't run scripts that way — so `install.bat` is the
double-click entry point; it just launches the PowerShell installer for you.)

From a terminal it's equivalent to:

```powershell
cd paperWiz\windows
.\install.ps1
```

This builds a self-contained `PaperWiz.exe`, installs it to
`%LOCALAPPDATA%\Programs\paperWiz`, adds **Start menu + Desktop shortcuts** (with the app
icon), and registers it under **Settings → Apps** so it can be uninstalled normally. No admin
rights needed — everything is per-user.

Then find **paperWiz** in the Start menu or on the Desktop, **right-click → Pin to taskbar**,
and click it any time to open the UI. (Windows doesn't allow apps to pin themselves to the
taskbar, so that last click is manual — it's a one-time step.)

> The exe isn't code-signed, so the first launch may show a blue **"Windows protected your PC"**
> SmartScreen prompt — click **More info → Run anyway**. (Sign the exe to avoid this if you
> distribute it.)

Uninstall any time by double-clicking `uninstall.bat`, running `.\uninstall.ps1`, or from
**Settings → Apps**.

## Other ways to run it

```powershell
# Run from source (Debug), no install:
.\run.ps1              # or:  dotnet run --project src\PaperWiz

# Just build the standalone exe without installing shortcuts:
.\publish.ps1          # produces windows\publish\PaperWiz.exe
```

You can also open `windows\PaperWiz.sln` in Visual Studio 2022 and press F5.

## Using it

1. **Choose a wallpaper** — drag an image onto the window, click the preview to browse for a
   file, or click **Open folder…** to load a whole folder as a **thumbnail gallery** and click
   any photo to try it.
2. **Pick which display shows it** — click a monitor in the live preview. The others get the
   accent colour.
3. **Set the accent colour** — the accent card offers, in order:
   - **Pywal palette** — a 16-colour complementary scheme generated the pywal way (see below);
     hover a swatch for its role, click to use it.
   - **From your wallpaper** — labelled picks (**Most common** = the Linux algorithm, plus
     **Most vibrant**, **Average**, **Darkest**, **Lightest**).
   - Or type a hex code / drag the R/G/B sliders for a custom colour.

   The accent always comes from the wallpaper, and **your pick is remembered as a *role*** — if
   you choose "Darkest" (or a pywal slot), the next photo you load uses *its* darkest, and so on.
   A custom hex/RGB colour instead stays fixed until you pick a swatch again.
4. **Choose how it sits** (Placement):
   - **Auto** — covers the display if the image is big enough, otherwise frames it. *(Recommended.)*
   - **Frame** — always fit the whole image inside and surround it with the accent colour.
   - **Cover** — always fill the display, cropping the overflow.
   - **Position** — where a framed image sits (the 3×3 grid: centre, edges, corners).
   - **Shrink tall wallpapers** — cap the image height first (great for portrait wallpapers,
     mirrors the Linux script's `-s`). The default follows the old README's rule of thumb
     (monitor height ÷ 9 × 7).
   - **Golden-ratio border** — shrink the image one golden step (to ≈62%) so the accent colour
     frames it. Great for admiring the extracted colour around a photo.

**There's no Apply button — every change applies to your desktop automatically** (rapid tweaks
like slider drags are coalesced so it applies once you pause). The preview is faithful: what you
see per monitor is what gets set.

## How it works

- **`ColorExtractor`** picks the accent colour with the **same algorithm as the Linux script**:
  it shrinks the image to ≤ 500px and takes the single most frequent *exact* colour (no
  quantisation, no weighting). That's the **Most common** suggestion and the default. It also
  offers extra labelled suggestions unique to the Windows build — **Most vibrant**, **Average**,
  **Darkest**, **Lightest**, **Also common** — each with a tooltip explaining why it was picked.
  *(Pixel values can differ from Linux by a hair because ImageMagick and GDI+ use different
  resize filters, but the selection logic is identical.)*
- **`PywalGenerator`** natively re-implements pywal's default (`wal`) backend so you get the
  Linux `-c 0..15` experience on Windows. pywal shrinks the image and asks ImageMagick for 16
  representative colours (`-colors 16`, i.e. median-cut quantisation), then assembles a scheme
  (darkened background, six accents + bright variants, light foreground). We do the same:
  median-cut quantisation into 16 colours + the same slot assembly and adjustments, with a gentle
  saturation lift so the accents pop like pywal's — no ImageMagick or Python required.
- **`WallpaperComposer`** renders a full-resolution image per monitor: the wallpaper covered or
  framed by the accent colour (optionally inset by one golden-ratio step, 1/φ), or a solid accent
  fill for the other screens.
- **`MonitorService` / `DesktopWallpaper`** enumerate displays and set each one's wallpaper via
  the shell `IDesktopWallpaper` COM interface, with position `Fill` so a monitor-sized composite
  maps 1:1 regardless of DPI.

Generated images are cached in `%LOCALAPPDATA%\PaperWiz\cache`.

## Project layout

```
(../install.bat)                # ← double-click this in the repo root to install
windows/
├── PaperWiz.sln
├── install.ps1                 # the installer (invoked by ../install.bat)
├── uninstall.bat / uninstall.ps1
├── run.ps1                     # build + run (Debug)
├── publish.ps1                 # single-file self-contained .exe
└── src/PaperWiz/
    ├── App.xaml(.cs)           # app bootstrap, theme + converters
    ├── MainWindow.xaml(.cs)    # the UI + drag-and-drop + folder gallery
    ├── app.manifest            # PerMonitorV2 DPI awareness
    ├── Assets/paperwiz.ico     # app icon
    ├── Converters.cs
    ├── Themes/Theme.xaml       # dark Fluent-style theme
    ├── Interop/DesktopWallpaper.cs   # IDesktopWallpaper COM projection
    ├── Models/WallpaperModels.cs
    ├── Services/               # ColorExtractor, PywalGenerator, WallpaperComposer, MonitorService, WallpaperService
    └── ViewModels/             # MVVM: Main / Monitor view-models
```
