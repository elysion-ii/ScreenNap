using ScreenNap.App;
using ScreenNap.Core;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.App;

public sealed class BlackoutManagerTests
{
    [Fact]
    public void Toggle_CreatesWindowAndRaisesEvent()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.Toggle(Monitor("DISPLAY1", Identity(1)));

        Assert.Equal(1, manager.ActiveCount);
        Assert.True(manager.IsActive("DISPLAY1"));
        Assert.Single(factory.Requests);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Toggle_ExistingWindowDestroysAndRemovesIt()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);

        manager.Toggle(monitor);

        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(1, factory.Windows[0].DestroyCalls);
    }

    [Fact]
    public void Toggle_SuccessClearsDesiredBeforeChangeEvent()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        manager.ActiveCountChanged += () => manager.Reconcile([monitor]);

        manager.Toggle(monitor);

        Assert.Single(factory.Requests);
        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Toggle_DestroyFailurePreservesActiveAndDesiredState()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        FakeBlackoutWindow window = factory.Windows[0];
        window.DestroySucceeds = false;
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.Toggle(monitor);

        Assert.Equal(1, manager.ActiveCount);
        Assert.True(manager.IsActive("DISPLAY1"));
        Assert.Equal(0, changes);

        window.IsAlive = false;
        manager.Reconcile([monitor]);
        Assert.Equal(2, factory.Requests.Count);
    }

    [Fact]
    public void Toggle_FactoryFailureLeavesStateUnchanged()
    {
        var factory = new FakeBlackoutWindowFactory();
        factory.ReturnFailure();
        var manager = new BlackoutManager(factory);
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.Toggle(Monitor("DISPLAY1", Identity(1)));

        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(0, changes);
        manager.Reconcile([Monitor("DISPLAY1", Identity(1))]);
        Assert.Single(factory.Requests);
    }

    [Fact]
    public void Toggle_DefaultIdentityIsNotRestoredAfterOsDismissal()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        manager.Toggle(Monitor("DISPLAY1", default));
        factory.Windows[0].Destroy();

        manager.Reconcile([Monitor("DISPLAY1", default)]);

        Assert.Single(factory.Requests);
    }

    [Fact]
    public void ReleaseAll_WithNoWindowsDoesNothing()
    {
        var manager = new BlackoutManager(new FakeBlackoutWindowFactory());
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.ReleaseAll();

        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void ReleaseAll_DestroysEveryWindow()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        manager.Toggle(Monitor("DISPLAY1", Identity(1)));
        manager.Toggle(Monitor("DISPLAY2", Identity(2)));

        manager.ReleaseAll();

        Assert.Equal(0, manager.ActiveCount);
        Assert.All(factory.Windows, window => Assert.Equal(1, window.DestroyCalls));
    }

    [Fact]
    public void ReleaseAll_DestroyFailurePreservesOnlyFailedWindowDesiredState()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo failedMonitor = Monitor("DISPLAY1", Identity(1));
        MonitorInfo releasedMonitor = Monitor("DISPLAY2", Identity(2));
        manager.Toggle(failedMonitor);
        manager.Toggle(releasedMonitor);
        factory.Windows[0].DestroySucceeds = false;

        manager.ReleaseAll();

        Assert.Equal(1, manager.ActiveCount);
        Assert.True(manager.IsActive("DISPLAY1"));
        Assert.False(manager.IsActive("DISPLAY2"));
        Assert.All(factory.Windows, window => Assert.Equal(1, window.DestroyCalls));

        factory.Windows[0].IsAlive = false;
        manager.Reconcile([failedMonitor, releasedMonitor]);

        Assert.Equal(3, factory.Requests.Count);
        Assert.Equal("DISPLAY1", factory.Requests[2].DevicePath);
    }

    [Fact]
    public void ReleaseAll_ClearsDesiredState()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        manager.ReleaseAll();

        manager.Reconcile([monitor]);

        Assert.Single(factory.Requests);
    }

    [Fact]
    public void Reconcile_WithNoDesiredStateDoesNothing()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);

        manager.Reconcile([Monitor("DISPLAY1", Identity(1))]);

        Assert.Empty(factory.Requests);
    }

    [Fact]
    public void Reconcile_RemovesStaleWindowAndRestoresDesiredMonitor()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].IsAlive = false;

        manager.Reconcile([monitor]);

        Assert.Equal(2, factory.Requests.Count);
        Assert.Equal(1, manager.ActiveCount);
    }

    [Fact]
    public void Reconcile_DisconnectedMonitorIsRestoredWhenReconnected()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo oldMonitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(oldMonitor);
        factory.Windows[0].Destroy();
        MonitorInfo reconnected = Monitor("DISPLAY9", Identity(1));

        manager.Reconcile([reconnected]);

        Assert.True(manager.IsActive("DISPLAY9"));
        Assert.Equal(2, factory.Requests.Count);
    }

    [Fact]
    public void Reconcile_LiveWindowIsNotRecreated()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);

        manager.Reconcile([monitor]);

        Assert.Single(factory.Requests);
    }

    [Fact]
    public void Reconcile_UndesiredMonitorIsIgnored()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo desired = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(desired);
        factory.Windows[0].Destroy();

        manager.Reconcile([Monitor("DISPLAY2", Identity(2))]);

        Assert.Single(factory.Requests);
    }

    [Fact]
    public void Reconcile_FactoryFailureIsSkipped()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].Destroy();
        factory.ReturnFailure();

        manager.Reconcile([monitor]);

        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Reconcile_RaisesEventOnlyWhenStateChanges()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.Reconcile([monitor]);
        factory.Windows[0].IsAlive = false;
        manager.Reconcile([monitor]);

        Assert.Equal(1, changes);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    public void DestroyedWindow_UserDismissalControlsRestoration(bool userDismissed, int expectedRequests)
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].UserDismissed = userDismissed;
        factory.Windows[0].Destroy();

        manager.Reconcile([monitor]);

        Assert.Equal(expectedRequests, factory.Requests.Count);
    }

    private static MonitorInfo Monitor(string path, MonitorIdentity identity)
        => new(path, path, new RECT { Right = 1920, Bottom = 1080 }, false, identity);

    private static MonitorIdentity Identity(ushort value) => new(value, value, value);
}
