using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Versioning;

namespace ScreenNap.Tray;

internal sealed class ContextMenu
{
    private readonly BlackoutManager _manager;
    private readonly Func<IReadOnlyList<MonitorInfo>> _enumerateMonitors;
    private readonly IPopupMenuApi _api;
    private readonly Action _exit;
    private readonly CultureInfo _culture;
    private IReadOnlyList<MonitorInfo> _lastMonitors = [];

    internal ContextMenu(BlackoutManager manager, Func<IReadOnlyList<MonitorInfo>> enumerateMonitors, IPopupMenuApi api, Action exit, CultureInfo culture)
    {
        _manager = manager;
        _enumerateMonitors = enumerateMonitors;
        _api = api;
        _exit = exit;
        _culture = culture;
    }

    internal void Show(IntPtr hwnd)
    {
        _lastMonitors = _enumerateMonitors();

        IntPtr hMenu = _api.CreatePopupMenu();
        if (hMenu == IntPtr.Zero)
        {
            Logger.Warn("CreatePopupMenu failed", _api.LastError);
            return;
        }

        IReadOnlyList<MenuItem> items = MenuModelBuilder.Build(
            _lastMonitors,
            _lastMonitors.Where(_manager.IsActive).Select(monitor => monitor.DevicePath).ToHashSet(StringComparer.Ordinal),
            _manager.ActiveCount,
            AppVersion.DisplayText,
            _culture);
        foreach (MenuItem item in items)
        {
            uint flags = item.IsSeparator ? WindowStyles.MF_SEPARATOR : WindowStyles.MF_STRING;
            if (item.Checked)
                flags |= WindowStyles.MF_CHECKED;
            if (item.Disabled)
                flags |= WindowStyles.MF_GRAYED;

            if (!_api.AppendMenu(hMenu, flags, (nuint)item.CommandId, item.Text))
                Logger.Warn($"AppendMenuW failed for command {item.CommandId}", _api.LastError);
        }

        // Required for menu to dismiss on outside click (KB Q135788)
        if (!_api.SetForegroundWindow(hwnd))
            Logger.Warn("SetForegroundWindow failed before displaying context menu");

        // Without a cursor position the menu still opens, at the screen origin (specification TRAY-4)
        if (!_api.GetCursorPos(out POINT pt))
        {
            Logger.Warn("GetCursorPos failed before displaying context menu; opening it at 0,0", _api.LastError);
            pt = default;
        }
        if (!_api.TrackPopupMenu(hMenu, WindowStyles.TPM_RIGHTBUTTON, pt.X, pt.Y, hwnd))
            Logger.Warn("TrackPopupMenuEx failed", _api.LastError);

        // Post WM_NULL to fix menu tracking (KB Q135788)
        if (!_api.PostMessage(hwnd, WindowStyles.WM_NULL, 0, 0))
            Logger.Warn("PostMessageW failed after displaying context menu", _api.LastError);

        if (!_api.DestroyMenu(hMenu))
            Logger.Warn("DestroyMenu failed for context menu", _api.LastError);
    }

    internal void HandleCommand(int commandId)
    {
        MenuCommand command = MenuCommandInterpreter.Interpret(commandId, _lastMonitors.Count);
        if (command.Kind == MenuCommandKind.Exit)
        {
            _exit();
            return;
        }

        if (command.Kind == MenuCommandKind.ReleaseAll)
        {
            _manager.ReleaseAll();
            return;
        }

        if (command.Kind == MenuCommandKind.ToggleMonitor)
            ToggleSelected(_lastMonitors[command.MonitorIndex]);
    }

    // The display may have changed while the menu was open: a live blackout closes, and a new
    // one covers the monitor as it is now, or nothing happens when it is gone (specification MENU-6)
    private void ToggleSelected(MonitorInfo selected)
    {
        if (_manager.IsActive(selected))
        {
            _manager.Toggle(selected);
            return;
        }

        MonitorInfo? current = _enumerateMonitors().FirstOrDefault(monitor => monitor.Denotes(selected.Identity, selected.DevicePath));
        if (current is null)
        {
            Logger.Info($"Menu selection ignored: {selected.FriendlyName} ({selected.DevicePath}) is no longer present");
            return;
        }

        _manager.Toggle(current);
    }
}
