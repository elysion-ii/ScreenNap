using System.Runtime.InteropServices;
using ScreenNap.Native;

namespace ScreenNap.Tray;

internal sealed class Win32PopupMenuApi : IPopupMenuApi
{
    public int LastError => Marshal.GetLastWin32Error();

    public IntPtr CreatePopupMenu() => User32.CreatePopupMenu();

    public bool AppendMenu(IntPtr menu, uint flags, nuint itemId, string? text) => User32.AppendMenuW(menu, flags, itemId, text);

    public bool SetForegroundWindow(IntPtr hwnd) => User32.SetForegroundWindow(hwnd);

    public bool GetCursorPos(out POINT point) => User32.GetCursorPos(out point);

    public bool TrackPopupMenu(IntPtr menu, uint flags, int x, int y, IntPtr hwnd) => User32.TrackPopupMenuEx(menu, flags, x, y, hwnd, IntPtr.Zero);

    public bool PostMessage(IntPtr hwnd, uint message, nuint wParam, nint lParam) => User32.PostMessageW(hwnd, message, wParam, lParam);

    public bool DestroyMenu(IntPtr menu) => User32.DestroyMenu(menu);
}
