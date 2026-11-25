using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct NOTIFYICONDATAW
{
    internal uint cbSize;
    internal IntPtr hWnd;
    internal uint uID;
    internal uint uFlags;
    internal uint uCallbackMessage;
    internal IntPtr hIcon;
    internal fixed char szTip[WindowStyles.NotifyIconTipCapacity];
    internal uint dwState;
    internal uint dwStateMask;
    internal fixed char szInfo[WindowStyles.NotifyIconInfoCapacity];
    internal uint uVersion;
    internal fixed char szInfoTitle[WindowStyles.NotifyIconInfoTitleCapacity];
    internal uint dwInfoFlags;
    internal Guid guidItem;
    internal IntPtr hBalloonIcon;
}
