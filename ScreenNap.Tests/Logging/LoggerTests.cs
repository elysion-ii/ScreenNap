using ScreenNap.Logging;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Logging;

public sealed class LoggerTests
{
    [Theory]
    [Trait("Requirement", "LOG-4")]
    [Trait("Requirement", "LOG-5")]
    [InlineData("en-US", 9, "2026-07-03 12:34:56.789 +09:00 [INFO] Started")]
    [InlineData("ja-JP", 9, "2026-07-03 12:34:56.789 +09:00 [INFO] Started")]
    [InlineData("en-US", 0, "2026-07-03 12:34:56.789 +00:00 [INFO] Started")]
    [InlineData("en-US", -5, "2026-07-03 12:34:56.789 -05:00 [INFO] Started")]
    internal void FormatLine_AnyCultureAndOffset_FormatsLocalTimeWithOffset(string culture, int offsetHours, string expected)
    {
        using var scope = new CultureScope(culture);
        var timestamp = new DateTimeOffset(2026, 7, 3, 12, 34, 56, 789, TimeSpan.FromHours(offsetHours));

        string line = Logger.FormatLine(timestamp, "INFO", "Started");

        Assert.Equal(expected, line);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ja-JP")]
    internal void WithWin32Error_AnyCulture_AppendsTheErrorCode(string culture)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal("SetTimer failed (Win32 error: 1400)", Logger.WithWin32Error("SetTimer failed", 1400));
    }

    [Theory]
    [Trait("Requirement", "LOG-3")]
    [InlineData("en-US")]
    [InlineData("ja-JP")]
    internal void FileNameFor_AnyCulture_UsesTheLocalDate(string culture)
    {
        using var scope = new CultureScope(culture);
        var timestamp = new DateTimeOffset(2026, 7, 3, 23, 59, 0, TimeSpan.FromHours(9));

        Assert.Equal("ScreenNap_20260703.log", Logger.FileNameFor(timestamp));
    }

    [Theory]
    [Trait("Requirement", "LOG-1")]
    [Trait("Requirement", "LOG-2")]
    [InlineData(" ", false)]
    [InlineData("logs", false)]
    [InlineData(@"logs\screen", false)]
    [InlineData(@"C:logs", false)]
    [InlineData(@"\logs", false)]
    [InlineData(@"C:\logs", true)]
    [InlineData(@"\\server\share\logs", true)]
    internal void IsUsableDirectorySetting_AnyValue_AcceptsOnlyFullyQualifiedPaths(string value, bool expected)
    {
        Assert.Equal(expected, Logger.IsUsableDirectorySetting(value));
    }

    [Theory]
    [Trait("Requirement", "LOG-1")]
    [Trait("Requirement", "LOG-2")]
    [Trait("Requirement", "LOG-9")]
    [Trait("Requirement", "LOG-11")]
    [InlineData(null, true, true, @"D:\default", "INFO", @"SCREENNAP_LOG_DIR is not set; logging to D:\default")]
    [InlineData("", true, true, @"D:\default", "INFO", @"SCREENNAP_LOG_DIR is not set; logging to D:\default")]
    [InlineData(null, true, false, null, null, null)]
    [InlineData("logs", true, true, @"D:\default", "WARN", @"SCREENNAP_LOG_DIR ""logs"" is not an absolute directory path that can be created; logging to D:\default")]
    [InlineData(@"C:\logs", true, true, @"C:\logs", null, null)]
    [InlineData(@"C:\logs", false, true, @"D:\default", "WARN", @"SCREENNAP_LOG_DIR ""C:\logs"" is not an absolute directory path that can be created; logging to D:\default")]
    [InlineData(@"C:\logs", false, false, null, null, null)]
    internal void ChooseDirectory_AnySettingAndDirectories_ChoosesTheDirectoryAndItsNotice(
        string? configured,
        bool configuredCreatable,
        bool defaultCreatable,
        string? expectedDirectory,
        string? expectedLevel,
        string? expectedNotice)
    {
        const string DefaultDirectory = @"D:\default";
        bool TryCreate(string directory) => directory == DefaultDirectory ? defaultCreatable : configuredCreatable;

        LogDirectoryChoice choice = Logger.ChooseDirectory(configured, DefaultDirectory, TryCreate);

        Assert.Equal(new LogDirectoryChoice(expectedDirectory, expectedLevel, expectedNotice), choice);
    }

    [Theory]
    [Trait("Requirement", "LOG-2")]
    [InlineData("logs")]
    [InlineData(@"C:logs")]
    internal void Open_RejectedSetting_WritesOneWarningNamingTheValueAndTheDefaultDirectory(string configured)
    {
        string defaultDirectory = ScratchDirectory();
        try
        {
            string? directory = Logger.Open(configured, defaultDirectory, CreateDirectory);

            Assert.Equal(defaultDirectory, directory);
            Assert.Equal(
                [$"[WARN] SCREENNAP_LOG_DIR \"{configured}\" is not an absolute directory path that can be created; logging to {defaultDirectory}"],
                LoggedEntries(defaultDirectory));
        }
        finally
        {
            Directory.Delete(defaultDirectory, true);
        }
    }

    [Theory]
    [Trait("Requirement", "LOG-11")]
    [InlineData(null)]
    [InlineData("")]
    internal void Open_SettingUnset_WritesOneInfoEntryNamingTheDefaultDirectory(string? configured)
    {
        string defaultDirectory = ScratchDirectory();
        try
        {
            string? directory = Logger.Open(configured, defaultDirectory, CreateDirectory);

            Assert.Equal(defaultDirectory, directory);
            Assert.Equal([$"[INFO] SCREENNAP_LOG_DIR is not set; logging to {defaultDirectory}"], LoggedEntries(defaultDirectory));
        }
        finally
        {
            Directory.Delete(defaultDirectory, true);
        }
    }

    [Fact]
    [Trait("Requirement", "LOG-1")]
    internal void Open_UsableSetting_WritesNoNotice()
    {
        string configuredDirectory = ScratchDirectory();
        string defaultDirectory = ScratchDirectory();
        try
        {
            string? directory = Logger.Open(configuredDirectory, defaultDirectory, CreateDirectory);

            Assert.Equal(configuredDirectory, directory);
            Assert.Empty(Directory.GetFiles(configuredDirectory));
            Assert.Empty(Directory.GetFiles(defaultDirectory));
        }
        finally
        {
            Directory.Delete(configuredDirectory, true);
            Directory.Delete(defaultDirectory, true);
        }
    }

    // A directory of the test's own under the temporary directory; Open writes no file into
    // it unless the test expects one
    private static string ScratchDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ScreenNapTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static bool CreateDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        return true;
    }

    // Each entry without its timestamp, which comes from the clock
    private static List<string> LoggedEntries(string directory)
        => [.. Directory.GetFiles(directory, "ScreenNap_*.log")
            .SelectMany(File.ReadAllLines)
            .Select(line => line[(line.IndexOf('[', StringComparison.Ordinal))..])];

    [Fact]
    [Trait("Requirement", "LOG-7")]
    internal void SelectExpiredLogs_EmptyInput_ReturnsEmpty()
    {
        IReadOnlyList<string> expired = Logger.SelectExpiredLogs([], new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero), 7);

        Assert.Empty(expired);
    }

    [Fact]
    [Trait("Requirement", "LOG-7")]
    internal void SelectExpiredLogs_FilesAroundCutoff_SelectsOnlyOlderThanCutoff()
    {
        var now = new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero);
        var files = new List<(string Path, DateTimeOffset LastWrite)>
        {
            ("before", now.AddDays(-7).AddTicks(-1)),
            ("cutoff", now.AddDays(-7)),
            ("cutoff in another offset", now.AddDays(-7).ToOffset(TimeSpan.FromHours(9))),
            ("after", now.AddDays(-7).AddTicks(1))
        };

        IReadOnlyList<string> expired = Logger.SelectExpiredLogs(files, now, 7);

        Assert.Equal(["before"], expired);
    }

    [Theory]
    [Trait("Requirement", "L10N-5")]
    [InlineData("en-US")]
    [InlineData("sv-SE")]
    [InlineData("ar-SA")]
    internal void InvariantLogMessage_AnyCulture_FormatsValuesInvariantly(string culture)
    {
        using var scope = new CultureScope(culture);
        int left = -1920;
        double ratio = 1.5;

        InvariantLogMessage message = $"at {left} scale {ratio} id {255:X4}";

        Assert.Equal("at -1920 scale 1.5 id 00FF", message.ToStringAndClear());
    }
}
