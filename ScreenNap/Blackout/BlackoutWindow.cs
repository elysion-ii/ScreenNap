using System.Runtime.InteropServices;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.Blackout;

internal sealed class BlackoutWindow
{
    private const string WindowClassName = "ScreenNap_Blackout";

    // Pin delegate to prevent GC collection
    private static readonly WNDPROC s_wndProc = WndProc;
    private static bool s_classRegistered;
    private static readonly Dictionary<IntPtr, BlackoutWindow> s_instances = [];

    internal IntPtr Handle { get; private set; }
    internal string DevicePath { get; }
    internal Action<BlackoutWindow>? OnDestroyed { get; set; }

    private long _lastMouseMoveTick;
    private int _lastMouseX;
    private int _lastMouseY;
    private bool _cursorHidden;

    internal BlackoutWindow(string devicePath, RECT bounds)
    {
        DevicePath = devicePath;
        IntPtr hInstance = Kernel32.GetModuleHandleW(null);

        RegisterClassOnce(hInstance);

        Handle = User32.CreateWindowExW(
            WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST | WindowStyles.WS_EX_NOACTIVATE,
            WindowClassName,
            "ScreenNap Blackout",
            WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
            bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (Handle == IntPtr.Zero)
        {
            Logger.Error($"CreateWindowExW failed for blackout window on {devicePath} (Win32 error: {Marshal.GetLastWin32Error()})");
            return;
        }
        Logger.Info($"Blackout window created: {devicePath} ({bounds.Left},{bounds.Top} {bounds.Width}x{bounds.Height})");

        s_instances[Handle] = this;
        _lastMouseMoveTick = Environment.TickCount64;

        // TopMost maintenance timer (non-critical: window still works without it)
        _ = User32.SetTimer(Handle, WindowStyles.TOPMOST_TIMER_ID, WindowStyles.TOPMOST_TIMER_INTERVAL_MS, IntPtr.Zero);
    }

    internal void Destroy()
    {
        if (Handle != IntPtr.Zero)
            User32.DestroyWindow(Handle);
    }

    internal static void UnregisterClass(IntPtr hInstance)
    {
        if (s_classRegistered)
        {
            User32.UnregisterClassW(WindowClassName, hInstance);
            s_classRegistered = false;
        }
    }

    private static void RegisterClassOnce(IntPtr hInstance)
    {
        if (s_classRegistered)
            return;

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            style = WindowStyles.CS_DBLCLKS | WindowStyles.CS_HREDRAW | WindowStyles.CS_VREDRAW,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
            hInstance = hInstance,
            hbrBackground = Gdi32.GetStockObject(WindowStyles.BLACK_BRUSH),
            hCursor = User32.LoadCursorW(IntPtr.Zero, WindowStyles.IDC_ARROW),
            lpszClassName = Marshal.StringToHGlobalUni(WindowClassName)
        };

        ushort atom = User32.RegisterClassExW(ref wc);
        Marshal.FreeHGlobal(wc.lpszClassName);

        if (atom != 0)
            s_classRegistered = true;
        else
            Logger.Error($"RegisterClassExW failed for blackout window (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    private static nint WndProc(IntPtr hWnd, uint msg, nuint wParam, nint lParam)
    {
        switch (msg)
        {
            // WM_ERASEBKGND: let DefWindowProc paint with BLACK_BRUSH

            case WindowStyles.WM_TIMER when wParam == WindowStyles.TOPMOST_TIMER_ID:
                User32.SetWindowPos(hWnd, WindowStyles.HWND_TOPMOST,
                    0, 0, 0, 0,
                    WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE);

                // Auto-hide cursor after idle timeout
                if (s_instances.TryGetValue(hWnd, out BlackoutWindow? timerInstance) &&
                    !timerInstance._cursorHidden &&
                    Environment.TickCount64 - timerInstance._lastMouseMoveTick >= WindowStyles.CURSOR_HIDE_TIMEOUT_MS)
                {
                    User32.SetCursor(IntPtr.Zero);
                    timerInstance._cursorHidden = true;
                }
                return 0;

            case WindowStyles.WM_MOUSEMOVE:
                if (s_instances.TryGetValue(hWnd, out BlackoutWindow? moveInstance))
                {
                    int x = (short)(lParam & 0xFFFF);
                    int y = (short)((lParam >> 16) & 0xFFFF);

                    // Only react to actual position changes (ignore synthetic messages)
                    if (x != moveInstance._lastMouseX || y != moveInstance._lastMouseY)
                    {
                        moveInstance._lastMouseX = x;
                        moveInstance._lastMouseY = y;
                        moveInstance._lastMouseMoveTick = Environment.TickCount64;

                        if (moveInstance._cursorHidden)
                        {
                            User32.SetCursor(User32.LoadCursorW(IntPtr.Zero, WindowStyles.IDC_ARROW));
                            moveInstance._cursorHidden = false;
                        }
                    }
                }
                return 0;

            case WindowStyles.WM_SETCURSOR:
                if ((lParam & 0xFFFF) == WindowStyles.HTCLIENT &&
                    s_instances.TryGetValue(hWnd, out BlackoutWindow? cursorInstance) &&
                    cursorInstance._cursorHidden)
                {
                    User32.SetCursor(IntPtr.Zero);
                    return 1;
                }
                break;

            case WindowStyles.WM_LBUTTONDBLCLK:
                User32.DestroyWindow(hWnd);
                return 0;

            // Right-click also dismisses (safety: allows recovery when main monitor is blacked out)
            case WindowStyles.WM_RBUTTONUP:
                User32.DestroyWindow(hWnd);
                return 0;

            case WindowStyles.WM_DESTROY:
                User32.KillTimer(hWnd, WindowStyles.TOPMOST_TIMER_ID);
                if (s_instances.TryGetValue(hWnd, out BlackoutWindow? instance))
                {
                    Logger.Info($"Blackout window destroyed: {instance.DevicePath}");
                    s_instances.Remove(hWnd);
                    instance.Handle = IntPtr.Zero;
                    instance.OnDestroyed?.Invoke(instance);
                }
                return 0;
        }

        return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }
}
