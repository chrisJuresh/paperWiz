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

    /// <summary>Clockwise rotation in quarter turns (0, 90, 180, or 270 degrees).</summary>
    public int RotationDegrees { get; init; }

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

/// <summary>The user choices restored when paperWiz next starts.</summary>
public sealed class PaperWizSettings
{
    public int Version { get; init; } = 1;

    public string? WallpaperPath { get; init; }

    /// <summary>
    /// The stable shell device path is preferred over a list index because display order can
    /// change after a reboot or docking/undocking.
    /// </summary>
    public string? WallpaperMonitorDeviceId { get; init; }

    public int WallpaperMonitorIndex { get; init; }

    public string AccentHex { get; init; } = "#202228";

    /// <summary>
    /// Palette role to re-derive from the image. Null means <see cref="AccentHex"/> is a fixed
    /// custom colour.
    /// </summary>
    public string? SelectedAccentKey { get; init; } = "Most common";

    public Anchor Anchor { get; init; } = Anchor.Center;
    public FitMode FitMode { get; init; } = FitMode.Auto;
    public int RotationDegrees { get; init; }
    public bool Shrink { get; init; }
    public int ShrinkHeight { get; init; } = 1080;
    public bool FillOtherMonitors { get; init; } = true;
    public bool GoldenMargin { get; init; }
}
