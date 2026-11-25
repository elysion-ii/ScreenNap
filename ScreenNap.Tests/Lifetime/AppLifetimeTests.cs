using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Hotkeys;
using ScreenNap.Lifetime;
using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Resources;
using ScreenNap.Tests.TestDoubles;
using ScreenNap.Tray;
using Xunit;

namespace ScreenNap.Tests.Lifetime;

public sealed class AppLifetimeTests
{
    private const string MessageWindowClass = "ScreenNap_MessageWindow";
    private const string BlackoutClass = "ScreenNap_Blackout";
    private const string OverlayClass = "ScreenNap_Identify";
    private static readonly CultureInfo s_english = CultureInfo.GetCultureInfo("en-US");
    private static readonly MonitorInfo s_monitor = new(
        @"\\.\DISPLAY1", "Monitor", new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 }, true, new MonitorIdentity(1, 2, 0));

    [Fact]
    [Trait("Requirement", "LIFE-1")]
    [Trait("Requirement", "LIFE-3")]
    [Trait("Requirement", "INV-5")]
    internal void Run_NoOtherInstance_RunsWithTheTrayIconAndHotkeysThenReleasesEverything()
    {
        var harness = new Harness();
        bool trayShownWhileRunning = false;
        int hotkeysWhileRunning = 0;
        harness.Lifetime.Loop = () =>
        {
            trayShownWhileRunning = harness.Tray.IconShown;
            hotkeysWhileRunning = harness.Hotkeys.Registered.Count;
        };

        harness.Run();

        Assert.Equal(1, harness.Lifetime.LoopRuns);
        Assert.True(trayShownWhileRunning);
        Assert.Equal(HotkeyInterpreter.HotkeyIds.Count, hotkeysWhileRunning);
        Assert.Empty(harness.Lifetime.Messages);
        Assert.False(harness.Tray.IconShown);
        Assert.Empty(harness.Hotkeys.Registered);
        Assert.False(harness.Windows.Windows[harness.MessageWindow].IsAlive);
        Assert.Contains(MessageWindowClass, harness.Windows.UnregisteredClasses);
        Assert.True(harness.Lifetime.MutexReleased);
    }

    [Fact]
    [Trait("Requirement", "LIFE-2")]
    internal void Run_AnotherInstanceRuns_ShowsTheMessageAndEndsWithoutStarting()
    {
        var harness = new Harness();
        harness.Lifetime.AnotherInstanceRuns = true;

        harness.Run();

        (string text, string caption, uint type) = Assert.Single(harness.Lifetime.Messages);
        Assert.Equal(Strings.NotifyAlreadyRunning(s_english), text);
        Assert.Equal(Strings.NotifyTitle(s_english), caption);
        Assert.Equal(WindowStyles.MB_ICONINFORMATION, type & WindowStyles.MB_ICONINFORMATION);
        Assert.Empty(harness.Windows.Windows);
        Assert.Equal(0, harness.Lifetime.LoopRuns);
    }

    [Fact]
    [Trait("Requirement", "LIFE-2")]
    internal void Run_AnotherInstanceRunsAndTheMessageCannotBeShown_LogsItAndEndsAsAfterTheMessage()
    {
        var harness = new Harness();
        harness.Lifetime.AnotherInstanceRuns = true;
        harness.Lifetime.MessageBoxFails = true;
        using var log = new LogCapture();

        harness.Run();

        LogAssert.Logged(log, "ERROR", "MessageBoxW failed for the single-instance notification");
        Assert.Empty(harness.Windows.Windows);
        Assert.Equal(0, harness.Lifetime.LoopRuns);
    }

    [Theory]
    [Trait("Requirement", "ERR-1")]
    [InlineData(StartupStep.ModuleHandle, "GetModuleHandleW failed")]
    [InlineData(StartupStep.ClassRegistration, "RegisterClassExW failed for message window")]
    [InlineData(StartupStep.WindowCreation, "CreateWindowExW failed for message window")]
    internal void Run_HiddenWindowCannotBeCreated_LogsItShowsTheStartupFailureAndStops(StartupStep failingStep, string logged)
    {
        var harness = new Harness();
        harness.Fail(failingStep);
        using var log = new LogCapture();

        harness.Run();

        LogAssert.Logged(log, "ERROR", logged);
        (string text, _, uint type) = Assert.Single(harness.Lifetime.Messages);
        Assert.Equal(Strings.NotifyStartFailed(s_english), text);
        Assert.Equal(WindowStyles.MB_ICONERROR, type & WindowStyles.MB_ICONERROR);
        Assert.Equal(0, harness.Lifetime.LoopRuns);
        Assert.Empty(harness.Tray.Calls);
        Assert.Empty(harness.Hotkeys.Registered);
    }

    [Fact]
    [Trait("Requirement", "LIFE-2")]
    [Trait("Requirement", "ERR-1")]
    internal void Run_StartupFailureCannotBeShown_LogsItAndStops()
    {
        var harness = new Harness();
        harness.Fail(StartupStep.WindowCreation);
        harness.Lifetime.MessageBoxFails = true;
        using var log = new LogCapture();

        harness.Run();

        LogAssert.Logged(log, "ERROR", "MessageBoxW failed for a failure notification");
        Assert.Equal(0, harness.Lifetime.LoopRuns);
    }

    [Theory]
    [Trait("Requirement", "ERR-2")]
    [InlineData(false, "Shell_NotifyIconW(NIM_ADD) failed")]
    [InlineData(true, "Tray icon image could not be loaded (icon-active.ico)")]
    internal void Run_TrayIconCannotBeCreated_LogsItShowsTheFailureAndShutsDownNormally(bool imageMissing, string logged)
    {
        var harness = new Harness();
        if (imageMissing)
            harness.Tray.IconsFailingToLoad.Add("icon-active.ico");
        else
            harness.Tray.FailingCommands.Add(WindowStyles.NIM_ADD);
        using var log = new LogCapture();

        harness.Run();

        LogAssert.Logged(log, "ERROR", logged);
        (string text, _, uint type) = Assert.Single(harness.Lifetime.Messages);
        Assert.Equal(Strings.NotifyTrayFailed(s_english), text);
        Assert.Equal(WindowStyles.MB_ICONERROR, type & WindowStyles.MB_ICONERROR);
        Assert.Equal(0, harness.Lifetime.LoopRuns);
        Assert.Empty(harness.Hotkeys.Registered);
        Assert.False(harness.Windows.Windows[harness.MessageWindow].IsAlive);
        Assert.Contains(MessageWindowClass, harness.Windows.UnregisteredClasses);
        LogAssert.Logged(log, "INFO", "Application exiting");
    }

    [Fact]
    [Trait("Requirement", "ERR-12")]
    internal void Run_MessagesCannotBeReceived_LogsItShowsTheFailureAndShutsDownNormally()
    {
        var harness = new Harness();
        harness.Lifetime.MessagesFail = true;
        using var log = new LogCapture();

        harness.Run();

        LogAssert.Logged(log, "ERROR", $"GetMessageW failed; ScreenNap can no longer receive messages (Win32 error: {FakeLifetimeApi.FailureError})");
        (string text, _, uint type) = Assert.Single(harness.Lifetime.Messages);
        Assert.Equal(Strings.NotifyStopped(s_english), text);
        Assert.Equal(WindowStyles.MB_ICONERROR, type & WindowStyles.MB_ICONERROR);
        Assert.False(harness.Tray.IconShown);
        Assert.Empty(harness.Hotkeys.Registered);
        Assert.False(harness.Windows.Windows[harness.MessageWindow].IsAlive);
        LogAssert.Logged(log, "INFO", "Application exiting");
    }

    [Fact]
    [Trait("Requirement", "TRAY-5")]
    internal void TaskbarCreated_IconGone_AddsTheIconAgainWithTheCurrentCount()
    {
        var harness = new Harness();
        harness.Lifetime.Loop = () =>
        {
            harness.Send(WindowStyles.WM_HOTKEY, HotkeyInterpreter.MonitorHotkeyIdBase);
            harness.Tray.RecreateTaskbar();

            harness.Send(FakeLifetimeApi.RegisteredTaskbarCreated);

            Assert.True(harness.Tray.IconShown);
            Assert.Equal(FakeTrayIconApi.ActiveIcon, harness.Tray.ShownIcon);
        };

        harness.Run();

        Assert.Equal(1, harness.Lifetime.LoopRuns);
    }

    [Fact]
    [Trait("Requirement", "TRAY-5")]
    internal void TaskbarNotificationCannotBeRegistered_LogsItAndKeepsRunningWithoutAddingTheIconAgain()
    {
        var harness = new Harness();
        harness.Lifetime.RegistrationFails = true;
        using var log = new LogCapture();
        harness.Lifetime.Loop = () =>
        {
            harness.Tray.RecreateTaskbar();

            // WM_NULL, which the menu posts, is the value a failed registration leaves
            harness.Send(WindowStyles.WM_NULL);
            harness.Send(FakeLifetimeApi.RegisteredTaskbarCreated);

            Assert.False(harness.Tray.IconShown);
        };

        harness.Run();

        Assert.Equal(1, harness.Lifetime.LoopRuns);
        Assert.Equal(1, harness.Tray.Count(WindowStyles.NIM_ADD));
        LogAssert.Logged(log, "WARN", $"RegisterWindowMessageW failed for TaskbarCreated; the tray icon is not added again after the taskbar is recreated (Win32 error: {FakeLifetimeApi.FailureError})");
    }

    [Fact]
    [Trait("Requirement", "LIFE-3")]
    internal void Exit_BlackoutCannotBeClosed_LogsItShowsNoNotificationAndEnds()
    {
        var harness = new Harness();
        harness.Lifetime.Loop = () =>
        {
            harness.Send(WindowStyles.WM_HOTKEY, HotkeyInterpreter.MonitorHotkeyIdBase);
            Assert.Single(harness.Windows.LiveWindows(BlackoutClass));
            harness.Windows.ClassesFailingDestruction.Add(BlackoutClass);
        };
        using var log = new LogCapture();

        harness.Run();

        LogAssert.Logged(log, "WARN", "1 blackout window(s) could not be closed at exit");
        Assert.Empty(harness.Tray.ShownNotifications);
        Assert.Equal(0, harness.Tray.Calls.Count(call => (call.Flags & WindowStyles.NIF_INFO) != 0));
        Assert.Empty(harness.Lifetime.Messages);
        Assert.False(harness.Windows.Windows[harness.MessageWindow].IsAlive);
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void Shutdown_CleanupFails_LogsItWithoutShowingAnything()
    {
        var harness = new Harness();
        harness.Lifetime.Loop = () =>
        {
            harness.Windows.ClassesFailingDestruction.Add(MessageWindowClass);
            harness.Windows.ClassesFailingUnregistration.Add(MessageWindowClass);
            harness.Windows.ClassesFailingUnregistration.Add(BlackoutClass);
            harness.Windows.ClassesFailingUnregistration.Add(OverlayClass);
            harness.Send(WindowStyles.WM_HOTKEY, HotkeyInterpreter.MonitorHotkeyIdBase);
            harness.Send(WindowStyles.WM_HOTKEY, HotkeyInterpreter.IdentifyHotkeyId);
        };
        using var log = new LogCapture();

        harness.Run();

        LogAssert.Logged(log, "WARN", "DestroyWindow failed for message window");
        LogAssert.Logged(log, "WARN", "UnregisterClassW failed for message window");
        LogAssert.Logged(log, "WARN", "UnregisterClassW failed for blackout window");
        LogAssert.Logged(log, "WARN", "UnregisterClassW failed for identify overlay");
        Assert.Empty(harness.Lifetime.Messages);
        Assert.Empty(harness.Tray.ShownNotifications);
    }

    [Fact]
    [Trait("Requirement", "TRAY-4")]
    [Trait("Requirement", "MENU-8")]
    internal void TrayIconRelease_EitherButtonThenExit_OpensTheMenuAndRequestsTheQuit()
    {
        var harness = new Harness();
        int quitRequestsAfterExit = 0;
        harness.Lifetime.Loop = () =>
        {
            harness.Send(WindowStyles.WM_TRAYICON, 0, (nint)WindowStyles.WM_LBUTTONUP);
            harness.Send(WindowStyles.WM_TRAYICON, 0, (nint)WindowStyles.WM_RBUTTONUP);
            harness.Send(WindowStyles.WM_COMMAND, MenuCommandIds.Exit);
            quitRequestsAfterExit = harness.Lifetime.QuitRequests;
        };

        harness.Run();

        Assert.Equal(2, harness.Menu.OpenedAt.Count);
        Assert.Equal(1, quitRequestsAfterExit);
    }

    [Fact]
    [Trait("Requirement", "DISP-1")]
    internal void DisplayChange_TimerElapses_ReconcilesOnce()
    {
        var harness = new Harness();
        using var log = new LogCapture();
        harness.Lifetime.Loop = () =>
        {
            harness.Send(WindowStyles.WM_DISPLAYCHANGE);
            harness.Send(WindowStyles.WM_DISPLAYCHANGE);
            harness.Send(WindowStyles.WM_TIMER, DisplayChangeDebounce.TimerId);
        };

        harness.Run();

        Assert.Single(log.Entries, entry => entry.Message == "Display configuration changed (debounced)");
    }

    internal enum StartupStep
    {
        ModuleHandle,
        ClassRegistration,
        WindowCreation
    }

    private sealed class Harness
    {
        internal FakeLifetimeApi Lifetime { get; } = new();
        internal FakeWindowApi Windows { get; } = new();
        internal FakeTrayIconApi Tray { get; } = new();
        internal FakePopupMenuApi Menu { get; } = new();
        internal FakeHotkeyApi Hotkeys { get; } = new();
        internal FakeOverlayPaintApi Paint { get; } = new();

        internal IntPtr MessageWindow => Windows.Windows.Single(pair => pair.Value.ClassName == MessageWindowClass).Key;

        internal void Fail(StartupStep step)
        {
            switch (step)
            {
                case StartupStep.ModuleHandle:
                    Windows.ModuleHandleFails = true;
                    break;
                case StartupStep.ClassRegistration:
                    Windows.ClassesFailingRegistration.Add(MessageWindowClass);
                    break;
                case StartupStep.WindowCreation:
                    Windows.ClassesFailingCreation.Add(MessageWindowClass);
                    break;
            }
        }

        internal void Run()
            => new AppLifetime(Lifetime, Windows, Tray, Menu, Hotkeys, Paint, () => [s_monitor], s_english).Run();

        internal void Send(uint message, nuint wParam = 0, nint lParam = 0) => Windows.Send(MessageWindow, message, wParam, lParam);
    }
}
