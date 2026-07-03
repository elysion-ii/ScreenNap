namespace ScreenNap.Core;

internal readonly record struct MonitorDisplayInfo(string FriendlyName, MonitorIdentity Identity);

internal static class MonitorNameResolver
{
    internal static (string FriendlyName, MonitorIdentity Identity) Resolve(
        string devicePath,
        MonitorDisplayInfo? qdcInfo,
        string? enumDisplayDeviceName)
    {
        if (qdcInfo is { } info && !string.IsNullOrWhiteSpace(info.FriendlyName))
            return (info.FriendlyName, info.Identity);

        if (!string.IsNullOrWhiteSpace(enumDisplayDeviceName))
            return (enumDisplayDeviceName, default);

        string name = devicePath.StartsWith(@"\\.\", StringComparison.Ordinal)
            ? devicePath[4..]
            : devicePath;
        return (name, default);
    }
}
