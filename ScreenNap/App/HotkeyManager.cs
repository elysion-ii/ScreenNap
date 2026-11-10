using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal sealed class HotkeyManager
{
    private const uint Modifiers = WindowStyles.MOD_CONTROL | WindowStyles.MOD_SHIFT | WindowStyles.MOD_ALT | WindowStyles.MOD_NOREPEAT;

    private readonly BlackoutManager _manager;
    private readonly MonitorIdentifier _identifier;
    private readonly IUserNotifier _notifier;
    private readonly List<int> _registeredIds = [];

    internal HotkeyManager(BlackoutManager manager, MonitorIdentifier identifier, IUserNotifier notifier)
    {
        _manager = manager;
        _identifier = identifier;
        _notifier = notifier;
    }

    internal void Register(IntPtr hwnd)
    {
        var unavailable = new List<string>();
        for (int i = 0; i < HotkeyInterpreter.MonitorHotkeyCount; i++)
        {
            if (!TryRegister(hwnd, HotkeyInterpreter.MonitorHotkeyIdBase + i, WindowStyles.VK_1 + (uint)i, i + 1))
                unavailable.Add(HotkeyInterpreter.CombinationFor(i + 1));
        }

        if (!TryRegister(hwnd, HotkeyInterpreter.IdentifyHotkeyId, WindowStyles.VK_0, HotkeyInterpreter.IdentifyDigit))
            unavailable.Add(HotkeyInterpreter.CombinationFor(HotkeyInterpreter.IdentifyDigit));

        if (unavailable.Count > 0)
            _notifier.Notify(FailureNotices.HotkeysUnavailable(unavailable));
    }

    internal void Unregister(IntPtr hwnd)
    {
        foreach (int id in _registeredIds)
        {
            if (!User32.UnregisterHotKey(hwnd, id))
                Logger.Warn($"Failed to unregister hotkey {id}", Marshal.GetLastWin32Error());
        }
        _registeredIds.Clear();
    }

    internal void HandleHotkey(int hotkeyId)
    {
        HotkeyAction action = HotkeyInterpreter.Interpret(hotkeyId);
        if (action.Kind == HotkeyActionKind.Identify)
        {
            _identifier.Toggle();
            return;
        }

        if (action.Kind != HotkeyActionKind.ToggleMonitor)
            return;

        List<MonitorInfo> monitors = MonitorEnumerator.EnumerateMonitors();
        if (action.MonitorIndex < monitors.Count)
            _manager.Toggle(monitors[action.MonitorIndex]);
    }

    private bool TryRegister(IntPtr hwnd, int id, uint virtualKey, int digit)
    {
        if (!User32.RegisterHotKey(hwnd, id, Modifiers, virtualKey))
        {
            int error = Marshal.GetLastWin32Error();
            Logger.Warn($"Failed to register hotkey {HotkeyInterpreter.CombinationFor(digit)}", error);
            return false;
        }

        _registeredIds.Add(id);
        return true;
    }
}
