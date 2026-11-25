using ScreenNap.Native;

namespace ScreenNap.Tray;

// The menu and window functions the context menu calls, behind one interface so a unit test
// can make any of them fail (docs/rules/ScreenNap.md, Win32 and P/Invoke)
internal interface IPopupMenuApi
{
    // The Win32 error code the most recent call left
    int LastError { get; }

    IntPtr CreatePopupMenu();

    bool AppendMenu(IntPtr menu, uint flags, nuint itemId, string? text);

    bool SetForegroundWindow(IntPtr hwnd);

    bool GetCursorPos(out POINT point);

    // Returns once the menu closes
    bool TrackPopupMenu(IntPtr menu, uint flags, int x, int y, IntPtr hwnd);

    bool PostMessage(IntPtr hwnd, uint message, nuint wParam, nint lParam);

    bool DestroyMenu(IntPtr menu);
}
