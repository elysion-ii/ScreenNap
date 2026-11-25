using System.Globalization;

namespace ScreenNap.Logging;

// The daily log files in the chosen log directory: how a line is formatted and appended, and
// which old files are deleted (specification LOG-1 to LOG-11)
internal sealed class LogFile : ILogSink
{
    private const int RetentionDays = 7;
    private const string FilePrefix = "ScreenNap_";
    private const string FileExtension = ".log";

    private readonly object _lock = new();
    private readonly string _directory;

    private LogFile(string directory)
    {
        _directory = directory;
    }

    internal static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenNap", "Logs");

    // Chooses the directory, writes the notice about that choice into it, and deletes the
    // expired logs there; null when no directory can be used, so nothing is logged (LOG-9)
    internal static LogFile? Open(string? configuredDirectory, string defaultDirectory, DateTimeOffset now)
    {
        LogDirectoryChoice choice = LogDirectoryChoice.Choose(configuredDirectory, defaultDirectory, TryCreateDirectory);
        if (choice.Directory is not { } directory)
            return null;

        var file = new LogFile(directory);
        if (choice is { NoticeLevel: { } level, Notice: { } notice })
            file.Write(now, level, notice);
        file.DeleteExpiredLogs(now);
        return file;
    }

    public void Write(DateTimeOffset timestamp, string level, string message)
    {
        string line = $"{timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)} [{level}] {message}";
        string path = Path.Combine(_directory, $"{FilePrefix}{timestamp.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}{FileExtension}");

        try
        {
            lock (_lock)
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never crash the application (docs/rules/ScreenNap.md, Logging Override)
        }
    }

    private static bool TryCreateDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    // Best effort: a file that cannot be deleted is logged and the others are still deleted (LOG-7)
    private void DeleteExpiredLogs(DateTimeOffset now)
    {
        DateTimeOffset cutoff = now.AddDays(-RetentionDays);
        var expired = new List<string>();
        try
        {
            foreach (string path in Directory.GetFiles(_directory, $"{FilePrefix}*{FileExtension}"))
            {
                if (new DateTimeOffset(File.GetLastWriteTimeUtc(path)) < cutoff)
                    expired.Add(path);
            }
        }
        catch (Exception ex)
        {
            Write(now, Logger.WarnLevel, $"Log retention scan failed: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        foreach (string path in expired)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Write(now, Logger.WarnLevel, $"Failed to delete expired log {Path.GetFileName(path)}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
