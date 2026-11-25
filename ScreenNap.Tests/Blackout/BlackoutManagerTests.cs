using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Blackout;

public sealed class BlackoutManagerTests
{
    private static readonly CultureInfo s_english = CultureInfo.GetCultureInfo("en-US");
    private static readonly RECT s_fullHd = new() { Right = 1920, Bottom = 1080 };

    private readonly FakeBlackoutWindowFactory _factory = new();
    private readonly FakeUserNotifier _notifier = new();
    private readonly BlackoutManager _manager;

    public BlackoutManagerTests()
    {
        _manager = new BlackoutManager(_factory, _notifier, s_english);
    }

    [Fact]
    [Trait("Requirement", "BLK-1")]
    [Trait("Requirement", "BLK-5")]
    [Trait("Requirement", "INV-3")]
    [Trait("Requirement", "ERR-3")]
    internal void Toggle_InactiveMonitor_CreatesWindowAndRaisesEvent()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        int changes = 0;
        _manager.ActiveCountChanged += () => changes++;

        _manager.Toggle(monitor);

        Assert.Equal(1, _manager.ActiveCount);
        Assert.True(_manager.IsActive(monitor));
        Assert.Equal([monitor], _factory.Requests);
        Assert.Equal(1, changes);
        Assert.Empty(_notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "BLK-2")]
    [Trait("Requirement", "INV-4")]
    internal void Toggle_ActiveMonitor_ClosesItsBlackoutAndClearsItsDesiredState()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        _manager.Toggle(monitor);

        _manager.Toggle(monitor);

        Assert.Equal(0, _manager.ActiveCount);
        Assert.False(_factory.Windows[0].IsOpen);
        _manager.Reconcile([monitor]);
        Assert.Single(_factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "BLK-2")]
    [Trait("Requirement", "ERR-4")]
    internal void Toggle_DestroyFailure_PreservesActiveAndDesiredStateAndNotifies()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        _manager.Toggle(monitor);
        _factory.Windows[0].DestroySucceeds = false;

        _manager.Toggle(monitor);

        Assert.True(_manager.IsActive(monitor));
        Assert.Equal(["Could not release the blackout on DISPLAY1."], _notifier.Messages);

        // Still desired: closed for a disconnection, it is restored when the monitor returns
        _factory.Windows[0].DestroySucceeds = true;
        _manager.Reconcile([]);
        _manager.Reconcile([monitor]);
        Assert.Equal(2, _factory.Requests.Count);
    }

    [Fact]
    [Trait("Requirement", "BLK-1")]
    [Trait("Requirement", "ERR-3")]
    internal void Toggle_FactoryFailure_LeavesTheMonitorInactiveAndNotifies()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        _factory.ReturnFailure();

        _manager.Toggle(monitor);

        Assert.Equal(0, _manager.ActiveCount);
        Assert.Equal(["Could not black out DISPLAY1."], _notifier.Messages);
        _manager.Reconcile([monitor]);
        Assert.Single(_factory.Requests);
    }

    // A display device path that another monitor takes over carries no blackout with it
    [Fact]
    [Trait("Requirement", "INV-2")]
    [Trait("Requirement", "DISP-2")]
    [Trait("Requirement", "HK-2")]
    internal void Toggle_PathTakenOverByAnotherMonitor_ActsOnlyOnTheMonitorItNames()
    {
        _manager.Toggle(Monitor("DISPLAY1", Identity(1)));
        MonitorInfo newcomer = Monitor("DISPLAY1", Identity(2));

        _manager.Toggle(newcomer);

        Assert.Equal(2, _manager.ActiveCount);
        Assert.Equal(0, _factory.Windows[0].DestroyCalls);
        Assert.True(_manager.IsActive(newcomer));
    }

    [Fact]
    [Trait("Requirement", "INV-2")]
    internal void Toggle_MonitorWithoutIdentity_IsTrackedByItsPath()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", null);
        _manager.Toggle(monitor);

        Assert.True(_manager.IsActive(monitor));
        Assert.False(_manager.IsActive(Monitor("DISPLAY2", null)));
        Assert.False(_manager.IsActive(Monitor("DISPLAY1", Identity(1))));

        _manager.Toggle(monitor);
        Assert.Equal(0, _manager.ActiveCount);
    }

    [Fact]
    [Trait("Requirement", "INV-2")]
    [Trait("Requirement", "DISP-6")]
    internal void Toggle_ReconcileInsideChangeEvent_DoesNotRecreateWindow()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        _manager.ActiveCountChanged += () => _manager.Reconcile([monitor]);

        _manager.Toggle(monitor);

        Assert.Single(_factory.Requests);
        Assert.Equal(1, _manager.ActiveCount);
    }

    [Fact]
    [Trait("Requirement", "BLK-4")]
    [Trait("Requirement", "ERR-5")]
    internal void ReleaseAll_NoWindows_DoesNothing()
    {
        _manager.ReleaseAll();

        Assert.Equal(0, _manager.ActiveCount);
        Assert.Empty(_notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "BLK-3")]
    [Trait("Requirement", "BLK-4")]
    [Trait("Requirement", "ERR-5")]
    internal void ReleaseAll_MultipleWindows_ClosesEveryWindowAndClearsTheirDesiredState()
    {
        MonitorInfo first = Monitor("DISPLAY1", Identity(1));
        MonitorInfo second = Monitor("DISPLAY2", Identity(2));
        _manager.Toggle(first);
        _manager.Toggle(second);

        _manager.ReleaseAll();

        Assert.Equal(0, _manager.ActiveCount);
        Assert.All(_factory.Windows, window => Assert.False(window.IsOpen));
        Assert.Empty(_notifier.Messages);
        _manager.Reconcile([first, second]);
        Assert.Equal(2, _factory.Requests.Count);
    }

    [Fact]
    [Trait("Requirement", "BLK-4")]
    [Trait("Requirement", "ERR-5")]
    internal void ReleaseAll_DestroyFailure_KeepsOnlyThatWindowsDesiredStateAndNotifiesTheCount()
    {
        MonitorInfo first = Monitor("DISPLAY1", Identity(1));
        MonitorInfo second = Monitor("DISPLAY2", Identity(2));
        _manager.Toggle(first);
        _manager.Toggle(second);
        _factory.Windows[0].DestroySucceeds = false;

        _manager.ReleaseAll();

        Assert.Equal(1, _manager.ActiveCount);
        Assert.True(_manager.IsActive(first));
        Assert.Equal(["1 blackout(s) could not be released."], _notifier.Messages);

        // The first stays desired across a disconnection; the second is not restored
        _factory.Windows[0].DestroySucceeds = true;
        _manager.Reconcile([]);
        _manager.Reconcile([first, second]);
        Assert.Equal([first, second, first], _factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "BLK-4")]
    [Trait("Requirement", "DISP-4")]
    internal void ReleaseAll_DisconnectedDesiredMonitor_IsNotRestoredOnReconnection()
    {
        MonitorInfo disconnected = Monitor("DISPLAY2", Identity(2));
        MonitorInfo present = Monitor("DISPLAY1", Identity(1));
        _manager.Toggle(present);
        _manager.Toggle(disconnected);
        _manager.Reconcile([present]);

        _manager.ReleaseAll();
        _manager.Reconcile([present, disconnected]);

        Assert.Equal(0, _manager.ActiveCount);
        Assert.Equal(2, _factory.Requests.Count);
    }

    [Fact]
    [Trait("Requirement", "LIFE-3")]
    [Trait("Requirement", "INV-5")]
    internal void CloseAllAtExit_OneWindowCannotBeClosed_ClosesTheOthersClearsIntentAndLogsWithoutNotification()
    {
        MonitorInfo first = Monitor("DISPLAY1", Identity(1));
        MonitorInfo second = Monitor("DISPLAY2", Identity(2));
        _manager.Toggle(first);
        _manager.Toggle(second);
        _factory.Windows[0].DestroySucceeds = false;
        using var log = new LogCapture();

        _manager.CloseAllAtExit();

        Assert.True(_manager.IsActive(first));
        Assert.False(_factory.Windows[1].IsOpen);
        Assert.Empty(_notifier.Messages);
        LogAssert.Logged(log, "WARN", "1 blackout window(s) could not be closed at exit");

        // Intent is gone: neither monitor is restored after its blackout closes
        _factory.Windows[0].DestroySucceeds = true;
        _manager.Reconcile([]);
        _manager.Reconcile([first, second]);
        Assert.Equal(2, _factory.Requests.Count);
    }

    [Fact]
    [Trait("Requirement", "DISP-7")]
    [Trait("Requirement", "WIN-1")]
    [Trait("Requirement", "DISP-6")]
    internal void Reconcile_MonitorResizedOnAnotherPath_FitsTheLiveBlackoutWithoutRecreatingIt()
    {
        _manager.Toggle(Monitor("DISPLAY1", Identity(1)));
        var resized = new RECT { Left = 1920, Top = -360, Right = 4480, Bottom = 1080 };

        _manager.Reconcile([Monitor("DISPLAY3", Identity(1), resized)]);

        Assert.Single(_factory.Requests);
        Assert.Equal(resized, _factory.Windows[0].Bounds);
        Assert.True(_factory.Windows[0].IsOpen);
    }

    [Fact]
    [Trait("Requirement", "DISP-2")]
    [Trait("Requirement", "DISP-3")]
    [Trait("Requirement", "DISP-4")]
    [Trait("Requirement", "INV-3")]
    [Trait("Requirement", "INV-4")]
    [Trait("Requirement", "MON-6")]
    internal void Reconcile_MonitorDisconnected_ClosesItsBlackoutAndRestoresItOnReconnection()
    {
        _manager.Toggle(Monitor("DISPLAY1", Identity(1)));
        int changes = 0;
        _manager.ActiveCountChanged += () => changes++;

        _manager.Reconcile([]);

        Assert.False(_factory.Windows[0].IsOpen);
        Assert.Equal(0, _manager.ActiveCount);
        Assert.Equal(1, changes);

        MonitorInfo reconnected = Monitor("DISPLAY2", Identity(1));
        _manager.Reconcile([reconnected]);

        Assert.Equal(reconnected, _factory.Requests[^1]);
        Assert.True(_manager.IsActive(reconnected));
        Assert.Equal(2, changes);
        Assert.Empty(_notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "DISP-2")]
    internal void Reconcile_DisconnectedBlackoutCannotBeClosed_TriesAgainAtTheNextReconciliationWithoutNotification()
    {
        _manager.Toggle(Monitor("DISPLAY1", Identity(1)));
        _factory.Windows[0].DestroySucceeds = false;

        _manager.Reconcile([]);

        Assert.Equal(1, _manager.ActiveCount);
        Assert.Empty(_notifier.Messages);

        _factory.Windows[0].DestroySucceeds = true;
        _manager.Reconcile([]);
        Assert.Equal(2, _factory.Windows[0].DestroyCalls);
        Assert.Equal(0, _manager.ActiveCount);
    }

    // Two monitors whose paths swap while one of them waits for restoration
    [Fact]
    [Trait("Requirement", "INV-2")]
    [Trait("Requirement", "INV-3")]
    [Trait("Requirement", "DISP-2")]
    [Trait("Requirement", "DISP-3")]
    internal void Reconcile_PathsSwapped_KeepsEveryBlackoutOnItsOwnMonitorAndWithinReach()
    {
        MonitorInfo first = Monitor("DISPLAY1", Identity(1));
        MonitorInfo second = Monitor("DISPLAY2", Identity(2), new RECT { Left = 1920, Right = 3840, Bottom = 1080 });
        _manager.Toggle(first);
        _manager.Toggle(second);
        _manager.Reconcile([second]);

        _manager.Reconcile([second with { DevicePath = "DISPLAY1" }, first with { DevicePath = "DISPLAY2" }]);

        Assert.Equal(2, _manager.ActiveCount);
        Assert.Equal(second.Bounds, _factory.Windows[1].Bounds);
        _manager.ReleaseAll();
        Assert.Equal(0, _manager.ActiveCount);
        Assert.All(_factory.Windows, window => Assert.False(window.IsOpen));
    }

    [Fact]
    [Trait("Requirement", "DISP-6")]
    internal void Reconcile_UndesiredMonitor_IsIgnored()
    {
        _manager.Reconcile([Monitor("DISPLAY1", Identity(1))]);

        Assert.Empty(_factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "DISP-5")]
    internal void Reconcile_MonitorWithoutIdentity_ClosesItsBlackoutWhenGoneAndNeverRestoresIt()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", null);
        _manager.Toggle(monitor);

        _manager.Reconcile([]);
        _manager.Reconcile([monitor]);

        Assert.Equal(0, _manager.ActiveCount);
        Assert.Single(_factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "WIN-4")]
    [Trait("Requirement", "INV-4")]
    internal void Reconcile_AfterUserDismissal_DoesNotRestore()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        _manager.Toggle(monitor);

        _factory.Windows[0].DismissByUser();
        _manager.Reconcile([monitor]);

        Assert.Equal(0, _manager.ActiveCount);
        Assert.Single(_factory.Requests);
    }

    [Fact]
    [Trait("Requirement", "ERR-6")]
    internal void Reconcile_FactoryFailure_LeavesTheMonitorInactiveAndNotifies()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        _manager.Toggle(monitor);
        _manager.Reconcile([]);
        _factory.ReturnFailure();

        _manager.Reconcile([monitor]);

        Assert.Equal(0, _manager.ActiveCount);
        Assert.Equal(["Could not restore the blackout on DISPLAY1."], _notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "BLK-5")]
    [Trait("Requirement", "INV-3")]
    internal void Reconcile_NothingChanges_RaisesNoEvent()
    {
        MonitorInfo monitor = Monitor("DISPLAY1", Identity(1));
        _manager.Toggle(monitor);
        int changes = 0;
        _manager.ActiveCountChanged += () => changes++;

        _manager.Reconcile([monitor]);
        _manager.Reconcile([monitor]);

        Assert.Equal(0, changes);
    }

    private static MonitorInfo Monitor(string path, MonitorIdentity? identity, RECT? bounds = null)
        => new(path, path, bounds ?? s_fullHd, false, identity);

    private static MonitorIdentity Identity(int value) => MonitorIdentity.For(null, $"connection {value}");
}
