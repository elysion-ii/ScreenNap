using System.Globalization;

namespace ScreenNap.Logging;

internal static class Logger
{
    private const int RetentionDays = 7;
    private const string LogFilePrefix = "ScreenNap_";
    private const string LogFileExtension = ".log";

    private static readonly object s_lock = new();
    private static string? s_logDirectory;

    internal static void Initialize()
    {
        try
        {
            s_logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScreenNap", "Logs");
            Directory.CreateDirectory(s_logDirectory);
            PurgeOldLogs();
        }
        catch
        {
            s_logDirectory = null;
        }
    }

    internal static void Info(string message) => Write("INFO", message);

    internal static void Warn(string message) => Write("WARN", message);

    internal static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        if (s_logDirectory == null)
            return;

        DateTime now = DateTime.Now;
        string line = FormatLine(now, level, message);
        string filePath = Path.Combine(s_logDirectory,
            $"{LogFilePrefix}{now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}{LogFileExtension}");

        try
        {
            lock (s_lock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never crash the application
        }
    }

    private static void PurgeOldLogs()
    {
        try
        {
            string[] paths = Directory.GetFiles(
                s_logDirectory!, $"{LogFilePrefix}*{LogFileExtension}");
            var files = paths.Select(path => (Path: path, LastWrite: File.GetLastWriteTime(path))).ToList();
            foreach (string file in SelectExpiredLogs(files, DateTime.Now, RetentionDays))
                File.Delete(file);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    internal static string FormatLine(DateTime timestamp, string level, string message)
    {
        return $"{timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} [{level}] {message}";
    }

    internal static IReadOnlyList<string> SelectExpiredLogs(
        IReadOnlyList<(string Path, DateTime LastWrite)> files,
        DateTime now,
        int retentionDays)
    {
        DateTime cutoff = now.AddDays(-retentionDays);
        return files.Where(file => file.LastWrite < cutoff).Select(file => file.Path).ToList();
    }
}
