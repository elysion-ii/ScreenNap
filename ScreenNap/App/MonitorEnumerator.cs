using System.Runtime.InteropServices;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal static class MonitorEnumerator
{
    internal static List<MonitorInfo> EnumerateMonitors()
    {
        var monitors = new List<MonitorInfo>();
        var friendlyNames = GetFriendlyNames();

        User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, nint _) =>
        {
            var info = new MONITORINFOEXW { cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>() };
            if (!User32.GetMonitorInfoW(hMonitor, ref info))
                return true;

            string devicePath;
            unsafe
            {
                devicePath = new string(info.szDevice);
            }

            bool isPrimary = (info.dwFlags & MONITORINFOEXW.MONITORINFOF_PRIMARY) != 0;
            string friendlyName = ResolveFriendlyName(devicePath, friendlyNames);

            monitors.Add(new MonitorInfo(devicePath, friendlyName, info.rcMonitor, isPrimary));
            return true;
        }, 0);

        Logger.Info($"Monitors enumerated: {monitors.Count} found");
        for (int i = 0; i < monitors.Count; i++)
        {
            MonitorInfo m = monitors[i];
            string primary = m.IsPrimary ? " Primary" : "";
            Logger.Info($"  #{i + 1} \"{m.FriendlyName}\" ({m.Bounds.Left},{m.Bounds.Top} {m.Bounds.Width}x{m.Bounds.Height}){primary} {m.DevicePath}");
        }

        return monitors;
    }

    private static string ResolveFriendlyName(string devicePath, Dictionary<string, string> friendlyNames)
    {
        // Try DisplayConfig friendly name first
        if (friendlyNames.TryGetValue(devicePath, out string? name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        // Fallback: EnumDisplayDevices
        string? fallbackName = GetDisplayDeviceFriendlyName(devicePath);
        if (!string.IsNullOrWhiteSpace(fallbackName))
            return fallbackName;

        // Final fallback: strip \\.\ prefix from device path
        if (devicePath.StartsWith(@"\\.\", StringComparison.Ordinal))
            return devicePath[4..];

        return devicePath;
    }

    private static Dictionary<string, string> GetFriendlyNames()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        int status = DisplayConfigApi.GetDisplayConfigBufferSizes(
            DisplayConfigApi.QDC_ONLY_ACTIVE_PATHS,
            out uint pathCount,
            out uint modeCount);

        if (status != 0)
            return result;

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        status = DisplayConfigApi.QueryDisplayConfig(
            DisplayConfigApi.QDC_ONLY_ACTIVE_PATHS,
            ref pathCount, paths,
            ref modeCount, modes,
            IntPtr.Zero);

        if (status != 0)
            return result;

        for (int i = 0; i < pathCount; i++)
        {
            var deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            deviceName.header.type = DisplayConfigApi.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            deviceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
            deviceName.header.adapterId = paths[i].targetInfo.adapterId;
            deviceName.header.id = paths[i].targetInfo.id;

            status = DisplayConfigApi.DisplayConfigGetDeviceInfo(ref deviceName);
            if (status != 0)
                continue;

            // Resolve the GDI device name for this source
            var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            sourceName.header.type = DisplayConfigApi.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            sourceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
            sourceName.header.adapterId = paths[i].sourceInfo.adapterId;
            sourceName.header.id = paths[i].sourceInfo.id;

            int sourceStatus = DisplayConfigApi.DisplayConfigGetSourceDeviceInfo(ref sourceName);
            if (sourceStatus != 0)
                continue;

            string friendlyName;
            string gdiDeviceName;
            unsafe
            {
                friendlyName = new string(deviceName.monitorFriendlyDeviceName);
                gdiDeviceName = new string(sourceName.viewGdiDeviceName);
            }

            if (!string.IsNullOrWhiteSpace(friendlyName) &&
                !string.IsNullOrWhiteSpace(gdiDeviceName))
            {
                result[gdiDeviceName] = friendlyName;
            }
        }

        return result;
    }

    private static string? GetDisplayDeviceFriendlyName(string devicePath)
    {
        var device = new DISPLAY_DEVICEW { cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>() };
        if (!User32.EnumDisplayDevicesW(devicePath, 0, ref device, 0))
            return null;

        unsafe
        {
            var name = new string(device.DeviceString);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }
}
