using ScreenNap.Monitors;
using ScreenNap.Native;

namespace ScreenNap.Tests.Monitors;

// Stands in for Windows' display functions: it reports the monitors a test lists, fails the
// information request of the monitors a test names, and can end the enumeration with a failure
internal sealed class FakeMonitorApi : IMonitorApi
{
    internal const int FailureError = 1461;

    public int LastError { get; private set; }

    internal List<(RECT Bounds, bool IsPrimary, string DevicePath)> Monitors { get; } = [];
    internal HashSet<int> MonitorsFailingInformation { get; } = [];
    // The enumeration fails after reporting this many monitors
    internal int? EnumerationFailsAfter { get; set; }

    public bool EnumDisplayMonitors(Func<IntPtr, bool> onMonitor)
    {
        int reported = EnumerationFailsAfter ?? Monitors.Count;
        for (int i = 0; i < reported; i++)
        {
            if (!onMonitor(new IntPtr(i + 1)))
                return true;
        }

        if (EnumerationFailsAfter is null)
            return true;
        LastError = FailureError;
        return false;
    }

    public bool GetMonitorInfo(IntPtr monitor, out RECT bounds, out uint flags, out string devicePath)
    {
        int index = checked((int)monitor) - 1;
        if (MonitorsFailingInformation.Contains(index))
        {
            LastError = FailureError;
            bounds = default;
            flags = 0;
            devicePath = string.Empty;
            return false;
        }

        (bounds, bool isPrimary, devicePath) = Monitors[index];
        flags = isPrimary ? WindowStyles.MONITORINFOF_PRIMARY : 0;
        return true;
    }

    public Dictionary<string, MonitorDisplayInfo> QueryDisplayInfo() => new(StringComparer.Ordinal);

    public string? GetDisplayDeviceName(string devicePath) => null;
}
