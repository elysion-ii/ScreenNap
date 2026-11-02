using ScreenNap.App;
using ScreenNap.Core;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.App;

public sealed class BlackoutManagerTests
{
    [Fact]
    [Trait("Requirement", "BLK-1")]
    [Trait("Requirement", "BLK-5")]
    [Trait("Requirement", "INV-3")]
    internal void Toggle_InactiveMonitor_CreatesWindowAndRaisesEvent()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.Toggle(Monitor("DISPLAY1", Identity(1)));

        Assert.Equal(1, manager.ActiveCount);
        Assert.Contains("DISPLAY1", manager.ActiveDevicePaths);
        Assert.Single(factory.Requests);
        Assert.Equal(1, changes);
    }

    [Fact]
    [Trait("Requirement", "BLK-2")]
    internal void Toggle_ActiveMonitor_DestroysAndRemovesWindow()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);

        manager.Toggle(monitor);

        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(1, factory.Windows[0].DestroyCalls);
    }

    [Fact]
    [Trait("Requirement", "INV-2")]
    [Trait("Requirement", "DISP-6")]
    internal void Toggle_ReconcileInsideChangeEvent_DoesNotRecreateWindow()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        manager.ActiveCountChanged += () => manager.Reconcile([monitor]);

        manager.Toggle(monitor);

        Assert.Single(factory.Requests);
        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    [Trait("Requirement", "BLK-2")]
    [Trait("Requirement", "ERR-4")]
    internal void Toggle_DestroyFailure_PreservesActiveAndDesiredState()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        FakeBlackoutWindow window = factory.Windows[0];
        window.DestroySucceeds = false;
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.Toggle(monitor);

        Assert.Equal(1, manager.ActiveCount);
        Assert.Contains("DISPLAY1", manager.ActiveDevicePaths);
        Assert.Equal(0, changes);

        window.IsAlive = false;
        manager.Reconcile([monitor]);
        Assert.Equal(2, factory.Requests.Count);
    }

    [Fact]
    [Trait("Requirement", "BLK-1")]
    [Trait("Requirement", "ERR-3")]
    internal void Toggle_FactoryFailure_LeavesStateUnchanged()
    {
        var factory = new FakeBlackoutWindowFactory();
        factory.ReturnFailure();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.Toggle(Monitor("DISPLAY1", Identity(1)));

        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(0, changes);
        manager.Reconcile([Monitor("DISPLAY1", Identity(1))]);
        Assert.Single(factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "DISP-5")]
    [Trait("Requirement", "INV-4")]
    internal void Toggle_DefaultIdentityAfterOsDismissal_DoesNotRestore()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        manager.Toggle(Monitor("DISPLAY1", default));
        factory.Windows[0].Destroy();

        manager.Reconcile([Monitor("DISPLAY1", default)]);

        Assert.Single(factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "BLK-4")]
    internal void ReleaseAll_NoWindows_DoesNothing()
    {
        var manager = new BlackoutManager(new FakeBlackoutWindowFactory(), new FakeUserNotifier());
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.ReleaseAll();

        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(0, changes);
    }

    [Fact]
    [Trait("Requirement", "BLK-3")]
    [Trait("Requirement", "BLK-4")]
    internal void ReleaseAll_MultipleWindows_DestroysEveryWindow()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        manager.Toggle(Monitor("DISPLAY1", Identity(1)));
        manager.Toggle(Monitor("DISPLAY2", Identity(2)));

        manager.ReleaseAll();

        Assert.Equal(0, manager.ActiveCount);
        Assert.All(factory.Windows, window => Assert.Equal(1, window.DestroyCalls));
    }

    [Fact]
    [Trait("Requirement", "BLK-4")]
    [Trait("Requirement", "ERR-5")]
    internal void ReleaseAll_DestroyFailure_PreservesOnlyFailedWindowDesiredState()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo failedMonitor = Monitor("DISPLAY1", Identity(1));
        MonitorInfo releasedMonitor = Monitor("DISPLAY2", Identity(2));
        manager.Toggle(failedMonitor);
        manager.Toggle(releasedMonitor);
        factory.Windows[0].DestroySucceeds = false;

        manager.ReleaseAll();

        Assert.Equal(1, manager.ActiveCount);
        Assert.Contains("DISPLAY1", manager.ActiveDevicePaths);
        Assert.DoesNotContain("DISPLAY2", manager.ActiveDevicePaths);
        Assert.All(factory.Windows, window => Assert.Equal(1, window.DestroyCalls));

        factory.Windows[0].IsAlive = false;
        manager.Reconcile([failedMonitor, releasedMonitor]);

        Assert.Equal(3, factory.Requests.Count);
        Assert.Equal("DISPLAY1", factory.Requests[2].DevicePath);
    }

    [Fact]
    [Trait("Requirement", "BLK-4")]
    internal void ReleaseAll_DestroySucceeds_ClearsDesiredState()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        manager.ReleaseAll();

        manager.Reconcile([monitor]);

        Assert.Single(factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "DISP-6")]
    internal void Reconcile_NoDesiredState_DoesNothing()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());

        manager.Reconcile([Monitor("DISPLAY1", Identity(1))]);

        Assert.Empty(factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "DISP-6")]
    [Trait("Requirement", "INV-3")]
    internal void Reconcile_StaleWindowWithNothingDesired_RemovesItAndRaisesEvent()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", default);
        manager.Toggle(monitor);
        factory.Windows[0].IsAlive = false;
        int changes = 0;
        manager.ActiveCountChanged += () => changes++;

        manager.Reconcile([monitor]);

        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(1, changes);
        Assert.Single(factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "DISP-2")]
    [Trait("Requirement", "DISP-6")]
    internal void Reconcile_StaleWindow_RemovesItAndRestoresDesiredMonitor()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].IsAlive = false;

        manager.Reconcile([monitor]);

        Assert.Equal(2, factory.Requests.Count);
        Assert.Equal(1, manager.ActiveCount);
    }

    [Fact]
    [Trait("Requirement", "DISP-3")]
    internal void Reconcile_ReconnectedMonitorOnNewPath_RestoresBlackout()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo oldMonitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(oldMonitor);
        factory.Windows[0].Destroy();
        MonitorInfo reconnected = Monitor("DISPLAY9", Identity(1));

        manager.Reconcile([reconnected]);

        Assert.Contains("DISPLAY9", manager.ActiveDevicePaths);
        Assert.Equal(2, factory.Requests.Count);
    }

    [Fact]
    [Trait("Requirement", "DISP-6")]
    [Trait("Requirement", "INV-2")]
    internal void Reconcile_LiveWindow_DoesNotRecreateIt()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);

        manager.Reconcile([monitor]);

        Assert.Single(factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "DISP-6")]
    internal void Reconcile_UndesiredMonitor_IsIgnored()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo desired = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(desired);
        factory.Windows[0].Destroy();

        manager.Reconcile([Monitor("DISPLAY2", Identity(2))]);

        Assert.Single(factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "ERR-6")]
    internal void Reconcile_FactoryFailure_LeavesMonitorInactive()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].Destroy();
        factory.ReturnFailure();

        manager.Reconcile([monitor]);

        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    [Trait("Requirement", "BLK-5")]
    [Trait("Requirement", "INV-3")]
    internal void Reconcile_RepeatedCalls_RaisesEventOnlyWhenCountChanges()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
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
    [Trait("Requirement", "DISP-2")]
    [Trait("Requirement", "WIN-4")]
    [Trait("Requirement", "INV-4")]
    internal void Reconcile_DestroyedWindow_RestoresOnlyWhenNotUserDismissed(bool userDismissed, int expectedRequests)
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].UserDismissed = userDismissed;
        factory.Windows[0].Destroy();

        manager.Reconcile([monitor]);

        Assert.Equal(expectedRequests, factory.Requests.Count);
    }

    [Fact]
    [Trait("Requirement", "DISP-4")]
    internal void Reconcile_DisconnectedDesiredMonitor_IsRestoredOnReconnection()
    {
        var factory = new FakeBlackoutWindowFactory();
        var manager = new BlackoutManager(factory, new FakeUserNotifier());
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].Destroy();

        manager.Reconcile([]);
        manager.Reconcile([monitor]);

        Assert.Equal(2, factory.Requests.Count);
        Assert.Contains("DISPLAY1", manager.ActiveDevicePaths);
    }

    [Fact]
    [Trait("Requirement", "ERR-3")]
    internal void Toggle_InactiveMonitor_DoesNotNotify()
    {
        var notifier = new FakeUserNotifier();
        var manager = new BlackoutManager(new FakeBlackoutWindowFactory(), notifier);

        manager.Toggle(Monitor("DISPLAY1", Identity(1)));

        Assert.Empty(notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "ERR-3")]
    internal void Toggle_FactoryFailure_NotifiesTheMonitor()
    {
        using var scope = new CultureScope("en-US");
        var factory = new FakeBlackoutWindowFactory();
        factory.ReturnFailure();
        var notifier = new FakeUserNotifier();
        var manager = new BlackoutManager(factory, notifier);

        manager.Toggle(Monitor("DISPLAY1", Identity(1)));

        Assert.Equal(["Could not black out DISPLAY1."], notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "ERR-4")]
    internal void Toggle_DestroyFailure_NotifiesTheMonitor()
    {
        using var scope = new CultureScope("en-US");
        var factory = new FakeBlackoutWindowFactory();
        var notifier = new FakeUserNotifier();
        var manager = new BlackoutManager(factory, notifier);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].DestroySucceeds = false;

        manager.Toggle(monitor);

        Assert.Equal(["Could not release the blackout on DISPLAY1."], notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "ERR-5")]
    internal void ReleaseAll_DestroyFailures_NotifiesTheRemainingCount()
    {
        using var scope = new CultureScope("en-US");
        var factory = new FakeBlackoutWindowFactory();
        var notifier = new FakeUserNotifier();
        var manager = new BlackoutManager(factory, notifier);
        manager.Toggle(Monitor("DISPLAY1", Identity(1)));
        manager.Toggle(Monitor("DISPLAY2", Identity(2)));
        manager.Toggle(Monitor("DISPLAY3", Identity(3)));
        factory.Windows[0].DestroySucceeds = false;
        factory.Windows[2].DestroySucceeds = false;

        manager.ReleaseAll();

        Assert.Equal(["2 blackout(s) could not be released."], notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "ERR-5")]
    internal void ReleaseAll_EveryWindowCloses_DoesNotNotify()
    {
        var factory = new FakeBlackoutWindowFactory();
        var notifier = new FakeUserNotifier();
        var manager = new BlackoutManager(factory, notifier);
        manager.Toggle(Monitor("DISPLAY1", Identity(1)));

        manager.ReleaseAll();

        Assert.Empty(notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "ERR-6")]
    internal void Reconcile_FactoryFailure_NotifiesTheMonitor()
    {
        using var scope = new CultureScope("en-US");
        var factory = new FakeBlackoutWindowFactory();
        var notifier = new FakeUserNotifier();
        var manager = new BlackoutManager(factory, notifier);
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        manager.Toggle(monitor);
        factory.Windows[0].Destroy();
        factory.ReturnFailure();

        manager.Reconcile([monitor]);

        Assert.Equal(["Could not restore the blackout on DISPLAY1."], notifier.Messages);
    }

    private static MonitorInfo Monitor(string path, MonitorIdentity identity)
        => new(path, path, new RECT { Right = 1920, Bottom = 1080 }, false, identity);

    private static MonitorIdentity Identity(ushort value) => new(value, value, value);
}
