using System.Runtime.InteropServices;

namespace PaperWiz.Interop;

/// <summary>
/// How Windows maps a wallpaper file onto a monitor. We build our composites at the
/// monitor's exact pixel size and use <see cref="Fill"/>, so the mapping is 1:1 with
/// no distortion regardless of DPI.
/// </summary>
public enum DesktopWallpaperPosition
{
    Center = 0,
    Tile = 1,
    Stretch = 2,
    Fit = 3,
    Fill = 4,
    Span = 5,
}

[StructLayout(LayoutKind.Sequential)]
public struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;
    public readonly int Height => Bottom - Top;
    public readonly bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>
/// Managed projection of the shell <c>IDesktopWallpaper</c> interface (Windows 8+).
/// Methods are declared in exact vtable order up to the ones we call; the trailing
/// slideshow / status methods are intentionally omitted (we never invoke them).
/// </summary>
[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopWallpaper
{
    void SetWallpaper(
        [MarshalAs(UnmanagedType.LPWStr)] string? monitorID,
        [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMonitorDevicePathAt(uint monitorIndex);

    uint GetMonitorDevicePathCount();

    Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

    void SetBackgroundColor(uint color);

    uint GetBackgroundColor();

    void SetPosition(DesktopWallpaperPosition position);

    DesktopWallpaperPosition GetPosition();
}

/// <summary>CoClass for <see cref="IDesktopWallpaper"/> (CLSID_DesktopWallpaper).</summary>
[ComImport]
[Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")]
public class DesktopWallpaperClass
{
}

public static class DesktopWallpaperFactory
{
    public static IDesktopWallpaper Create() => (IDesktopWallpaper)new DesktopWallpaperClass();
}
