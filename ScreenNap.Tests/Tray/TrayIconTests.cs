using System.Globalization;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using ScreenNap.Tray;
using Xunit;

namespace ScreenNap.Tests.Tray;

public sealed class TrayIconTests
{
    private static readonly CultureInfo s_english = CultureInfo.GetCultureInfo("en-US");
    private static readonly IntPtr s_messageWindow = new(0x77);

    [Fact]
    [Trait("Requirement", "TRAY-1")]
    [Trait("Requirement", "TRAY-2")]
    [Trait("Requirement", "BLK-5")]
    internal void UpdateState_CountChanges_ShowsTheIconAndTooltipOfTheCount()
    {
        var api = new FakeTrayIconApi();
        TrayIcon trayIcon = CreatedTrayIcon(api);
        Assert.Equal((FakeTrayIconApi.NormalIcon, "ScreenNap"), (api.ShownIcon, api.ShownTip));

        trayIcon.UpdateState(2);

        Assert.Equal(FakeTrayIconApi.ActiveIcon, api.ShownIcon);
        Assert.Equal("ScreenNap (2 active)", api.ShownTip);

        trayIcon.UpdateState(0);

        Assert.Equal((FakeTrayIconApi.NormalIcon, "ScreenNap"), (api.ShownIcon, api.ShownTip));
    }

    [Fact]
    [Trait("Requirement", "BLK-5")]
    internal void UpdateState_WindowsRejectsTheUpdate_LogsKeepsTheShownStateAndTheNextChangeTriesAgain()
    {
        var api = new FakeTrayIconApi();
        TrayIcon trayIcon = CreatedTrayIcon(api);
        api.FailingCommands.Add(WindowStyles.NIM_MODIFY);
        using var log = new LogCapture();

        trayIcon.UpdateState(1);

        Assert.Equal(FakeTrayIconApi.NormalIcon, api.ShownIcon);
        Assert.Equal("ScreenNap", api.ShownTip);
        LogAssert.Logged(log, "WARN", $"Shell_NotifyIconW(NIM_MODIFY) failed for the tray state; the next change of the blackout count tries again (Win32 error: {FakeTrayIconApi.FailureError})");

        api.FailingCommands.Clear();
        trayIcon.UpdateState(2);

        Assert.Equal(FakeTrayIconApi.ActiveIcon, api.ShownIcon);
        Assert.Contains("2", api.ShownTip, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "BLK-5")]
    internal void Notify_WindowsRejectsTheNotification_LogsIt()
    {
        var api = new FakeTrayIconApi();
        TrayIcon trayIcon = CreatedTrayIcon(api);
        api.FailingCommands.Add(WindowStyles.NIM_MODIFY);
        using var log = new LogCapture();

        trayIcon.Notify("Blackout could not be created");

        Assert.Empty(api.ShownNotifications);
        LogAssert.Logged(log, "WARN", "Shell_NotifyIconW(NIM_MODIFY) failed for a tray notification");
    }

    [Fact]
    [Trait("Requirement", "TRAY-5")]
    internal void Restore_TaskbarRecreated_AddsTheIconWithTheCurrentCount()
    {
        var api = new FakeTrayIconApi();
        TrayIcon trayIcon = CreatedTrayIcon(api);
        api.RecreateTaskbar();

        trayIcon.Restore(3);

        Assert.True(api.IconShown);
        Assert.Equal(FakeTrayIconApi.ActiveIcon, api.ShownIcon);
        Assert.Contains("3", api.ShownTip, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "ERR-2")]
    internal void Create_WindowsRejectsTheIcon_LogsItAndReportsFailure()
    {
        var api = new FakeTrayIconApi();
        api.FailingCommands.Add(WindowStyles.NIM_ADD);
        var trayIcon = new TrayIcon(s_messageWindow, s_english, api);
        using var log = new LogCapture();

        Assert.False(trayIcon.Create());
        LogAssert.Logged(log, "ERROR", "Shell_NotifyIconW(NIM_ADD) failed");
    }

    [Theory]
    [Trait("Requirement", "ERR-2")]
    [InlineData("icon-normal.ico")]
    [InlineData("icon-active.ico")]
    internal void Create_ImageCannotBeLoaded_LogsItAndAddsNoIcon(string missingImage)
    {
        var api = new FakeTrayIconApi();
        api.IconsFailingToLoad.Add(missingImage);
        var trayIcon = new TrayIcon(s_messageWindow, s_english, api);
        using var log = new LogCapture();

        Assert.False(trayIcon.Create());
        Assert.Equal(0, api.Count(WindowStyles.NIM_ADD));
        LogAssert.Logged(log, "ERROR", $"Tray icon image could not be loaded ({missingImage})");
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void Remove_IconsCannotBeDestroyed_LogsItWithoutANotification()
    {
        var api = new FakeTrayIconApi();
        TrayIcon trayIcon = CreatedTrayIcon(api);
        api.DestroyIconFails = true;
        using var log = new LogCapture();

        trayIcon.Remove();

        Assert.False(api.IconShown);
        Assert.Empty(api.ShownNotifications);
        LogAssert.Logged(log, "WARN", "DestroyIcon failed for normal tray icon");
        LogAssert.Logged(log, "WARN", "DestroyIcon failed for active tray icon");
    }

    [Fact]
    [Trait("Requirement", "LIFE-3")]
    internal void Notify_AfterTheIconIsRemoved_ShowsNothing()
    {
        var api = new FakeTrayIconApi();
        TrayIcon trayIcon = CreatedTrayIcon(api);
        trayIcon.Remove();

        trayIcon.Notify("Blackout could not be closed");

        Assert.Equal(0, api.Calls.Count(call => (call.Flags & WindowStyles.NIF_INFO) != 0));
    }

    private static TrayIcon CreatedTrayIcon(FakeTrayIconApi api)
    {
        var trayIcon = new TrayIcon(s_messageWindow, s_english, api);
        Assert.True(trayIcon.Create());
        return trayIcon;
    }
}
