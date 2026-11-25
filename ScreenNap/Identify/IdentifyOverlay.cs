using System.Globalization;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.Identify;

internal sealed class IdentifyOverlay : IIdentifyOverlays
{
    private const nuint DismissTimerId = 2;
    private const nuint TopmostTimerId = 3;
    private const string FontFace = "Segoe UI";
    private const uint White = 0x00FFFFFF;
    private const uint DismissDelayMs = 3000;
    private const uint TopmostIntervalMs = 250;

    private readonly IWindowApi _api;
    private readonly IOverlayPaintApi _paintApi;
    private readonly WindowClass _windowClass;
    private readonly Dictionary<IntPtr, OverlayWindow> _windows = [];

    internal IdentifyOverlay(IWindowApi api, IOverlayPaintApi paintApi)
    {
        _api = api;
        _paintApi = paintApi;
        _windowClass = new WindowClass(
            "ScreenNap_Identify",
            WindowStyles.CS_HREDRAW | WindowStyles.CS_VREDRAW,
            WndProc,
            api);
    }

    public bool AnyShown => _windows.Count > 0;

    public OverlayShowResult Show(int displayNumber, RECT bounds, bool startsClosingTimer)
    {
        IntPtr hInstance = _api.GetModuleHandle();
        if (hInstance == IntPtr.Zero)
        {
            Logger.Error($"GetModuleHandleW failed for the identify overlay on monitor {displayNumber}", _api.LastError);
            return OverlayShowResult.Failed;
        }

        if (!RegisterClassOnce(hInstance))
            return OverlayShowResult.Failed;

        // Created at its 100% size inside the monitor, so the window takes that monitor's DPI
        OverlayLayout unscaled = OverlayLayout.For(bounds, WindowStyles.USER_DEFAULT_SCREEN_DPI);
        IntPtr hwnd = _api.CreateWindow(
            WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST | WindowStyles.WS_EX_NOACTIVATE,
            _windowClass.Name,
            string.Empty,
            WindowStyles.WS_POPUP,
            unscaled.X, unscaled.Y, unscaled.Width, unscaled.Height,
            hInstance);

        if (hwnd == IntPtr.Zero)
        {
            Logger.Warn($"Failed to create identify overlay for monitor {displayNumber}", _api.LastError);
            return OverlayShowResult.Failed;
        }

        uint dpi = _api.GetDpiForWindow(hwnd);
        if (dpi == 0)
        {
            Logger.Warn($"GetDpiForWindow failed for the identify overlay on monitor {displayNumber}; showing it at 100% size");
            dpi = WindowStyles.USER_DEFAULT_SCREEN_DPI;
        }

        OverlayLayout layout = OverlayLayout.For(bounds, dpi);
        if (!_api.SetWindowPos(hwnd, WindowStyles.HWND_TOPMOST,
            layout.X, layout.Y, layout.Width, layout.Height,
            WindowStyles.SWP_NOACTIVATE | WindowStyles.SWP_SHOWWINDOW))
        {
            Logger.Warn($"SetWindowPos failed to show identify overlay for monitor {displayNumber}", _api.LastError);
            if (!_api.DestroyWindow(hwnd))
                Logger.Warn($"DestroyWindow failed for unshown identify overlay on monitor {displayNumber}", _api.LastError);
            return OverlayShowResult.Failed;
        }

        bool hasTopmostTimer = _api.SetTimer(hwnd, TopmostTimerId, TopmostIntervalMs);
        if (!hasTopmostTimer)
            Logger.Warn($"SetTimer failed for identify overlay topmost maintenance on monitor {displayNumber}", _api.LastError);

        bool hasDismissTimer = false;
        if (startsClosingTimer)
        {
            hasDismissTimer = _api.SetTimer(hwnd, DismissTimerId, DismissDelayMs);
            if (!hasDismissTimer)
                Logger.Warn($"SetTimer failed for identify overlay dismissal on monitor {displayNumber}", _api.LastError);
        }

        var window = new OverlayWindow(displayNumber, layout, hasTopmostTimer, hasDismissTimer);
        _windows[hwnd] = window;

        // Painting now tells, before Show returns, whether the number appeared (specification ERR-8)
        if (!_api.UpdateWindow(hwnd))
            Logger.Warn($"UpdateWindow failed for the identify overlay on monitor {displayNumber}");
        return new OverlayShowResult(true, hasDismissTimer, window.NumberDrawn == true);
    }

    public void DismissAll()
    {
        foreach (IntPtr hwnd in _windows.Keys.ToArray())
        {
            if (!_api.DestroyWindow(hwnd))
                Logger.Warn("DestroyWindow failed for identify overlay", _api.LastError);
        }
    }

    // Puts every overlay back above the blackouts after one of them took the top of the
    // topmost band (specification IDN-7)
    internal void RaiseAll()
    {
        foreach (IntPtr hwnd in _windows.Keys)
            Raise(hwnd);
    }

    internal void UnregisterClass(IntPtr hInstance)
    {
        if (!_windowClass.Unregister(hInstance))
            Logger.Warn("UnregisterClassW failed for identify overlay", _api.LastError);
    }

    private bool RegisterClassOnce(IntPtr hInstance)
    {
        if (_windowClass.IsRegistered)
            return true;

        IntPtr background = _api.GetStockObject(WindowStyles.BLACK_BRUSH);
        if (background == IntPtr.Zero)
        {
            Logger.Error("GetStockObject(BLACK_BRUSH) failed for identify overlay class");
            return false;
        }

        if (_windowClass.Register(hInstance, background, IntPtr.Zero))
            return true;

        Logger.Error("RegisterClassExW failed for identify overlay", _api.LastError);
        return false;
    }

    // False when the number is missing, invisible, or not in its large type
    private bool Paint(IntPtr hWnd, OverlayWindow window)
    {
        IntPtr hdc = _paintApi.BeginPaint(hWnd, out PAINTSTRUCT ps);
        if (hdc == IntPtr.Zero)
        {
            Logger.Warn($"BeginPaint failed for the identify overlay on monitor {window.DisplayNumber}");
            return false;
        }

        bool drawn = true;
        IntPtr hFont = _paintApi.CreateFont(window.Layout.FontHeight, WindowStyles.FW_BOLD, FontFace);
        IntPtr oldFont = hFont == IntPtr.Zero ? IntPtr.Zero : _paintApi.SelectObject(hdc, hFont);
        if (hFont == IntPtr.Zero)
        {
            Logger.Warn($"CreateFontW failed for the identify overlay on monitor {window.DisplayNumber}");
            drawn = false;
        }
        else if (oldFont == IntPtr.Zero)
        {
            Logger.Warn($"SelectObject failed for the identify overlay font on monitor {window.DisplayNumber}");
            drawn = false;
        }

        if (!_paintApi.SetTextColor(hdc, White))
        {
            Logger.Warn($"SetTextColor failed for the identify overlay on monitor {window.DisplayNumber}");
            drawn = false;
        }
        // An opaque background behind white text leaves the number visible
        if (!_paintApi.SetTransparentBackground(hdc))
            Logger.Warn($"SetBkMode failed for the identify overlay on monitor {window.DisplayNumber}");

        var rect = new RECT { Left = 0, Top = 0, Right = window.Layout.Width, Bottom = window.Layout.Height };
        string text = window.DisplayNumber.ToString(CultureInfo.InvariantCulture);
        if (!_paintApi.DrawText(hdc, text, ref rect, WindowStyles.DT_CENTER | WindowStyles.DT_VCENTER | WindowStyles.DT_SINGLELINE))
        {
            Logger.Warn($"DrawTextW failed for the identify overlay on monitor {window.DisplayNumber}");
            drawn = false;
        }

        if (oldFont != IntPtr.Zero && _paintApi.SelectObject(hdc, oldFont) == IntPtr.Zero)
            Logger.Warn("SelectObject failed to restore the identify overlay font");
        if (hFont != IntPtr.Zero && !_paintApi.DeleteObject(hFont))
            Logger.Warn("DeleteObject failed for identify overlay font");

        _paintApi.EndPaint(hWnd, ref ps);
        return drawn;
    }

    private void Raise(IntPtr hWnd)
    {
        if (!_api.SetWindowPos(hWnd, WindowStyles.HWND_TOPMOST,
            0, 0, 0, 0,
            WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE))
        {
            Logger.Warn("SetWindowPos failed for identify overlay", _api.LastError);
        }
    }

    private nint WndProc(IntPtr hWnd, uint msg, nuint wParam, nint lParam)
    {
        switch (msg)
        {
            case WindowStyles.WM_PAINT:
                if (!_windows.TryGetValue(hWnd, out OverlayWindow? paintWindow))
                    break;
                // Only the paint as the overlay appears is reported; a later failed redraw is logged only (ERR-8, ERR-10)
                bool drawn = Paint(hWnd, paintWindow);
                paintWindow.NumberDrawn ??= drawn;
                return 0;

            case WindowStyles.WM_TIMER when wParam == DismissTimerId:
                DismissAll();
                return 0;

            case WindowStyles.WM_TIMER when wParam == TopmostTimerId:
                Raise(hWnd);
                return 0;

            case WindowStyles.WM_DESTROY:
                if (_windows.Remove(hWnd, out OverlayWindow? destroyedWindow))
                {
                    if (destroyedWindow.HasDismissTimer && !_api.KillTimer(hWnd, DismissTimerId))
                        Logger.Warn("KillTimer failed for identify overlay dismissal", _api.LastError);
                    if (destroyedWindow.HasTopmostTimer && !_api.KillTimer(hWnd, TopmostTimerId))
                        Logger.Warn("KillTimer failed for identify overlay topmost maintenance", _api.LastError);
                }
                return 0;
        }

        return _api.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private sealed class OverlayWindow(int displayNumber, OverlayLayout layout, bool hasTopmostTimer, bool hasDismissTimer)
    {
        internal int DisplayNumber { get; } = displayNumber;
        internal OverlayLayout Layout { get; } = layout;
        internal bool HasTopmostTimer { get; } = hasTopmostTimer;
        internal bool HasDismissTimer { get; } = hasDismissTimer;
        // Whether the first paint, as the overlay appeared, drew the number; null before it
        internal bool? NumberDrawn { get; set; }
    }
}
