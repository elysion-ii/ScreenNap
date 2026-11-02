using System.Runtime.InteropServices;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class BlackoutTests
{
    private const uint Black = 0x00000000;
    private static readonly TimeSpan s_paintTimeout = TimeSpan.FromSeconds(5);

    public BlackoutTests() => TestAssets.RequireNoRunningScreenNap();

    [Fact]
    [Trait("Requirement", "MENU-6")]
    [Trait("Requirement", "BLK-1")]
    [Trait("Requirement", "WIN-1")]
    [Trait("Requirement", "WIN-2")]
    [Trait("Requirement", "NFR-3")]
    [Trait("Requirement", "INV-1")]
    [Trait("Requirement", "LOG-6")]
    internal void SelectMonitor_Inactive_CoversItInBlackWithoutTakingFocus()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        IReadOnlyList<RECT> monitorsBefore = app.Monitors();
        RECT monitor = monitorsBefore[0];

        app.Select(ScreenNapApp.MonitorCommandBase);
        IntPtr blackout = app.WaitForBlackoutCovering(monitor);

        long style = Desktop.ExtendedStyle(blackout);
        Assert.NotEqual(0, style & Win32.WS_EX_TOPMOST);
        Assert.NotEqual(0, style & Win32.WS_EX_TOOLWINDOW);
        Assert.NotEqual(0, style & Win32.WS_EX_NOACTIVATE);
        Assert.NotEqual(blackout, Win32.GetForegroundWindow());
        Wait.Until(
            () => app.Measure(() => Desktop.PixelAt(monitor.Left + monitor.Width / 2, monitor.Top + monitor.Height / 2)) == Black,
            s_paintTimeout,
            "the monitor's center pixel is black");
        Wait.Until(
            () => app.Measure(() => Desktop.PixelAt(monitor.Left, monitor.Bottom - 1)) == Black,
            s_paintTimeout,
            "the monitor's bottom-left pixel, where a taskbar sits, is black");
        Assert.Equal(monitorsBefore, app.Monitors());
        app.WaitForLog("Blackout toggled on");
    }

    [Fact]
    [Trait("Requirement", "BLK-2")]
    [Trait("Requirement", "LOG-6")]
    internal void SelectMonitor_Active_RemovesItsBlackout()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.Select(ScreenNapApp.MonitorCommandBase);
        app.WaitForBlackoutCount(1);

        app.Select(ScreenNapApp.MonitorCommandBase);

        app.WaitForBlackoutCount(0);
        app.WaitForLog("Blackout toggled off");
    }

    [Fact]
    [Trait("Requirement", "BLK-3")]
    [Trait("Requirement", "BLK-4")]
    [Trait("Requirement", "MENU-7")]
    internal void ReleaseAll_EveryMonitorBlackedOut_ClosesEveryBlackout()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        IReadOnlyList<RECT> monitors = app.Monitors();
        for (int i = 0; i < monitors.Count; i++)
        {
            app.Select(ScreenNapApp.MonitorCommandBase + (uint)i);
            app.WaitForBlackoutCovering(monitors[i]);
        }
        app.WaitForBlackoutCount(monitors.Count);

        app.Select(ScreenNapApp.ReleaseAllCommand);

        app.WaitForBlackoutCount(0);
        Assert.DoesNotContain(app.OpenMenu(), entry => entry.Id == ScreenNapApp.ReleaseAllCommand);
    }

    [Theory]
    [Trait("Requirement", "WIN-4")]
    [Trait("Requirement", "INV-4")]
    [InlineData(Win32.WM_RBUTTONUP)]
    [InlineData(Win32.WM_LBUTTONDBLCLK)]
    internal void MouseDismissal_OnTheBlackout_ClosesItAndClearsTheDesiredState(uint mouseMessage)
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.Select(ScreenNapApp.MonitorCommandBase);
        IntPtr blackout = app.WaitForBlackoutCovering(app.Monitors()[0]);

        if (!Win32.PostMessageW(blackout, mouseMessage, 0, 0))
            throw new InvalidOperationException($"PostMessageW failed (Win32 error: {Marshal.GetLastWin32Error()})");
        app.WaitForBlackoutCount(0);
        app.Post(Win32.WM_DISPLAYCHANGE, 0, 0);
        app.WaitForLog("Display configuration changed (debounced)");

        Assert.Empty(app.BlackoutWindows());
        app.WaitForLog("Blackout window destroyed");
    }

    [Fact]
    [Trait("Requirement", "WIN-5")]
    [Trait("Requirement", "WIN-3")]
    internal void IdleCursor_OverTheBlackout_HidesAfterTenSecondsAndShowsOnMovement()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        RECT monitor = app.Monitors()[0];
        app.Select(ScreenNapApp.MonitorCommandBase);
        IntPtr blackout = app.WaitForBlackoutCovering(monitor);
        int centerX = monitor.Left + monitor.Width / 2;
        int centerY = monitor.Top + monitor.Height / 2;
        app.Measure(() => Desktop.MoveCursor(centerX, centerY));

        Assert.False(Desktop.CursorIsHidden());
        Wait.Until(Desktop.CursorIsHidden, TimeSpan.FromSeconds(15), "the cursor hides over the idle blackout");
        app.Measure(() => Desktop.MoveCursor(centerX + 10, centerY + 10));
        Wait.Until(() => !Desktop.CursorIsHidden(), TimeSpan.FromSeconds(5), "the cursor shows again after it moves");

        Assert.NotEqual(0, Desktop.ExtendedStyle(blackout) & Win32.WS_EX_TOPMOST);
    }
}
