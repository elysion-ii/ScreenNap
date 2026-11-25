using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct NOTIFYICONDATAW
{
    internal uint cbSize;
    internal IntPtr hWnd;
    internal uint uID;
    internal uint uFlags;
    internal uint uCallbackMessage;
    internal IntPtr hIcon;
    internal fixed char szTip[128];
    internal uint dwState;
    internal uint dwStateMask;
    internal fixed char szInfo[256];
    internal uint uVersion;
    internal fixed char szInfoTitle[64];
    internal uint dwInfoFlags;
    internal Guid guidItem;
    internal IntPtr hBalloonIcon;
}
