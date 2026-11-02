using System.Globalization;

namespace ScreenNap.Logging;

internal static class Logger
{
    internal const string DirectoryVariable = "SCREENNAP_LOG_DIR";

    private const int RetentionDays = 7;
    private const string LogFilePrefix = "ScreenNap_";
    private const string LogFileExtension = ".log";

    private static readonly object s_lock = new();
    private static string? s_logDirectory;

    internal static void Initialize()
    {
        string defaultDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenNap", "Logs");
        string? configuredDirectory = Environment.GetEnvironmentVariable(DirectoryVariable);

        string? rejectedDirectory = null;
        if (!string.IsNullOrEmpty(configuredDirectory))
        {
            if (IsUsableDirectorySetting(configuredDirectory) && TryCreateDirectory(configuredDirectory))
                s_logDirectory = configuredDirectory;
            else
                rejectedDirectory = configuredDirectory;
        }

        if (s_logDirectory is null && TryCreateDirectory(defaultDirectory))
            s_logDirectory = defaultDirectory;

        if (s_logDirectory is null)
            return;

        if (rejectedDirectory is not null)
            Warn($"{DirectoryVariable} \"{rejectedDirectory}\" is not an absolute directory path that can be created; logging to {defaultDirectory}");

        PurgeOldLogs();
    }

    internal static void Info(string message) => Write("INFO", message);

    internal static void Warn(string message) => Write("WARN", message);

    internal static void Error(string message) => Write("ERROR", message);

    internal static bool IsUsableDirectorySetting(string value) => Path.IsPathFullyQualified(value);

    internal static string FormatLine(DateTimeOffset timestamp, string level, string message)
    {
        return $"{timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)} [{level}] {message}";
    }

    internal static string FileNameFor(DateTimeOffset timestamp)
    {
        return $"{LogFilePrefix}{timestamp.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}{LogFileExtension}";
    }

    internal static IReadOnlyList<string> SelectExpiredLogs(
        IReadOnlyList<(string Path, DateTime LastWrite)> files,
        DateTime now,
        int retentionDays)
    {
        DateTime cutoff = now.AddDays(-retentionDays);
        return files.Where(file => file.LastWrite < cutoff).Select(file => file.Path).ToList();
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

    private static void Write(string level, string message)
    {
        if (s_logDirectory is null)
            return;

        DateTimeOffset now = DateTimeOffset.Now;
        string line = FormatLine(now, level, message);
        string filePath = Path.Combine(s_logDirectory, FileNameFor(now));

        try
        {
            lock (s_lock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never crash the application (docs/rules/ScreenNap.md, Logging Override)
        }
    }

    private static void PurgeOldLogs()
    {
        List<(string Path, DateTime LastWrite)> files;
        try
        {
            string[] paths = Directory.GetFiles(
                s_logDirectory!, $"{LogFilePrefix}*{LogFileExtension}");
            files = paths.Select(path => (Path: path, LastWrite: File.GetLastWriteTime(path))).ToList();
        }
        catch (Exception ex)
        {
            Warn($"Log retention scan failed: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        foreach (string file in SelectExpiredLogs(files, DateTime.Now, RetentionDays))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                Warn($"Failed to delete expired log {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
