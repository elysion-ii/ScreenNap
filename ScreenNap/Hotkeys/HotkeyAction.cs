namespace ScreenNap.Hotkeys;

internal readonly record struct HotkeyAction(HotkeyActionKind Kind, int MonitorIndex = -1);
