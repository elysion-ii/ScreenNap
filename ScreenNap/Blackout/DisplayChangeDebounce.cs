using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;

namespace ScreenNap.Blackout;

// Waits for a burst of display changes to settle before reconciling (specification DISP-1)
internal sealed class DisplayChangeDebounce
{
    internal const nuint TimerId = 4;
    private const uint DelayMs = 500;

    private readonly IWindowApi _api;
    private readonly BlackoutManager _manager;
    private readonly Func<IReadOnlyList<MonitorInfo>> _enumerateMonitors;

    internal DisplayChangeDebounce(IWindowApi api, BlackoutManager manager, Func<IReadOnlyList<MonitorInfo>> enumerateMonitors)
    {
        _api = api;
        _manager = manager;
        _enumerateMonitors = enumerateMonitors;
    }

    // Setting the timer again restarts its delay, so only the last change of a burst reconciles
    internal void OnDisplayChange(IntPtr hwnd)
    {
        if (!_api.SetTimer(hwnd, TimerId, DelayMs))
            Logger.Warn("SetTimer failed for display-change debounce; this display change is not reconciled", _api.LastError);
    }

    internal void OnTimer(IntPtr hwnd)
    {
        if (!_api.KillTimer(hwnd, TimerId))
            Logger.Warn("KillTimer failed for display-change debounce", _api.LastError);
        Logger.Info("Display configuration changed (debounced)");
        _manager.Reconcile(_enumerateMonitors());
    }
}
