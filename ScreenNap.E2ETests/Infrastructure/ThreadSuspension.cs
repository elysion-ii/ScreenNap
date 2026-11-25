using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

// Keeps one thread of another process suspended until disposed; a suspended window thread
// answers no message, as a process that is busy or waiting for the user does not
internal sealed class ThreadSuspension : IDisposable
{
    private readonly IntPtr _thread;

    private ThreadSuspension(IntPtr thread)
    {
        _thread = thread;
    }

    internal static ThreadSuspension Suspend(uint threadId)
    {
        IntPtr thread = Win32.OpenThread(Win32.THREAD_SUSPEND_RESUME, false, threadId);
        if (thread == IntPtr.Zero)
            throw new InvalidOperationException($"OpenThread failed for thread {threadId} (Win32 error: {Marshal.GetLastWin32Error()})");

        if (Win32.SuspendThread(thread) == Win32.SuspendThreadFailed)
        {
            int error = Marshal.GetLastWin32Error();
            _ = Win32.CloseHandle(thread);
            throw new InvalidOperationException($"SuspendThread failed for thread {threadId} (Win32 error: {error})");
        }
        return new ThreadSuspension(thread);
    }

    public void Dispose()
    {
        try
        {
            if (Win32.ResumeThread(_thread) == Win32.SuspendThreadFailed)
                throw new InvalidOperationException($"ResumeThread failed (Win32 error: {Marshal.GetLastWin32Error()})");
        }
        finally
        {
            _ = Win32.CloseHandle(_thread);
        }
    }
}
