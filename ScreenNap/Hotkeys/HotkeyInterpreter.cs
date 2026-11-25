using System.Globalization;
using ScreenNap.Native;

namespace ScreenNap.Hotkeys;

internal static class HotkeyInterpreter
{
    internal const int MonitorHotkeyCount = 9;
    internal const int MonitorHotkeyIdBase = 3000;
    internal const int IdentifyHotkeyId = 3009;
    internal const int IdentifyDigit = 0;

    internal static IReadOnlyList<int> HotkeyIds { get; } =
        [.. Enumerable.Range(MonitorHotkeyIdBase, MonitorHotkeyCount), IdentifyHotkeyId];

    internal static string CombinationFor(int digit) => string.Create(CultureInfo.InvariantCulture, $"Ctrl+Shift+Alt+{digit}");

    internal static int DigitFor(int hotkeyId)
        => hotkeyId == IdentifyHotkeyId ? IdentifyDigit : hotkeyId - MonitorHotkeyIdBase + 1;

    internal static uint VirtualKeyFor(int hotkeyId) => WindowStyles.VK_0 + (uint)DigitFor(hotkeyId);

    internal static HotkeyAction Interpret(int hotkeyId)
    {
        if (hotkeyId == IdentifyHotkeyId)
            return new HotkeyAction(HotkeyActionKind.Identify);

        int monitorIndex = hotkeyId - MonitorHotkeyIdBase;
        return monitorIndex >= 0 && monitorIndex < MonitorHotkeyCount
            ? new HotkeyAction(HotkeyActionKind.ToggleMonitor, monitorIndex)
            : new HotkeyAction(HotkeyActionKind.None);
    }

    // A digit pressed while the digit of the last hotkey acted on is still held is part of a
    // chord, and only the first digit of a chord acts (specification HK-5)
    internal static bool IsChordedPress(int? lastActedHotkeyId, int hotkeyId, bool lastActedKeyHeld)
        => lastActedHotkeyId is { } lastActed && lastActed != hotkeyId && lastActedKeyHeld;
}
