using System.Globalization;
using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal sealed class IdentifyOverlay : IIdentifyOverlays
{
    private const int OverlayWidth = 200;
    private const int OverlayHeight = 150;
    private const int FontHeight = 96;
    private const string FontFace = "Segoe UI";
    private const uint White = 0x00FFFFFF;
    private const nuint DismissTimerId = 2;
    private const nuint TopmostTimerId = 3;
    private const uint DismissDelayMs = 3000;
    private const uint TopmostIntervalMs = 250;

    // Pin delegate to prevent GC collection
    private static readonly WNDPROC s_wndProc = WndProc;
    private static readonly WindowClass s_windowClass = new(
        "ScreenNap_Identify",
        WindowStyles.CS_HREDRAW | WindowStyles.CS_VREDRAW,
        s_wndProc);
    private static readonly Dictionary<IntPtr, OverlayWindow> s_windows = [];

    public bool AnyShown => s_windows.Count > 0;

    public bool Show(int displayNumber, RECT bounds, bool closesAllAfterDelay)
    {
        IntPtr hInstance = Kernel32.GetModuleHandleW(null);
        if (hInstance == IntPtr.Zero)
        {
            Logger.Error($"GetModuleHandleW failed for the identify overlay on monitor {displayNumber}", Marshal.GetLastWin32Error());
            return false;
        }

        if (!RegisterClassOnce(hInstance))
            return false;

        int x = bounds.Left + (bounds.Width - OverlayWidth) / 2;
        int y = bounds.Top + (bounds.Height - OverlayHeight) / 2;
        IntPtr hwnd = User32.CreateWindowExW(
            WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST | WindowStyles.WS_EX_NOACTIVATE,
            s_windowClass.Name,
            string.Empty,
            WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
            x, y, OverlayWidth, OverlayHeight,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
        {
            Logger.Warn($"Failed to create identify overlay for monitor {displayNumber}", Marshal.GetLastWin32Error());
            return false;
        }

        bool hasTopmostTimer = User32.SetTimer(hwnd, TopmostTimerId, TopmostIntervalMs, IntPtr.Zero) != 0;
        if (!hasTopmostTimer)
            Logger.Warn($"SetTimer failed for identify overlay topmost maintenance on monitor {displayNumber}", Marshal.GetLastWin32Error());

        bool hasDismissTimer = false;
        if (closesAllAfterDelay)
        {
            hasDismissTimer = User32.SetTimer(hwnd, DismissTimerId, DismissDelayMs, IntPtr.Zero) != 0;
            if (!hasDismissTimer)
                Logger.Warn("SetTimer failed for identify overlay dismissal", Marshal.GetLastWin32Error());
        }

        s_windows[hwnd] = new OverlayWindow(displayNumber, hasTopmostTimer, hasDismissTimer);
        return true;
    }

    public void DismissAll() => DestroyAll();

    internal static void UnregisterClass(IntPtr hInstance)
    {
        if (!s_windowClass.Unregister(hInstance))
            Logger.Warn("UnregisterClassW failed for identify overlay", Marshal.GetLastWin32Error());
    }

    private static bool RegisterClassOnce(IntPtr hInstance)
    {
        if (s_windowClass.IsRegistered)
            return true;

        IntPtr background = Gdi32.GetStockObject(WindowStyles.BLACK_BRUSH);
        if (background == IntPtr.Zero)
        {
            Logger.Error("GetStockObject(BLACK_BRUSH) failed for identify overlay class");
            return false;
        }

        if (s_windowClass.Register(hInstance, background, IntPtr.Zero))
            return true;

        Logger.Error("RegisterClassExW failed for identify overlay", Marshal.GetLastWin32Error());
        return false;
    }

    private static void DestroyAll()
    {
        foreach (IntPtr hwnd in s_windows.Keys.ToArray())
        {
            if (!User32.DestroyWindow(hwnd))
                Logger.Warn("DestroyWindow failed for identify overlay", Marshal.GetLastWin32Error());
        }
    }

    private static void Paint(IntPtr hWnd, OverlayWindow window)
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

        if (Gdi32.SetTextColor(hdc, White) == WindowStyles.CLR_INVALID)
            Logger.Warn("SetTextColor failed for identify overlay");
        if (Gdi32.SetBkMode(hdc, WindowStyles.TRANSPARENT_BK) == 0)
            Logger.Warn("SetBkMode failed for identify overlay");

        var rect = new RECT { Left = 0, Top = 0, Right = OverlayWidth, Bottom = OverlayHeight };
        string text = window.DisplayNumber.ToString(CultureInfo.InvariantCulture);
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
                if (!s_windows.TryGetValue(hWnd, out OverlayWindow? paintWindow))
                    break;
                Paint(hWnd, paintWindow);
                return 0;

            case WindowStyles.WM_TIMER when wParam == DismissTimerId:
                DestroyAll();
                return 0;

            case WindowStyles.WM_TIMER when wParam == TopmostTimerId:
                if (!User32.SetWindowPos(hWnd, WindowStyles.HWND_TOPMOST,
                    0, 0, 0, 0,
                    WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE))
                {
                    Logger.Warn("SetWindowPos failed for identify overlay", Marshal.GetLastWin32Error());
                }
                return 0;

            case WindowStyles.WM_DESTROY:
                if (s_windows.Remove(hWnd, out OverlayWindow? destroyedWindow))
                {
                    if (destroyedWindow.HasDismissTimer && !User32.KillTimer(hWnd, DismissTimerId))
                        Logger.Warn("KillTimer failed for identify overlay dismissal", Marshal.GetLastWin32Error());
                    if (destroyedWindow.HasTopmostTimer && !User32.KillTimer(hWnd, TopmostTimerId))
                        Logger.Warn("KillTimer failed for identify overlay topmost maintenance", Marshal.GetLastWin32Error());
                }
                return 0;
        }

        return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private sealed record OverlayWindow(int DisplayNumber, bool HasTopmostTimer, bool HasDismissTimer);
}
