using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class MenuTests
{
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
