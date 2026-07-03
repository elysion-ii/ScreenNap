using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.Core;

internal sealed record MenuItem(bool Checked, bool IsSeparator, int CommandId, string? Text);

internal static class MenuModelBuilder
{
    internal static IReadOnlyList<MenuItem> Build(
        IReadOnlyList<MonitorInfo> monitors,
        IReadOnlySet<string> activeDevicePaths)
    {
        var items = new List<MenuItem>();
        for (int i = 0; i < monitors.Count; i++)
        {
            MonitorInfo monitor = monitors[i];
            bool isActive = activeDevicePaths.Contains(monitor.DevicePath);
            items.Add(new MenuItem(
                isActive,
                false,
                WindowStyles.MENU_ID_MONITOR_BASE + i,
                monitor.BuildMenuLabel(i + 1, isActive)));
        }

        if (activeDevicePaths.Count > 0)
        {
            items.Add(new MenuItem(false, true, 0, null));
            items.Add(new MenuItem(false, false, WindowStyles.MENU_ID_RELEASE_ALL, Strings.MenuReleaseAll));
        }

        items.Add(new MenuItem(false, true, 0, null));
        items.Add(new MenuItem(false, false, WindowStyles.MENU_ID_EXIT, Strings.MenuExit));
        return items;
    }
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
        if (commandId == WindowStyles.MENU_ID_EXIT)
            return new MenuCommand(MenuCommandKind.Exit);
        if (commandId == WindowStyles.MENU_ID_RELEASE_ALL)
            return new MenuCommand(MenuCommandKind.ReleaseAll);

        int monitorIndex = commandId - WindowStyles.MENU_ID_MONITOR_BASE;
        return monitorIndex >= 0 && monitorIndex < monitorCount
            ? new MenuCommand(MenuCommandKind.ToggleMonitor, monitorIndex)
            : new MenuCommand(MenuCommandKind.None);
    }
}
