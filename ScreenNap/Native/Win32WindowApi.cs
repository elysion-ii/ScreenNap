using System.Runtime.InteropServices;

namespace ScreenNap.Native;

internal sealed class Win32WindowApi : IWindowApi
{
    public int LastError => Marshal.GetLastWin32Error();

    public IntPtr GetModuleHandle() => Kernel32.GetModuleHandleW(null);

    public bool RegisterClass(string className, uint classStyle, WNDPROC wndProc, IntPtr hInstance, IntPtr background, IntPtr cursor)
    {
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            style = classStyle,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
            hInstance = hInstance,
            hbrBackground = background,
            hCursor = cursor,
            lpszClassName = Marshal.StringToHGlobalUni(className)
        };

        ushort atom = User32.RegisterClassExW(ref wc);
        Marshal.FreeHGlobal(wc.lpszClassName);
        return atom != 0;
    }

    public bool UnregisterClass(string className, IntPtr hInstance) => User32.UnregisterClassW(className, hInstance);

    public IntPtr CreateWindow(uint extendedStyle, string className, string title, uint style, int x, int y, int width, int height, IntPtr hInstance)
        => User32.CreateWindowExW(extendedStyle, className, title, style, x, y, width, height, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

    public bool DestroyWindow(IntPtr hwnd) => User32.DestroyWindow(hwnd);


    public bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags)
        => User32.SetWindowPos(hwnd, insertAfter, x, y, width, height, flags);

    public bool UpdateWindow(IntPtr hwnd) => User32.UpdateWindow(hwnd);

    public bool SetTimer(IntPtr hwnd, nuint timerId, uint intervalMs) => User32.SetTimer(hwnd, timerId, intervalMs, IntPtr.Zero) != 0;

    public bool KillTimer(IntPtr hwnd, nuint timerId) => User32.KillTimer(hwnd, timerId);

    public uint GetDpiForWindow(IntPtr hwnd) => User32.GetDpiForWindow(hwnd);

    public nint DefWindowProc(IntPtr hwnd, uint message, nuint wParam, nint lParam) => User32.DefWindowProcW(hwnd, message, wParam, lParam);

    public IntPtr GetStockObject(int stockObject) => Gdi32.GetStockObject(stockObject);

    public IntPtr LoadSystemCursor(int cursorId) => User32.LoadCursorW(IntPtr.Zero, cursorId);

    // SetCursor returns the previous cursor, which ScreenNap does not restore
    public void SetCursor(IntPtr cursor) => _ = User32.SetCursor(cursor);
}
