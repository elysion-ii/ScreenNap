using ScreenNap.Native;

namespace ScreenNap.Tray;

// The shell and icon functions the tray icon calls, behind one interface so a unit test can
// make any of them fail (docs/rules/ScreenNap.md, Win32 and P/Invoke)
internal interface ITrayIconApi
{
    // The Win32 error code the most recent call left
    int LastError { get; }

    bool NotifyIcon(uint command, ref NOTIFYICONDATAW data);

    // Zero when the icon cannot be loaded
    IntPtr LoadIcon(string resourceFileName);

    bool DestroyIcon(IntPtr icon);
}
