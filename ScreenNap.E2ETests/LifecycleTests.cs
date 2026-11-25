using System.Diagnostics;
using System.Runtime.InteropServices;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class LifecycleTests
{
    // Longer than the one-second blackout maintenance and the overlays' timers, so a quiet
    // period that long shows that no periodic work is left
    private static readonly TimeSpan s_quietPeriod = TimeSpan.FromSeconds(2.5);
    private static readonly TimeSpan s_idleTimeout = TimeSpan.FromSeconds(20);

    // The name ScreenNap gives its single-instance mutex, without a namespace prefix
    private const string SingleInstanceMutex = "ScreenNap_SingleInstance";

    public LifecycleTests() => TestAssets.RequireNoRunningScreenNap();

    [Fact]
    [Trait("Requirement", "LIFE-1")]
    [Trait("Requirement", "NFR-1")]
    internal void Start_NoOtherInstance_RunsInTheTrayWithoutAVisibleWindow()
    {
        using ScreenNapApp app = ScreenNapApp.Start();

        Assert.True(app.TrayIconExists());
        Assert.Empty(Desktop.VisibleTopLevelWindows(app.Process.Id));
        Assert.False(app.Process.HasExited);
    }

    [Fact]
    [Trait("Requirement", "NFR-2")]
    internal void Idle_NothingShown_UsesNoCpuUntilAnEventArrives()
    {
        using ScreenNapApp app = ScreenNapApp.Start();

        app.WaitForIdleUiThread(s_quietPeriod, s_idleTimeout);

        // A live blackout's maintenance wakes the thread; once it and the overlays are gone, nothing does
        app.Select(ScreenNapApp.MonitorCommandBase);
        app.WaitForBlackoutCount(1);
        ulong cyclesWithBlackout = app.UiThreadCycles();
        Wait.Until(() => app.UiThreadCycles() != cyclesWithBlackout, s_quietPeriod, "the live blackout's maintenance runs");
        app.Select(ScreenNapApp.ReleaseAllCommand);
        app.WaitForBlackoutCount(0);
        Keyboard.PressHotkey(0);
        Wait.Until(() => app.Overlays().Count > 0, s_idleTimeout, "the overlays show");
        Wait.Until(() => app.Overlays().Count == 0, s_idleTimeout, "the overlays close by themselves");

        app.WaitForIdleUiThread(s_quietPeriod, s_idleTimeout);
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

    // The single-instance lock lives in the Windows session's namespace, not the global one
    [Fact]
    [Trait("Requirement", "LIFE-2")]
    internal void Start_NoOtherInstance_HoldsItsSingleInstanceLockForTheWindowsSessionOnly()
    {
        using ScreenNapApp app = ScreenNapApp.Start();

        Assert.True(MutexExists(@"Local\" + SingleInstanceMutex), "ScreenNap holds no lock in the logon session");
        Assert.False(MutexExists(@"Global\" + SingleInstanceMutex), "ScreenNap holds a lock for every session");
    }

    [Fact]
    [Trait("Requirement", "TRAY-7")]
    internal void Start_AnyScaling_DrawsTheTrayIconAtTheSmallIconSizeOfThePrimaryMonitor()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        int expected = app.Measure(() =>
        {
            IntPtr primary = Win32.MonitorFromPoint(default, Win32.MONITOR_DEFAULTTOPRIMARY);
            if (Win32.GetDpiForMonitor(primary, Win32.MDT_EFFECTIVE_DPI, out uint dpi, out _) != 0)
                throw new InvalidOperationException("GetDpiForMonitor failed for the primary monitor");
            return Win32.GetSystemMetricsForDpi(Win32.SM_CXSMICON, dpi);
        });

        app.WaitForLog($"Tray icon size: {expected} pixels");
    }

    // What an elevated ScreenNap leaves to a ScreenNap without elevation: a lock it may not open
    [Fact]
    [Trait("Requirement", "LIFE-2")]
    internal void Start_LockHeldThatCannotBeOpened_ShowsTheAlreadyRunningMessageInsteadOfFailing()
    {
        IntPtr securityDescriptor = DenyEveryone();
        IntPtr held;
        try
        {
            var attributes = new SECURITY_ATTRIBUTES
            {
                nLength = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                lpSecurityDescriptor = securityDescriptor,
            };
            held = Win32.CreateMutexW(in attributes, false, SingleInstanceMutex);
        }
        finally
        {
            _ = Win32.LocalFree(securityDescriptor);
        }
        if (held == IntPtr.Zero)
            throw new InvalidOperationException($"CreateMutexW failed (Win32 error: {Marshal.GetLastWin32Error()})");

        try
        {
            IReadOnlyList<string> log = StartAndCloseItsMessage();

            Assert.DoesNotContain(log, line => line.Contains("[ERROR]", StringComparison.Ordinal));
            Assert.DoesNotContain(log, line => line.Contains("Application started", StringComparison.Ordinal));
        }
        finally
        {
            _ = Win32.CloseHandle(held);
        }
    }

    [Fact]
    [Trait("Requirement", "LIFE-2")]
    [Trait("Requirement", "ERR-1")]
    internal void Start_NameTakenByAnotherKindOfObject_ReportsAStartupFailure()
    {
        IntPtr squatter = Win32.CreateEventW(IntPtr.Zero, true, false, SingleInstanceMutex);
        if (squatter == IntPtr.Zero)
            throw new InvalidOperationException($"CreateEventW failed (Win32 error: {Marshal.GetLastWin32Error()})");

        try
        {
            IReadOnlyList<string> log = StartAndCloseItsMessage();

            Assert.Contains(log, line => line.Contains("[ERROR] The single-instance check failed", StringComparison.Ordinal));
        }
        finally
        {
            _ = Win32.CloseHandle(squatter);
        }
    }

    [Fact]
    [Trait("Requirement", "LIFE-5")]
    [Trait("Requirement", "LIFE-3")]
    [Trait("Requirement", "WIN-6")]
    internal void CloseRequest_ToEveryWindow_ShutsDownNormallyWithoutLoggingAFailure()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.Select(ScreenNapApp.MonitorCommandBase);
        app.WaitForBlackoutCount(1);

        // A program closing ScreenNap asks each of its top-level windows to close
        foreach (IntPtr window in Desktop.TopLevelWindows(app.Process.Id))
        {
            if (!Win32.PostMessageW(window, Win32.WM_CLOSE, 0, 0))
                throw new InvalidOperationException("PostMessageW could not send the close request");
        }

        Assert.True(app.Process.WaitForExit(TimeSpan.FromSeconds(30)), "ScreenNap did not close on the request");
        Assert.False(app.TrayIconExists());
        for (int digit = 0; digit <= 9; digit++)
            Assert.False(Keyboard.HotkeyIsHeldElsewhere(digit), $"Ctrl+Shift+Alt+{digit} is still held after the close");
        List<string> shutdown = [.. app.LogLines().SkipWhile(line => !line.EndsWith("Application exiting", StringComparison.Ordinal))];
        Assert.NotEmpty(shutdown);
        Assert.DoesNotContain(shutdown, line => line.Contains("[WARN]", StringComparison.Ordinal) || line.Contains("[ERROR]", StringComparison.Ordinal));
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
    [Trait("Requirement", "TRAY-5")]
    internal void TaskbarRecreated_IconGone_AddsTheIconAgain()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.RemoveTrayIcon();
        Wait.Until(() => !app.TrayIconExists(), TimeSpan.FromSeconds(10), "the tray icon is gone");

        app.SendTaskbarCreated();

        Wait.Until(app.TrayIconExists, TimeSpan.FromSeconds(10), "ScreenNap adds its tray icon again");
        app.WaitForLog("Tray icon added after the taskbar was created");
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

    private static bool MutexExists(string name)
    {
        IntPtr handle = Win32.OpenMutexW(Win32.SYNCHRONIZE, false, name);
        if (handle == IntPtr.Zero)
            return false;
        _ = Win32.CloseHandle(handle);
        return true;
    }

    // Starts ScreenNap, closes the message box it shows, and returns its log once it ends normally
    private static IReadOnlyList<string> StartAndCloseItsMessage()
    {
        string logDirectory = TestAssets.CreateScratchDirectory();
        try
        {
            var startInfo = new ProcessStartInfo(TestAssets.ScreenNapExe) { UseShellExecute = false };
            startInfo.Environment["SCREENNAP_LOG_DIR"] = logDirectory;
            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("ScreenNap did not start");
            try
            {
                IntPtr messageBox = ScreenNapApp.WaitForMessageBox(process);
                Assert.Equal("ScreenNap", Desktop.Text(messageBox));
                if (!Win32.PostMessageW(messageBox, Win32.WM_CLOSE, 0, 0))
                    throw new InvalidOperationException("PostMessageW could not close the message box");

                Assert.True(process.WaitForExit(TimeSpan.FromSeconds(10)), "ScreenNap did not end after its message");
                Assert.Equal(0, process.ExitCode);
            }
            finally
            {
                if (!process.HasExited)
                    process.Kill();
            }

            return [.. Directory.GetFiles(logDirectory, "ScreenNap_*.log").SelectMany(File.ReadAllLines)];
        }
        finally
        {
            TestAssets.DeleteScratchDirectory(logDirectory);
        }
    }

    // A security descriptor with an empty access list, which grants nothing to anyone
    private static IntPtr DenyEveryone()
    {
        if (!Win32.ConvertStringSecurityDescriptorToSecurityDescriptorW("D:", Win32.SDDL_REVISION_1, out IntPtr descriptor, IntPtr.Zero))
            throw new InvalidOperationException($"ConvertStringSecurityDescriptorToSecurityDescriptorW failed (Win32 error: {Marshal.GetLastWin32Error()})");
        return descriptor;
    }
}
