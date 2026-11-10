namespace ScreenNap.Native;

internal static class WindowStyles
{
    // Window styles
    internal const uint WS_POPUP = 0x80000000;
    internal const uint WS_VISIBLE = 0x10000000;

    // Extended window styles
    internal const uint WS_EX_TOOLWINDOW = 0x00000080;
    internal const uint WS_EX_TOPMOST = 0x00000008;
    internal const uint WS_EX_NOACTIVATE = 0x08000000;

    // Class styles
    internal const uint CS_DBLCLKS = 0x0008;
    internal const uint CS_HREDRAW = 0x0002;
    internal const uint CS_VREDRAW = 0x0001;

    // Window messages
    internal const uint WM_DISPLAYCHANGE = 0x007E;
    internal const uint WM_DESTROY = 0x0002;
    internal const uint WM_PAINT = 0x000F;
    internal const uint WM_TIMER = 0x0113;
    internal const uint WM_COMMAND = 0x0111;
    internal const uint WM_LBUTTONUP = 0x0202;
    internal const uint WM_LBUTTONDBLCLK = 0x0203;
    internal const uint WM_RBUTTONUP = 0x0205;
    internal const uint WM_SETCURSOR = 0x0020;
    internal const uint WM_MOUSEMOVE = 0x0200;
    internal const uint WM_NULL = 0x0000;
    internal const uint WM_HOTKEY = 0x0312;
    internal const uint WM_USER = 0x0400;
    internal const uint WM_TRAYICON = WM_USER + 1;

    // Shell_NotifyIcon commands
    internal const uint NIM_ADD = 0x00000000;
    internal const uint NIM_MODIFY = 0x00000001;
    internal const uint NIM_DELETE = 0x00000002;
    internal const uint NIM_SETVERSION = 0x00000004;

    // Shell_NotifyIcon flags
    internal const uint NIF_MESSAGE = 0x00000001;
    internal const uint NIF_ICON = 0x00000002;
    internal const uint NIF_TIP = 0x00000004;
    internal const uint NIF_INFO = 0x00000010;
    internal const uint NIIF_WARNING = 0x00000002;
    internal const uint NOTIFYICON_VERSION_4 = 4;

    // NOTIFYICONDATAW text buffers, in characters including the terminating null
    internal const int NotifyIconTipCapacity = 128;
    internal const int NotifyIconInfoCapacity = 256;
    internal const int NotifyIconInfoTitleCapacity = 64;

    // Monitor information
    internal const uint MONITORINFOF_PRIMARY = 0x00000001;

    // QueryDisplayConfig and DisplayConfigGetDeviceInfo
    internal const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
    internal const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    internal const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;

    // Menu flags
    internal const uint MF_STRING = 0x00000000;
    internal const uint MF_SEPARATOR = 0x00000800;
    internal const uint MF_CHECKED = 0x00000008;
    internal const uint MF_GRAYED = 0x00000001;
    internal const uint TPM_RIGHTBUTTON = 0x0002;

    // SetWindowPos
    internal static readonly IntPtr HWND_TOPMOST = new(-1);
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOACTIVATE = 0x0010;

    // Icons
    internal const uint LR_DEFAULTCOLOR = 0x00000000;

    // Cursors and hit testing
    internal const int IDC_ARROW = 32512;
    internal const int HTCLIENT = 1;

    // Stock objects
    internal const int BLACK_BRUSH = 4;

    // MessageBox
    internal const uint MB_OK = 0x00000000;
    internal const uint MB_ICONINFORMATION = 0x00000040;
    internal const uint MB_ICONERROR = 0x00000010;

    // Hotkey modifiers and virtual keys
    internal const uint MOD_CONTROL = 0x0002;
    internal const uint MOD_SHIFT = 0x0004;
    internal const uint MOD_ALT = 0x0001;
    internal const uint MOD_NOREPEAT = 0x4000;
    internal const uint VK_0 = 0x30;
    internal const uint VK_1 = 0x31;

    // DrawText flags
    internal const uint DT_CENTER = 0x01;
    internal const uint DT_VCENTER = 0x04;
    internal const uint DT_SINGLELINE = 0x20;

    // GDI
    internal const uint CLR_INVALID = 0xFFFFFFFF;
    internal const int TRANSPARENT_BK = 1;
    internal const int FW_BOLD = 700;
}
