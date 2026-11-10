using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal static class MonitorEnumerator
{
    internal static List<MonitorInfo> EnumerateMonitors()
    {
        var monitors = new List<MonitorInfo>();
        Dictionary<string, MonitorDisplayInfo> displayInfo = GetDisplayInfo();

        bool enumerated = User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, nint _) =>
        {
            var info = new MONITORINFOEXW { cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>() };
            if (!User32.GetMonitorInfoW(hMonitor, ref info))
            {
                Logger.Warn("GetMonitorInfoW failed; skipping the monitor", Marshal.GetLastWin32Error());
                return true;
            }

            string devicePath;
            unsafe
            {
                devicePath = new string(info.szDevice);
            }

            bool isPrimary = (info.dwFlags & WindowStyles.MONITORINFOF_PRIMARY) != 0;
            displayInfo.TryGetValue(devicePath, out MonitorDisplayInfo qdcInfo);
            string? fallbackName = GetDisplayDeviceFriendlyName(devicePath);
            (string friendlyName, MonitorIdentity identity) = MonitorNameResolver.Resolve(
                devicePath,
                displayInfo.ContainsKey(devicePath) ? qdcInfo : null,
                fallbackName);

            monitors.Add(new MonitorInfo(devicePath, friendlyName, info.rcMonitor, isPrimary, identity));
            return true;
        }, 0);
        if (!enumerated)
            Logger.Warn("EnumDisplayMonitors failed", Marshal.GetLastWin32Error());

        Logger.Info($"Monitors enumerated: {monitors.Count} found");
        for (int i = 0; i < monitors.Count; i++)
        {
            MonitorInfo m = monitors[i];
            string primary = m.IsPrimary ? " Primary" : "";
            Logger.Info($"  #{i + 1} \"{m.FriendlyName}\" ({m.Bounds.Left},{m.Bounds.Top} {m.Bounds.Width}x{m.Bounds.Height}){primary} {m.DevicePath} [EDID:{m.Identity.EdidManufacturerId:X4}:{m.Identity.EdidProductCodeId:X4} CI:{m.Identity.ConnectorInstance}]");
        }

        return monitors;
    }

    private static Dictionary<string, MonitorDisplayInfo> GetDisplayInfo()
    {
        var result = new Dictionary<string, MonitorDisplayInfo>(StringComparer.Ordinal);

        int status = DisplayConfigApi.GetDisplayConfigBufferSizes(
            WindowStyles.QDC_ONLY_ACTIVE_PATHS,
            out uint pathCount,
            out uint modeCount);

        if (status != 0)
        {
            Logger.Warn("GetDisplayConfigBufferSizes failed; friendly monitor names are unavailable", status);
            return result;
        }

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        status = DisplayConfigApi.QueryDisplayConfig(
            WindowStyles.QDC_ONLY_ACTIVE_PATHS,
            ref pathCount, paths,
            ref modeCount, modes,
            IntPtr.Zero);

        if (status != 0)
        {
            Logger.Warn("QueryDisplayConfig failed; friendly monitor names are unavailable", status);
            return result;
        }

        for (int i = 0; i < pathCount; i++)
        {
            var deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            deviceName.header.type = WindowStyles.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            deviceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
            deviceName.header.adapterId = paths[i].targetInfo.adapterId;
            deviceName.header.id = paths[i].targetInfo.id;

            status = DisplayConfigApi.DisplayConfigGetDeviceInfo(ref deviceName);
            if (status != 0)
            {
                Logger.Warn("DisplayConfigGetDeviceInfo failed for a target name", status);
                continue;
            }

            var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            sourceName.header.type = WindowStyles.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            sourceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
            sourceName.header.adapterId = paths[i].sourceInfo.adapterId;
            sourceName.header.id = paths[i].sourceInfo.id;

            int sourceStatus = DisplayConfigApi.DisplayConfigGetSourceDeviceInfo(ref sourceName);
            if (sourceStatus != 0)
            {
                Logger.Warn("DisplayConfigGetDeviceInfo failed for a source name", sourceStatus);
                continue;
            }

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
                var identity = new MonitorIdentity(
                    deviceName.edidManufactureId,
                    deviceName.edidProductCodeId,
                    deviceName.connectorInstance);
                result[gdiDeviceName] = new MonitorDisplayInfo(friendlyName, identity);
            }
        }

        return result;
    }

    private static string? GetDisplayDeviceFriendlyName(string devicePath)
    {
        var device = new DISPLAY_DEVICEW { cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>() };
        if (!User32.EnumDisplayDevicesW(devicePath, 0, ref device, 0))
        {
            Logger.Warn($"EnumDisplayDevicesW failed for {devicePath}", Marshal.GetLastWin32Error());
            return null;
        }

        unsafe
        {
            var name = new string(device.DeviceString);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }
}
