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
