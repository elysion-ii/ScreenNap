using ScreenNap.Monitors;

namespace ScreenNap.Blackout;

internal interface IBlackoutWindow
{
    string DevicePath { get; }
    MonitorIdentity Identity { get; }
    bool UserDismissed { get; }
    bool IsAlive { get; }
    Action<IBlackoutWindow>? OnDestroyed { get; set; }
    bool Destroy();
}
