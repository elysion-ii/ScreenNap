using ScreenNap.Blackout;
using ScreenNap.Monitors;
using ScreenNap.Native;

namespace ScreenNap.Tests.Blackout;

internal sealed class FakeBlackoutWindow : IBlackoutWindow
{
    internal FakeBlackoutWindow(string devicePath, MonitorIdentity? identity, RECT bounds)
    {
        DevicePath = devicePath;
        Identity = identity;
        Bounds = bounds;
    }

    public string DevicePath { get; }
    public MonitorIdentity? Identity { get; }
    public bool UserDismissed { get; private set; }
    public Action<IBlackoutWindow>? OnDestroyed { get; set; }
    internal RECT Bounds { get; private set; }
    internal bool IsOpen { get; private set; } = true;
    internal bool DestroySucceeds { get; set; } = true;
    internal int DestroyCalls { get; private set; }

    public bool Destroy()
    {
        DestroyCalls++;
        if (!DestroySucceeds)
            return false;

        IsOpen = false;
        OnDestroyed?.Invoke(this);
        return true;
    }

    public void MoveTo(RECT bounds) => Bounds = bounds;

    // The user's double-click or right-click on the window (specification WIN-4)
    internal void DismissByUser()
    {
        UserDismissed = true;
        Destroy();
    }
}
