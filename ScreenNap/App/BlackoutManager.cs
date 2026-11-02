using ScreenNap.Core;
using ScreenNap.Logging;

namespace ScreenNap.App;

internal sealed class BlackoutManager
{
    private readonly IBlackoutWindowFactory _factory;
    private readonly IUserNotifier _notifier;
    private readonly Dictionary<string, IBlackoutWindow> _windows = new(StringComparer.Ordinal);
    private readonly HashSet<MonitorIdentity> _desired = [];

    internal BlackoutManager(IBlackoutWindowFactory factory, IUserNotifier notifier)
    {
        _factory = factory;
        _notifier = notifier;
    }

    internal int ActiveCount => _windows.Count;
    internal IReadOnlySet<string> ActiveDevicePaths => _windows.Keys.ToHashSet(StringComparer.Ordinal);
    internal event Action? ActiveCountChanged;

    internal void Toggle(MonitorInfo monitor)
    {
        if (_windows.TryGetValue(monitor.DevicePath, out IBlackoutWindow? existing))
        {
            bool desiredRemoved = _desired.Remove(monitor.Identity);
            if (existing.Destroy())
            {
                Logger.Info($"Blackout toggled off: {monitor.FriendlyName} ({monitor.DevicePath})");
                return;
            }

            if (desiredRemoved)
                _desired.Add(monitor.Identity);
            _notifier.Notify(FailureNotices.BlackoutNotClosed(monitor.FriendlyName));
        }
        else
        {
            IBlackoutWindow? window = _factory.Create(monitor);
            if (window is null)
            {
                _notifier.Notify(FailureNotices.BlackoutNotCreated(monitor.FriendlyName));
                return;
            }

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

        int remaining = 0;
        List<string> paths = _windows.Keys.ToList();
        foreach (string path in paths)
        {
            if (_windows.TryGetValue(path, out IBlackoutWindow? window) && !window.Destroy())
            {
                remaining++;
                if (window.Identity != default)
                    _desired.Add(window.Identity);
            }
        }

        if (remaining > 0)
            _notifier.Notify(FailureNotices.ReleaseAllIncomplete(remaining));
    }

    internal void Reconcile(IReadOnlyList<MonitorInfo> currentMonitors)
    {
        List<string> staleKeys = _windows
            .Where(entry => !entry.Value.IsAlive)
            .Select(entry => entry.Key)
            .ToList();
        foreach (string key in staleKeys)
        {
            Logger.Warn($"Stale blackout window handle detected: {key}");
            _windows.Remove(key);
        }

        int restored = _desired.Count > 0 ? RestoreDesired(currentMonitors) : 0;

        if (staleKeys.Count > 0 || restored > 0)
            ActiveCountChanged?.Invoke();
    }

    private int RestoreDesired(IReadOnlyList<MonitorInfo> currentMonitors)
    {
        Logger.Info($"Reconciling display change: {_desired.Count} desired, {currentMonitors.Count} monitors, {_windows.Count} live windows");

        var monitorsByIdentity = new Dictionary<MonitorIdentity, MonitorInfo>();
        foreach (MonitorInfo m in currentMonitors)
        {
            if (m.Identity == default)
                continue;
            monitorsByIdentity[m.Identity] = m;
        }

        HashSet<MonitorIdentity> activeIdentities = _windows.Values.Select(w => w.Identity).ToHashSet();

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
            {
                _notifier.Notify(FailureNotices.BlackoutNotRestored(monitor.FriendlyName));
                continue;
            }

            window.OnDestroyed = OnBlackoutDestroyed;
            _windows[monitor.DevicePath] = window;
            restored++;
        }

        return restored;
    }

    private void OnBlackoutDestroyed(IBlackoutWindow window)
    {
        _windows.Remove(window.DevicePath);

        if (window.UserDismissed)
            _desired.Remove(window.Identity);

        ActiveCountChanged?.Invoke();
    }
}
