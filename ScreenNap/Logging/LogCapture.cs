namespace ScreenNap.Logging;

// Takes the entries logged in the asynchronous flow that created it, instead of the log
// file, until it is disposed, so a unit test can check what a failure logs. The flow keeps
// the entries of tests that run in parallel apart.
internal sealed class LogCapture : IDisposable
{
    private static readonly AsyncLocal<LogCapture?> s_current = new();

    private readonly LogCapture? _outer;
    private readonly List<LogEntry> _entries = [];

    internal LogCapture()
    {
        _outer = s_current.Value;
        s_current.Value = this;
    }

    internal static LogCapture? Current => s_current.Value;

    internal IReadOnlyList<LogEntry> Entries => _entries;

    internal void Add(LogEntry entry) => _entries.Add(entry);

    public void Dispose() => s_current.Value = _outer;
}
