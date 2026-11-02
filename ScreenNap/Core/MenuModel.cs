using ScreenNap.Resources;

namespace ScreenNap.Core;

internal sealed record MenuItem(bool Checked, bool IsSeparator, int CommandId, string? Text, bool Disabled = false);

internal static class MenuCommandIds
{
    internal const int None = 0;
    internal const int MonitorBase = 1000;
    internal const int ReleaseAll = 2000;
    internal const int Exit = 9999;
}

internal static class MenuModelBuilder
{
    internal static IReadOnlyList<MenuItem> Build(
        IReadOnlyList<MonitorInfo> monitors,
        IReadOnlySet<string> activeDevicePaths,
        string versionText)
    {
        var items = new List<MenuItem>
        {
            new(false, false, MenuCommandIds.None, versionText, true),
            Separator()
        };

        for (int i = 0; i < monitors.Count; i++)
        {
            MonitorInfo monitor = monitors[i];
            bool isActive = activeDevicePaths.Contains(monitor.DevicePath);
            items.Add(new MenuItem(
                isActive,
                false,
                MenuCommandIds.MonitorBase + i,
                monitor.BuildMenuLabel(i + 1, isActive)));
        }

        if (activeDevicePaths.Count > 0)
        {
            items.Add(Separator());
            items.Add(new MenuItem(false, false, MenuCommandIds.ReleaseAll, Strings.MenuReleaseAll));
        }

        if (!items[^1].IsSeparator)
            items.Add(Separator());
        items.Add(new MenuItem(false, false, MenuCommandIds.Exit, Strings.MenuExit));
        return items;
    }

    private static MenuItem Separator()
        => new(false, true, MenuCommandIds.None, null);
}

internal enum MenuCommandKind
{
    None,
    Exit,
    ReleaseAll,
    ToggleMonitor
}

internal readonly record struct MenuCommand(MenuCommandKind Kind, int MonitorIndex = -1);

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
