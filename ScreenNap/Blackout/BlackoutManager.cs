using System.Globalization;
using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Notifications;

namespace ScreenNap.Blackout;

// Which monitors are blacked out and which should be restored. A live blackout belongs to a
// physical monitor, or to a display device path for a monitor without a physical identity,
// so a path that moves to another monitor never carries a blackout with it (specification
// INV-2, DISP-2, MON-7)
internal sealed class BlackoutManager
{
    private readonly IBlackoutWindowFactory _factory;
    private readonly IUserNotifier _notifier;
    private readonly CultureInfo _culture;
    private readonly List<IBlackoutWindow> _windows = [];
    private readonly HashSet<MonitorIdentity> _desired = [];

    internal BlackoutManager(IBlackoutWindowFactory factory, IUserNotifier notifier, CultureInfo culture)
    {
        _factory = factory;
        _notifier = notifier;
        _culture = culture;
    }

    internal int ActiveCount => _windows.Count;
    internal event Action? ActiveCountChanged;

    internal bool IsActive(MonitorInfo monitor) => WindowOf(monitor) is not null;

    internal void Toggle(MonitorInfo monitor)
    {
        if (WindowOf(monitor) is { } existing)
        {
            bool desiredRemoved = monitor.Identity is { } identity && _desired.Remove(identity);
            if (existing.Destroy())
            {
                Logger.Info($"Blackout toggled off: {monitor.FriendlyName} ({monitor.DevicePath})");
                return;
            }

            if (desiredRemoved)
                _desired.Add(monitor.Identity!.Value);
            _notifier.Notify(FailureNotices.BlackoutNotClosed(monitor.FriendlyName, _culture));
            return;
        }

        if (!TryCreate(monitor))
        {
            _notifier.Notify(FailureNotices.BlackoutNotCreated(monitor.FriendlyName, _culture));
            return;
        }

        if (monitor.Identity is { } desired)
            _desired.Add(desired);
        Logger.Info($"Blackout toggled on: {monitor.FriendlyName} ({monitor.DevicePath})");
        ActiveCountChanged?.Invoke();
    }

    // Clears every desired state, including that of disconnected monitors waiting for
    // restoration; a blackout that cannot be closed keeps only its own (specification BLK-4)
    internal void ReleaseAll()
    {
        if (_windows.Count > 0)
            Logger.Info($"Releasing all blackout windows ({_windows.Count})");

        _desired.Clear();

        int remaining = 0;
        foreach (IBlackoutWindow window in _windows.ToList())
        {
            if (window.Destroy())
                continue;

            remaining++;
            if (window.Identity is { } identity)
                _desired.Add(identity);
        }

        if (remaining > 0)
            _notifier.Notify(FailureNotices.ReleaseAllIncomplete(remaining, _culture));
    }

    // At exit the tray icon is already gone and ScreenNap ends either way, so a blackout that
    // cannot be closed is only logged; Windows removes the windows of the ended process
    // (specification LIFE-3, INV-5)
    internal void CloseAllAtExit()
    {
        _desired.Clear();

        int remaining = 0;
        foreach (IBlackoutWindow window in _windows.ToList())
        {
            if (!window.Destroy())
                remaining++;
        }

        if (remaining > 0)
            Logger.Warn($"{remaining} blackout window(s) could not be closed at exit");
    }

    // Closes the blackouts of monitors that are gone, fits the others to their monitors'
    // current bounds, and restores desired monitors that are present (specification DISP-2 to DISP-7)
    internal void Reconcile(IReadOnlyList<MonitorInfo> currentMonitors)
    {
        Logger.Info($"Reconciling display change: {_desired.Count} desired, {currentMonitors.Count} monitors, {_windows.Count} live windows");

        foreach (IBlackoutWindow window in _windows.ToList())
        {
            MonitorInfo? monitor = currentMonitors.FirstOrDefault(m => Covers(window, m));
            if (monitor is not null)
            {
                window.MoveTo(monitor.Bounds);
                continue;
            }

            // A failed close is logged by the window and tried again at the next reconciliation
            Logger.Info($"Closing the blackout of a monitor no longer present: {window.DevicePath}");
            window.Destroy();
        }

        int restored = 0;
        foreach (MonitorIdentity desired in _desired)
        {
            if (_windows.Any(window => window.Identity == desired))
                continue;

            MonitorInfo? monitor = currentMonitors.FirstOrDefault(m => m.Identity == desired);
            if (monitor is null)
                continue;

            Logger.Info($"Restoring blackout: {monitor.FriendlyName} ({monitor.DevicePath})");
            if (TryCreate(monitor))
                restored++;
            else
                _notifier.Notify(FailureNotices.BlackoutNotRestored(monitor.FriendlyName, _culture));
        }

        if (restored > 0)
            ActiveCountChanged?.Invoke();
    }

    private bool TryCreate(MonitorInfo monitor)
    {
        IBlackoutWindow? window = _factory.Create(monitor);
        if (window is null)
            return false;

        window.OnDestroyed = OnBlackoutDestroyed;
        _windows.Add(window);
        return true;
    }

    private IBlackoutWindow? WindowOf(MonitorInfo monitor) => _windows.Find(window => Covers(window, monitor));

    private static bool Covers(IBlackoutWindow window, MonitorInfo monitor) => monitor.Denotes(window.Identity, window.DevicePath);

    private void OnBlackoutDestroyed(IBlackoutWindow window)
    {
        _windows.Remove(window);

        if (window.UserDismissed && window.Identity is { } identity)
            _desired.Remove(identity);

        ActiveCountChanged?.Invoke();
    }
}
