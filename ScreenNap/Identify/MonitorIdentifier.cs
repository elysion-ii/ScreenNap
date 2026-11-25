using System.Globalization;
using ScreenNap.Monitors;
using ScreenNap.Notifications;

namespace ScreenNap.Identify;

internal sealed class MonitorIdentifier
{
    private readonly IIdentifyOverlays _overlays;
    private readonly IUserNotifier _notifier;
    private readonly Func<IReadOnlyList<MonitorInfo>> _enumerateMonitors;
    private readonly CultureInfo _culture;

    internal MonitorIdentifier(IIdentifyOverlays overlays, IUserNotifier notifier, Func<IReadOnlyList<MonitorInfo>> enumerateMonitors, CultureInfo culture)
    {
        _overlays = overlays;
        _notifier = notifier;
        _enumerateMonitors = enumerateMonitors;
        _culture = culture;
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
        bool closingTimerStarted = false;
        for (int i = 0; i < monitors.Count; i++)
        {
            int displayNumber = i + 1;
            // One timer closes every overlay; each overlay offers to start it until one has (specification IDN-3)
            OverlayShowResult result = _overlays.Show(displayNumber, monitors[i].Bounds, startsClosingTimer: !closingTimerStarted);
            // An overlay without its number shows the user nothing, as a missing overlay does (specification ERR-8)
            if (!result.Shown || !result.NumberDrawn)
                failedNumbers.Add(displayNumber);
            if (result.StartedClosingTimer)
                closingTimerStarted = true;
        }

        if (failedNumbers.Count > 0)
            _notifier.Notify(FailureNotices.OverlaysNotCreated(failedNumbers, _culture));
    }
}
