using ScreenNap.Monitors;

namespace ScreenNap.Blackout;

internal interface IBlackoutWindowFactory
{
    IBlackoutWindow? Create(MonitorInfo monitor);
}
