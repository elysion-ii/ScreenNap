using ScreenNap.Native;

namespace ScreenNap.Monitors;

// Identity is null when Windows reports no display path for the monitor (specification MON-4)
internal sealed record MonitorInfo(string DevicePath, string FriendlyName, RECT Bounds, bool IsPrimary, MonitorIdentity? Identity)
{
    // Whether this record denotes the monitor of that identity and path: the same physical
    // monitor, or for a monitor without a physical identity the same display device path
    // (specification MON-7, INV-2)
    internal bool Denotes(MonitorIdentity? identity, string devicePath)
        => Identity is { } own
            ? identity == own
            : identity is null && string.Equals(DevicePath, devicePath, StringComparison.Ordinal);
}
