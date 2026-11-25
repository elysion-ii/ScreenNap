using System.Runtime.InteropServices;
using ScreenNap.Native;

namespace ScreenNap.Lifetime;

internal sealed class Win32LifetimeApi : ILifetimeApi
{
    private const string MutexName = "ScreenNap_SingleInstance";

    public int LastError => Marshal.GetLastWin32Error();

    public IDisposable? AcquireSingleInstance()
    {
        var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (createdNew)
            return mutex;

        mutex.Dispose();
        return null;
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
