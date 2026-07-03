using ScreenNap.Core;

namespace ScreenNap.Tests.TestDoubles;

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

        var window = new FakeBlackoutWindow(monitor.DevicePath, monitor.Identity);
        Windows.Add(window);
        return window;
    }
}
