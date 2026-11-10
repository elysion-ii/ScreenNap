using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed partial class LoggingTests
{
    private static readonly DateTime s_longAgo = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);
    private static readonly DateTime s_farFuture = new(2999, 1, 1, 0, 0, 0, DateTimeKind.Local);

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

        // The date and the offset come from a clock and a time zone this test does not set;
        // LoggerTests checks their values
        string[] logFiles = Directory.GetFiles(app.LogDirectory, "ScreenNap_*.log");
        Assert.NotEmpty(logFiles);
        Assert.All(logFiles, file => Assert.Matches(LogFileName(), Path.GetFileName(file)));
        Assert.All(app.LogLines(), line => Assert.Matches(LogLine(), line));
    }

    [Fact]
    [Trait("Requirement", "LOG-7")]
    internal void Start_ExpiredLogsPresent_DeletesOnlyTheExpiredLogs()
    {
        string logDirectory = TestAssets.CreateScratchDirectory();
        try
        {
            // Last-write times far from any date the test runs on keep the outcome off the clock
            string expired = WriteFile(logDirectory, "ScreenNap_20200101.log", s_longAgo);
            string retained = WriteFile(logDirectory, "ScreenNap_20200102.log", s_farFuture);
            string foreign = WriteFile(logDirectory, "Other_20200101.log", s_longAgo);

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
            // Denying file creation in the log directory makes every log write fail
            SetFileCreationDenied(logDirectory, true);
            try
            {
                using ScreenNapApp app = ScreenNapApp.Start(logDirectory: logDirectory);
                app.Select(ScreenNapApp.MonitorCommandBase);
                app.WaitForBlackoutCount(1);

                Assert.False(app.Process.HasExited);
                Assert.Empty(Directory.GetFiles(logDirectory));
                Assert.Empty(Desktop.VisibleTopLevelWindows(app.Process.Id, "#32770"));
            }
            finally
            {
                SetFileCreationDenied(logDirectory, false);
            }
        }
        finally
        {
            TestAssets.DeleteScratchDirectory(logDirectory);
        }
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2} \[(INFO|WARN|ERROR)\] .+$")]
    private static partial Regex LogLine();

    [GeneratedRegex(@"^ScreenNap_\d{8}\.log$")]
    private static partial Regex LogFileName();

    private static void SetFileCreationDenied(string directory, bool denied)
    {
        using WindowsIdentity user = WindowsIdentity.GetCurrent();
        var rule = new FileSystemAccessRule(user.User!, FileSystemRights.CreateFiles, AccessControlType.Deny);
        var info = new DirectoryInfo(directory);
        DirectorySecurity security = info.GetAccessControl();
        if (denied)
            security.AddAccessRule(rule);
        else
            security.RemoveAccessRule(rule);
        info.SetAccessControl(security);
    }

    private static string WriteFile(string directory, string name, DateTime lastWrite)
    {
        string path = Path.Combine(directory, name);
        File.WriteAllText(path, "old entry");
        File.SetLastWriteTime(path, lastWrite);
        return path;
    }
}
