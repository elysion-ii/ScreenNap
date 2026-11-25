namespace ScreenNap.Lifetime;

// The process-level functions startup and shutdown call, behind one interface so a unit test
// can make any of them fail (docs/rules/ScreenNap.md, Win32 and P/Invoke)
internal interface ILifetimeApi
{
    // The Win32 error code the most recent call left
    int LastError { get; }

    // On Acquired, the lock holds the single-instance mutex until disposed; otherwise it is null
    SingleInstanceState AcquireSingleInstance(out IDisposable? instanceLock);

    // False when the message box cannot be shown
    bool ShowMessage(string text, string caption, uint type);

    // Zero when the message cannot be registered
    uint RegisterWindowMessage(string name);

    // Dispatches the thread's messages until the quit message arrives; false when Windows
    // can no longer deliver messages
    bool RunMessageLoop();

    void PostQuit();
}
