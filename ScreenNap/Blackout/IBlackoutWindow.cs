using ScreenNap.Monitors;
using ScreenNap.Native;

namespace ScreenNap.Blackout;

internal interface IBlackoutWindow
{
    string DevicePath { get; }
    MonitorIdentity? Identity { get; }
    bool UserDismissed { get; }
    Action<IBlackoutWindow>? OnDestroyed { get; set; }
    bool Destroy();

    // Fits the window to the bounds without changing its place in the z-order or activating it;
    // a failure is logged and the window keeps its bounds (specification DISP-7)
    void MoveTo(RECT bounds);
}
