using ScreenNap.Native;

namespace ScreenNap.Core;

internal enum HotkeyActionKind
{
    None,
    Identify,
    ToggleMonitor
}

internal readonly record struct HotkeyAction(HotkeyActionKind Kind, int MonitorIndex = -1);

internal static class HotkeyInterpreter
{
    private const int MonitorHotkeyCount = 9;

    internal static HotkeyAction Interpret(int hotkeyId)
    {
        if (hotkeyId == WindowStyles.HOTKEY_ID_IDENTIFY)
            return new HotkeyAction(HotkeyActionKind.Identify);

        int monitorIndex = hotkeyId - WindowStyles.HOTKEY_ID_BASE;
        return monitorIndex >= 0 && monitorIndex < MonitorHotkeyCount
            ? new HotkeyAction(HotkeyActionKind.ToggleMonitor, monitorIndex)
            : new HotkeyAction(HotkeyActionKind.None);
    }
}
