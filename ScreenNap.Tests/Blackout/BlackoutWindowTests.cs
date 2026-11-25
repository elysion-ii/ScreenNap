using ScreenNap.Blackout;
using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Blackout;

public sealed class BlackoutWindowTests
{
    private const string BlackoutClass = "ScreenNap_Blackout";
    private static readonly MonitorInfo s_monitor = new(
        @"\\.\DISPLAY1", "Monitor", new RECT { Left = -1920, Top = 0, Right = 0, Bottom = 1080 }, false, new MonitorIdentity(1, 2, 0));

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
        Assert.Contains((handle, BlackoutWindow.TopmostTimerId), api.Timers);
        api.Positions.Clear();

        api.Send(handle, WindowStyles.WM_TIMER, BlackoutWindow.TopmostTimerId);

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
        api.TimersFailingToStart.Add(BlackoutWindow.TopmostTimerId);
        using var log = new LogCapture();

        IBlackoutWindow? window = new BlackoutWindowFactory(api).Create(s_monitor);

        Assert.NotNull(window);
        Assert.True(window.IsAlive);
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

        api.Send(handle, WindowStyles.WM_TIMER, BlackoutWindow.TopmostTimerId);

        Assert.True(window.IsAlive);
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

        Assert.True(window.IsAlive);
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
        Assert.False(window.IsAlive);
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void Destroy_TimerCannotBeStopped_LogsItAndStillReportsTheWindowClosed()
    {
        var api = new FakeWindowApi();
        IBlackoutWindow window = new BlackoutWindowFactory(api).Create(s_monitor)!;
        api.TimersFailingToStop.Add(BlackoutWindow.TopmostTimerId);
        int destroyedNotices = 0;
        window.OnDestroyed = _ => destroyedNotices++;
        using var log = new LogCapture();

        Assert.True(window.Destroy());

        Assert.Equal(1, destroyedNotices);
        Assert.False(window.UserDismissed);
        LogAssert.Logged(log, "WARN", @"KillTimer failed for blackout window \\.\DISPLAY1");
    }

    private static (IntPtr Handle, FakeWindowApi.FakeWindow Window) Single(FakeWindowApi api)
    {
        KeyValuePair<IntPtr, FakeWindowApi.FakeWindow> entry = Assert.Single(api.Windows, pair => pair.Value.ClassName == BlackoutClass);
        return (entry.Key, entry.Value);
    }
}
