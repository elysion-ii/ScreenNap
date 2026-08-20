using ScreenNap.Resources;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Resources;

public sealed class StringsTests
{
    // fr-FR has no resource set of its own, so it must land on the neutral English one
    [Theory]
    [InlineData("fr-FR", nameof(Strings.MenuReleaseAll), "Release All")]
    [InlineData("en-US", nameof(Strings.MenuReleaseAll), "Release All")]
    [InlineData("ja-JP", nameof(Strings.MenuReleaseAll), "すべて解除")]
    [InlineData("fr-FR", nameof(Strings.MenuExit), "Exit")]
    [InlineData("en-US", nameof(Strings.MenuExit), "Exit")]
    [InlineData("ja-JP", nameof(Strings.MenuExit), "終了")]
    [InlineData("fr-FR", nameof(Strings.MenuPrimary), "[Primary]")]
    [InlineData("en-US", nameof(Strings.MenuPrimary), "[Primary]")]
    [InlineData("ja-JP", nameof(Strings.MenuPrimary), "[メイン]")]
    [InlineData("fr-FR", nameof(Strings.MenuActive), "(Active)")]
    [InlineData("en-US", nameof(Strings.MenuActive), "(Active)")]
    [InlineData("ja-JP", nameof(Strings.MenuActive), "(暗転中)")]
    [InlineData("fr-FR", nameof(Strings.TooltipNormal), "ScreenNap")]
    [InlineData("en-US", nameof(Strings.TooltipNormal), "ScreenNap")]
    [InlineData("ja-JP", nameof(Strings.TooltipNormal), "ScreenNap")]
    [InlineData("fr-FR", nameof(Strings.TooltipActive), "ScreenNap ({0} active)")]
    [InlineData("en-US", nameof(Strings.TooltipActive), "ScreenNap ({0} active)")]
    [InlineData("ja-JP", nameof(Strings.TooltipActive), "ScreenNap ({0}台 暗転中)")]
    [InlineData("fr-FR", nameof(Strings.NotifyAlreadyRunning), "ScreenNap is already running.")]
    [InlineData("en-US", nameof(Strings.NotifyAlreadyRunning), "ScreenNap is already running.")]
    [InlineData("ja-JP", nameof(Strings.NotifyAlreadyRunning), "ScreenNap は既に起動しています。")]
    [InlineData("fr-FR", nameof(Strings.NotifyTitle), "ScreenNap")]
    [InlineData("en-US", nameof(Strings.NotifyTitle), "ScreenNap")]
    [InlineData("ja-JP", nameof(Strings.NotifyTitle), "ScreenNap")]
    public void Value_AnyCultureAndKey_SelectsTheResourceSetForThatCulture(string culture, string key, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, Value(key));
    }

    // A culture whose resources ship separately must still resolve through its parent
    [Theory]
    [InlineData("ja")]
    [InlineData("ja-JP")]
    public void Value_JapaneseCultureOrItsParent_SelectsJapanese(string culture)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal("終了", Strings.MenuExit);
    }

    private static string Value(string key) => key switch
    {
        nameof(Strings.MenuReleaseAll) => Strings.MenuReleaseAll,
        nameof(Strings.MenuExit) => Strings.MenuExit,
        nameof(Strings.MenuPrimary) => Strings.MenuPrimary,
        nameof(Strings.MenuActive) => Strings.MenuActive,
        nameof(Strings.TooltipNormal) => Strings.TooltipNormal,
        nameof(Strings.TooltipActive) => Strings.TooltipActive,
        nameof(Strings.NotifyAlreadyRunning) => Strings.NotifyAlreadyRunning,
        nameof(Strings.NotifyTitle) => Strings.NotifyTitle,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown resource key"),
    };
}
