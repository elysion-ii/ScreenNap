using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;

namespace ScreenNap.Blackout;

internal sealed class BlackoutWindowFactory : IBlackoutWindowFactory
{
    private const string Title = "ScreenNap Blackout";

    private readonly IWindowApi _api;
    private readonly WindowClass _windowClass;
    private readonly Dictionary<IntPtr, BlackoutWindow> _windows = [];

    // Raised whenever a blackout window takes the top of the topmost band, as it does when it
    // appears and each time it reasserts its topmost position (specification WIN-3, IDN-7)
    internal event Action? WindowRaised;

    internal BlackoutWindowFactory(IWindowApi api)
    {
        _api = api;
        _windowClass = new WindowClass(
            "ScreenNap_Blackout",
            WindowStyles.CS_DBLCLKS | WindowStyles.CS_HREDRAW | WindowStyles.CS_VREDRAW,
            WndProc,
            api);
    }

    public IBlackoutWindow? Create(MonitorInfo monitor)
    {
        IntPtr hInstance = _api.GetModuleHandle();
        if (hInstance == IntPtr.Zero)
        {
            Logger.Error("GetModuleHandleW failed for blackout window", _api.LastError);
            return null;
        }

        RegisterClassOnce(hInstance);

        RECT bounds = monitor.Bounds;
        IntPtr handle = _api.CreateWindow(
            WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST | WindowStyles.WS_EX_NOACTIVATE,
            _windowClass.Name,
            Title,
            WindowStyles.WS_POPUP,
            bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            hInstance);
        if (handle == IntPtr.Zero)
        {
            Logger.Error($"CreateWindowExW failed for blackout window on {monitor.DevicePath}", _api.LastError);
            return null;
        }

        if (!_api.SetWindowPos(handle, WindowStyles.HWND_TOPMOST,
            0, 0, 0, 0,
            WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE | WindowStyles.SWP_SHOWWINDOW))
        {
            Logger.Error($"SetWindowPos failed to show blackout window on {monitor.DevicePath}", _api.LastError);
            if (!_api.DestroyWindow(handle))
                Logger.Warn($"DestroyWindow failed for unshown blackout window on {monitor.DevicePath}", _api.LastError);
            return null;
        }
        Logger.Info($"Blackout window created: {monitor.DevicePath} ({bounds.Left},{bounds.Top} {bounds.Width}x{bounds.Height})");

        var window = new BlackoutWindow(_api, handle, monitor.DevicePath, monitor.Identity, Environment.TickCount64, () => WindowRaised?.Invoke());
        _windows[handle] = window;
        window.StartTopmostMaintenance();
        WindowRaised?.Invoke();
        return window;
    }

    internal void UnregisterClass(IntPtr hInstance)
    {
        if (!_windowClass.Unregister(hInstance))
            Logger.Warn("UnregisterClassW failed for blackout window", _api.LastError);
    }

    private void RegisterClassOnce(IntPtr hInstance)
    {
        if (_windowClass.IsRegistered)
            return;

        IntPtr backgroundBrush = _api.GetStockObject(WindowStyles.BLACK_BRUSH);
        if (backgroundBrush == IntPtr.Zero)
        {
            Logger.Error("GetStockObject(BLACK_BRUSH) failed for blackout window class");
            return;
        }

        IntPtr cursor = _api.LoadSystemCursor(WindowStyles.IDC_ARROW);
        if (cursor == IntPtr.Zero)
        {
            Logger.Error("LoadCursorW failed for blackout window class", _api.LastError);
            return;
        }

        if (!_windowClass.Register(hInstance, backgroundBrush, cursor))
            Logger.Error("RegisterClassExW failed for blackout window", _api.LastError);
    }

    private nint WndProc(IntPtr hWnd, uint msg, nuint wParam, nint lParam)
    {
        // Messages that arrive while CreateWindowExW is still running precede the window's entry
        if (!_windows.TryGetValue(hWnd, out BlackoutWindow? window))
            return _api.DefWindowProc(hWnd, msg, wParam, lParam);

        nint result = window.HandleMessage(msg, wParam, lParam);
        if (msg == WindowStyles.WM_DESTROY)
            _windows.Remove(hWnd);
        return result;
    }
}
