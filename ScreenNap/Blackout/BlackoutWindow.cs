using System.Runtime.InteropServices;
using ScreenNap.Native;
using ScreenNap.Resources;

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

    private IntPtr _tooltipHandle;

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
            return;

        s_instances[Handle] = this;

        // TopMost maintenance timer
        User32.SetTimer(Handle, WindowStyles.TOPMOST_TIMER_ID, WindowStyles.TOPMOST_TIMER_INTERVAL_MS, IntPtr.Zero);

        // Tooltip
        CreateTooltip(hInstance);
    }

    internal void Destroy()
    {
        if (Handle != IntPtr.Zero)
            User32.DestroyWindow(Handle);
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
    }

    private void CreateTooltip(IntPtr hInstance)
    {
        _tooltipHandle = User32.CreateWindowExW(
            0,
            WindowStyles.TOOLTIPS_CLASSW,
            string.Empty,
            WindowStyles.TTS_ALWAYSTIP | WindowStyles.TTS_NOPREFIX,
            0, 0, 0, 0,
            Handle, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_tooltipHandle == IntPtr.Zero)
            return;

        string tipText = Strings.BlackoutDismissHint;
        IntPtr pText = Marshal.StringToHGlobalUni(tipText);

        var toolInfo = new TOOLINFOW
        {
            cbSize = (uint)Marshal.SizeOf<TOOLINFOW>(),
            uFlags = WindowStyles.TTF_SUBCLASS | WindowStyles.TTF_IDISHWND,
            hwnd = Handle,
            uId = (nuint)Handle,
            lpszText = pText
        };

        User32.SendMessageW(_tooltipHandle, WindowStyles.TTM_ADDTOOLW, 0, ref toolInfo);
        Marshal.FreeHGlobal(pText);
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
                return 0;

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
                    if (instance._tooltipHandle != IntPtr.Zero)
                        User32.DestroyWindow(instance._tooltipHandle);

                    s_instances.Remove(hWnd);
                    instance.Handle = IntPtr.Zero;
                    instance.OnDestroyed?.Invoke(instance);
                }
                return 0;
        }

        return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }
}
