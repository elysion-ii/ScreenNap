using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;

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
        {
            Logger.Warn($"CreatePopupMenu failed (Win32 error: {Marshal.GetLastWin32Error()})");
            return;
        }

        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build(
            _lastMonitors,
            _manager.ActiveDevicePaths);
        foreach (MenuItem item in items)
        {
            uint flags = item.IsSeparator ? WindowStyles.MF_SEPARATOR : WindowStyles.MF_STRING;
            if (item.Checked)
                flags |= WindowStyles.MF_CHECKED;

            if (!User32.AppendMenuW(hMenu, flags, (nuint)item.CommandId, item.Text))
                Logger.Warn($"AppendMenuW failed for command {item.CommandId} (Win32 error: {Marshal.GetLastWin32Error()})");
        }

        // Required for menu to dismiss on outside click (KB Q135788)
        if (!User32.SetForegroundWindow(hwnd))
            Logger.Warn("SetForegroundWindow failed before displaying context menu");

        if (!User32.GetCursorPos(out POINT pt))
        {
            Logger.Warn($"GetCursorPos failed before displaying context menu (Win32 error: {Marshal.GetLastWin32Error()})");
            pt = default;
        }
        if (!User32.TrackPopupMenuEx(hMenu, WindowStyles.TPM_RIGHTBUTTON, pt.X, pt.Y, hwnd, IntPtr.Zero))
            Logger.Warn($"TrackPopupMenuEx failed (Win32 error: {Marshal.GetLastWin32Error()})");

        // Post WM_NULL to fix menu tracking (KB Q135788)
        if (!User32.PostMessageW(hwnd, WindowStyles.WM_NULL, 0, 0))
            Logger.Warn($"PostMessageW failed after displaying context menu (Win32 error: {Marshal.GetLastWin32Error()})");

        if (!User32.DestroyMenu(hMenu))
            Logger.Warn($"DestroyMenu failed for context menu (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    internal void HandleCommand(int commandId)
    {
        MenuCommand command = MenuCommandInterpreter.Interpret(commandId, _lastMonitors.Count);
        if (command.Kind == MenuCommandKind.Exit)
        {
            User32.PostQuitMessage(0);
            return;
        }

        if (command.Kind == MenuCommandKind.ReleaseAll)
        {
            _manager.ReleaseAll();
            return;
        }

        if (command.Kind == MenuCommandKind.ToggleMonitor)
            _manager.Toggle(_lastMonitors[command.MonitorIndex]);
    }
}
