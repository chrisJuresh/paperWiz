using System.Drawing;
using PaperWiz.Interop;

namespace PaperWiz.Models;

/// <summary>Where a contained wallpaper is anchored within a larger colour canvas.</summary>
public enum Anchor
{
    TopLeft,
    Top,
    TopRight,
    Left,
    Center,
    Right,
    BottomLeft,
    Bottom,
    BottomRight,
}

/// <summary>How the wallpaper is placed on the chosen wallpaper monitor.</summary>
public enum FitMode
{
    /// <summary>Cover if the image already fills the monitor, otherwise frame it. (Recommended.)</summary>
    Auto,

    /// <summary>Scale to fit inside the monitor and surround the remainder with the accent colour.</summary>
    Frame,

    /// <summary>Scale to cover the whole monitor, cropping the overflow.</summary>
    Cover,
}

/// <summary>A physical display, as reported by the shell wallpaper API.</summary>
public sealed class MonitorInfo
{
    public required int Index { get; init; }

    /// <summary>Opaque monitor device path used by <c>IDesktopWallpaper.SetWallpaper</c>.</summary>
    public required string DeviceId { get; init; }

    /// <summary>Bounds in virtual-desktop pixels.</summary>
    public required Rect Bounds { get; init; }

    public required bool IsPrimary { get; init; }

    public int Width => Bounds.Width;
    public int Height => Bounds.Height;

    public string ResolutionText => $"{Width} × {Height}";
}

/// <summary>Everything needed to render and apply a wallpaper set.</summary>
public sealed class PaperWizOptions
{
    public required string WallpaperPath { get; init; }

    /// <summary>Index (into the monitor list) of the display that shows the wallpaper.</summary>
    public required int WallpaperMonitorIndex { get; init; }

    /// <summary>Accent colour used for the frame and the other monitors.</summary>
    public required Color AccentColor { get; init; }

    public Anchor Anchor { get; init; } = Anchor.Center;

    public FitMode FitMode { get; init; } = FitMode.Auto;

    /// <summary>If set, the wallpaper is first shrunk so its height is at most this many pixels.</summary>
    public int? ShrinkHeight { get; init; }

    /// <summary>
    /// Shrink the placed image by one golden-ratio step (to 1/φ ≈ 61.8% of the monitor) and
    /// surround it with the accent colour, so the accent shows as a border. Implies framing.
    /// </summary>
    public bool GoldenMargin { get; init; }

    /// <summary>Fill every non-wallpaper monitor with the accent colour (original paperWiz behaviour).</summary>
    public bool FillOtherMonitors { get; init; } = true;
}
