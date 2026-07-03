using ScreenNap.Core;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class TrayStateTests
{
    [Theory]
    [InlineData("en-US", 0, false, "ScreenNap")]
    [InlineData("en-US", 1, true, "ScreenNap (1 active)")]
    [InlineData("en-US", 3, true, "ScreenNap (3 active)")]
    [InlineData("ja-JP", 0, false, "ScreenNap")]
    [InlineData("ja-JP", 1, true, "ScreenNap (1台 暗転中)")]
    [InlineData("ja-JP", 3, true, "ScreenNap (3台 暗転中)")]
    public void For_SelectsLocalizedState(string culture, int count, bool expectedActive, string expectedTip)
    {
        using var scope = new CultureScope(culture);

        TrayState state = TrayState.For(count);

        Assert.Equal(expectedActive, state.UseActiveIcon);
        Assert.Equal(expectedTip, state.TipText);
    }

    [Theory]
    [InlineData(126, 126)]
    [InlineData(127, 127)]
    [InlineData(128, 127)]
    public void TruncateTip_EnforcesMaximumLength(int inputLength, int expectedLength)
    {
        string result = TrayState.TruncateTip(new string('x', inputLength));

        Assert.Equal(expectedLength, result.Length);
    }
}
