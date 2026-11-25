using System.Runtime.InteropServices;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.Lifetime;

internal sealed class Win32LifetimeApi : ILifetimeApi
{
    private const string MutexName = "ScreenNap_SingleInstance";

    public int LastError => Marshal.GetLastWin32Error();

    public SingleInstanceState AcquireSingleInstance(out IDisposable? instanceLock)
    {
        instanceLock = null;
        try
        {
            var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (createdNew)
            {
                instanceLock = mutex;
                return SingleInstanceState.Acquired;
            }

            mutex.Dispose();
            return SingleInstanceState.HeldByAnotherInstance;
        }
        catch (UnauthorizedAccessException)
        {
            // The mutex exists, created by a ScreenNap with other privileges (for example
            // elevated) whose lock this process may not open
            return SingleInstanceState.HeldByAnotherInstance;
        }
        catch (Exception ex) when (ex is WaitHandleCannotBeOpenedException or IOException)
        {
            Logger.Error($"The single-instance check failed: {ex.GetType().Name}: {ex.Message}");
            return SingleInstanceState.CheckFailed;
        }
    }

    public bool ShowMessage(string text, string caption, uint type) => User32.MessageBoxW(IntPtr.Zero, text, caption, type) != 0;

    public uint RegisterWindowMessage(string name) => User32.RegisterWindowMessageW(name);

    public bool RunMessageLoop()
    {
        const int GetMessageFailed = -1;
        int result;
        while ((result = User32.GetMessageW(out MSG msg, IntPtr.Zero, 0, 0)) != 0)
        {
            if (result == GetMessageFailed)
                return false;
            _ = User32.TranslateMessage(ref msg);
            _ = User32.DispatchMessageW(ref msg);
        }
        return true;
    }

    public void PostQuit() => User32.PostQuitMessage(0);
}
