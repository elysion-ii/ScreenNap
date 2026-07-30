using ScreenNap.Core;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class MenuModelTests
{
    private const string VersionText = "ScreenNap 1.4.0";

    [Fact]
    public void Build_NoMonitors_ContainsVersionSingleSeparatorAndExit()
    {
        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build([], new HashSet<string>(), VersionText);

        Assert.Collection(items,
            item => Assert.Equal(VersionText, item.Text),
            item => Assert.True(item.IsSeparator),
            item => Assert.Equal(WindowStyles.MENU_ID_EXIT, item.CommandId));
    }

    [Fact]
    public void Build_OneMonitor_PlacesDisabledVersionItemFirst()
    {
        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build([Monitor("DISPLAY1")], new HashSet<string>(), VersionText);

        Assert.Equal(new MenuItem(false, false, WindowStyles.MENU_ID_NONE, VersionText, true), items[0]);
    }

    [Fact]
    public void Build_OneInactiveMonitor_CreatesUncheckedMonitorItem()
    {
        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build([Monitor("DISPLAY1")], new HashSet<string>(), VersionText);

        Assert.False(items[2].Checked);
        Assert.Equal(WindowStyles.MENU_ID_MONITOR_BASE, items[2].CommandId);
        Assert.StartsWith("&1", items[2].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_MixedActiveMonitors_CreatesCompleteOrderedModel()
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
    [InlineData(0, 2, 0, -1)]
    [InlineData(1000, 2, 3, 0)]
    [InlineData(1001, 2, 3, 1)]
    [InlineData(2000, 2, 2, -1)]
    [InlineData(9999, 2, 1, -1)]
    [InlineData(999, 2, 0, -1)]
    [InlineData(1002, 2, 0, -1)]
    public void Interpret_AnyCommandId_MapsToCommand(int id, int count, int expectedKind, int expectedIndex)
    {
        MenuCommand command = MenuCommandInterpreter.Interpret(id, count);

        Assert.Equal((MenuCommandKind)expectedKind, command.Kind);
        Assert.Equal(expectedIndex, command.MonitorIndex);
    }

    private static MonitorInfo Monitor(string path)
        => new(path, path, new RECT { Right = 1920, Bottom = 1080 }, false, default);
}
