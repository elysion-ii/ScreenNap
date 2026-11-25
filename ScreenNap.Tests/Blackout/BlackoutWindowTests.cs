using ScreenNap.Blackout;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Blackout;

public sealed class BlackoutWindowTests
{
    private const string BlackoutClass = "ScreenNap_Blackout";
    private static readonly MonitorInfo s_monitor = new(
        @"\\.\DISPLAY1", "Monitor", new RECT { Left = -1920, Top = 0, Right = 0, Bottom = 1080 }, false, MonitorIdentity.For(null, "connection 1"));

    [Fact]
    [Trait("Requirement", "WIN-1")]
    [Trait("Requirement", "WIN-2")]
    internal void Create_Monitor_ShowsATopmostToolWindowOverTheWholeMonitorWithoutActivatingIt()
    {
        var api = new FakeWindowApi();

        Assert.NotNull(new BlackoutWindowFactory(api).Create(s_monitor));

        (IntPtr handle, FakeWindowApi.FakeWindow window) = Single(api);
        Assert.Equal((-1920, 0, 1920, 1080), window.CreatedBounds);
        Assert.Equal(WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST | WindowStyles.WS_EX_NOACTIVATE, window.ExtendedStyle);
        Assert.True(window.IsVisible);
        Assert.Contains(api.Positions, position => position.Window == handle && (position.Flags & WindowStyles.SWP_NOACTIVATE) != 0);
    }

    [Fact]
    [Trait("Requirement", "WIN-3")]
    internal void TopmostTimer_Tick_RestoresTheTopmostPositionWithoutMovingResizingOrActivating()
    {
        var api = new FakeWindowApi();
        Assert.NotNull(new BlackoutWindowFactory(api).Create(s_monitor));
        (IntPtr handle, _) = Single(api);
        api.Positions.Clear();

        api.Tick(handle);

        Assert.Equal(
            [(handle, WindowStyles.HWND_TOPMOST, 0, 0, 0, 0, WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE)],
            api.Positions);
    }

    [Fact]
    [Trait("Requirement", "ERR-9")]
    [Trait("Requirement", "ERR-10")]
    internal void Create_TopmostTimerCannotStart_LogsItAndKeepsTheBlackout()
    {
        var api = new FakeWindowApi();
        api.TimerStartFails = (_, _) => true;
        using var log = new LogCapture();

        IBlackoutWindow? window = new BlackoutWindowFactory(api).Create(s_monitor);

        Assert.NotNull(window);
        Assert.True(Single(api).Window.IsAlive);
        LogAssert.Logged(log, "WARN", @"SetTimer failed for blackout window \\.\DISPLAY1");
    }

    [Fact]
    [Trait("Requirement", "ERR-9")]
    [Trait("Requirement", "ERR-10")]
    internal void TopmostTimer_PositionCannotBeRestored_LogsItAndKeepsTheBlackout()
    {
        var api = new FakeWindowApi();
        IBlackoutWindow window = new BlackoutWindowFactory(api).Create(s_monitor)!;
        (IntPtr handle, _) = Single(api);
        api.SetWindowPosFails = true;
        using var log = new LogCapture();

        api.Tick(handle);

        Assert.True(api.Windows[handle].IsAlive);
        LogAssert.Logged(log, "WARN", @"SetWindowPos failed for blackout window \\.\DISPLAY1");
    }

    [Theory]
    [Trait("Requirement", "ERR-10")]
    [InlineData(WindowStyles.WM_RBUTTONUP, "right-click")]
    [InlineData(WindowStyles.WM_LBUTTONDBLCLK, "double-click")]
    internal void Dismissal_WindowCannotBeClosed_LogsItAndKeepsTheBlackout(uint mouseMessage, string gesture)
    {
        var api = new FakeWindowApi();
        IBlackoutWindow window = new BlackoutWindowFactory(api).Create(s_monitor)!;
        (IntPtr handle, _) = Single(api);
        int destroyedNotices = 0;
        window.OnDestroyed = _ => destroyedNotices++;
        api.ClassesFailingDestruction.Add(BlackoutClass);
        using var log = new LogCapture();

        api.Send(handle, mouseMessage);

        Assert.True(api.Windows[handle].IsAlive);
        Assert.Equal(0, destroyedNotices);
        LogAssert.Logged(log, "WARN", $@"DestroyWindow failed after {gesture} on blackout window \\.\DISPLAY1");
    }

    [Fact]
    [Trait("Requirement", "WIN-4")]
    internal void Dismissal_RightClick_ClosesTheWindowAsTheUsersDismissal()
    {
        var api = new FakeWindowApi();
        IBlackoutWindow window = new BlackoutWindowFactory(api).Create(s_monitor)!;
        (IntPtr handle, _) = Single(api);
        IBlackoutWindow? destroyed = null;
        window.OnDestroyed = closed => destroyed = closed;

        api.Send(handle, WindowStyles.WM_RBUTTONUP);

        Assert.Same(window, destroyed);
        Assert.True(window.UserDismissed);
        Assert.False(api.Windows[handle].IsAlive);
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void Destroy_TimerCannotBeStopped_LogsItAndStillReportsTheWindowClosed()
    {
        var api = new FakeWindowApi();
        IBlackoutWindow window = new BlackoutWindowFactory(api).Create(s_monitor)!;
        api.TimerStopFails = (_, _) => true;
        int destroyedNotices = 0;
        window.OnDestroyed = _ => destroyedNotices++;
        using var log = new LogCapture();

        Assert.True(window.Destroy());

        Assert.Equal(1, destroyedNotices);
        Assert.False(window.UserDismissed);
        LogAssert.Logged(log, "WARN", @"KillTimer failed for blackout window \\.\DISPLAY1");
    }

    [Fact]
    [Trait("Requirement", "WIN-6")]
    internal void CloseRequest_FromAnotherProcess_LeavesTheBlackoutOpen()
    {
        var api = new FakeWindowApi();
        IBlackoutWindow window = new BlackoutWindowFactory(api).Create(s_monitor)!;
        (IntPtr handle, _) = Single(api);
        int destroyedNotices = 0;
        window.OnDestroyed = _ => destroyedNotices++;

        api.Send(handle, WindowStyles.WM_CLOSE);

        Assert.True(api.Windows[handle].IsAlive);
        Assert.Equal(0, destroyedNotices);
    }

    [Fact]
    [Trait("Requirement", "DISP-7")]
    internal void MoveTo_NewBounds_FitsTheWindowWithoutChangingItsZOrderOrActivatingIt()
    {
        var api = new FakeWindowApi();
        IBlackoutWindow window = new BlackoutWindowFactory(api).Create(s_monitor)!;
        (IntPtr handle, _) = Single(api);
        api.Positions.Clear();

        window.MoveTo(new RECT { Left = 0, Top = -200, Right = 2560, Bottom = 1240 });

        Assert.Equal(
            [(handle, IntPtr.Zero, 0, -200, 2560, 1440, WindowStyles.SWP_NOZORDER | WindowStyles.SWP_NOACTIVATE)],
            api.Positions);
    }

    [Fact]
    [Trait("Requirement", "DISP-7")]
    internal void MoveTo_WindowCannotBeMoved_LogsItAndKeepsTheBlackout()
    {
        var api = new FakeWindowApi();
        IBlackoutWindow window = new BlackoutWindowFactory(api).Create(s_monitor)!;
        (IntPtr handle, _) = Single(api);
        api.SetWindowPosFails = true;
        using var log = new LogCapture();

        window.MoveTo(new RECT { Left = 0, Top = 0, Right = 2560, Bottom = 1440 });

        Assert.True(api.Windows[handle].IsAlive);
        LogAssert.Logged(log, "WARN", @"SetWindowPos failed to fit blackout window \\.\DISPLAY1 to its monitor");
    }

    [Fact]
    [Trait("Requirement", "IDN-7")]
    internal void WindowRaised_BlackoutAppearsAndReassertsItsPosition_IsRaisedEachTime()
    {
        var api = new FakeWindowApi();
        var factory = new BlackoutWindowFactory(api);
        int raised = 0;
        factory.WindowRaised += () => raised++;

        Assert.NotNull(factory.Create(s_monitor));
        Assert.Equal(1, raised);

        api.Tick(Single(api).Handle);
        Assert.Equal(2, raised);

        api.SetWindowPosFails = true;
        api.Tick(Single(api).Handle);
        Assert.Equal(2, raised);
    }

    private static (IntPtr Handle, FakeWindowApi.FakeWindow Window) Single(FakeWindowApi api)
    {
        KeyValuePair<IntPtr, FakeWindowApi.FakeWindow> entry = Assert.Single(api.Windows, pair => pair.Value.ClassName == BlackoutClass);
        return (entry.Key, entry.Value);
    }
}
