using System.Globalization;

namespace ScreenNap.Core;

internal static class HotkeyInterpreter
{
    internal const int MonitorHotkeyCount = 9;
    internal const int MonitorHotkeyIdBase = 3000;
    internal const int IdentifyHotkeyId = 3009;
    internal const int IdentifyDigit = 0;

    internal static string CombinationFor(int digit) => string.Create(CultureInfo.InvariantCulture, $"Ctrl+Shift+Alt+{digit}");

    internal static HotkeyAction Interpret(int hotkeyId)
    {
        if (hotkeyId == IdentifyHotkeyId)
            return new HotkeyAction(HotkeyActionKind.Identify);

        int monitorIndex = hotkeyId - MonitorHotkeyIdBase;
        return monitorIndex >= 0 && monitorIndex < MonitorHotkeyCount
            ? new HotkeyAction(HotkeyActionKind.ToggleMonitor, monitorIndex)
            : new HotkeyAction(HotkeyActionKind.None);
    }
}
