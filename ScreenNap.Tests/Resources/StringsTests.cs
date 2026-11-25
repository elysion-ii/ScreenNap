using System.Globalization;
using System.Resources;
using ScreenNap.Resources;
using Xunit;

namespace ScreenNap.Tests.Resources;

public sealed class StringsTests
{
    // fr-FR has no resource set of its own, so it must land on the neutral English one
    [Theory]
    [Trait("Requirement", "L10N-1")]
    [Trait("Requirement", "L10N-2")]
    [Trait("Requirement", "L10N-3")]
    [Trait("Requirement", "LIFE-2")]
    [Trait("Requirement", "ERR-1")]
    [Trait("Requirement", "ERR-2")]
    [Trait("Requirement", "ERR-12")]
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
    [InlineData("fr-FR", nameof(Strings.NotifyStartFailed), "ScreenNap could not start. See the log for details.")]
    [InlineData("en-US", nameof(Strings.NotifyStartFailed), "ScreenNap could not start. See the log for details.")]
    [InlineData("ja-JP", nameof(Strings.NotifyStartFailed), "ScreenNap を起動できませんでした。詳細はログを参照してください。")]
    [InlineData("fr-FR", nameof(Strings.NotifyTrayFailed), "ScreenNap could not create its tray icon and will exit.")]
    [InlineData("en-US", nameof(Strings.NotifyTrayFailed), "ScreenNap could not create its tray icon and will exit.")]
    [InlineData("ja-JP", nameof(Strings.NotifyTrayFailed), "ScreenNap はトレイアイコンを作成できなかったため終了します。")]
    [InlineData("fr-FR", nameof(Strings.NotifyStopped), "ScreenNap can no longer receive messages from Windows and will exit. See the log for details.")]
    [InlineData("en-US", nameof(Strings.NotifyStopped), "ScreenNap can no longer receive messages from Windows and will exit. See the log for details.")]
    [InlineData("ja-JP", nameof(Strings.NotifyStopped), "ScreenNap は Windows からのメッセージを受け取れなくなったため終了します。詳細はログを参照してください。")]
    [InlineData("fr-FR", nameof(Strings.NotifyBlackoutNotCreated), "Could not black out {0}.")]
    [InlineData("en-US", nameof(Strings.NotifyBlackoutNotCreated), "Could not black out {0}.")]
    [InlineData("ja-JP", nameof(Strings.NotifyBlackoutNotCreated), "{0} を暗転できませんでした。")]
    [InlineData("fr-FR", nameof(Strings.NotifyBlackoutNotClosed), "Could not release the blackout on {0}.")]
    [InlineData("en-US", nameof(Strings.NotifyBlackoutNotClosed), "Could not release the blackout on {0}.")]
    [InlineData("ja-JP", nameof(Strings.NotifyBlackoutNotClosed), "{0} の暗転を解除できませんでした。")]
    [InlineData("fr-FR", nameof(Strings.NotifyReleaseAllIncomplete), "{0} blackout(s) could not be released.")]
    [InlineData("en-US", nameof(Strings.NotifyReleaseAllIncomplete), "{0} blackout(s) could not be released.")]
    [InlineData("ja-JP", nameof(Strings.NotifyReleaseAllIncomplete), "{0} 台の暗転を解除できませんでした。")]
    [InlineData("fr-FR", nameof(Strings.NotifyBlackoutNotRestored), "Could not restore the blackout on {0}.")]
    [InlineData("en-US", nameof(Strings.NotifyBlackoutNotRestored), "Could not restore the blackout on {0}.")]
    [InlineData("ja-JP", nameof(Strings.NotifyBlackoutNotRestored), "{0} の暗転を復元できませんでした。")]
    [InlineData("fr-FR", nameof(Strings.NotifyHotkeysUnavailable), "These hotkeys are unavailable: {0}")]
    [InlineData("en-US", nameof(Strings.NotifyHotkeysUnavailable), "These hotkeys are unavailable: {0}")]
    [InlineData("ja-JP", nameof(Strings.NotifyHotkeysUnavailable), "次のホットキーは使用できません: {0}")]
    [InlineData("fr-FR", nameof(Strings.NotifyOverlaysNotCreated), "Could not show the identification number on monitor {0}.")]
    [InlineData("en-US", nameof(Strings.NotifyOverlaysNotCreated), "Could not show the identification number on monitor {0}.")]
    [InlineData("ja-JP", nameof(Strings.NotifyOverlaysNotCreated), "モニター {0} に識別番号を表示できませんでした。")]
    internal void Value_AnyCultureAndKey_SelectsTheResourceSetForThatCulture(string culture, string key, string expected)
    {
        Assert.Equal(expected, Value(key, CultureInfo.GetCultureInfo(culture)));
    }

    // A culture whose resources ship separately must still resolve through its parent
    [Theory]
    [Trait("Requirement", "L10N-1")]
    [InlineData("ja")]
    [InlineData("ja-JP")]
    internal void Value_JapaneseCultureOrItsParent_SelectsJapanese(string culture)
    {
        Assert.Equal("終了", Strings.MenuExit(CultureInfo.GetCultureInfo(culture)));
    }

    [Fact]
    [Trait("Requirement", "L10N-4")]
    internal void Lookup_MissingResourceSet_ReturnsTheKey()
    {
        var missing = new ResourceManager("ScreenNap.Resources.Missing", typeof(Strings).Assembly);

        Assert.Equal("MenuExit", LocalizedText.Lookup(missing, "MenuExit", CultureInfo.GetCultureInfo("ja-JP")));
    }

    [Fact]
    [Trait("Requirement", "L10N-4")]
    internal void Lookup_MissingKey_ReturnsTheKey()
    {
        var resources = new ResourceManager("ScreenNap.Resources.Strings", typeof(Strings).Assembly);

        Assert.Equal("NoSuchKey", LocalizedText.Lookup(resources, "NoSuchKey", CultureInfo.GetCultureInfo("ja-JP")));
    }

    private static string Value(string key, CultureInfo culture) => key switch
    {
        nameof(Strings.MenuReleaseAll) => Strings.MenuReleaseAll(culture),
        nameof(Strings.MenuExit) => Strings.MenuExit(culture),
        nameof(Strings.MenuPrimary) => Strings.MenuPrimary(culture),
        nameof(Strings.MenuActive) => Strings.MenuActive(culture),
        nameof(Strings.TooltipNormal) => Strings.TooltipNormal(culture),
        nameof(Strings.TooltipActive) => Strings.TooltipActive(culture),
        nameof(Strings.NotifyAlreadyRunning) => Strings.NotifyAlreadyRunning(culture),
        nameof(Strings.NotifyTitle) => Strings.NotifyTitle(culture),
        nameof(Strings.NotifyStartFailed) => Strings.NotifyStartFailed(culture),
        nameof(Strings.NotifyTrayFailed) => Strings.NotifyTrayFailed(culture),
        nameof(Strings.NotifyStopped) => Strings.NotifyStopped(culture),
        nameof(Strings.NotifyBlackoutNotCreated) => Strings.NotifyBlackoutNotCreated(culture),
        nameof(Strings.NotifyBlackoutNotClosed) => Strings.NotifyBlackoutNotClosed(culture),
        nameof(Strings.NotifyReleaseAllIncomplete) => Strings.NotifyReleaseAllIncomplete(culture),
        nameof(Strings.NotifyBlackoutNotRestored) => Strings.NotifyBlackoutNotRestored(culture),
        nameof(Strings.NotifyHotkeysUnavailable) => Strings.NotifyHotkeysUnavailable(culture),
        nameof(Strings.NotifyOverlaysNotCreated) => Strings.NotifyOverlaysNotCreated(culture),
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown resource key"),
    };
}
