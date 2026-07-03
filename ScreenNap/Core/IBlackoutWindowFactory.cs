namespace ScreenNap.Core;

internal interface IBlackoutWindowFactory
{
    IBlackoutWindow? Create(MonitorInfo monitor);
}
