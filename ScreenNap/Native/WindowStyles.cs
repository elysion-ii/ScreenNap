namespace ScreenNap.Native;

// Win32 constants grouped by category
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
    internal const uint WM_CLOSE = 0x0010;
    internal const uint WM_PAINT = 0x000F;
    internal const uint WM_ERASEBKGND = 0x0014;
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
    internal const uint NOTIFYICON_VERSION_4 = 4;

    // Menu flags
    internal const uint MF_STRING = 0x00000000;
    internal const uint MF_SEPARATOR = 0x00000800;
    internal const uint MF_CHECKED = 0x00000008;
    internal const uint TPM_RIGHTBUTTON = 0x0002;
    internal const uint TPM_BOTTOMALIGN = 0x0020;

    // SetWindowPos
    internal static readonly IntPtr HWND_TOPMOST = new(-1);
    internal static readonly IntPtr HWND_MESSAGE = new(-3);
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_SHOWWINDOW = 0x0040;

    // ShowWindow
    internal const int SW_HIDE = 0;

    // Icons and images
    internal const uint IMAGE_ICON = 1;
    internal const uint LR_DEFAULTCOLOR = 0x00000000;

    // Cursors
    internal const int IDC_ARROW = 32512;

    // Stock objects
    internal const int BLACK_BRUSH = 4;

    // MessageBox
    internal const uint MB_OK = 0x00000000;
    internal const uint MB_ICONINFORMATION = 0x00000040;

    // Timer ID for TopMost maintenance
    internal const nuint TOPMOST_TIMER_ID = 1;
    internal const uint TOPMOST_TIMER_INTERVAL_MS = 1000;

    // Cursor auto-hide on blackout window
    internal const int CURSOR_HIDE_TIMEOUT_MS = 10000;
    internal const int HTCLIENT = 1;

    // Hotkey modifiers
    internal const uint MOD_CONTROL = 0x0002;
    internal const uint MOD_SHIFT = 0x0004;
    internal const uint MOD_ALT = 0x0001;
    internal const uint MOD_NOREPEAT = 0x4000;

    // DrawText flags
    internal const uint DT_CENTER = 0x01;
    internal const uint DT_VCENTER = 0x04;
    internal const uint DT_SINGLELINE = 0x20;

    // GDI background mode
    internal const int TRANSPARENT_BK = 1;

    // Identify overlay timers
    internal const nuint IDENTIFY_DISMISS_TIMER_ID = 2;
    internal const nuint IDENTIFY_TOPMOST_TIMER_ID = 3;
    internal const uint IDENTIFY_DISMISS_MS = 3000;
    internal const uint IDENTIFY_TOPMOST_MS = 250;

    // Display change debounce timer
    internal const nuint DISPLAYCHANGE_DEBOUNCE_TIMER_ID = 4;
    internal const uint DISPLAYCHANGE_DEBOUNCE_MS = 500;

    // Menu item IDs
    internal const int MENU_ID_MONITOR_BASE = 1000;
    internal const int MENU_ID_RELEASE_ALL = 2000;
    internal const int MENU_ID_EXIT = 9999;
    internal const int HOTKEY_ID_BASE = 3000;
    internal const int HOTKEY_ID_IDENTIFY = 3009;
}
