using ScreenNap.Core;
using ScreenNap.Logging;

namespace ScreenNap.App;

internal sealed class BlackoutManager
{
    private readonly IBlackoutWindowFactory _factory;
    private readonly Dictionary<string, IBlackoutWindow> _windows = new(StringComparer.Ordinal);
    private readonly HashSet<MonitorIdentity> _desired = [];

    internal BlackoutManager(IBlackoutWindowFactory factory)
    {
        _factory = factory;
    }

    internal int ActiveCount => _windows.Count;
    internal IReadOnlySet<string> ActiveDevicePaths => _windows.Keys.ToHashSet(StringComparer.Ordinal);
    internal event Action? ActiveCountChanged;

    internal bool IsActive(string devicePath)
    {
        return _windows.ContainsKey(devicePath);
    }

    internal void Toggle(MonitorInfo monitor)
    {
        if (_windows.TryGetValue(monitor.DevicePath, out IBlackoutWindow? existing))
        {
            bool desiredRemoved = _desired.Remove(monitor.Identity);
            if (existing.Destroy())
            {
                Logger.Info($"Blackout toggled off: {monitor.FriendlyName} ({monitor.DevicePath})");
            }
            else if (desiredRemoved)
            {
                _desired.Add(monitor.Identity);
            }
        }
        else
        {
            IBlackoutWindow? window = _factory.Create(monitor);
            if (window is null)
                return;

            if (monitor.Identity != default)
                _desired.Add(monitor.Identity);

            window.OnDestroyed = OnBlackoutDestroyed;
            _windows[monitor.DevicePath] = window;
            Logger.Info($"Blackout toggled on: {monitor.FriendlyName} ({monitor.DevicePath})");
            ActiveCountChanged?.Invoke();
        }
    }

    internal void ReleaseAll()
    {
        if (_windows.Count > 0)
            Logger.Info($"Releasing all blackout windows ({_windows.Count})");

        _desired.Clear();

        var paths = _windows.Keys.ToList();
        foreach (string path in paths)
        {
            if (_windows.TryGetValue(path, out IBlackoutWindow? window))
            {
                if (!window.Destroy() && window.Identity != default)
                    _desired.Add(window.Identity);
            }
        }
    }

    internal void Reconcile(IReadOnlyList<MonitorInfo> currentMonitors)
    {
        if (_desired.Count == 0)
            return;

        Logger.Info($"Reconciling display change: {_desired.Count} desired, {currentMonitors.Count} monitors, {_windows.Count} live windows");

        var staleKeys = new List<string>();
        foreach (var kvp in _windows)
        {
            if (!kvp.Value.IsAlive)
            {
                Logger.Warn($"Stale blackout window handle detected: {kvp.Key}");
                staleKeys.Add(kvp.Key);
            }
        }
        foreach (string key in staleKeys)
            _windows.Remove(key);

        var monitorsByIdentity = new Dictionary<MonitorIdentity, MonitorInfo>();
        foreach (MonitorInfo m in currentMonitors)
        {
            if (m.Identity == default)
                continue;
            monitorsByIdentity[m.Identity] = m;
        }

        var activeIdentities = new HashSet<MonitorIdentity>();
        foreach (IBlackoutWindow w in _windows.Values)
            activeIdentities.Add(w.Identity);

        int restored = 0;
        foreach (MonitorIdentity desired in _desired)
        {
            if (activeIdentities.Contains(desired))
                continue;

            if (!monitorsByIdentity.TryGetValue(desired, out MonitorInfo? monitor))
                continue;

            Logger.Info($"Restoring blackout: {monitor.FriendlyName} ({monitor.DevicePath})");
            IBlackoutWindow? window = _factory.Create(monitor);
            if (window is null)
                continue;

            window.OnDestroyed = OnBlackoutDestroyed;
            _windows[monitor.DevicePath] = window;
            restored++;
        }

        if (staleKeys.Count > 0 || restored > 0)
            ActiveCountChanged?.Invoke();
    }

    private void OnBlackoutDestroyed(IBlackoutWindow window)
    {
        _windows.Remove(window.DevicePath);

        if (window.UserDismissed)
            _desired.Remove(window.Identity);

        ActiveCountChanged?.Invoke();
    }
}
