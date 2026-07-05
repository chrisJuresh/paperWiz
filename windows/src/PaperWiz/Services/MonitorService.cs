using PaperWiz.Interop;
using PaperWiz.Models;

namespace PaperWiz.Services;

/// <summary>Enumerates the connected displays using the shell wallpaper API.</summary>
public static class MonitorService
{
    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var wallpaper = DesktopWallpaperFactory.Create();
        var monitors = new List<MonitorInfo>();

        uint count = wallpaper.GetMonitorDevicePathCount();
        for (uint i = 0; i < count; i++)
        {
            string deviceId = wallpaper.GetMonitorDevicePathAt(i);
            if (string.IsNullOrEmpty(deviceId))
                continue;

            Rect bounds;
            try
            {
                bounds = wallpaper.GetMonitorRECT(deviceId);
            }
            catch (Exception)
            {
                // Detached / disconnected monitors keep a device path but have no rect.
                continue;
            }

            if (bounds.IsEmpty)
                continue;

            monitors.Add(new MonitorInfo
            {
                Index = monitors.Count,
                DeviceId = deviceId,
                Bounds = bounds,
                IsPrimary = bounds.Left == 0 && bounds.Top == 0,
            });
        }

        // Order left-to-right, top-to-bottom so the list matches the visual layout.
        monitors.Sort((a, b) =>
        {
            int byX = a.Bounds.Left.CompareTo(b.Bounds.Left);
            return byX != 0 ? byX : a.Bounds.Top.CompareTo(b.Bounds.Top);
        });

        // Re-index after sorting.
        for (int i = 0; i < monitors.Count; i++)
        {
            monitors[i] = new MonitorInfo
            {
                Index = i,
                DeviceId = monitors[i].DeviceId,
                Bounds = monitors[i].Bounds,
                IsPrimary = monitors[i].IsPrimary,
            };
        }

        return monitors;
    }
}
