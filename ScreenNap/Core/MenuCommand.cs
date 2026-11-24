namespace ScreenNap.Core;

internal readonly record struct MenuCommand(MenuCommandKind Kind, int MonitorIndex = -1);
