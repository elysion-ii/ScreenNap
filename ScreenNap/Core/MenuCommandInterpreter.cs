namespace ScreenNap.Core;

internal static class MenuCommandInterpreter
{
    internal static MenuCommand Interpret(int commandId, int monitorCount)
    {
        if (commandId == MenuCommandIds.Exit)
            return new MenuCommand(MenuCommandKind.Exit);
        if (commandId == MenuCommandIds.ReleaseAll)
            return new MenuCommand(MenuCommandKind.ReleaseAll);

        int monitorIndex = commandId - MenuCommandIds.MonitorBase;
        return monitorIndex >= 0 && monitorIndex < monitorCount
            ? new MenuCommand(MenuCommandKind.ToggleMonitor, monitorIndex)
            : new MenuCommand(MenuCommandKind.None);
    }
}
