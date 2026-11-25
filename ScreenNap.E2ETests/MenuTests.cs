using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class MenuTests
{
    // The identifier ScreenNap registers Ctrl+Shift+Alt+1 under, which its WM_HOTKEY carries
    private const nuint FirstMonitorHotkeyId = 3000;

    public MenuTests() => TestAssets.RequireNoRunningScreenNap();

    [Fact]
    [Trait("Requirement", "TRAY-4")]
    [Trait("Requirement", "MENU-1")]
    [Trait("Requirement", "MENU-3")]
    [Trait("Requirement", "MENU-4")]
    [Trait("Requirement", "MENU-7")]
    [Trait("Requirement", "MENU-8")]
    [Trait("Requirement", "MON-1")]
    [Trait("Requirement", "MON-2")]
    [Trait("Requirement", "MON-5")]
    [Trait("Requirement", "VER-1")]
    [Trait("Requirement", "VER-4")]
    internal void Open_NoBlackout_ListsTheVersionEveryMonitorAndExit()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        IReadOnlyList<RECT> monitors = app.Monitors();

        IReadOnlyList<MenuEntry> entries = app.OpenMenu();

        Assert.Equal(monitors.Count + 4, entries.Count);
        Assert.Equal($"ScreenNap {TestAssets.Version}", entries[0].Text);
        Assert.True(entries[1].IsSeparator);
        for (int i = 0; i < monitors.Count; i++)
        {
            MenuEntry monitorEntry = entries[2 + i];
            Assert.Equal(ScreenNapApp.MonitorCommandBase + (uint)i, monitorEntry.Id);
            Assert.StartsWith($"&{i + 1}  ", monitorEntry.Text, StringComparison.Ordinal);
            Assert.Contains($"{monitors[i].Width}x{monitors[i].Height}", monitorEntry.Text, StringComparison.Ordinal);
            Assert.False(monitorEntry.IsChecked);
        }
        Assert.True(entries[^2].IsSeparator);
        Assert.Equal(ScreenNapApp.ExitCommand, entries[^1].Id);
        Assert.DoesNotContain(entries, entry => entry.Id == ScreenNapApp.ReleaseAllCommand);
        app.WaitForLog("Monitors enumerated");
    }

    [Fact]
    [Trait("Requirement", "RUN-1")]
    [Trait("Requirement", "MENU-6")]
    internal void Events_OfDifferentKindsSentBackToBack_AreEachHandledInTheOrderSent()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.OpenMenu();

        // One burst, posted without waiting for any of them to be handled
        app.Post(Win32.WM_COMMAND, ScreenNapApp.MonitorCommandBase, 0);
        app.Post(Win32.WM_HOTKEY, FirstMonitorHotkeyId, 0);
        app.Post(Win32.WM_COMMAND, ScreenNapApp.MonitorCommandBase, 0);
        app.Post(Win32.WM_COMMAND, ScreenNapApp.ReleaseAllCommand, 0);
        app.Post(Win32.WM_HOTKEY, FirstMonitorHotkeyId, 0);

        Wait.Until(() => BlackoutEvents(app).Count == 5, TimeSpan.FromSeconds(10), "ScreenNap handles the five events");
        Assert.Equal(["on", "off", "on", "release", "on"], BlackoutEvents(app));
        app.WaitForBlackoutCount(1);
    }

    private static List<string> BlackoutEvents(ScreenNapApp app)
        => [.. app.LogLines()
            .Select(line => line.Contains("Blackout toggled on", StringComparison.Ordinal) ? "on"
                : line.Contains("Blackout toggled off", StringComparison.Ordinal) ? "off"
                : line.Contains("Releasing all blackout windows", StringComparison.Ordinal) ? "release"
                : null)
            .OfType<string>()];

    [Fact]
    [Trait("Requirement", "TRAY-4")]
    internal void Open_LeftButtonRelease_ShowsTheMenu()
    {
        using ScreenNapApp app = ScreenNapApp.Start();

        IReadOnlyList<MenuEntry> entries = app.OpenMenu(Win32.WM_LBUTTONUP);

        Assert.Equal(ScreenNapApp.ExitCommand, entries[^1].Id);
    }

    [Fact]
    [Trait("Requirement", "MENU-1")]
    [Trait("Requirement", "VER-2")]
    internal void VersionItem_Selected_IsDisabledAndDoesNothing()
    {
        using ScreenNapApp app = ScreenNapApp.Start();

        MenuEntry version = app.OpenMenu()[0];
        app.Select(version.Id);
        app.OpenMenu();

        Assert.True(version.IsDisabled);
        Assert.Empty(app.BlackoutWindows());
        Assert.False(app.Process.HasExited);
    }

    [Fact]
    [Trait("Requirement", "MENU-2")]
    [Trait("Requirement", "MENU-5")]
    [Trait("Requirement", "MENU-7")]
    [Trait("Requirement", "RUN-3")]
    internal void Open_WithABlackout_ChecksTheMonitorAndOffersReleaseAll()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.Select(ScreenNapApp.MonitorCommandBase);
        app.WaitForBlackoutCount(1);

        IReadOnlyList<MenuEntry> entries = app.OpenMenu();

        Assert.True(entries.Single(entry => entry.Id == ScreenNapApp.MonitorCommandBase).IsChecked);
        Assert.Contains(entries, entry => entry.Id == ScreenNapApp.ReleaseAllCommand);
        for (int i = 1; i < entries.Count; i++)
            Assert.False(entries[i - 1].IsSeparator && entries[i].IsSeparator, $"Separators at positions {i - 1} and {i}");
    }
}
