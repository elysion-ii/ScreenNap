using System.Runtime.InteropServices;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal sealed class IdentifyOverlay
{
    private const string WindowClassName = "ScreenNap_Identify";
    private const int OverlayWidth = 200;
    private const int OverlayHeight = 150;
    private const int FontHeight = 96;
    private const uint White = 0x00FFFFFF;

    private static readonly WNDPROC s_wndProc = WndProc;
    private static bool s_classRegistered;
    private static readonly Dictionary<IntPtr, IdentifyOverlay> s_instances = [];

    private readonly int _displayNumber;

    private IdentifyOverlay(int displayNumber)
    {
        _displayNumber = displayNumber;
    }

    internal static void Toggle()
    {
        if (s_instances.Count > 0)
        {
            DismissAll();
            return;
        }

        var monitors = MonitorEnumerator.EnumerateMonitors();
        IntPtr hInstance = Kernel32.GetModuleHandleW(null);
        RegisterClassOnce(hInstance);

        for (int i = 0; i < monitors.Count; i++)
        {
            var overlay = new IdentifyOverlay(i + 1);
            RECT bounds = monitors[i].Bounds;

            // Center the overlay on the monitor
            int x = bounds.Left + (bounds.Width - OverlayWidth) / 2;
            int y = bounds.Top + (bounds.Height - OverlayHeight) / 2;

            IntPtr hwnd = User32.CreateWindowExW(
                WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST | WindowStyles.WS_EX_NOACTIVATE,
                WindowClassName,
                string.Empty,
                WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                x, y, OverlayWidth, OverlayHeight,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (hwnd == IntPtr.Zero)
            {
                Logger.Warn($"Failed to create identify overlay for monitor {i + 1} (Win32 error: {Marshal.GetLastWin32Error()})");
                continue;
            }

            s_instances[hwnd] = overlay;

            // Topmost maintenance on all windows
            User32.SetTimer(hwnd, WindowStyles.IDENTIFY_TOPMOST_TIMER_ID, WindowStyles.IDENTIFY_TOPMOST_MS, IntPtr.Zero);

            // Dismiss timer on first window only
            if (i == 0)
                User32.SetTimer(hwnd, WindowStyles.IDENTIFY_DISMISS_TIMER_ID, WindowStyles.IDENTIFY_DISMISS_MS, IntPtr.Zero);
        }
    }

    internal static void DismissAll()
    {
        foreach (IntPtr hwnd in s_instances.Keys.ToArray())
        {
            User32.DestroyWindow(hwnd);
        }
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
            style = WindowStyles.CS_HREDRAW | WindowStyles.CS_VREDRAW,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
            hInstance = hInstance,
            hbrBackground = Gdi32.GetStockObject(WindowStyles.BLACK_BRUSH),
            lpszClassName = Marshal.StringToHGlobalUni(WindowClassName)
        };

        ushort atom = User32.RegisterClassExW(ref wc);
        Marshal.FreeHGlobal(wc.lpszClassName);

        if (atom != 0)
            s_classRegistered = true;
        else
            Logger.Error($"RegisterClassExW failed for identify overlay (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    private static nint WndProc(IntPtr hWnd, uint msg, nuint wParam, nint lParam)
    {
        switch (msg)
        {
            case WindowStyles.WM_PAINT:
                if (s_instances.TryGetValue(hWnd, out IdentifyOverlay? paintInstance))
                {
                    IntPtr hdc = User32.BeginPaint(hWnd, out PAINTSTRUCT ps);

                    IntPtr hFont = Gdi32.CreateFontW(
                        FontHeight, 0, 0, 0, 700, 0, 0, 0, 0, 0, 0, 0, 0, "Segoe UI");

                    IntPtr oldFont = Gdi32.SelectObject(hdc, hFont);
                    Gdi32.SetTextColor(hdc, White);
                    Gdi32.SetBkMode(hdc, WindowStyles.TRANSPARENT_BK);

                    var rect = new RECT { Left = 0, Top = 0, Right = OverlayWidth, Bottom = OverlayHeight };
                    string text = paintInstance._displayNumber.ToString();
                    User32.DrawTextW(hdc, text, -1, ref rect,
                        WindowStyles.DT_CENTER | WindowStyles.DT_VCENTER | WindowStyles.DT_SINGLELINE);

                    Gdi32.SelectObject(hdc, oldFont);
                    Gdi32.DeleteObject(hFont);
                    User32.EndPaint(hWnd, ref ps);
                }
                return 0;

            case WindowStyles.WM_TIMER when wParam == WindowStyles.IDENTIFY_DISMISS_TIMER_ID:
                DismissAll();
                return 0;

            case WindowStyles.WM_TIMER when wParam == WindowStyles.IDENTIFY_TOPMOST_TIMER_ID:
                User32.SetWindowPos(hWnd, WindowStyles.HWND_TOPMOST,
                    0, 0, 0, 0,
                    WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE);
                return 0;

            case WindowStyles.WM_DESTROY:
                User32.KillTimer(hWnd, WindowStyles.IDENTIFY_DISMISS_TIMER_ID);
                User32.KillTimer(hWnd, WindowStyles.IDENTIFY_TOPMOST_TIMER_ID);
                s_instances.Remove(hWnd);
                return 0;
        }

        return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }
}
