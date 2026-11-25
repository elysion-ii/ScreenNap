using System.Globalization;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tray;
using Xunit;

namespace ScreenNap.Tests.Tray;

public sealed class MonitorMenuLabelTests
{
    [Theory]
    [InlineData("en-US", 1, false, false, "&1  Display  1920x1080")]
    [InlineData("en-US", 2, true, false, "&2  Display  1920x1080  [Primary]")]
    [InlineData("en-US", 3, false, true, "&3  Display  1920x1080  (Active)")]
    [InlineData("en-US", 4, true, true, "&4  Display  1920x1080  [Primary]  (Active)")]
    [InlineData("en-US", 10, false, false, "&10  Display  1920x1080")]
    [InlineData("ja-JP", 1, false, false, "&1  Display  1920x1080")]
    [InlineData("ja-JP", 2, true, false, "&2  Display  1920x1080  [メイン]")]
    [InlineData("ja-JP", 3, false, true, "&3  Display  1920x1080  (暗転中)")]
    [InlineData("ja-JP", 4, true, true, "&4  Display  1920x1080  [メイン]  (暗転中)")]
    [Trait("Requirement", "MENU-4")]
    [Trait("Requirement", "MON-4")]
    [Trait("Requirement", "L10N-1")]
    [Trait("Requirement", "MENU-9")]
    internal void Build_AnyCultureAndState_FormatsLocalizedLabel(
        string culture,
        int number,
        bool isPrimary,
        bool isActive,
        string expected)
    {
        var monitor = new MonitorInfo(
            "DISPLAY1",
            "Display",
            new RECT { Right = 1920, Bottom = 1080 },
            isPrimary,
            default);

        Assert.Equal(expected, MonitorMenuLabel.Build(monitor, number, isActive, CultureInfo.GetCultureInfo(culture)));
    }
}
