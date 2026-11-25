using System.Globalization;

namespace ScreenNap.Logging;

internal static class Logger
{
    internal const string DirectoryVariable = "SCREENNAP_LOG_DIR";

    private const int RetentionDays = 7;
    private const string LogFilePrefix = "ScreenNap_";
    private const string LogFileExtension = ".log";
    private const string InfoLevel = "INFO";
    private const string WarnLevel = "WARN";
    private const string ErrorLevel = "ERROR";

    private static readonly object s_lock = new();
    private static string? s_logDirectory;

    internal static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenNap", "Logs");

    internal static void Initialize(string? configuredDirectory, string defaultDirectory)
    {
        s_logDirectory = Open(configuredDirectory, defaultDirectory, TryCreateDirectory);
        if (s_logDirectory is not null)
            PurgeOldLogs();
    }

    // Chooses the log directory and writes the notice about that choice into it
    // (specification LOG-2, LOG-11); returns null when no directory can be used (LOG-9)
    internal static string? Open(string? configuredDirectory, string defaultDirectory, Func<string, bool> tryCreate)
    {
        LogDirectoryChoice choice = ChooseDirectory(configuredDirectory, defaultDirectory, tryCreate);
        if (choice is { Directory: { } directory, NoticeLevel: { } level, Notice: { } notice })
            Write(directory, level, notice);
        return choice.Directory;
    }

    internal static LogDirectoryChoice ChooseDirectory(string? configuredDirectory, string defaultDirectory, Func<string, bool> tryCreate)
    {
        ArgumentNullException.ThrowIfNull(tryCreate);
        if (string.IsNullOrEmpty(configuredDirectory))
        {
            return tryCreate(defaultDirectory)
                ? new LogDirectoryChoice(defaultDirectory, InfoLevel, $"{DirectoryVariable} is not set; logging to {defaultDirectory}")
                : default;
        }

        if (IsUsableDirectorySetting(configuredDirectory) && tryCreate(configuredDirectory))
            return new LogDirectoryChoice(configuredDirectory, null, null);

        return tryCreate(defaultDirectory)
            ? new LogDirectoryChoice(defaultDirectory, WarnLevel, $"{DirectoryVariable} \"{configuredDirectory}\" is not an absolute directory path that can be created; logging to {defaultDirectory}")
            : default;
    }

    internal static void Info(string message) => Write(InfoLevel, message);

    internal static void Info(ref InvariantLogMessage message) => Write(InfoLevel, message.ToStringAndClear());

    internal static void Warn(string message) => Write(WarnLevel, message);

    internal static void Warn(ref InvariantLogMessage message) => Write(WarnLevel, message.ToStringAndClear());

    internal static void Error(string message) => Write(ErrorLevel, message);

    internal static void Error(ref InvariantLogMessage message) => Write(ErrorLevel, message.ToStringAndClear());

    internal static void Warn(string message, int win32Error) => Write(WarnLevel, WithWin32Error(message, win32Error));

    internal static void Warn(ref InvariantLogMessage message, int win32Error) => Write(WarnLevel, WithWin32Error(message.ToStringAndClear(), win32Error));

    internal static void Error(string message, int win32Error) => Write(ErrorLevel, WithWin32Error(message, win32Error));

    internal static void Error(ref InvariantLogMessage message, int win32Error) => Write(ErrorLevel, WithWin32Error(message.ToStringAndClear(), win32Error));

    internal static string WithWin32Error(string message, int win32Error)
        => string.Create(CultureInfo.InvariantCulture, $"{message} (Win32 error: {win32Error})");

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
        IReadOnlyList<(string Path, DateTimeOffset LastWrite)> files,
        DateTimeOffset now,
        int retentionDays)
    {
        DateTimeOffset cutoff = now.AddDays(-retentionDays);
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

    private static void Write(string level, string message) => Write(s_logDirectory, level, message);

    private static void Write(string? directory, string level, string message)
    {
        if (LogCapture.Current is { } capture)
        {
            capture.Add(new LogEntry(level, message));
            return;
        }

        if (directory is null)
            return;

        DateTimeOffset now = DateTimeOffset.Now;
        string line = FormatLine(now, level, message);
        string filePath = Path.Combine(directory, FileNameFor(now));

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
        List<(string Path, DateTimeOffset LastWrite)> files;
        try
        {
            string[] paths = Directory.GetFiles(
                s_logDirectory!, $"{LogFilePrefix}*{LogFileExtension}");
            files = paths.Select(path => (Path: path, LastWrite: new DateTimeOffset(File.GetLastWriteTimeUtc(path)))).ToList();
        }
        catch (Exception ex)
        {
            Warn($"Log retention scan failed: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        foreach (string file in SelectExpiredLogs(files, DateTimeOffset.UtcNow, RetentionDays))
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
