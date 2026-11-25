using ScreenNap.Blackout;
using ScreenNap.Monitors;

namespace ScreenNap.Tests.Blackout;

internal sealed class FakeBlackoutWindowFactory : IBlackoutWindowFactory
{
    private readonly Queue<bool> _results = [];

    internal List<MonitorInfo> Requests { get; } = [];
    internal List<FakeBlackoutWindow> Windows { get; } = [];

    internal void ReturnFailure() => _results.Enqueue(false);

    public IBlackoutWindow? Create(MonitorInfo monitor)
    {
        Requests.Add(monitor);
        if (_results.Count > 0 && !_results.Dequeue())
            return null;

        var window = new FakeBlackoutWindow(monitor.DevicePath, monitor.Identity, monitor.Bounds);
        Windows.Add(window);
        return window;
    }
}
