using ScreenNap.Core;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class FailureNoticesTests
{
    [Theory]
    [Trait("Requirement", "ERR-3")]
    [Trait("Requirement", "L10N-3")]
    [InlineData("en-US", "Could not black out DELL U2723QE.")]
    [InlineData("ja-JP", "DELL U2723QE を暗転できませんでした。")]
    internal void BlackoutNotCreated_AnyCulture_NamesTheMonitor(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, FailureNotices.BlackoutNotCreated("DELL U2723QE"));
    }

    [Theory]
    [Trait("Requirement", "ERR-4")]
    [Trait("Requirement", "L10N-3")]
    [InlineData("en-US", "Could not release the blackout on DELL U2723QE.")]
    [InlineData("ja-JP", "DELL U2723QE の暗転を解除できませんでした。")]
    internal void BlackoutNotClosed_AnyCulture_NamesTheMonitor(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, FailureNotices.BlackoutNotClosed("DELL U2723QE"));
    }

    [Theory]
    [Trait("Requirement", "ERR-5")]
    [Trait("Requirement", "L10N-3")]
    [InlineData("en-US", 1, "1 blackout(s) could not be released.")]
    [InlineData("en-US", 2, "2 blackout(s) could not be released.")]
    [InlineData("ja-JP", 2, "2 台の暗転を解除できませんでした。")]
    internal void ReleaseAllIncomplete_AnyCultureAndCount_StatesTheCount(string culture, int remaining, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, FailureNotices.ReleaseAllIncomplete(remaining));
    }

    [Theory]
    [Trait("Requirement", "ERR-6")]
    [Trait("Requirement", "L10N-3")]
    [InlineData("en-US", "Could not restore the blackout on DELL U2723QE.")]
    [InlineData("ja-JP", "DELL U2723QE の暗転を復元できませんでした。")]
    internal void BlackoutNotRestored_AnyCulture_NamesTheMonitor(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, FailureNotices.BlackoutNotRestored("DELL U2723QE"));
    }

    [Theory]
    [Trait("Requirement", "ERR-7")]
    [Trait("Requirement", "L10N-3")]
    [Trait("Requirement", "L10N-5")]
    [InlineData("en-US", "These hotkeys are unavailable: Ctrl+Shift+Alt+1, Ctrl+Shift+Alt+0")]
    [InlineData("ja-JP", "次のホットキーは使用できません: Ctrl+Shift+Alt+1, Ctrl+Shift+Alt+0")]
    internal void HotkeysUnavailable_AnyCulture_ListsTheCombinations(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, FailureNotices.HotkeysUnavailable(["Ctrl+Shift+Alt+1", "Ctrl+Shift+Alt+0"]));
    }

    [Theory]
    [Trait("Requirement", "ERR-8")]
    [Trait("Requirement", "L10N-3")]
    [InlineData("en-US", "Could not show the identification number on monitor 2, 3.")]
    [InlineData("ja-JP", "モニター 2, 3 に識別番号を表示できませんでした。")]
    internal void OverlaysNotCreated_AnyCulture_ListsTheMonitorNumbers(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, FailureNotices.OverlaysNotCreated([2, 3]));
    }
}
