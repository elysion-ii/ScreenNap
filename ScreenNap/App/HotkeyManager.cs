using System.Runtime.InteropServices;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal sealed class HotkeyManager
{
    private const int MaxHotkeys = 9;
    private const uint Modifiers = WindowStyles.MOD_CONTROL | WindowStyles.MOD_ALT | WindowStyles.MOD_SHIFT | WindowStyles.MOD_NOREPEAT;

    private readonly BlackoutManager _manager;

    internal HotkeyManager(BlackoutManager manager)
    {
        _manager = manager;
    }

    internal void Register(IntPtr hwnd)
    {
        for (int i = 0; i < MaxHotkeys; i++)
        {
            int id = WindowStyles.HOTKEY_ID_BASE + i;
            uint vk = (uint)(0x31 + i); // VK_1 through VK_9

            if (!User32.RegisterHotKey(hwnd, id, Modifiers, vk))
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Warn($"Failed to register hotkey Ctrl+Alt+Shift+{i + 1} (Win32 error: {error})");
            }
        }
    }

    internal void Unregister(IntPtr hwnd)
    {
        for (int i = 0; i < MaxHotkeys; i++)
        {
            User32.UnregisterHotKey(hwnd, WindowStyles.HOTKEY_ID_BASE + i);
        }
    }

    internal void HandleHotkey(int hotkeyId)
    {
        int index = hotkeyId - WindowStyles.HOTKEY_ID_BASE;
        if (index < 0 || index >= MaxHotkeys)
            return;

        var monitors = MonitorEnumerator.EnumerateMonitors();
        if (index < monitors.Count)
        {
            _manager.Toggle(monitors[index]);
        }
    }
}
