namespace ScreenNap.Core;

internal readonly record struct HotkeyAction(HotkeyActionKind Kind, int MonitorIndex = -1);
