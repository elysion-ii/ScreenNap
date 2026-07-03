using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.Blackout;

internal sealed class BlackoutWindow : IBlackoutWindow
{
    private const string WindowClassName = "ScreenNap_Blackout";

    // Pin delegate to prevent GC collection
    private static readonly WNDPROC s_wndProc = WndProc;
    private static bool s_classRegistered;
    private static readonly Dictionary<IntPtr, BlackoutWindow> s_instances = [];

    internal IntPtr Handle { get; private set; }
    public string DevicePath { get; }
    public MonitorIdentity Identity { get; }
    public bool UserDismissed { get; private set; }
    public bool IsAlive => Handle != IntPtr.Zero && User32.IsWindow(Handle);
    public Action<IBlackoutWindow>? OnDestroyed { get; set; }

    private readonly CursorIdleTracker _cursorIdleTracker;

    internal BlackoutWindow(string devicePath, RECT bounds, MonitorIdentity identity)
    {
        DevicePath = devicePath;
        Identity = identity;
        _cursorIdleTracker = new CursorIdleTracker(Environment.TickCount64);
        IntPtr hInstance = Kernel32.GetModuleHandleW(null);
        if (hInstance == IntPtr.Zero)
        {
            Logger.Error($"GetModuleHandleW failed for blackout window (Win32 error: {Marshal.GetLastWin32Error()})");
            return;
        }

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
        // TopMost maintenance timer (non-critical: window still works without it)
        nuint timerId = User32.SetTimer(
            Handle,
            WindowStyles.TOPMOST_TIMER_ID,
            WindowStyles.TOPMOST_TIMER_INTERVAL_MS,
            IntPtr.Zero);
        if (timerId == 0)
            Logger.Warn($"SetTimer failed for blackout window {devicePath} (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    public bool Destroy()
    {
        if (Handle == IntPtr.Zero)
            return true;

        if (User32.DestroyWindow(Handle))
            return true;

        Logger.Warn($"DestroyWindow failed for blackout window {DevicePath} (Win32 error: {Marshal.GetLastWin32Error()})");
        return false;
    }

    internal static void UnregisterClass(IntPtr hInstance)
    {
        if (s_classRegistered)
        {
            if (User32.UnregisterClassW(WindowClassName, hInstance))
                s_classRegistered = false;
            else
                Logger.Warn($"UnregisterClassW failed for blackout window (Win32 error: {Marshal.GetLastWin32Error()})");
        }
    }

    private static void RegisterClassOnce(IntPtr hInstance)
    {
        if (s_classRegistered)
            return;

        IntPtr backgroundBrush = Gdi32.GetStockObject(WindowStyles.BLACK_BRUSH);
        IntPtr cursor = User32.LoadCursorW(IntPtr.Zero, WindowStyles.IDC_ARROW);
        if (backgroundBrush == IntPtr.Zero || cursor == IntPtr.Zero)
        {
            Logger.Error("Failed to load resources for blackout window class");
            return;
        }

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            style = WindowStyles.CS_DBLCLKS | WindowStyles.CS_HREDRAW | WindowStyles.CS_VREDRAW,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
            hInstance = hInstance,
            hbrBackground = backgroundBrush,
            hCursor = cursor,
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
            case WindowStyles.WM_TIMER when wParam == WindowStyles.TOPMOST_TIMER_ID:
                if (!User32.SetWindowPos(hWnd, WindowStyles.HWND_TOPMOST,
                    0, 0, 0, 0,
                    WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE))
                {
                    Logger.Warn($"SetWindowPos failed for blackout window (Win32 error: {Marshal.GetLastWin32Error()})");
                }

                if (s_instances.TryGetValue(hWnd, out BlackoutWindow? timerInstance) &&
                    timerInstance._cursorIdleTracker.OnTimerTick(Environment.TickCount64) == CursorAction.Hide)
                {
                    _ = User32.SetCursor(IntPtr.Zero);
                }
                return 0;

            case WindowStyles.WM_MOUSEMOVE:
                if (s_instances.TryGetValue(hWnd, out BlackoutWindow? moveInstance))
                {
                    int x = (short)(lParam & 0xFFFF);
                    int y = (short)((lParam >> 16) & 0xFFFF);

                    if (moveInstance._cursorIdleTracker.OnMouseMove(
                        x, y, Environment.TickCount64) == CursorAction.Show)
                    {
                        IntPtr cursor = User32.LoadCursorW(IntPtr.Zero, WindowStyles.IDC_ARROW);
                        if (cursor != IntPtr.Zero)
                            _ = User32.SetCursor(cursor);
                    }
                }
                return 0;

            case WindowStyles.WM_SETCURSOR:
                if ((lParam & 0xFFFF) == WindowStyles.HTCLIENT &&
                    s_instances.TryGetValue(hWnd, out BlackoutWindow? cursorInstance) &&
                    cursorInstance._cursorIdleTracker.IsHidden)
                {
                    _ = User32.SetCursor(IntPtr.Zero);
                    return 1;
                }
                break;

            case WindowStyles.WM_LBUTTONDBLCLK:
                if (s_instances.TryGetValue(hWnd, out BlackoutWindow? dblClickInstance))
                    dblClickInstance.UserDismissed = true;
                if (!User32.DestroyWindow(hWnd))
                    Logger.Warn($"DestroyWindow failed after double-click (Win32 error: {Marshal.GetLastWin32Error()})");
                return 0;

            // Right-click also dismisses (safety: allows recovery when main monitor is blacked out)
            case WindowStyles.WM_RBUTTONUP:
                if (s_instances.TryGetValue(hWnd, out BlackoutWindow? rClickInstance))
                    rClickInstance.UserDismissed = true;
                if (!User32.DestroyWindow(hWnd))
                    Logger.Warn($"DestroyWindow failed after right-click (Win32 error: {Marshal.GetLastWin32Error()})");
                return 0;

            case WindowStyles.WM_DESTROY:
                if (!User32.KillTimer(hWnd, WindowStyles.TOPMOST_TIMER_ID))
                    Logger.Warn($"KillTimer failed for blackout window (Win32 error: {Marshal.GetLastWin32Error()})");
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
