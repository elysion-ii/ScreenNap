using ScreenNap.Core;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class MonitorInfoTests
{
    [Theory]
    [InlineData("en-US", 1, false, false, "&1  Display  1920x1080")]
    [InlineData("en-US", 2, true, false, "&2  Display  1920x1080  [Primary]")]
    [InlineData("en-US", 3, false, true, "&3  Display  1920x1080  (Active)")]
    [InlineData("en-US", 4, true, true, "&4  Display  1920x1080  [Primary]  (Active)")]
    [InlineData("ja-JP", 1, false, false, "&1  Display  1920x1080")]
    [InlineData("ja-JP", 2, true, false, "&2  Display  1920x1080  [メイン]")]
    [InlineData("ja-JP", 3, false, true, "&3  Display  1920x1080  (暗転中)")]
    [InlineData("ja-JP", 4, true, true, "&4  Display  1920x1080  [メイン]  (暗転中)")]
    public void BuildMenuLabel_FormatsLocalizedState(
        string culture,
        int index,
        bool isPrimary,
        bool isActive,
        string expected)
    {
        using var scope = new CultureScope(culture);
        var monitor = new MonitorInfo(
            "DISPLAY1",
            "Display",
            new RECT { Right = 1920, Bottom = 1080 },
            isPrimary,
            default);

        Assert.Equal(expected, monitor.BuildMenuLabel(index, isActive));
    }

    [Fact]
    public void MonitorIdentity_DefaultIsAllZeros()
    {
        MonitorIdentity identity = default;

        Assert.Equal((ushort)0, identity.EdidManufacturerId);
        Assert.Equal((ushort)0, identity.EdidProductCodeId);
        Assert.Equal(0u, identity.ConnectorInstance);
    }

    [Theory]
    [InlineData(1, 2, 3, 1, 2, 3, true)]
    [InlineData(1, 2, 3, 1, 2, 4, false)]
    public void MonitorIdentity_UsesValueEquality(
        ushort manufacturer1,
        ushort product1,
        uint connector1,
        ushort manufacturer2,
        ushort product2,
        uint connector2,
        bool expected)
    {
        var first = new MonitorIdentity(manufacturer1, product1, connector1);
        var second = new MonitorIdentity(manufacturer2, product2, connector2);

        Assert.Equal(expected, first == second);
    }
}
