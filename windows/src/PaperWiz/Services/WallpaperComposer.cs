using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PaperWiz.Models;

namespace PaperWiz.Services;

/// <summary>
/// Builds the final full-resolution image for a single monitor: either the wallpaper
/// (covered or framed by the accent colour) or a solid accent fill for the other monitors.
/// </summary>
public static class WallpaperComposer
{
    /// <summary>1/φ — one golden-ratio step smaller.</summary>
    public const double GoldenFraction = 0.6180339887498949;

    /// <summary>Renders a solid accent-colour image at the monitor's size.</summary>
    public static Bitmap ComposeSolid(int width, int height, Color accent)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(accent);
        return bmp;
    }

    /// <summary>
    /// Renders the wallpaper onto a monitor-sized canvas. When the (optionally shrunk)
    /// wallpaper does not cover the monitor, it is scaled to fit and surrounded by the
    /// accent colour at the requested anchor. Otherwise it is scaled to cover.
    /// </summary>
    public static Bitmap ComposeWallpaper(string wallpaperPath, int monWidth, int monHeight, PaperWizOptions options)
    {
        using Bitmap wallpaper = LoadPossiblyShrunk(wallpaperPath, options.ShrinkHeight);

        bool covers = wallpaper.Width >= monWidth && wallpaper.Height >= monHeight;
        bool useCover = options.FitMode switch
        {
            FitMode.Cover => true,
            FitMode.Frame => false,
            _ => covers, // Auto
        };

        // A golden-ratio border means the whole image must be visible and inset, so it
        // always frames (never covers), fitting inside a box 1/φ of the monitor.
        if (options.GoldenMargin)
            useCover = false;
        double fitFraction = options.GoldenMargin ? GoldenFraction : 1.0;

        var canvas = new Bitmap(monWidth, monHeight, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(canvas);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(options.AccentColor);

        Rectangle dest = useCover
            ? CoverRect(wallpaper.Width, wallpaper.Height, monWidth, monHeight)
            : ContainRect(wallpaper.Width, wallpaper.Height, monWidth, monHeight, options.Anchor, fitFraction);

        g.DrawImage(wallpaper, dest);
        return canvas;
    }

    private static Bitmap LoadPossiblyShrunk(string path, int? shrinkHeight)
    {
        var original = new Bitmap(path);
        if (shrinkHeight is not int target || original.Height <= target)
            return original;

        double scale = (double)target / original.Height;
        int w = Math.Max(1, (int)Math.Round(original.Width * scale));
        int h = Math.Max(1, target);

        var shrunk = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(shrunk))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(original, 0, 0, w, h);
        }
        original.Dispose();
        return shrunk;
    }

    /// <summary>Scale-to-cover: fill the monitor, cropping the overflow, centred.</summary>
    private static Rectangle CoverRect(int srcW, int srcH, int dstW, int dstH)
    {
        double scale = Math.Max((double)dstW / srcW, (double)dstH / srcH);
        int w = (int)Math.Ceiling(srcW * scale);
        int h = (int)Math.Ceiling(srcH * scale);
        int x = (dstW - w) / 2;
        int y = (dstH - h) / 2;
        return new Rectangle(x, y, w, h);
    }

    /// <summary>
    /// Scale-to-fit: fit inside the monitor (optionally within a smaller fraction of it, e.g.
    /// 1/φ for a golden border), positioned by the anchor. Never upscales beyond the source.
    /// </summary>
    private static Rectangle ContainRect(int srcW, int srcH, int dstW, int dstH, Anchor anchor, double fitFraction = 1.0)
    {
        double availW = dstW * fitFraction, availH = dstH * fitFraction;
        double scale = Math.Min(1.0, Math.Min(availW / srcW, availH / srcH));
        int w = Math.Max(1, (int)Math.Round(srcW * scale));
        int h = Math.Max(1, (int)Math.Round(srcH * scale));

        int x = anchor switch
        {
            Anchor.TopLeft or Anchor.Left or Anchor.BottomLeft => 0,
            Anchor.TopRight or Anchor.Right or Anchor.BottomRight => dstW - w,
            _ => (dstW - w) / 2,
        };
        int y = anchor switch
        {
            Anchor.TopLeft or Anchor.Top or Anchor.TopRight => 0,
            Anchor.BottomLeft or Anchor.Bottom or Anchor.BottomRight => dstH - h,
            _ => (dstH - h) / 2,
        };
        return new Rectangle(x, y, w, h);
    }
}
