using System.Runtime.InteropServices;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.Monitors;

internal sealed class Win32MonitorApi : IMonitorApi
{
    public int LastError => Marshal.GetLastWin32Error();

    public bool EnumDisplayMonitors(Func<IntPtr, bool> onMonitor)
        => User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, nint _) => onMonitor(hMonitor), 0);

    public bool GetMonitorInfo(IntPtr monitor, out RECT bounds, out uint flags, out string devicePath)
    {
        var info = new MONITORINFOEXW { cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>() };
        if (!User32.GetMonitorInfoW(monitor, ref info))
        {
            bounds = default;
            flags = 0;
            devicePath = string.Empty;
            return false;
        }

        bounds = info.rcMonitor;
        flags = info.dwFlags;
        unsafe
        {
            devicePath = new string(info.szDevice);
        }
        return true;
    }

    public Dictionary<string, MonitorDisplayInfo> QueryDisplayInfo()
    {
        var result = new Dictionary<string, MonitorDisplayInfo>(StringComparer.Ordinal);

        int status = DisplayConfig.GetDisplayConfigBufferSizes(
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

        status = DisplayConfig.QueryDisplayConfig(
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

            status = DisplayConfig.DisplayConfigGetDeviceInfo(ref deviceName);
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

            int sourceStatus = DisplayConfig.DisplayConfigGetSourceDeviceInfo(ref sourceName);
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

    public string? GetDisplayDeviceName(string devicePath)
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
