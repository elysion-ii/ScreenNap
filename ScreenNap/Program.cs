using System.Runtime.InteropServices;
using ScreenNap.App;
using ScreenNap.Blackout;
using ScreenNap.Logging;
using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap;

internal static class Program
{
    private const string MutexName = "ScreenNap_SingleInstance";
    private const string MessageWindowClassName = "ScreenNap_MessageWindow";

    // Pin delegate to prevent GC collection
    private static readonly WNDPROC s_wndProc = MessageWndProc;

    private static TrayIcon? s_trayIcon;
    private static BlackoutManager? s_blackoutManager;
    private static ContextMenu? s_contextMenu;
    private static HotkeyManager? s_hotkeyManager;
    private static IntPtr s_messageWindow;

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            int messageResult = User32.MessageBoxW(
                IntPtr.Zero,
                Strings.NotifyAlreadyRunning,
                Strings.NotifyTitle,
                WindowStyles.MB_OK | WindowStyles.MB_ICONINFORMATION);
            if (messageResult == 0)
                Logger.Error("MessageBoxW failed for the single-instance notification");
            return;
        }

        Logger.Initialize();
        string version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        Logger.Info($"Application started (v{version})");

        IntPtr hInstance = Kernel32.GetModuleHandleW(null);
        if (hInstance == IntPtr.Zero)
        {
            Logger.Error($"GetModuleHandleW failed (Win32 error: {Marshal.GetLastWin32Error()})");
            return;
        }

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
            hInstance = hInstance,
            lpszClassName = Marshal.StringToHGlobalUni(MessageWindowClassName)
        };

        ushort atom = User32.RegisterClassExW(ref wc);
        Marshal.FreeHGlobal(wc.lpszClassName);

        if (atom == 0)
        {
            Logger.Error($"RegisterClassExW failed for message window (Win32 error: {Marshal.GetLastWin32Error()})");
            return;
        }

        s_messageWindow = User32.CreateWindowExW(
            0, MessageWindowClassName, string.Empty, 0,
            0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (s_messageWindow == IntPtr.Zero)
        {
            Logger.Error($"CreateWindowExW failed for message window (Win32 error: {Marshal.GetLastWin32Error()})");
            if (!User32.UnregisterClassW(MessageWindowClassName, hInstance))
                Logger.Warn($"UnregisterClassW failed after message window creation failure (Win32 error: {Marshal.GetLastWin32Error()})");
            return;
        }

        s_blackoutManager = new BlackoutManager(new BlackoutWindowFactory());
        s_trayIcon = new TrayIcon(s_messageWindow);
        s_contextMenu = new ContextMenu(s_blackoutManager);
        s_hotkeyManager = new HotkeyManager(s_blackoutManager);

        s_blackoutManager.ActiveCountChanged += () =>
        {
            s_trayIcon.UpdateState(s_blackoutManager.ActiveCount);
        };

        s_trayIcon.Create();
        s_hotkeyManager.Register(s_messageWindow);

        while (User32.GetMessageW(out MSG msg, IntPtr.Zero, 0, 0))
        {
            _ = User32.TranslateMessage(ref msg);
            _ = User32.DispatchMessageW(ref msg);
        }

        Logger.Info("Application exiting");
        s_hotkeyManager.Unregister(s_messageWindow);
        s_trayIcon.Remove();
        s_blackoutManager.ReleaseAll();
        IdentifyOverlay.DismissAll();
        if (!User32.DestroyWindow(s_messageWindow))
            Logger.Warn($"DestroyWindow failed for message window (Win32 error: {Marshal.GetLastWin32Error()})");
        BlackoutWindow.UnregisterClass(hInstance);
        IdentifyOverlay.UnregisterClass(hInstance);
        if (!User32.UnregisterClassW(MessageWindowClassName, hInstance))
            Logger.Warn($"UnregisterClassW failed for message window (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    private static nint MessageWndProc(IntPtr hWnd, uint msg, nuint wParam, nint lParam)
    {
        switch (msg)
        {
            case WindowStyles.WM_TRAYICON:
                uint eventMsg = (uint)(lParam & 0xFFFF);
                if (eventMsg == WindowStyles.WM_LBUTTONUP || eventMsg == WindowStyles.WM_RBUTTONUP)
                {
                    s_contextMenu?.Show(hWnd);
                }
                return 0;

            case WindowStyles.WM_COMMAND:
                int commandId = (int)(wParam & 0xFFFF);
                s_contextMenu?.HandleCommand(commandId);
                return 0;

            case WindowStyles.WM_HOTKEY:
                s_hotkeyManager?.HandleHotkey((int)wParam);
                return 0;

            case WindowStyles.WM_DISPLAYCHANGE:
                nuint timerId = User32.SetTimer(hWnd, WindowStyles.DISPLAYCHANGE_DEBOUNCE_TIMER_ID,
                    WindowStyles.DISPLAYCHANGE_DEBOUNCE_MS, IntPtr.Zero);
                if (timerId == 0)
                    Logger.Warn($"SetTimer failed for display-change debounce (Win32 error: {Marshal.GetLastWin32Error()})");
                return 0;

            case WindowStyles.WM_TIMER when wParam == WindowStyles.DISPLAYCHANGE_DEBOUNCE_TIMER_ID:
                if (!User32.KillTimer(hWnd, WindowStyles.DISPLAYCHANGE_DEBOUNCE_TIMER_ID))
                    Logger.Warn($"KillTimer failed for display-change debounce (Win32 error: {Marshal.GetLastWin32Error()})");
                Logger.Info("Display configuration changed (debounced)");
                var monitors = MonitorEnumerator.EnumerateMonitors();
                s_blackoutManager?.Reconcile(monitors);
                return 0;

            case WindowStyles.WM_DESTROY:
                User32.PostQuitMessage(0);
                return 0;
        }

        return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }
}
