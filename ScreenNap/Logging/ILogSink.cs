namespace ScreenNap.Logging;

// Where log entries go: the log file in ScreenNap, a capture in a unit test
internal interface ILogSink
{
    void Write(DateTimeOffset timestamp, string level, string message);
}
