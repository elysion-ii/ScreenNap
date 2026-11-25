using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Hotkeys;
using ScreenNap.Identify;
using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Resources;
using ScreenNap.Tray;
using ScreenNap.Versioning;

namespace ScreenNap.Lifetime;

// Starts ScreenNap, routes the hidden message window's messages to the features, and shuts
// ScreenNap down (specification LIFE-1 to LIFE-3, ERR-1, ERR-2, ERR-12)
internal sealed class AppLifetime
{
    private readonly ILifetimeApi _lifetimeApi;
    private readonly IWindowApi _windowApi;
    private readonly ITrayIconApi _trayIconApi;
    private readonly IPopupMenuApi _menuApi;
    private readonly IHotkeyApi _hotkeyApi;
    private readonly IOverlayPaintApi _overlayPaintApi;
    private readonly Func<IReadOnlyList<MonitorInfo>> _enumerateMonitors;
    private readonly CultureInfo _culture;
    private readonly WindowClass _messageWindowClass;
    private uint _taskbarCreatedMessage;
    private Features? _features;

    internal AppLifetime(
        ILifetimeApi lifetimeApi,
        IWindowApi windowApi,
        ITrayIconApi trayIconApi,
        IPopupMenuApi menuApi,
        IHotkeyApi hotkeyApi,
        IOverlayPaintApi overlayPaintApi,
        Func<IReadOnlyList<MonitorInfo>> enumerateMonitors,
        CultureInfo culture)
    {
        _lifetimeApi = lifetimeApi;
        _windowApi = windowApi;
        _trayIconApi = trayIconApi;
        _menuApi = menuApi;
        _hotkeyApi = hotkeyApi;
        _overlayPaintApi = overlayPaintApi;
        _enumerateMonitors = enumerateMonitors;
        _culture = culture;
        _messageWindowClass = new WindowClass("ScreenNap_MessageWindow", 0, MessageWndProc, windowApi);
    }

    internal void Run()
    {
        using IDisposable? singleInstance = _lifetimeApi.AcquireSingleInstance();
        if (singleInstance is null)
        {
            if (!_lifetimeApi.ShowMessage(
                Strings.NotifyAlreadyRunning(_culture),
                Strings.NotifyTitle(_culture),
                WindowStyles.MB_OK | WindowStyles.MB_ICONINFORMATION))
            {
                Logger.Error("MessageBoxW failed for the single-instance notification");
            }
            return;
        }

        Logger.Info($"Application started (v{AppVersion.Current})");

        _taskbarCreatedMessage = _lifetimeApi.RegisterWindowMessage(WindowStyles.TaskbarCreatedMessage);
        if (_taskbarCreatedMessage == 0)
            Logger.Warn("RegisterWindowMessageW failed for TaskbarCreated; the tray icon is not added again after the taskbar is recreated", _lifetimeApi.LastError);

        IntPtr hInstance = _windowApi.GetModuleHandle();
        if (hInstance == IntPtr.Zero)
        {
            Logger.Error("GetModuleHandleW failed", _windowApi.LastError);
            ShowFailure(Strings.NotifyStartFailed(_culture));
            return;
        }

        if (!_messageWindowClass.Register(hInstance, IntPtr.Zero, IntPtr.Zero))
        {
            Logger.Error("RegisterClassExW failed for message window", _windowApi.LastError);
            ShowFailure(Strings.NotifyStartFailed(_culture));
            return;
        }

        IntPtr messageWindow = _windowApi.CreateWindow(0, _messageWindowClass.Name, string.Empty, 0, 0, 0, 0, 0, hInstance);
        if (messageWindow == IntPtr.Zero)
        {
            Logger.Error("CreateWindowExW failed for message window", _windowApi.LastError);
            if (!_messageWindowClass.Unregister(hInstance))
                Logger.Warn("UnregisterClassW failed after message window creation failure", _windowApi.LastError);
            ShowFailure(Strings.NotifyStartFailed(_culture));
            return;
        }

        Features features = CreateFeatures(messageWindow);
        _features = features;

        if (features.TrayIcon.Create())
        {
            features.Hotkeys.Register(messageWindow);
            // Without messages ScreenNap can react to no input (specification ERR-12)
            if (!_lifetimeApi.RunMessageLoop())
            {
                Logger.Error("GetMessageW failed; ScreenNap can no longer receive messages", _lifetimeApi.LastError);
                ShowFailure(Strings.NotifyStopped(_culture));
            }
        }
        else
        {
            // Without the tray icon the user has no way to open the menu or exit (specification ERR-2)
            ShowFailure(Strings.NotifyTrayFailed(_culture));
        }

        Shutdown(features, messageWindow, hInstance);
    }

    private Features CreateFeatures(IntPtr messageWindow)
    {
        var trayIcon = new TrayIcon(messageWindow, _culture, _trayIconApi);
        var blackoutWindows = new BlackoutWindowFactory(_windowApi);
        var blackoutManager = new BlackoutManager(blackoutWindows, trayIcon, _culture);
        blackoutManager.ActiveCountChanged += () => trayIcon.UpdateState(blackoutManager.ActiveCount);
        var overlays = new IdentifyOverlay(_windowApi, _overlayPaintApi);
        var identifier = new MonitorIdentifier(overlays, trayIcon, _enumerateMonitors, _culture);

        return new Features(
            trayIcon,
            blackoutWindows,
            blackoutManager,
            new ContextMenu(blackoutManager, _enumerateMonitors, _menuApi, _lifetimeApi.PostQuit, _culture),
            new HotkeyManager(blackoutManager, identifier, trayIcon, _enumerateMonitors, _hotkeyApi, _culture),
            overlays,
            new DisplayChangeDebounce(_windowApi, blackoutManager, _enumerateMonitors));
    }

    // Cleanup failures are logged and never shown to the user (specification ERR-10)
    private void Shutdown(Features features, IntPtr messageWindow, IntPtr hInstance)
    {
        Logger.Info("Application exiting");
        features.Hotkeys.Unregister(messageWindow);
        features.TrayIcon.Remove();
        features.BlackoutManager.CloseAllAtExit();
        features.Overlays.DismissAll();
        if (!_windowApi.DestroyWindow(messageWindow))
            Logger.Warn("DestroyWindow failed for message window", _windowApi.LastError);
        features.BlackoutWindows.UnregisterClass(hInstance);
        features.Overlays.UnregisterClass(hInstance);
        if (!_messageWindowClass.Unregister(hInstance))
            Logger.Warn("UnregisterClassW failed for message window", _windowApi.LastError);
    }

    // A failure that stops ScreenNap ends it even when its message cannot be shown
    // (specification LIFE-2, ERR-1, ERR-2, ERR-12)
    private void ShowFailure(string message)
    {
        if (!_lifetimeApi.ShowMessage(message, Strings.NotifyTitle(_culture), WindowStyles.MB_OK | WindowStyles.MB_ICONERROR))
            Logger.Error("MessageBoxW failed for a failure notification");
    }

    private nint MessageWndProc(IntPtr hWnd, uint msg, nuint wParam, nint lParam)
    {
        if (msg == WindowStyles.WM_DESTROY)
        {
            _lifetimeApi.PostQuit();
            return 0;
        }

        // Messages that arrive while CreateWindowExW is still running precede the features
        if (_features is not { } features)
            return _windowApi.DefWindowProc(hWnd, msg, wParam, lParam);

        // A failed registration leaves zero, the value of WM_NULL, which the menu posts (TRAY-5)
        if (_taskbarCreatedMessage != 0 && msg == _taskbarCreatedMessage)
        {
            features.TrayIcon.Restore(features.BlackoutManager.ActiveCount);
            return 0;
        }

        switch (msg)
        {
            case WindowStyles.WM_TRAYICON:
                uint eventMsg = (uint)(lParam & 0xFFFF);
                if (eventMsg == WindowStyles.WM_LBUTTONUP || eventMsg == WindowStyles.WM_RBUTTONUP)
                    features.ContextMenu.Show(hWnd);
                return 0;

            case WindowStyles.WM_COMMAND:
                features.ContextMenu.HandleCommand((int)(wParam & 0xFFFF));
                return 0;

            case WindowStyles.WM_HOTKEY:
                features.Hotkeys.HandleHotkey((int)wParam);
                return 0;

            case WindowStyles.WM_DISPLAYCHANGE:
                features.DisplayChange.OnDisplayChange(hWnd);
                return 0;

            case WindowStyles.WM_TIMER when wParam == DisplayChangeDebounce.TimerId:
                features.DisplayChange.OnTimer(hWnd);
                return 0;
        }

        return _windowApi.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private sealed record Features(
        TrayIcon TrayIcon,
        BlackoutWindowFactory BlackoutWindows,
        BlackoutManager BlackoutManager,
        ContextMenu ContextMenu,
        HotkeyManager Hotkeys,
        IdentifyOverlay Overlays,
        DisplayChangeDebounce DisplayChange);
}
