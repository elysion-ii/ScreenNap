using ScreenNap.Native;

namespace ScreenNap.Monitors;

// The display functions monitor enumeration calls, behind one interface so a unit test can
// make any of them fail (docs/rules/ScreenNap.md, Win32 and P/Invoke)
internal interface IMonitorApi
{
    // The Win32 error code the most recent call left
    int LastError { get; }

    // Calls onMonitor with each display monitor's handle while it returns true; false when
    // the enumeration itself fails, possibly after some monitors were reported
    bool EnumDisplayMonitors(Func<IntPtr, bool> onMonitor);

    bool GetMonitorInfo(IntPtr monitor, out RECT bounds, out uint flags, out string devicePath);

    // The friendly name and identity of each active display path, keyed by GDI device name;
    // empty when Windows cannot report them
    Dictionary<string, MonitorDisplayInfo> QueryDisplayInfo();

    // Null when the display device has no name
    string? GetDisplayDeviceName(string devicePath);
}
