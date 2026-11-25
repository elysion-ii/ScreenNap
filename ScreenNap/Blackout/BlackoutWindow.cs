using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;

namespace ScreenNap.Blackout;

internal sealed class BlackoutWindow : IBlackoutWindow
{
    internal const nuint TopmostTimerId = 1;
    private const uint TopmostIntervalMs = 1000;

    private readonly IWindowApi _api;
    private readonly CursorIdleTracker _cursorIdleTracker;
    private IntPtr _handle;

    internal BlackoutWindow(IWindowApi api, IntPtr handle, string devicePath, MonitorIdentity identity, long createdTick)
    {
        _api = api;
        _handle = handle;
        DevicePath = devicePath;
        Identity = identity;
        _cursorIdleTracker = new CursorIdleTracker(createdTick);
    }

    public string DevicePath { get; }
    public MonitorIdentity Identity { get; }
    public bool UserDismissed { get; private set; }
    public bool IsAlive => _handle != IntPtr.Zero && _api.IsWindow(_handle);
    public Action<IBlackoutWindow>? OnDestroyed { get; set; }

    // The blackout works without the timer; it only stops reasserting its topmost position
    // (specification WIN-3, ERR-9)
    internal void StartTopmostMaintenance()
    {
        if (!_api.SetTimer(_handle, TopmostTimerId, TopmostIntervalMs))
            Logger.Warn($"SetTimer failed for blackout window {DevicePath}", _api.LastError);
    }

    public bool Destroy()
    {
        if (_handle == IntPtr.Zero)
            return true;

        if (_api.DestroyWindow(_handle))
            return true;

        Logger.Warn($"DestroyWindow failed for blackout window {DevicePath}", _api.LastError);
        return false;
    }

    internal nint HandleMessage(uint msg, nuint wParam, nint lParam)
    {
        switch (msg)
        {
            case WindowStyles.WM_TIMER when wParam == TopmostTimerId:
                if (!_api.SetWindowPos(_handle, WindowStyles.HWND_TOPMOST,
                    0, 0, 0, 0,
                    WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE))
                {
                    Logger.Warn($"SetWindowPos failed for blackout window {DevicePath}", _api.LastError);
                }

                if (_cursorIdleTracker.OnTimerTick(Environment.TickCount64) == CursorAction.Hide)
                    _api.SetCursor(IntPtr.Zero);
                return 0;

            case WindowStyles.WM_MOUSEMOVE:
                int x = (short)(lParam & 0xFFFF);
                int y = (short)((lParam >> 16) & 0xFFFF);
                if (_cursorIdleTracker.OnMouseMove(x, y, Environment.TickCount64) == CursorAction.Show)
                {
                    IntPtr cursor = _api.LoadSystemCursor(WindowStyles.IDC_ARROW);
                    if (cursor != IntPtr.Zero)
                        _api.SetCursor(cursor);
                }
                return 0;

            case WindowStyles.WM_SETCURSOR:
                if ((lParam & 0xFFFF) == WindowStyles.HTCLIENT && _cursorIdleTracker.IsHidden)
                {
                    _api.SetCursor(IntPtr.Zero);
                    return 1;
                }
                break;

            case WindowStyles.WM_LBUTTONDBLCLK:
                Dismiss("double-click");
                return 0;

            // Right-click also dismisses (safety: allows recovery when main monitor is blacked out)
            case WindowStyles.WM_RBUTTONUP:
                Dismiss("right-click");
                return 0;

            case WindowStyles.WM_DESTROY:
                if (!_api.KillTimer(_handle, TopmostTimerId))
                    Logger.Warn($"KillTimer failed for blackout window {DevicePath}", _api.LastError);
                Logger.Info($"Blackout window destroyed: {DevicePath}");
                _handle = IntPtr.Zero;
                OnDestroyed?.Invoke(this);
                return 0;
        }

        return _api.DefWindowProc(_handle, msg, wParam, lParam);
    }

    private void Dismiss(string gesture)
    {
        UserDismissed = true;
        if (!_api.DestroyWindow(_handle))
            Logger.Warn($"DestroyWindow failed after {gesture} on blackout window {DevicePath}", _api.LastError);
    }
}
