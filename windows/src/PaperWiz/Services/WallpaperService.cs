using System.Drawing.Imaging;
using System.IO;
using PaperWiz.Interop;
using PaperWiz.Models;

namespace PaperWiz.Services;

/// <summary>
/// Orchestrates a full apply: compose an image per monitor, write it to the cache,
/// then set it with the shell wallpaper API.
/// </summary>
public sealed class WallpaperService
{
    private static readonly string CacheDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaperWiz", "cache");

    /// <summary>
    /// Applies the wallpaper set described by <paramref name="options"/> across the given monitors.
    /// Safe to call from a background thread; performs no UI work.
    /// </summary>
    public void Apply(IReadOnlyList<MonitorInfo> monitors, PaperWizOptions options)
    {
        if (monitors.Count == 0)
            throw new InvalidOperationException("No monitors were detected.");
        if (!File.Exists(options.WallpaperPath))
            throw new FileNotFoundException("Wallpaper not found.", options.WallpaperPath);

        Directory.CreateDirectory(CacheDir);

        var wallpaper = DesktopWallpaperFactory.Create();

        // Position must be Fill so a monitor-sized composite maps 1:1 to the display.
        try { wallpaper.SetPosition(DesktopWallpaperPosition.Fill); } catch { /* best effort */ }

        foreach (var monitor in monitors)
        {
            bool isWallpaperMonitor = monitor.Index == options.WallpaperMonitorIndex;

            using var bitmap = isWallpaperMonitor
                ? WallpaperComposer.ComposeWallpaper(options.WallpaperPath, monitor.Width, monitor.Height, options)
                : WallpaperComposer.ComposeSolid(monitor.Width, monitor.Height, options.AccentColor);

            if (!isWallpaperMonitor && !options.FillOtherMonitors)
                continue;

            string outPath = Path.Combine(CacheDir, $"monitor_{monitor.Index}.png");
            SavePng(bitmap, outPath);
            wallpaper.SetWallpaper(monitor.DeviceId, outPath);
        }
    }

    private static void SavePng(System.Drawing.Bitmap bitmap, string path)
    {
        // Write to a temp file then move, so SetWallpaper never reads a half-written file
        // and Windows reliably notices the change even when the path is reused.
        string tmp = path + ".tmp";
        bitmap.Save(tmp, ImageFormat.Png);
        if (File.Exists(path))
            File.Delete(path);
        File.Move(tmp, path);
    }
}
