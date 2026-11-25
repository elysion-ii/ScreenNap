namespace ScreenNap.Native;

// The window functions ScreenNap's windows call, behind one interface so a unit test can make
// any of them fail (docs/rules/ScreenNap.md, Win32 and P/Invoke)
internal interface IWindowApi
{
    // The Win32 error code the most recent call left
    int LastError { get; }

    IntPtr GetModuleHandle();

    bool RegisterClass(string className, uint classStyle, WNDPROC wndProc, IntPtr hInstance, IntPtr background, IntPtr cursor);

    bool UnregisterClass(string className, IntPtr hInstance);

    IntPtr CreateWindow(uint extendedStyle, string className, string title, uint style, int x, int y, int width, int height, IntPtr hInstance);

    bool DestroyWindow(IntPtr hwnd);

    bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    // Paints the window's invalid part now, before returning
    bool UpdateWindow(IntPtr hwnd);

    bool SetTimer(IntPtr hwnd, nuint timerId, uint intervalMs);

    bool KillTimer(IntPtr hwnd, nuint timerId);

    // Zero when the window's DPI cannot be read
    uint GetDpiForWindow(IntPtr hwnd);

    nint DefWindowProc(IntPtr hwnd, uint message, nuint wParam, nint lParam);

    IntPtr GetStockObject(int stockObject);

    IntPtr LoadSystemCursor(int cursorId);

    void SetCursor(IntPtr cursor);
}
