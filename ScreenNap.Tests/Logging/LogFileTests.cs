using System.Text;
using ScreenNap.Logging;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Logging;

// Each test works in directories of its own under the temporary directory
public sealed class LogFileTests : IDisposable
{
    private static readonly DateTimeOffset s_now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
    private readonly List<string> _directories = [];

    [Theory]
    [Trait("Requirement", "LOG-3")]
    [Trait("Requirement", "LOG-4")]
    [Trait("Requirement", "LOG-10")]
    [InlineData("en-US", 9, "ScreenNap_20260703.log", "2026-07-03 23:34:56.789 +09:00 [INFO] Started")]
    [InlineData("ja-JP", 9, "ScreenNap_20260703.log", "2026-07-03 23:34:56.789 +09:00 [INFO] Started")]
    [InlineData("en-US", 0, "ScreenNap_20260703.log", "2026-07-03 23:34:56.789 +00:00 [INFO] Started")]
    [InlineData("en-US", -5, "ScreenNap_20260703.log", "2026-07-03 23:34:56.789 -05:00 [INFO] Started")]
    internal void Write_AnyCultureAndOffset_AppendsTheLineToTheFileOfItsLocalDate(string culture, int offsetHours, string fileName, string expected)
    {
        using var scope = new CultureScope(culture);
        string directory = Scratch();
        LogFile file = LogFile.Open(directory, Scratch(), s_now)!;
        var timestamp = new DateTimeOffset(2026, 7, 3, 23, 34, 56, 789, TimeSpan.FromHours(offsetHours));

        file.Write(timestamp, "INFO", "Started");

        byte[] bytes = File.ReadAllBytes(Path.Combine(directory, fileName));
        Assert.Equal(Encoding.UTF8.GetBytes(expected + "\r\n"), bytes);
    }

    [Theory]
    [Trait("Requirement", "LOG-2")]
    [InlineData("logs")]
    [InlineData(@"C:logs")]
    internal void Open_RejectedSetting_WritesOneWarningNamingTheValueAndTheDefaultDirectory(string configured)
    {
        string defaultDirectory = Scratch();

        Assert.NotNull(LogFile.Open(configured, defaultDirectory, s_now));

        Assert.Equal(
            [$"[WARN] SCREENNAP_LOG_DIR \"{configured}\" is not an absolute directory path that can be created; logging to {defaultDirectory}"],
            LoggedEntries(defaultDirectory));
    }

    [Theory]
    [Trait("Requirement", "LOG-11")]
    [InlineData(null)]
    [InlineData("")]
    internal void Open_SettingUnset_WritesOneInfoEntryNamingTheDefaultDirectory(string? configured)
    {
        string defaultDirectory = Scratch();

        Assert.NotNull(LogFile.Open(configured, defaultDirectory, s_now));

        Assert.Equal([$"[INFO] SCREENNAP_LOG_DIR is not set; logging to {defaultDirectory}"], LoggedEntries(defaultDirectory));
    }

    [Fact]
    [Trait("Requirement", "LOG-1")]
    internal void Open_UsableSetting_LogsThereAndWritesNoNotice()
    {
        string configuredDirectory = Path.Combine(Scratch(), "created");
        string defaultDirectory = Scratch();

        LogFile file = LogFile.Open(configuredDirectory, defaultDirectory, s_now)!;
        file.Write(s_now, "INFO", "Started");

        Assert.Equal(["[INFO] Started"], LoggedEntries(configuredDirectory));
        Assert.Empty(Directory.GetFiles(defaultDirectory));
    }

    [Fact]
    [Trait("Requirement", "LOG-9")]
    internal void Open_NoDirectoryCanBeCreated_ReturnsNull()
    {
        string blocker = Path.Combine(Scratch(), "file");
        File.WriteAllText(blocker, string.Empty);

        Assert.Null(LogFile.Open(Path.Combine(blocker, "configured"), Path.Combine(blocker, "default"), s_now));
    }

    [Fact]
    [Trait("Requirement", "LOG-7")]
    internal void Open_LogsAroundTheCutoff_DeletesOnlyLogsOlderThanSevenDays()
    {
        string directory = Scratch();
        DateTimeOffset cutoff = s_now.AddDays(-7);
        string before = LogWrittenAt(directory, "ScreenNap_20260702.log", cutoff.AddTicks(-1));
        string atCutoff = LogWrittenAt(directory, "ScreenNap_20260703.log", cutoff);
        string after = LogWrittenAt(directory, "ScreenNap_20260704.log", cutoff.AddTicks(1));
        string otherFile = LogWrittenAt(directory, "Other_20260101.log", cutoff.AddDays(-30));

        Assert.NotNull(LogFile.Open(directory, Scratch(), s_now));

        Assert.False(File.Exists(before));
        Assert.True(File.Exists(atCutoff));
        Assert.True(File.Exists(after));
        Assert.True(File.Exists(otherFile));
    }

    [Fact]
    [Trait("Requirement", "LOG-7")]
    internal void Open_ExpiredLogCannotBeDeleted_WarnsAndDeletesTheOthers()
    {
        string directory = Scratch();
        string locked = LogWrittenAt(directory, "ScreenNap_20260601.log", s_now.AddDays(-30));
        string deletable = LogWrittenAt(directory, "ScreenNap_20260602.log", s_now.AddDays(-30));

        using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.NotNull(LogFile.Open(directory, Scratch(), s_now));

        Assert.True(File.Exists(locked));
        Assert.False(File.Exists(deletable));
        Assert.Contains(LoggedEntries(directory, "ScreenNap_20260710.log"), entry => entry.StartsWith("[WARN] Failed to delete expired log ScreenNap_20260601.log", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        foreach (string directory in _directories)
            Directory.Delete(directory, true);
    }

    private string Scratch()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ScreenNapTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        _directories.Add(directory);
        return directory;
    }

    private static string LogWrittenAt(string directory, string name, DateTimeOffset lastWrite)
    {
        string path = Path.Combine(directory, name);
        File.WriteAllText(path, string.Empty);
        File.SetLastWriteTimeUtc(path, lastWrite.UtcDateTime);
        return path;
    }

    // Each entry without its timestamp, which comes from the clock
    private static List<string> LoggedEntries(string directory, string pattern = "ScreenNap_*.log")
        => [.. Directory.GetFiles(directory, pattern)
            .SelectMany(File.ReadAllLines)
            .Where(line => line.Length > 0)
            .Select(line => line[line.IndexOf('[', StringComparison.Ordinal)..])];
}
