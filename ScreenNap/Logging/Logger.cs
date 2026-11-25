using System.Globalization;

namespace ScreenNap.Logging;

// Where every part of ScreenNap logs: the entries go to the sink of the current flow of
// execution, which Program sets to the log file; a unit test sets its own capture, and the
// flow keeps the entries of tests running in parallel apart
internal static class Logger
{
    internal const string InfoLevel = "INFO";
    internal const string WarnLevel = "WARN";
    private const string ErrorLevel = "ERROR";

    private static readonly AsyncLocal<ILogSink?> s_sink = new();

    // Null logs nothing (specification LOG-9)
    internal static void Use(ILogSink? sink) => s_sink.Value = sink;

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

    private static string WithWin32Error(string message, int win32Error)
        => string.Create(CultureInfo.InvariantCulture, $"{message} (Win32 error: {win32Error})");

    private static void Write(string level, string message) => s_sink.Value?.Write(DateTimeOffset.Now, level, message);
}
