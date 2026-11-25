using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.Monitors;

internal sealed class Win32MonitorApi : IMonitorApi
{
    private const string EdidValueName = "EDID";

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

    public Dictionary<string, DisplayTarget> QueryDisplayTargets()
    {
        var result = new Dictionary<string, DisplayTarget>(StringComparer.Ordinal);

        int status = DisplayConfig.GetDisplayConfigBufferSizes(
            WindowStyles.QDC_ONLY_ACTIVE_PATHS,
            out uint pathCount,
            out uint modeCount);

        if (status != 0)
        {
            Logger.Warn("GetDisplayConfigBufferSizes failed; monitor names and identities are unavailable", status);
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
            Logger.Warn("QueryDisplayConfig failed; monitor names and identities are unavailable", status);
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
            string connection;
            string gdiDeviceName;
            unsafe
            {
                friendlyName = new string(deviceName.monitorFriendlyDeviceName);
                connection = new string(deviceName.monitorDevicePath);
                gdiDeviceName = new string(sourceName.viewGdiDeviceName);
            }

            if (string.IsNullOrWhiteSpace(gdiDeviceName) || string.IsNullOrWhiteSpace(connection))
                continue;

            result[gdiDeviceName] = new DisplayTarget(friendlyName, connection, ReadEdid(connection));
        }

        return result;
    }

    // The EDID the display driver stores under the monitor's device node; null when it cannot be read
    private static byte[]? ReadEdid(string connection)
    {
        string? instanceId = DeviceInstanceId(connection);
        if (instanceId is null)
            return null;

        try
        {
            using RegistryKey? parameters = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{instanceId}\Device Parameters");
            if (parameters?.GetValue(EdidValueName) is byte[] edid)
                return edid;

            Logger.Warn($"No EDID is stored for monitor {instanceId}");
            return null;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            Logger.Warn($"The EDID of monitor {instanceId} cannot be read: {ex.GetType().Name}");
            return null;
        }
    }

    private static string? DeviceInstanceId(string connection)
    {
        uint size = 0;
        uint status = Cfgmgr32.CM_Get_Device_Interface_PropertyW(
            connection, in WindowStyles.DEVPKEY_Device_InstanceId, out _, [], ref size, 0);
        if (status == WindowStyles.CR_BUFFER_SMALL)
        {
            byte[] buffer = new byte[size];
            status = Cfgmgr32.CM_Get_Device_Interface_PropertyW(
                connection, in WindowStyles.DEVPKEY_Device_InstanceId, out uint type, buffer, ref size, 0);
            if (status == WindowStyles.CR_SUCCESS && type == WindowStyles.DEVPROP_TYPE_STRING)
                return Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
        }

        Logger.Warn($"CM_Get_Device_Interface_PropertyW failed for the device instance of {connection} (CONFIGRET: {status})");
        return null;
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
