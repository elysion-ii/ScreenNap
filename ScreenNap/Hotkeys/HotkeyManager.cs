using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Identify;
using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Notifications;

namespace ScreenNap.Hotkeys;

internal sealed class HotkeyManager
{
    private const uint Modifiers = WindowStyles.MOD_CONTROL | WindowStyles.MOD_SHIFT | WindowStyles.MOD_ALT | WindowStyles.MOD_NOREPEAT;

    private readonly BlackoutManager _manager;
    private readonly MonitorIdentifier _identifier;
    private readonly IUserNotifier _notifier;
    private readonly Func<IReadOnlyList<MonitorInfo>> _enumerateMonitors;
    private readonly IHotkeyApi _api;
    private readonly CultureInfo _culture;
    private readonly List<int> _registeredIds = [];
    private int? _lastActedHotkeyId;

    internal HotkeyManager(
        BlackoutManager manager,
        MonitorIdentifier identifier,
        IUserNotifier notifier,
        Func<IReadOnlyList<MonitorInfo>> enumerateMonitors,
        IHotkeyApi api,
        CultureInfo culture)
    {
        _manager = manager;
        _identifier = identifier;
        _notifier = notifier;
        _enumerateMonitors = enumerateMonitors;
        _api = api;
        _culture = culture;
    }

    internal void Register(IntPtr hwnd)
    {
        var unavailable = new List<string>();
        foreach (int id in HotkeyInterpreter.HotkeyIds)
        {
            if (!TryRegister(hwnd, id))
                unavailable.Add(HotkeyInterpreter.CombinationFor(HotkeyInterpreter.DigitFor(id)));
        }

        if (unavailable.Count > 0)
            _notifier.Notify(FailureNotices.HotkeysUnavailable(unavailable, _culture));
    }

    internal void Unregister(IntPtr hwnd)
    {
        foreach (int id in _registeredIds)
        {
            if (!_api.UnregisterHotKey(hwnd, id))
                Logger.Warn($"Failed to unregister hotkey {id}", _api.LastError);
        }
        _registeredIds.Clear();
    }

    internal void HandleHotkey(int hotkeyId)
    {
        HotkeyAction action = HotkeyInterpreter.Interpret(hotkeyId);
        if (action.Kind == HotkeyActionKind.None)
            return;

        bool lastActedKeyHeld = _lastActedHotkeyId is { } lastActed
            && _api.IsKeyDown(HotkeyInterpreter.VirtualKeyFor(lastActed));
        if (HotkeyInterpreter.IsChordedPress(_lastActedHotkeyId, hotkeyId, lastActedKeyHeld))
        {
            Logger.Info($"Hotkey {HotkeyInterpreter.CombinationFor(HotkeyInterpreter.DigitFor(hotkeyId))} ignored while another digit is held");
            return;
        }

        _lastActedHotkeyId = hotkeyId;
        if (action.Kind == HotkeyActionKind.Identify)
        {
            _identifier.Toggle();
            return;
        }

        if (action.Kind != HotkeyActionKind.ToggleMonitor)
            return;

        IReadOnlyList<MonitorInfo> monitors = _enumerateMonitors();
        if (action.MonitorIndex < monitors.Count)
            _manager.Toggle(monitors[action.MonitorIndex]);
    }

    private bool TryRegister(IntPtr hwnd, int id)
    {
        if (!_api.RegisterHotKey(hwnd, id, Modifiers, HotkeyInterpreter.VirtualKeyFor(id)))
        {
            Logger.Warn($"Failed to register hotkey {HotkeyInterpreter.CombinationFor(HotkeyInterpreter.DigitFor(id))}", _api.LastError);
            return false;
        }

        _registeredIds.Add(id);
        return true;
    }
}
