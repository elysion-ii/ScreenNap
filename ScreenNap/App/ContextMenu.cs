using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.App;

internal sealed class ContextMenu
{
    private readonly BlackoutManager _manager;
    private List<MonitorInfo> _lastMonitors = [];

    internal ContextMenu(BlackoutManager manager)
    {
        _manager = manager;
    }

    internal void Show(IntPtr hwnd)
    {
        _lastMonitors = MonitorEnumerator.EnumerateMonitors();

        IntPtr hMenu = User32.CreatePopupMenu();
        if (hMenu == IntPtr.Zero)
            return;

        // Monitor items
        for (int i = 0; i < _lastMonitors.Count; i++)
        {
            MonitorInfo monitor = _lastMonitors[i];
            bool isActive = _manager.IsActive(monitor.DevicePath);
            string label = monitor.BuildMenuLabel(isActive);
            uint flags = WindowStyles.MF_STRING;
            if (isActive)
                flags |= WindowStyles.MF_CHECKED;

            User32.AppendMenuW(hMenu, flags, (nuint)(WindowStyles.MENU_ID_MONITOR_BASE + i), label);
        }

        // "Release All" (only when blackouts are active)
        if (_manager.ActiveCount > 0)
        {
            User32.AppendMenuW(hMenu, WindowStyles.MF_SEPARATOR, 0, null);
            User32.AppendMenuW(hMenu, WindowStyles.MF_STRING, (nuint)WindowStyles.MENU_ID_RELEASE_ALL, Strings.MenuReleaseAll);
        }

        // Exit
        User32.AppendMenuW(hMenu, WindowStyles.MF_SEPARATOR, 0, null);
        User32.AppendMenuW(hMenu, WindowStyles.MF_STRING, (nuint)WindowStyles.MENU_ID_EXIT, Strings.MenuExit);

        // Required for menu to dismiss on outside click (KB Q135788)
        User32.SetForegroundWindow(hwnd);

        User32.GetCursorPos(out POINT pt);
        User32.TrackPopupMenuEx(hMenu, WindowStyles.TPM_RIGHTBUTTON, pt.X, pt.Y, hwnd, IntPtr.Zero);

        // Post WM_NULL to fix menu tracking (KB Q135788)
        User32.PostMessageW(hwnd, WindowStyles.WM_NULL, 0, 0);

        User32.DestroyMenu(hMenu);
    }

    internal void HandleCommand(int commandId)
    {
        if (commandId == WindowStyles.MENU_ID_EXIT)
        {
            _manager.ReleaseAll();
            User32.PostQuitMessage(0);
            return;
        }

        if (commandId == WindowStyles.MENU_ID_RELEASE_ALL)
        {
            _manager.ReleaseAll();
            return;
        }

        // Monitor toggle
        int monitorIndex = commandId - WindowStyles.MENU_ID_MONITOR_BASE;
        if (monitorIndex >= 0 && monitorIndex < _lastMonitors.Count)
        {
            _manager.Toggle(_lastMonitors[monitorIndex]);
        }
    }
}
