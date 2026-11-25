namespace ScreenNap.Monitors;

// The order in which a monitor's name falls back (specification MON-3)
internal static class MonitorNameResolver
{
    internal static string Resolve(string devicePath, string? friendlyName, string? displayDeviceName)
    {
        if (!string.IsNullOrWhiteSpace(friendlyName))
            return friendlyName;

        if (!string.IsNullOrWhiteSpace(displayDeviceName))
            return displayDeviceName;

        return devicePath.StartsWith(@"\\.\", StringComparison.Ordinal)
            ? devicePath[4..]
            : devicePath;
    }
}
