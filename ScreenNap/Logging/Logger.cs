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
        s_logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenNap", "Logs");
        Directory.CreateDirectory(s_logDirectory);
        PurgeOldLogs();
    }

    internal static void Info(string message) => Write("INFO", message);

    internal static void Warn(string message) => Write("WARN", message);

    internal static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        if (s_logDirectory == null)
            return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string line = $"{timestamp} [{level}] {message}";
        string filePath = Path.Combine(s_logDirectory,
            $"{LogFilePrefix}{DateTime.Now:yyyyMMdd}{LogFileExtension}");

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
            var cutoff = DateTime.Now.AddDays(-RetentionDays);
            foreach (string file in Directory.GetFiles(
                s_logDirectory!, $"{LogFilePrefix}*{LogFileExtension}"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
