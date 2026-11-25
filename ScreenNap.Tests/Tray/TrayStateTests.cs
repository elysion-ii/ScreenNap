using System.Globalization;
using ScreenNap.Tray;
using Xunit;

namespace ScreenNap.Tests.Tray;

public sealed class TrayStateTests
{
    [Theory]
    [Trait("Requirement", "TRAY-1")]
    [Trait("Requirement", "TRAY-2")]
    [Trait("Requirement", "L10N-3")]
    [InlineData("en-US", 0, false, "ScreenNap")]
    [InlineData("en-US", 1, true, "ScreenNap (1 active)")]
    [InlineData("en-US", 3, true, "ScreenNap (3 active)")]
    [InlineData("ja-JP", 0, false, "ScreenNap")]
    [InlineData("ja-JP", 1, true, "ScreenNap (1台 暗転中)")]
    [InlineData("ja-JP", 3, true, "ScreenNap (3台 暗転中)")]
    internal void For_AnyActiveCountAndCulture_SelectsLocalizedState(string culture, int count, bool expectedActive, string expectedTip)
    {
        TrayState state = TrayState.For(count, CultureInfo.GetCultureInfo(culture));

        Assert.Equal(expectedActive, state.UseActiveIcon);
        Assert.Equal(expectedTip, state.TipText);
    }

    [Theory]
    [Trait("Requirement", "TRAY-3")]
    [InlineData(0, 0)]
    [InlineData(126, 126)]
    [InlineData(127, 127)]
    [InlineData(128, 127)]
    internal void TruncateTip_AnyLength_EnforcesMaximumLength(int inputLength, int expectedLength)
    {
        string result = TrayState.TruncateTip(new string('x', inputLength));

        Assert.Equal(expectedLength, result.Length);
    }

    [Theory]
    [Trait("Requirement", "TRAY-6")]
    [InlineData(0, 0)]
    [InlineData(254, 254)]
    [InlineData(255, 255)]
    [InlineData(256, 255)]
    internal void TruncateInfo_AnyLength_EnforcesMaximumLength(int inputLength, int expectedLength)
    {
        string result = TrayState.TruncateInfo(new string('x', inputLength));

        Assert.Equal(expectedLength, result.Length);
    }

    [Theory]
    [Trait("Requirement", "TRAY-6")]
    [InlineData(0, 0)]
    [InlineData(62, 62)]
    [InlineData(63, 63)]
    [InlineData(64, 63)]
    internal void TruncateInfoTitle_AnyLength_EnforcesMaximumLength(int inputLength, int expectedLength)
    {
        string result = TrayState.TruncateInfoTitle(new string('x', inputLength));

        Assert.Equal(expectedLength, result.Length);
    }
}
