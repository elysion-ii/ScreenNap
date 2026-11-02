using System.Diagnostics;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class LifecycleTests
{
    public LifecycleTests() => TestAssets.RequireNoRunningScreenNap();

    [Fact]
    [Trait("Requirement", "LIFE-1")]
    [Trait("Requirement", "NFR-1")]
    [Trait("Requirement", "NFR-2")]
    internal void Start_NoOtherInstance_RunsInTheTrayWithoutAVisibleWindow()
    {
        using ScreenNapApp app = ScreenNapApp.Start();

        Assert.True(app.TrayIconExists());
        Assert.Empty(Desktop.VisibleTopLevelWindows(app.Process.Id));
        Assert.False(app.Process.HasExited);
    }

    [Fact]
    [Trait("Requirement", "HK-1")]
    [Trait("Requirement", "HK-3")]
    internal void Start_NoOtherInstance_HoldsEveryHotkey()
    {
        using ScreenNapApp app = ScreenNapApp.Start();

        for (int digit = 0; digit <= 9; digit++)
            Assert.True(Keyboard.HotkeyIsHeldElsewhere(digit), $"Ctrl+Shift+Alt+{digit} is not held by ScreenNap");
    }

    [Fact]
    [Trait("Requirement", "LIFE-2")]
    internal void Start_SecondInstance_ShowsAMessageAndLeavesTheFirstRunning()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        using Process second = app.StartSecondInstance();
        try
        {
            IntPtr messageBox = ScreenNapApp.WaitForMessageBox(second);
            Assert.Equal("ScreenNap", Desktop.Text(messageBox));
            if (!Win32.PostMessageW(messageBox, Win32.WM_CLOSE, 0, 0))
                throw new InvalidOperationException("PostMessageW could not close the message box");

            Assert.True(second.WaitForExit(TimeSpan.FromSeconds(10)), "The second ScreenNap did not exit");
            Assert.False(app.Process.HasExited);
            Assert.True(app.TrayIconExists());
        }
        finally
        {
            if (!second.HasExited)
                second.Kill();
        }
    }

    [Fact]
    [Trait("Requirement", "MENU-8")]
    [Trait("Requirement", "LIFE-3")]
    [Trait("Requirement", "INV-5")]
    internal void Exit_WithABlackout_ReleasesEverythingScreenNapHeld()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.Select(ScreenNapApp.MonitorCommandBase);
        app.WaitForBlackoutCount(1);
        Assert.Contains(app.OpenMenu(), entry => entry.Id == ScreenNapApp.ExitCommand);

        app.Exit();

        Assert.False(app.TrayIconExists());
        Assert.Empty(Desktop.VisibleTopLevelWindows(app.Process.Id));
        for (int digit = 0; digit <= 9; digit++)
            Assert.False(Keyboard.HotkeyIsHeldElsewhere(digit), $"Ctrl+Shift+Alt+{digit} is still held after exit");
        Assert.Contains(app.LogLines(), line => line.EndsWith("Application exiting", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Requirement", "LIFE-4")]
    internal void Restart_AfterABlackout_StartsWithNoBlackout()
    {
        string logDirectory = TestAssets.CreateScratchDirectory();
        try
        {
            using (ScreenNapApp first = ScreenNapApp.Start(logDirectory: logDirectory))
            {
                first.Select(ScreenNapApp.MonitorCommandBase);
                first.WaitForBlackoutCount(1);
                first.Exit();
            }

            using ScreenNapApp second = ScreenNapApp.Start(logDirectory: logDirectory);

            Assert.Empty(second.BlackoutWindows());
            Assert.DoesNotContain(second.OpenMenu(), entry => entry.IsChecked);
        }
        finally
        {
            TestAssets.DeleteScratchDirectory(logDirectory);
        }
    }
}
