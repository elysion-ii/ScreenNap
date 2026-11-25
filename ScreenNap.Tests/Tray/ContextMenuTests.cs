using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using ScreenNap.Tray;
using Xunit;

namespace ScreenNap.Tests.Tray;

public sealed class ContextMenuTests
{
    private static readonly CultureInfo s_english = CultureInfo.GetCultureInfo("en-US");
    private static readonly IntPtr s_messageWindow = new(0x77);
    private static readonly MonitorInfo s_monitor = new(
        @"\\.\DISPLAY1", "Monitor", new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 }, true, MonitorIdentity.For(null, "connection 1"));

    [Fact]
    [Trait("Requirement", "TRAY-4")]
    internal void Show_CursorPositionKnown_OpensTheMenuThere()
    {
        var api = new FakePopupMenuApi { CursorPosition = new POINT { X = 1500, Y = 1040 } };

        Menu(api, new FakeUserNotifier()).Show(s_messageWindow);

        Assert.Equal([(1500, 1040)], api.OpenedAt);
    }

    [Fact]
    [Trait("Requirement", "TRAY-4")]
    internal void Show_CursorPositionUnavailable_OpensTheMenuAtTheScreenOriginAndLogsIt()
    {
        var api = new FakePopupMenuApi { CursorPosition = null };
        using var log = new LogCapture();

        Menu(api, new FakeUserNotifier()).Show(s_messageWindow);

        Assert.Equal([(0, 0)], api.OpenedAt);
        LogAssert.Logged(log, "WARN", $"GetCursorPos failed before displaying context menu; opening it at 0,0 (Win32 error: {FakePopupMenuApi.FailureError})");
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void Show_MenuCannotBeDestroyed_LogsItWithoutANotification()
    {
        var api = new FakePopupMenuApi { DestroyMenuFails = true };
        var notifier = new FakeUserNotifier();
        using var log = new LogCapture();

        Menu(api, notifier).Show(s_messageWindow);

        Assert.Empty(notifier.Messages);
        LogAssert.Logged(log, "WARN", "DestroyMenu failed for context menu");
    }

    [Fact]
    [Trait("Requirement", "MENU-8")]
    internal void HandleCommand_Exit_RequestsTheExit()
    {
        int exits = 0;
        var menu = new ContextMenu(Manager(new FakeUserNotifier()), () => [s_monitor], new FakePopupMenuApi(), () => exits++, s_english);
        menu.Show(s_messageWindow);

        menu.HandleCommand(MenuCommandIds.Exit);

        Assert.Equal(1, exits);
    }

    // The display changes while the menu is open, before the user selects the monitor
    [Theory]
    [Trait("Requirement", "MENU-6")]
    [Trait("Requirement", "WIN-1")]
    [InlineData(true)]
    [InlineData(false)]
    internal void HandleCommand_MonitorChangedWhileTheMenuWasOpen_ActsOnTheMonitorAsItIsNow(bool stillPresent)
    {
        var windows = new FakeWindowApi();
        var manager = new BlackoutManager(new BlackoutWindowFactory(windows), new FakeUserNotifier(), s_english);
        MonitorInfo moved = s_monitor with { DevicePath = @"\\.\DISPLAY2", Bounds = new RECT { Left = 1920, Right = 4480, Bottom = 1440 } };
        IReadOnlyList<MonitorInfo> current = [s_monitor];
        var menu = new ContextMenu(manager, () => current, new FakePopupMenuApi(), () => { }, s_english);
        menu.Show(s_messageWindow);
        current = stillPresent ? [moved] : [];

        menu.HandleCommand(MenuCommandIds.MonitorBase);

        if (stillPresent)
            Assert.Equal((1920, 0, 2560, 1440), Assert.Single(windows.LiveWindows("ScreenNap_Blackout")).CreatedBounds);
        else
            Assert.Empty(windows.LiveWindows("ScreenNap_Blackout"));
    }

    private static ContextMenu Menu(FakePopupMenuApi api, FakeUserNotifier notifier)
        => new(Manager(notifier), () => [s_monitor], api, () => { }, s_english);

    private static BlackoutManager Manager(FakeUserNotifier notifier)
        => new(new BlackoutWindowFactory(new FakeWindowApi()), notifier, s_english);
}
