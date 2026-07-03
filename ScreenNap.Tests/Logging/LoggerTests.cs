using System.Globalization;
using ScreenNap.Logging;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Logging;

public sealed class LoggerTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("ja-JP")]
    public void FormatLine_IsCultureIndependent(string culture)
    {
        using var scope = new CultureScope(culture);
        var timestamp = new DateTime(2026, 7, 3, 12, 34, 56, 789, DateTimeKind.Local);

        string line = Logger.FormatLine(timestamp, "INFO", "Started");

        Assert.Equal("2026-07-03 12:34:56.789 [INFO] Started", line);
    }

    [Fact]
    public void SelectExpiredLogs_UsesExclusiveCutoff()
    {
        var now = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Local);
        var files = new List<(string Path, DateTime LastWrite)>
        {
            ("before", now.AddDays(-7).AddTicks(-1)),
            ("cutoff", now.AddDays(-7)),
            ("after", now.AddDays(-7).AddTicks(1))
        };

        IReadOnlyList<string> expired = Logger.SelectExpiredLogs(files, now, 7);

        Assert.Equal(["before"], expired);
    }

    [Fact]
    public void SelectExpiredLogs_EmptyInputReturnsEmpty()
    {
        IReadOnlyList<string> expired = Logger.SelectExpiredLogs([], new DateTime(2026, 7, 10), 7);

        Assert.Empty(expired);
    }
}
