using ScreenNap.Core;

namespace ScreenNap.App;

internal sealed class MonitorIdentifier
{
    private readonly IIdentifyOverlays _overlays;
    private readonly IUserNotifier _notifier;
    private readonly Func<IReadOnlyList<MonitorInfo>> _enumerateMonitors;

    internal MonitorIdentifier(IIdentifyOverlays overlays, IUserNotifier notifier, Func<IReadOnlyList<MonitorInfo>> enumerateMonitors)
    {
        _overlays = overlays;
        _notifier = notifier;
        _enumerateMonitors = enumerateMonitors;
    }

    internal void Toggle()
    {
        if (_overlays.AnyShown)
        {
            _overlays.DismissAll();
            return;
        }

        IReadOnlyList<MonitorInfo> monitors = _enumerateMonitors();
        var failedNumbers = new List<int>();
        for (int i = 0; i < monitors.Count; i++)
        {
            int displayNumber = i + 1;
            // Only monitor 1's overlay closes every overlay after the delay (specification IDN-3)
            if (!_overlays.Show(displayNumber, monitors[i].Bounds, closesAllAfterDelay: displayNumber == 1))
                failedNumbers.Add(displayNumber);
        }

        if (failedNumbers.Count > 0)
            _notifier.Notify(FailureNotices.OverlaysNotCreated(failedNumbers));
    }
}
