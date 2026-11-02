using ScreenNap.Core;

namespace ScreenNap.Tests.TestDoubles;

internal sealed class FakeBlackoutWindow : IBlackoutWindow
{
    internal FakeBlackoutWindow(string devicePath, MonitorIdentity identity)
    {
        DevicePath = devicePath;
        Identity = identity;
    }

    public string DevicePath { get; }
    public MonitorIdentity Identity { get; }
    public bool UserDismissed { get; internal set; }
    public bool IsAlive { get; internal set; } = true;
    internal bool DestroySucceeds { get; set; } = true;
    public Action<IBlackoutWindow>? OnDestroyed { get; set; }
    internal int DestroyCalls { get; private set; }

    public bool Destroy()
    {
        DestroyCalls++;
        if (!DestroySucceeds)
            return false;

        IsAlive = false;
        OnDestroyed?.Invoke(this);
        return true;
    }
}
