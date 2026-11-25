using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.Monitors;

internal sealed class MonitorEnumerator
{
    private readonly IMonitorApi _api;

    internal MonitorEnumerator(IMonitorApi api)
    {
        _api = api;
    }

    internal IReadOnlyList<MonitorInfo> EnumerateMonitors()
    {
        var monitors = new List<MonitorInfo>();
        Dictionary<string, DisplayTarget> targets = _api.QueryDisplayTargets();

        bool enumerated = _api.EnumDisplayMonitors(hMonitor =>
        {
            // The monitors after this one take the following numbers (specification MON-5)
            if (!_api.GetMonitorInfo(hMonitor, out RECT bounds, out uint flags, out string devicePath))
            {
                Logger.Warn("GetMonitorInfoW failed; skipping the monitor", _api.LastError);
                return true;
            }

            bool isPrimary = (flags & WindowStyles.MONITORINFOF_PRIMARY) != 0;
            DisplayTarget? target = targets.GetValueOrDefault(devicePath);
            string friendlyName = MonitorNameResolver.Resolve(
                devicePath,
                target?.FriendlyName,
                string.IsNullOrWhiteSpace(target?.FriendlyName) ? _api.GetDisplayDeviceName(devicePath) : null);
            // The identity does not depend on whether Windows reports a friendly name (specification MON-7)
            MonitorIdentity? identity = target is null ? null : MonitorIdentity.For(target.Edid, target.Connection);

            monitors.Add(new MonitorInfo(devicePath, friendlyName, bounds, isPrimary, identity));
            return true;
        });
        // The monitors found before the failure are used (specification MON-6)
        if (!enumerated)
            Logger.Warn("EnumDisplayMonitors failed; using the monitors found before the failure", _api.LastError);

        Logger.Info($"Monitors enumerated: {monitors.Count} found");
        for (int i = 0; i < monitors.Count; i++)
        {
            MonitorInfo m = monitors[i];
            string primary = m.IsPrimary ? " Primary" : "";
            Logger.Info($"  #{i + 1} \"{m.FriendlyName}\" ({m.Bounds.Left},{m.Bounds.Top} {m.Bounds.Width}x{m.Bounds.Height}){primary} {m.DevicePath} [{m.Identity?.Value ?? "no identity"}]");
        }

        return monitors;
    }
}
