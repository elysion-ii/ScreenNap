using System.Runtime.InteropServices;
using ScreenNap.Native;

namespace ScreenNap.Tray;

internal sealed class Win32TrayIconApi : ITrayIconApi
{
    public int LastError => Marshal.GetLastWin32Error();

    public bool NotifyIcon(uint command, ref NOTIFYICONDATAW data) => Shell32.Shell_NotifyIconW(command, ref data);

    public IntPtr LoadIcon(string resourceFileName) => IconHelper.LoadIconFromResource(resourceFileName);

    public bool DestroyIcon(IntPtr icon) => User32.DestroyIcon(icon);
}
