# paperWiz
![Preview](https://imgur.com/iNTDo3D.gif)

**Give every monitor a wallpaper built from one image and its colour.**
Made for people with HiDPI and / or multiple monitors.

paperWiz pulls a colour from your wallpaper and uses it two ways:

- **Your other monitors** are painted that colour — no more hunting for a matching second
  wallpaper, duplicating one across screens, or spanning one and losing half of it.
- **Smaller / vertical wallpapers get framed** in that colour instead of being stretched or
  cropped — so low-resolution and portrait images look intentional on a big screen.

There are two versions in this repo — a **native Windows app** and the original **Linux script** —
each self-contained in its own folder. Jump to your platform below.

---

## Download

```
git clone https://github.com/chrisJuresh/paperWiz.git
```

…or click the green **Code → Download ZIP** button on GitHub and extract it.

---

## 🪟 Windows

A native app (C# / .NET 8 / WPF) that sets a real per-monitor wallpaper via the Windows
`IDesktopWallpaper` API — with a live multi-monitor preview, drag-and-drop, folder browsing, a
pywal-style colour palette, and changes that **apply automatically** as you make them.

**Install — just double-click [`install.bat`](install.bat).**

It builds a self-contained `PaperWiz.exe`, installs it to `%LOCALAPPDATA%\Programs\paperWiz`,
adds **Start-menu + Desktop shortcuts**, and registers it under **Settings → Apps**. No admin
rights needed. Then find **paperWiz** in the Start menu and **right-click → Pin to taskbar**.

- **Requirements:** Windows 10 (1903+) or 11. The installer needs the free
  [.NET 8 SDK](https://dot.net) to build the app the first time; the built `.exe` itself bundles
  everything and runs with nothing installed.
- The `.exe` isn't code-signed, so the first launch may show a **"Windows protected your PC"**
  prompt — click **More info → Run anyway**.
- **Uninstall** any time from **Settings → Apps**, or run `windows\uninstall.bat`.

**Full Windows guide → [windows/README.md](windows/README.md)**

---

## 🐧 Linux

The original shell script (Bash · `feh` · `pywal` · ImageMagick) for X11.

```
chmod +x install-paperWiz && ./install-paperWiz
cd linux && chmod +x paperWiz
./paperWiz -w /path/to/wallpaper.jpg
```

- **Dependencies:** `pywal`, `imagemagick`, `feh`.
- Set `resW` / `resH` in [`linux/paperWiz`](linux/paperWiz) to your main monitor's resolution.

**Full Linux guide → [linux/README.md](linux/README.md)**

---

## Repo layout

```
paperWiz/
├── install.bat            # Windows: double-click to install the app
├── install-paperWiz       # Linux: creates the cache dir
├── windows/               # native C#/WPF app  (see windows/README.md)
│   └── src/PaperWiz/       # source
└── linux/                 # original Bash script + docs  (see linux/README.md)
    └── paperWiz
```

Both versions share the same idea; they're implemented independently so each fits its platform
natively.
