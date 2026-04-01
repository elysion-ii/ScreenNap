namespace ScreenNap.Native;

/// <summary>
/// Win32 constants grouped by category.
/// </summary>
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

    // Tooltip styles
    internal const uint TTS_ALWAYSTIP = 0x01;
    internal const uint TTS_NOPREFIX = 0x02;
    internal const uint TTF_SUBCLASS = 0x0010;
    internal const uint TTF_IDISHWND = 0x0001;
    internal const uint TTM_ADDTOOLW = 0x0432;
    internal const uint TTM_DELTOOLW = 0x0433;
    internal const string TOOLTIPS_CLASSW = "tooltips_class32";

    // Common controls
    internal const uint ICC_WIN95_CLASSES = 0x000000FF;

    // Timer ID for TopMost maintenance
    internal const nuint TOPMOST_TIMER_ID = 1;
    internal const uint TOPMOST_TIMER_INTERVAL_MS = 1000;

    // Cursor auto-hide on blackout window
    internal const int CURSOR_HIDE_TIMEOUT_MS = 10000;
    internal const int HTCLIENT = 1;

    // Menu item IDs
    internal const int MENU_ID_MONITOR_BASE = 1000;
    internal const int MENU_ID_RELEASE_ALL = 2000;
    internal const int MENU_ID_EXIT = 9999;
}
