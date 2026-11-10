using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct WNDCLASSEXW
{
    internal uint cbSize;
    internal uint style;
    internal IntPtr lpfnWndProc;
    internal int cbClsExtra;
    internal int cbWndExtra;
    internal IntPtr hInstance;
    internal IntPtr hIcon;
    internal IntPtr hCursor;
    internal IntPtr hbrBackground;
    internal IntPtr lpszMenuName;
    internal IntPtr lpszClassName;
    internal IntPtr hIconSm;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MSG
{
    internal IntPtr hwnd;
    internal uint message;
    internal nuint wParam;
    internal nint lParam;
    internal uint time;
    internal POINT pt;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;

    internal int Width => Right - Left;
    internal int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    internal int X;
    internal int Y;
}

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

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct MONITORINFOEXW
{
    internal uint cbSize;
    internal RECT rcMonitor;
    internal RECT rcWork;
    internal uint dwFlags;
    internal fixed char szDevice[32];
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAY_DEVICEW
{
    internal uint cb;
    internal fixed char DeviceName[32];
    internal fixed char DeviceString[128];
    internal uint StateFlags;
    internal fixed char DeviceID[128];
    internal fixed char DeviceKey[128];
}

[StructLayout(LayoutKind.Sequential)]
internal struct PAINTSTRUCT
{
    internal IntPtr hdc;
    internal int fErase;
    internal RECT rcPaint;
    internal int fRestore;
    internal int fIncUpdate;
    private unsafe fixed byte rgbReserved[32];
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_INFO
{
    internal DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
    internal DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
    internal uint flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_SOURCE_INFO
{
    internal LUID adapterId;
    internal uint id;
    internal uint modeInfoIdx;
    internal uint statusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_TARGET_INFO
{
    internal LUID adapterId;
    internal uint id;
    internal uint modeInfoIdx;
    internal uint outputTechnology;
    internal uint rotation;
    internal uint scaling;
    internal DISPLAYCONFIG_RATIONAL refreshRate;
    internal uint scanLineOrdering;
    internal int targetAvailable;
    internal uint statusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_RATIONAL
{
    internal uint Numerator;
    internal uint Denominator;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LUID
{
    internal uint LowPart;
    internal int HighPart;
}

// DISPLAYCONFIG_MODE_INFO is a union; we only need the size for buffer allocation
[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_MODE_INFO
{
    internal uint infoType;
    internal uint id;
    internal LUID adapterId;
    // Union of target/source mode - pad to full struct size
    private unsafe fixed byte modeInfo[64];
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_DEVICE_INFO_HEADER
{
    internal uint type;
    internal uint size;
    internal LUID adapterId;
    internal uint id;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAYCONFIG_TARGET_DEVICE_NAME
{
    internal DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    internal uint flags;
    internal uint outputTechnology;
    internal ushort edidManufactureId;
    internal ushort edidProductCodeId;
    internal uint connectorInstance;
    internal fixed char monitorFriendlyDeviceName[64];
    internal fixed char monitorDevicePath[128];
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
{
    internal DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    internal fixed char viewGdiDeviceName[32];
}
