using System.Globalization;
using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal sealed class IdentifyOverlay
{
    private const string WindowClassName = "ScreenNap_Identify";
    private const int OverlayWidth = 200;
    private const int OverlayHeight = 150;
    private const int FontHeight = 96;
    private const string FontFace = "Segoe UI";
    private const uint White = 0x00FFFFFF;
    private const nuint DismissTimerId = 2;
    private const nuint TopmostTimerId = 3;
    private const uint DismissDelayMs = 3000;
    private const uint TopmostIntervalMs = 250;

    private static readonly WNDPROC s_wndProc = WndProc;
    private static bool s_classRegistered;
    private static readonly Dictionary<IntPtr, IdentifyOverlay> s_instances = [];

    private readonly int _displayNumber;
    private bool _hasTopmostTimer;
    private bool _hasDismissTimer;

    private IdentifyOverlay(int displayNumber)
    {
        _displayNumber = displayNumber;
    }

    internal static void Toggle(IUserNotifier notifier)
    {
        if (s_instances.Count > 0)
        {
            DismissAll();
            return;
        }

        List<MonitorInfo> monitors = MonitorEnumerator.EnumerateMonitors();
        IntPtr hInstance = Kernel32.GetModuleHandleW(null);
        if (hInstance == IntPtr.Zero)
        {
            Logger.Error($"GetModuleHandleW failed for identify overlay (Win32 error: {Marshal.GetLastWin32Error()})");
            return;
        }

        RegisterClassOnce(hInstance);

        var failedNumbers = new List<int>();
        for (int i = 0; i < monitors.Count; i++)
        {
            var overlay = new IdentifyOverlay(i + 1);
            RECT bounds = monitors[i].Bounds;
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
                failedNumbers.Add(i + 1);
                continue;
            }

            s_instances[hwnd] = overlay;

            overlay._hasTopmostTimer = User32.SetTimer(hwnd, TopmostTimerId, TopmostIntervalMs, IntPtr.Zero) != 0;
            if (!overlay._hasTopmostTimer)
                Logger.Warn($"SetTimer failed for identify overlay topmost maintenance on monitor {i + 1} (Win32 error: {Marshal.GetLastWin32Error()})");

            if (i == 0)
            {
                overlay._hasDismissTimer = User32.SetTimer(hwnd, DismissTimerId, DismissDelayMs, IntPtr.Zero) != 0;
                if (!overlay._hasDismissTimer)
                    Logger.Warn($"SetTimer failed for identify overlay dismissal (Win32 error: {Marshal.GetLastWin32Error()})");
            }
        }

        if (failedNumbers.Count > 0)
            notifier.Notify(FailureNotices.OverlaysNotCreated(failedNumbers));
    }

    internal static void DismissAll()
    {
        foreach (IntPtr hwnd in s_instances.Keys.ToArray())
        {
            if (!User32.DestroyWindow(hwnd))
                Logger.Warn($"DestroyWindow failed for identify overlay (Win32 error: {Marshal.GetLastWin32Error()})");
        }
    }

    internal static void UnregisterClass(IntPtr hInstance)
    {
        if (s_classRegistered)
        {
            if (User32.UnregisterClassW(WindowClassName, hInstance))
                s_classRegistered = false;
            else
                Logger.Warn($"UnregisterClassW failed for identify overlay (Win32 error: {Marshal.GetLastWin32Error()})");
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

    private void Paint(IntPtr hWnd)
    {
        IntPtr hdc = User32.BeginPaint(hWnd, out PAINTSTRUCT ps);
        if (hdc == IntPtr.Zero)
        {
            Logger.Warn("BeginPaint failed for identify overlay");
            return;
        }

        IntPtr hFont = Gdi32.CreateFontW(
            FontHeight, 0, 0, 0, WindowStyles.FW_BOLD, 0, 0, 0, 0, 0, 0, 0, 0, FontFace);
        IntPtr oldFont = hFont == IntPtr.Zero ? IntPtr.Zero : Gdi32.SelectObject(hdc, hFont);
        if (hFont == IntPtr.Zero)
            Logger.Warn("CreateFontW failed for identify overlay");
        else if (oldFont == IntPtr.Zero)
            Logger.Warn("SelectObject failed for identify overlay font");

        // Failure leaves default text attributes; the paint proceeds either way and
        // logging here would spam on every WM_PAINT
        _ = Gdi32.SetTextColor(hdc, White);
        _ = Gdi32.SetBkMode(hdc, WindowStyles.TRANSPARENT_BK);

        var rect = new RECT { Left = 0, Top = 0, Right = OverlayWidth, Bottom = OverlayHeight };
        string text = _displayNumber.ToString(CultureInfo.InvariantCulture);
        if (User32.DrawTextW(hdc, text, -1, ref rect,
            WindowStyles.DT_CENTER | WindowStyles.DT_VCENTER | WindowStyles.DT_SINGLELINE) == 0)
        {
            Logger.Warn("DrawTextW failed for identify overlay");
        }

        if (oldFont != IntPtr.Zero && Gdi32.SelectObject(hdc, oldFont) == IntPtr.Zero)
            Logger.Warn("SelectObject failed to restore the identify overlay font");
        if (hFont != IntPtr.Zero && !Gdi32.DeleteObject(hFont))
            Logger.Warn("DeleteObject failed for identify overlay font");

        // EndPaint always returns nonzero
        _ = User32.EndPaint(hWnd, ref ps);
    }

    private static nint WndProc(IntPtr hWnd, uint msg, nuint wParam, nint lParam)
    {
        switch (msg)
        {
            case WindowStyles.WM_PAINT:
                if (!s_instances.TryGetValue(hWnd, out IdentifyOverlay? paintInstance))
                    break;
                paintInstance.Paint(hWnd);
                return 0;

            case WindowStyles.WM_TIMER when wParam == DismissTimerId:
                DismissAll();
                return 0;

            case WindowStyles.WM_TIMER when wParam == TopmostTimerId:
                if (!User32.SetWindowPos(hWnd, WindowStyles.HWND_TOPMOST,
                    0, 0, 0, 0,
                    WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE))
                {
                    Logger.Warn($"SetWindowPos failed for identify overlay (Win32 error: {Marshal.GetLastWin32Error()})");
                }
                return 0;

            case WindowStyles.WM_DESTROY:
                if (s_instances.Remove(hWnd, out IdentifyOverlay? destroyedInstance))
                {
                    if (destroyedInstance._hasDismissTimer && !User32.KillTimer(hWnd, DismissTimerId))
                        Logger.Warn($"KillTimer failed for identify overlay dismissal (Win32 error: {Marshal.GetLastWin32Error()})");
                    if (destroyedInstance._hasTopmostTimer && !User32.KillTimer(hWnd, TopmostTimerId))
                        Logger.Warn($"KillTimer failed for identify overlay topmost maintenance (Win32 error: {Marshal.GetLastWin32Error()})");
                }
                return 0;
        }

        return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }
}
