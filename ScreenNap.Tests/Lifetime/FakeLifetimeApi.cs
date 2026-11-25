using ScreenNap.Lifetime;

namespace ScreenNap.Tests.Lifetime;

// Stands in for the process-level functions: it decides whether another ScreenNap holds the
// mutex, records message boxes, runs the test's events as the message loop, and fails the
// calls a test names
internal sealed class FakeLifetimeApi : ILifetimeApi
{
    internal const int FailureError = 1402;
    internal const uint RegisteredTaskbarCreated = 0xC0DE;

    public int LastError { get; private set; }

    internal SingleInstanceState Instance { get; set; } = SingleInstanceState.Acquired;
    internal bool MessageBoxFails { get; set; }
    internal bool RegistrationFails { get; set; }
    // The message loop ends because Windows can no longer deliver messages
    internal bool MessagesFail { get; set; }
    // The events Windows would deliver while the message loop runs
    internal Action? Loop { get; set; }

    internal List<(string Text, string Caption, uint Type)> Messages { get; } = [];
    internal int LoopRuns { get; private set; }
    internal int QuitRequests { get; private set; }
    internal bool MutexReleased { get; private set; }

    public SingleInstanceState AcquireSingleInstance(out IDisposable? instanceLock)
    {
        instanceLock = Instance == SingleInstanceState.Acquired ? new InstanceHold(this) : null;
        return Instance;
    }

    public bool ShowMessage(string text, string caption, uint type)
    {
        Messages.Add((text, caption, type));
        return !MessageBoxFails;
    }

    public uint RegisterWindowMessage(string name)
    {
        if (!RegistrationFails)
            return RegisteredTaskbarCreated;
        LastError = FailureError;
        return 0;
    }

    public bool RunMessageLoop()
    {
        LoopRuns++;
        Loop?.Invoke();
        if (!MessagesFail)
            return true;
        LastError = FailureError;
        return false;
    }

    public void PostQuit() => QuitRequests++;

    private sealed class InstanceHold(FakeLifetimeApi api) : IDisposable
    {
        public void Dispose() => api.MutexReleased = true;
    }
}
