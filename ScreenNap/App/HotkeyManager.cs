using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal sealed class HotkeyManager
{
    private const int MaxHotkeys = 9;
    private const uint Modifiers = WindowStyles.MOD_CONTROL | WindowStyles.MOD_SHIFT | WindowStyles.MOD_ALT | WindowStyles.MOD_NOREPEAT;

    private readonly BlackoutManager _manager;

    internal HotkeyManager(BlackoutManager manager)
    {
        _manager = manager;
    }

    internal static void Register(IntPtr hwnd)
    {
        for (int i = 0; i < MaxHotkeys; i++)
        {
            int id = WindowStyles.HOTKEY_ID_BASE + i;
            uint vk = WindowStyles.VK_1 + (uint)i;

            if (!User32.RegisterHotKey(hwnd, id, Modifiers, vk))
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Warn($"Failed to register hotkey Ctrl+Shift+Alt+{i + 1} (Win32 error: {error})");
            }
        }

        if (!User32.RegisterHotKey(hwnd, WindowStyles.HOTKEY_ID_IDENTIFY, Modifiers, WindowStyles.VK_0))
        {
            int error = Marshal.GetLastWin32Error();
            Logger.Warn($"Failed to register hotkey Ctrl+Shift+Alt+0 (Win32 error: {error})");
        }
    }

    internal static void Unregister(IntPtr hwnd)
    {
        for (int i = 0; i < MaxHotkeys; i++)
        {
            if (!User32.UnregisterHotKey(hwnd, WindowStyles.HOTKEY_ID_BASE + i))
                Logger.Warn($"Failed to unregister monitor hotkey {i + 1} (Win32 error: {Marshal.GetLastWin32Error()})");
        }
        if (!User32.UnregisterHotKey(hwnd, WindowStyles.HOTKEY_ID_IDENTIFY))
            Logger.Warn($"Failed to unregister identify hotkey (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    internal void HandleHotkey(int hotkeyId)
    {
        HotkeyAction action = HotkeyInterpreter.Interpret(hotkeyId);
        if (action.Kind == HotkeyActionKind.Identify)
        {
            IdentifyOverlay.Toggle();
            return;
        }

        if (action.Kind != HotkeyActionKind.ToggleMonitor)
            return;

        var monitors = MonitorEnumerator.EnumerateMonitors();
        if (action.MonitorIndex < monitors.Count)
            _manager.Toggle(monitors[action.MonitorIndex]);
    }
}
