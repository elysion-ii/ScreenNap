using ScreenNap.Logging;

namespace ScreenNap.Tests.TestDoubles;

// Takes the entries the code under test logs, instead of the log file, until it is disposed,
// so a unit test can check what a failure logs
internal sealed class LogCapture : ILogSink, IDisposable
{
    internal LogCapture()
    {
        Logger.Use(this);
    }

    internal List<Entry> Entries { get; } = [];

    public void Write(DateTimeOffset timestamp, string level, string message) => Entries.Add(new Entry(level, message));

    public void Dispose() => Logger.Use(null);

    internal sealed record Entry(string Level, string Message);
}
