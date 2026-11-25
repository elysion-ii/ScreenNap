using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Logging;
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
        @"\\.\DISPLAY1", "Monitor", new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 }, true, new MonitorIdentity(1, 2, 0));

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

    private static ContextMenu Menu(FakePopupMenuApi api, FakeUserNotifier notifier)
        => new(Manager(notifier), () => [s_monitor], api, () => { }, s_english);

    private static BlackoutManager Manager(FakeUserNotifier notifier)
        => new(new BlackoutWindowFactory(new FakeWindowApi()), notifier, s_english);
}
