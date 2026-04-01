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
    private static IntPtr s_messageWindow;

    [STAThread]
    private static void Main()
    {
        // Single-instance check
        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            User32.MessageBoxW(
                IntPtr.Zero,
                Strings.NotifyAlreadyRunning,
                Strings.NotifyTitle,
                WindowStyles.MB_OK | WindowStyles.MB_ICONINFORMATION);
            return;
        }

        Logger.Initialize();
        string version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        Logger.Info($"Application started (v{version})");



        IntPtr hInstance = Kernel32.GetModuleHandleW(null);

        // Register hidden message window class
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

        // Create hidden message-only window
        s_messageWindow = User32.CreateWindowExW(
            0, MessageWindowClassName, string.Empty, 0,
            0, 0, 0, 0,
            WindowStyles.HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (s_messageWindow == IntPtr.Zero)
        {
            Logger.Error($"CreateWindowExW failed for message window (Win32 error: {Marshal.GetLastWin32Error()})");
            return;
        }

        // Initialize components
        s_blackoutManager = new BlackoutManager();
        s_trayIcon = new TrayIcon(s_messageWindow);
        s_contextMenu = new ContextMenu(s_blackoutManager);

        s_blackoutManager.ActiveCountChanged += () =>
        {
            s_trayIcon.UpdateState(s_blackoutManager.ActiveCount);
        };

        s_trayIcon.Create();

        // Message loop
        while (User32.GetMessageW(out MSG msg, IntPtr.Zero, 0, 0))
        {
            User32.TranslateMessage(ref msg);
            User32.DispatchMessageW(ref msg);
        }

        // Cleanup
        Logger.Info("Application exiting");
        s_trayIcon.Remove();
        s_blackoutManager.ReleaseAll();
        User32.DestroyWindow(s_messageWindow);
        BlackoutWindow.UnregisterClass(hInstance);
        User32.UnregisterClassW(MessageWindowClassName, hInstance);
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

            case WindowStyles.WM_DESTROY:
                User32.PostQuitMessage(0);
                return 0;
        }

        return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }
}
