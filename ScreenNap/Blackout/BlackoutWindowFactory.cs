using ScreenNap.Core;

namespace ScreenNap.Blackout;

internal sealed class BlackoutWindowFactory : IBlackoutWindowFactory
{
    public IBlackoutWindow? Create(MonitorInfo monitor)
    {
        var window = new BlackoutWindow(monitor.DevicePath, monitor.Bounds, monitor.Identity);
        return window.Handle == IntPtr.Zero ? null : window;
    }
}
