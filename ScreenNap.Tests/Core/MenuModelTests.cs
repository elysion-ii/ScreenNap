using ScreenNap.Core;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class MenuModelTests
{
    private const string VersionText = "ScreenNap 1.4.0";

    [Fact]
    [Trait("Requirement", "MENU-1")]
    [Trait("Requirement", "MENU-2")]
    [Trait("Requirement", "MENU-8")]
    internal void Build_NoMonitors_ContainsVersionSingleSeparatorAndExit()
    {
        using var scope = new CultureScope("en-US");
        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build([], new HashSet<string>(), VersionText);

        Assert.Collection(items,
            item => Assert.Equal(VersionText, item.Text),
            item => Assert.True(item.IsSeparator),
            item => Assert.Equal(MenuCommandIds.Exit, item.CommandId));
    }

    [Fact]
    [Trait("Requirement", "MENU-1")]
    [Trait("Requirement", "VER-2")]
    internal void Build_OneMonitor_PlacesDisabledVersionItemFirst()
    {
        using var scope = new CultureScope("en-US");
        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build([Monitor("DISPLAY1")], new HashSet<string>(), VersionText);

        Assert.Equal(new MenuItem(false, false, MenuCommandIds.None, VersionText, true), items[0]);
    }

    [Fact]
    [Trait("Requirement", "MENU-3")]
    [Trait("Requirement", "MENU-5")]
    [Trait("Requirement", "MON-2")]
    internal void Build_OneInactiveMonitor_CreatesUncheckedMonitorItem()
    {
        using var scope = new CultureScope("en-US");
        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build([Monitor("DISPLAY1")], new HashSet<string>(), VersionText);

        Assert.False(items[2].Checked);
        Assert.Equal(MenuCommandIds.MonitorBase, items[2].CommandId);
        Assert.StartsWith("&1", items[2].Text, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "MENU-2")]
    [Trait("Requirement", "MENU-3")]
    [Trait("Requirement", "MENU-4")]
    [Trait("Requirement", "MENU-5")]
    [Trait("Requirement", "MENU-7")]
    internal void Build_MixedActiveMonitors_CreatesCompleteOrderedModel()
    {
        using var scope = new CultureScope("en-US");
        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build(
            [Monitor("DISPLAY1"), Monitor("DISPLAY2")],
            new HashSet<string>(["DISPLAY2"], StringComparer.Ordinal),
            VersionText);

        Assert.Collection(
            items,
            item => Assert.Equal(new MenuItem(false, false, 0, VersionText, true), item),
            item => Assert.Equal(new MenuItem(false, true, 0, null), item),
            item => Assert.Equal(new MenuItem(false, false, 1000, "&1  DISPLAY1  1920x1080"), item),
            item => Assert.Equal(new MenuItem(true, false, 1001, "&2  DISPLAY2  1920x1080  (Active)"), item),
            item => Assert.Equal(new MenuItem(false, true, 0, null), item),
            item => Assert.Equal(new MenuItem(false, false, 2000, "Release All"), item),
            item => Assert.Equal(new MenuItem(false, true, 0, null), item),
            item => Assert.Equal(new MenuItem(false, false, 9999, "Exit"), item));
    }

    [Theory]
    [InlineData(0, 2, MenuCommandKind.None, -1)]
    [InlineData(1000, 2, MenuCommandKind.ToggleMonitor, 0)]
    [InlineData(1001, 2, MenuCommandKind.ToggleMonitor, 1)]
    [InlineData(2000, 2, MenuCommandKind.ReleaseAll, -1)]
    [InlineData(9999, 2, MenuCommandKind.Exit, -1)]
    [InlineData(999, 2, MenuCommandKind.None, -1)]
    [InlineData(1002, 2, MenuCommandKind.None, -1)]
    [Trait("Requirement", "MENU-6")]
    [Trait("Requirement", "MENU-7")]
    [Trait("Requirement", "MENU-8")]
    [Trait("Requirement", "VER-2")]
    internal void Interpret_AnyCommandId_MapsToCommand(int id, int count, MenuCommandKind expectedKind, int expectedIndex)
    {
        MenuCommand command = MenuCommandInterpreter.Interpret(id, count);

        Assert.Equal(expectedKind, command.Kind);
        Assert.Equal(expectedIndex, command.MonitorIndex);
    }

    private static MonitorInfo Monitor(string path)
        => new(path, path, new RECT { Right = 1920, Bottom = 1080 }, false, default);
}
