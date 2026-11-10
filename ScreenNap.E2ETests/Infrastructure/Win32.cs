using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

internal delegate bool EnumWindowsProc(IntPtr hWnd, nint lParam);
internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, nint dwData);

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;

    internal readonly int Width => Right - Left;
    internal readonly int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    internal int X;
    internal int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO
{
    internal uint cbSize;
    internal RECT rcMonitor;
    internal RECT rcWork;
    internal uint dwFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CURSORINFO
{
    internal uint cbSize;
    internal uint flags;
    internal IntPtr hCursor;
    internal POINT ptScreenPos;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NOTIFYICONIDENTIFIER
{
    internal uint cbSize;
    internal IntPtr hWnd;
    internal uint uID;
    internal Guid guidItem;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    internal int dx;
    internal int dy;
    internal uint mouseData;
    internal uint dwFlags;
    internal uint time;
    internal nuint dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    internal ushort wVk;
    internal ushort wScan;
    internal uint dwFlags;
    internal uint time;
    internal nuint dwExtraInfo;
}

[StructLayout(LayoutKind.Explicit)]
internal struct INPUTUNION
{
    [FieldOffset(0)] internal MOUSEINPUT mi;
    [FieldOffset(0)] internal KEYBDINPUT ki;
}

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    internal uint type;
    internal INPUTUNION u;
}

internal static partial class Win32
{
    internal const uint WM_CLOSE = 0x0010;
    internal const uint WM_COMMAND = 0x0111;
    internal const uint WM_CANCELMODE = 0x001F;
    internal const uint WM_DISPLAYCHANGE = 0x007E;
    internal const uint WM_LBUTTONUP = 0x0202;
    internal const uint WM_LBUTTONDBLCLK = 0x0203;
    internal const uint WM_RBUTTONUP = 0x0205;
    internal const uint WM_USER = 0x0400;
    internal const uint WM_TRAYICON = WM_USER + 1;
    internal const uint MN_GETHMENU = 0x01E1;

    internal const int GWL_EXSTYLE = -20;
    internal const long WS_EX_TOPMOST = 0x00000008;
    internal const long WS_EX_TOOLWINDOW = 0x00000080;
    internal const long WS_EX_NOACTIVATE = 0x08000000;

    internal const uint MF_BYPOSITION = 0x00000400;
    internal const uint MF_GRAYED = 0x00000001;
    internal const uint MF_DISABLED = 0x00000002;
    internal const uint MF_CHECKED = 0x00000008;
    internal const uint MF_SEPARATOR = 0x00000800;

    internal const uint MOD_ALT = 0x0001;
    internal const uint MOD_CONTROL = 0x0002;
    internal const uint MOD_SHIFT = 0x0004;
    internal const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

    internal const uint INPUT_KEYBOARD = 1;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const ushort VK_SHIFT = 0x10;
    internal const ushort VK_CONTROL = 0x11;
    internal const ushort VK_MENU = 0x12;
    internal const ushort VK_0 = 0x30;

    internal const uint CURSOR_SHOWING = 0x00000001;

    [LibraryImport("user32.dll", EntryPoint = "EnumWindows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
    internal static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true)]
    internal static unsafe partial int GetClassNameW(IntPtr hWnd, char* lpClassName, int nMaxCount);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true)]
    internal static unsafe partial int GetWindowTextW(IntPtr hWnd, char* lpString, int nMaxCount);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    internal static partial nint GetWindowLongPtrW(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "IsWindowVisible")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessageW(IntPtr hWnd, uint msg, nuint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    internal static partial nint SendMessageW(IntPtr hWnd, uint msg, nuint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "GetMenuItemCount", SetLastError = true)]
    internal static partial int GetMenuItemCount(IntPtr hMenu);

    [LibraryImport("user32.dll", EntryPoint = "GetMenuItemID")]
    internal static partial uint GetMenuItemID(IntPtr hMenu, int nPos);

    [LibraryImport("user32.dll", EntryPoint = "GetMenuState")]
    internal static partial uint GetMenuState(IntPtr hMenu, uint uId, uint uFlags);

    [LibraryImport("user32.dll", EntryPoint = "GetMenuStringW")]
    internal static unsafe partial int GetMenuStringW(IntPtr hMenu, uint uIDItem, char* lpString, int cchMax, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", EntryPoint = "UnregisterHotKey", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    [LibraryImport("user32.dll", EntryPoint = "SendInput", SetLastError = true)]
    internal static partial uint SendInput(uint cInputs, [In] INPUT[] pInputs, int cbSize);

    [LibraryImport("user32.dll", EntryPoint = "EnumDisplayMonitors")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFO lpmi);

    [LibraryImport("user32.dll", EntryPoint = "AllowSetForegroundWindow", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AllowSetForegroundWindow(uint dwProcessId);

    [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    internal static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll", EntryPoint = "SetCursorPos", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetCursorPos(int x, int y);

    [LibraryImport("user32.dll", EntryPoint = "GetCursorInfo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorInfo(ref CURSORINFO pci);

    [LibraryImport("user32.dll", EntryPoint = "GetDC")]
    internal static partial IntPtr GetDC(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "ReleaseDC")]
    internal static partial int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowDpiAwarenessContext")]
    internal static partial nint GetWindowDpiAwarenessContext(IntPtr hwnd);

    [LibraryImport("user32.dll", EntryPoint = "SetThreadDpiAwarenessContext")]
    internal static partial nint SetThreadDpiAwarenessContext(nint dpiContext);

    [LibraryImport("gdi32.dll", EntryPoint = "GetPixel")]
    internal static partial uint GetPixel(IntPtr hdc, int x, int y);

    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconGetRect")]
    internal static partial int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);
}
