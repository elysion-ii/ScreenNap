using System.Globalization;
using System.Text.RegularExpressions;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed partial class LoggingTests
{
    public LoggingTests() => TestAssets.RequireNoRunningScreenNap();

    [Fact]
    [Trait("Requirement", "LOG-1")]
    [Trait("Requirement", "LOG-3")]
    [Trait("Requirement", "LOG-4")]
    [Trait("Requirement", "LOG-5")]
    [Trait("Requirement", "LOG-6")]
    internal void Start_LogDirectorySet_WritesFormattedEntriesThere()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.WaitForLog($"Application started (v{TestAssets.Version})");

        string expectedFile = $"ScreenNap_{DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log";
        Assert.True(File.Exists(Path.Combine(app.LogDirectory, expectedFile)), $"{expectedFile} is missing from the log directory");
        string offset = DateTimeOffset.Now.ToString("zzz", CultureInfo.InvariantCulture);
        Assert.All(app.LogLines(), line =>
        {
            Match match = LogLine().Match(line);
            Assert.True(match.Success, $"Malformed log line: {line}");
            Assert.Equal(offset, match.Groups["offset"].Value);
        });
    }

    [Fact]
    [Trait("Requirement", "LOG-7")]
    internal void Start_ExpiredLogsPresent_DeletesOnlyTheExpiredLogs()
    {
        string logDirectory = TestAssets.CreateScratchDirectory();
        try
        {
            string expired = WriteFile(logDirectory, "ScreenNap_20200101.log", DateTime.Now.AddDays(-8));
            string retained = WriteFile(logDirectory, "ScreenNap_20200102.log", DateTime.Now.AddDays(-6));
            string foreign = WriteFile(logDirectory, "Other_20200101.log", DateTime.Now.AddDays(-30));

            using ScreenNapApp app = ScreenNapApp.Start(logDirectory: logDirectory);

            Assert.False(File.Exists(expired));
            Assert.True(File.Exists(retained));
            Assert.True(File.Exists(foreign));
        }
        finally
        {
            TestAssets.DeleteScratchDirectory(logDirectory);
        }
    }

    [Fact]
    [Trait("Requirement", "LOG-8")]
    [Trait("Requirement", "ERR-11")]
    internal void Start_LogFileUnwritable_KeepsWorkingSilently()
    {
        string logDirectory = TestAssets.CreateScratchDirectory();
        try
        {
            // A directory with the log file's name makes every log write fail
            Directory.CreateDirectory(Path.Combine(logDirectory, $"ScreenNap_{DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log"));

            using ScreenNapApp app = ScreenNapApp.Start(logDirectory: logDirectory);
            app.Select(ScreenNapApp.MonitorCommandBase);
            app.WaitForBlackoutCount(1);

            Assert.False(app.Process.HasExited);
            Assert.Empty(Desktop.VisibleTopLevelWindows(app.Process.Id, "#32770"));
        }
        finally
        {
            TestAssets.DeleteScratchDirectory(logDirectory);
        }
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} (?<offset>[+-]\d{2}:\d{2}) \[(INFO|WARN|ERROR)\] .+$")]
    private static partial Regex LogLine();

    private static string WriteFile(string directory, string name, DateTime lastWrite)
    {
        string path = Path.Combine(directory, name);
        File.WriteAllText(path, "old entry");
        File.SetLastWriteTime(path, lastWrite);
        return path;
    }
}
