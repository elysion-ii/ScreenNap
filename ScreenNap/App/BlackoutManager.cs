using ScreenNap.Blackout;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal sealed class BlackoutManager
{
    private readonly Dictionary<string, BlackoutWindow> _windows = new(StringComparer.Ordinal);
    private readonly HashSet<MonitorIdentity> _desired = [];

    internal int ActiveCount => _windows.Count;
    internal event Action? ActiveCountChanged;

    internal bool IsActive(string devicePath)
    {
        return _windows.ContainsKey(devicePath);
    }

    internal void Toggle(MonitorInfo monitor)
    {
        if (_windows.TryGetValue(monitor.DevicePath, out BlackoutWindow? existing))
        {
            _desired.Remove(monitor.Identity);
            Logger.Info($"Blackout toggled off: {monitor.FriendlyName} ({monitor.DevicePath})");
            existing.Destroy();
            // Removal from _windows happens in OnBlackoutDestroyed callback
        }
        else
        {
            if (monitor.Identity != default)
                _desired.Add(monitor.Identity);

            Logger.Info($"Blackout toggled on: {monitor.FriendlyName} ({monitor.DevicePath})");
            var window = new BlackoutWindow(monitor.DevicePath, monitor.Bounds, monitor.Identity);
            if (window.Handle == IntPtr.Zero)
                return;

            window.OnDestroyed = OnBlackoutDestroyed;
            _windows[monitor.DevicePath] = window;
            ActiveCountChanged?.Invoke();
        }
    }

    internal void ReleaseAll()
    {
        if (_windows.Count > 0)
            Logger.Info($"Releasing all blackout windows ({_windows.Count})");

        _desired.Clear();

        // Snapshot keys to avoid modification during enumeration
        var paths = _windows.Keys.ToList();
        foreach (string path in paths)
        {
            if (_windows.TryGetValue(path, out BlackoutWindow? window))
                window.Destroy();
        }
    }

    internal void Reconcile(List<MonitorInfo> currentMonitors)
    {
        if (_desired.Count == 0)
            return;

        Logger.Info($"Reconciling display change: {_desired.Count} desired, {currentMonitors.Count} monitors, {_windows.Count} live windows");

        // Clean up stale window handles
        var staleKeys = new List<string>();
        foreach (var kvp in _windows)
        {
            if (!User32.IsWindow(kvp.Value.Handle))
            {
                Logger.Warn($"Stale blackout window handle detected: {kvp.Key}");
                staleKeys.Add(kvp.Key);
            }
        }
        foreach (string key in staleKeys)
            _windows.Remove(key);

        // Build lookup of currently-attached monitors by identity
        var monitorsByIdentity = new Dictionary<MonitorIdentity, MonitorInfo>();
        foreach (MonitorInfo m in currentMonitors)
        {
            if (m.Identity == default)
                continue;
            monitorsByIdentity[m.Identity] = m;
        }

        // Collect identities that already have live windows
        var activeIdentities = new HashSet<MonitorIdentity>();
        foreach (BlackoutWindow w in _windows.Values)
            activeIdentities.Add(w.Identity);

        // Restore blackout windows for desired monitors that reappeared
        int restored = 0;
        foreach (MonitorIdentity desired in _desired)
        {
            if (activeIdentities.Contains(desired))
                continue;

            if (!monitorsByIdentity.TryGetValue(desired, out MonitorInfo? monitor))
                continue;

            Logger.Info($"Restoring blackout: {monitor.FriendlyName} ({monitor.DevicePath})");
            var window = new BlackoutWindow(monitor.DevicePath, monitor.Bounds, monitor.Identity);
            if (window.Handle == IntPtr.Zero)
                continue;

            window.OnDestroyed = OnBlackoutDestroyed;
            _windows[monitor.DevicePath] = window;
            restored++;
        }

        if (staleKeys.Count > 0 || restored > 0)
            ActiveCountChanged?.Invoke();
    }

    private void OnBlackoutDestroyed(BlackoutWindow window)
    {
        _windows.Remove(window.DevicePath);

        // User-initiated dismissal: remove from desired set
        if (window.UserDismissed)
            _desired.Remove(window.Identity);

        ActiveCountChanged?.Invoke();
    }
}
