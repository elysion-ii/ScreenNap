using ScreenNap.Logging;
using Xunit;

namespace ScreenNap.Tests.Logging;

public sealed class LogDirectoryChoiceTests
{
    private const string DefaultDirectory = @"D:\default";

    [Theory]
    [Trait("Requirement", "LOG-1")]
    [Trait("Requirement", "LOG-2")]
    [Trait("Requirement", "LOG-9")]
    [Trait("Requirement", "LOG-11")]
    [InlineData(null, true, true, @"D:\default", "INFO", @"SCREENNAP_LOG_DIR is not set; logging to D:\default")]
    [InlineData("", true, true, @"D:\default", "INFO", @"SCREENNAP_LOG_DIR is not set; logging to D:\default")]
    [InlineData(null, true, false, null, null, null)]
    [InlineData(" ", true, true, @"D:\default", "WARN", @"SCREENNAP_LOG_DIR "" "" is not an absolute directory path that can be created; logging to D:\default")]
    [InlineData("logs", true, true, @"D:\default", "WARN", @"SCREENNAP_LOG_DIR ""logs"" is not an absolute directory path that can be created; logging to D:\default")]
    [InlineData(@"logs\screen", true, true, @"D:\default", "WARN", @"SCREENNAP_LOG_DIR ""logs\screen"" is not an absolute directory path that can be created; logging to D:\default")]
    [InlineData(@"C:logs", true, true, @"D:\default", "WARN", @"SCREENNAP_LOG_DIR ""C:logs"" is not an absolute directory path that can be created; logging to D:\default")]
    [InlineData(@"\logs", true, true, @"D:\default", "WARN", @"SCREENNAP_LOG_DIR ""\logs"" is not an absolute directory path that can be created; logging to D:\default")]
    [InlineData(@"C:\logs", true, true, @"C:\logs", null, null)]
    [InlineData(@"\\server\share\logs", true, true, @"\\server\share\logs", null, null)]
    [InlineData(@"C:\logs", false, true, @"D:\default", "WARN", @"SCREENNAP_LOG_DIR ""C:\logs"" is not an absolute directory path that can be created; logging to D:\default")]
    [InlineData(@"C:\logs", false, false, null, null, null)]
    internal void Choose_AnySettingAndDirectories_ChoosesTheDirectoryAndItsNotice(
        string? configured,
        bool configuredCreatable,
        bool defaultCreatable,
        string? expectedDirectory,
        string? expectedLevel,
        string? expectedNotice)
    {
        bool TryCreate(string directory) => directory == DefaultDirectory ? defaultCreatable : configuredCreatable;

        LogDirectoryChoice choice = LogDirectoryChoice.Choose(configured, DefaultDirectory, TryCreate);

        Assert.Equal(new LogDirectoryChoice(expectedDirectory, expectedLevel, expectedNotice), choice);
    }
}
