using System.Runtime.InteropServices;
using ScreenNap.Native;

namespace ScreenNap.Tray;

internal sealed class Win32TrayIconApi : ITrayIconApi
{
    public int LastError => Marshal.GetLastWin32Error();

    public bool NotifyIcon(uint command, ref NOTIFYICONDATAW data) => Shell32.Shell_NotifyIconW(command, ref data);

    public int SmallIconSize()
    {
        IntPtr primary = User32.MonitorFromPoint(default, WindowStyles.MONITOR_DEFAULTTOPRIMARY);
        if (primary == IntPtr.Zero || Shcore.GetDpiForMonitor(primary, WindowStyles.MDT_EFFECTIVE_DPI, out uint dpi, out _) != 0)
            return 0;
        return User32.GetSystemMetricsForDpi(WindowStyles.SM_CXSMICON, dpi);
    }

    public IntPtr LoadIcon(string resourceFileName, int pixelSize) => IconHelper.LoadIconFromResource(resourceFileName, pixelSize);

    public bool DestroyIcon(IntPtr icon) => User32.DestroyIcon(icon);
}
