using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

internal static class Desktop
{
    private const int ClassNameCapacity = 256;

    // Rectangles and pixels are read in the DPI awareness of the window they belong to, so they
    // compare with the coordinates that window's process used to place it
    internal static T MeasuredAs<T>(IntPtr window, Func<T> measurement)
    {
        nint context = Win32.GetWindowDpiAwarenessContext(window);
        if (context == 0)
            throw new InvalidOperationException("GetWindowDpiAwarenessContext returned no context");

        nint previous = Win32.SetThreadDpiAwarenessContext(context);
        if (previous == 0)
            throw new InvalidOperationException("SetThreadDpiAwarenessContext rejected the window's context");
        try
        {
            return measurement();
        }
        finally
        {
            _ = Win32.SetThreadDpiAwarenessContext(previous);
        }
    }

    internal static IReadOnlyList<RECT> Monitors()
    {
        var monitors = new List<RECT>();
        bool enumerated = Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, nint _) =>
        {
            var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (!Win32.GetMonitorInfoW(hMonitor, ref info))
                throw new InvalidOperationException("GetMonitorInfoW failed while listing monitors");
            monitors.Add(info.rcMonitor);
            return true;
        }, 0);
        if (!enumerated)
            throw new InvalidOperationException("EnumDisplayMonitors failed");
        return monitors;
    }

    internal static IReadOnlyList<IntPtr> TopLevelWindows(int processId, string className)
    {
        var windows = new List<IntPtr>();
        bool enumerated = Win32.EnumWindows((hWnd, lParam) =>
        {
            _ = Win32.GetWindowThreadProcessId(hWnd, out uint ownerId);
            if (ownerId == (uint)processId && ClassName(hWnd) == className)
                windows.Add(hWnd);
            return true;
        }, 0);
        if (!enumerated)
            throw new InvalidOperationException("EnumWindows failed");
        return windows;
    }

    internal static IReadOnlyList<IntPtr> VisibleTopLevelWindows(int processId, string className)
        => TopLevelWindows(processId, className).Where(Win32.IsWindowVisible).ToList();

    internal static IReadOnlyList<IntPtr> VisibleTopLevelWindows(int processId)
    {
        var windows = new List<IntPtr>();
        bool enumerated = Win32.EnumWindows((hWnd, lParam) =>
        {
            _ = Win32.GetWindowThreadProcessId(hWnd, out uint ownerId);
            if (ownerId == (uint)processId && Win32.IsWindowVisible(hWnd))
                windows.Add(hWnd);
            return true;
        }, 0);
        if (!enumerated)
            throw new InvalidOperationException("EnumWindows failed");
        return windows;
    }

    internal static RECT Bounds(IntPtr hWnd)
    {
        if (!Win32.GetWindowRect(hWnd, out RECT rect))
            throw new InvalidOperationException($"GetWindowRect failed (Win32 error: {Marshal.GetLastWin32Error()})");
        return rect;
    }

    internal static long ExtendedStyle(IntPtr hWnd) => Win32.GetWindowLongPtrW(hWnd, Win32.GWL_EXSTYLE);

    internal static unsafe string Text(IntPtr hWnd)
    {
        char* buffer = stackalloc char[ClassNameCapacity];
        int length = Win32.GetWindowTextW(hWnd, buffer, ClassNameCapacity);
        return new string(buffer, 0, length);
    }

    internal static uint PixelAt(int x, int y)
    {
        IntPtr screen = Win32.GetDC(IntPtr.Zero);
        if (screen == IntPtr.Zero)
            throw new InvalidOperationException("GetDC failed for the screen");
        try
        {
            return Win32.GetPixel(screen, x, y);
        }
        finally
        {
            _ = Win32.ReleaseDC(IntPtr.Zero, screen);
        }
    }

    internal static bool CursorIsHidden()
    {
        var info = new CURSORINFO { cbSize = (uint)Marshal.SizeOf<CURSORINFO>() };
        if (!Win32.GetCursorInfo(ref info))
            throw new InvalidOperationException($"GetCursorInfo failed (Win32 error: {Marshal.GetLastWin32Error()})");
        return info.hCursor == IntPtr.Zero || (info.flags & Win32.CURSOR_SHOWING) == 0;
    }

    internal static void MoveCursor(int x, int y)
    {
        if (!Win32.SetCursorPos(x, y))
            throw new InvalidOperationException($"SetCursorPos failed (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    internal static IntPtr First(IReadOnlyList<IntPtr> windows) => windows.Count > 0 ? windows[0] : IntPtr.Zero;

    private static unsafe string ClassName(IntPtr hWnd)
    {
        char* buffer = stackalloc char[ClassNameCapacity];
        int length = Win32.GetClassNameW(hWnd, buffer, ClassNameCapacity);
        return new string(buffer, 0, length);
    }
}
