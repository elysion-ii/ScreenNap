using System.Globalization;
using ScreenNap.Monitors;
using ScreenNap.Resources;

namespace ScreenNap.Tray;

internal static class MenuModelBuilder
{
    internal static IReadOnlyList<MenuItem> Build(
        IReadOnlyList<MonitorInfo> monitors,
        IReadOnlySet<string> activeDevicePaths,
        string versionText,
        CultureInfo culture)
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
                MonitorMenuLabel.Build(monitor, i + 1, isActive, culture)));
        }

        if (activeDevicePaths.Count > 0)
        {
            items.Add(Separator());
            items.Add(new MenuItem(false, false, MenuCommandIds.ReleaseAll, Strings.MenuReleaseAll(culture)));
        }

        if (!items[^1].IsSeparator)
            items.Add(Separator());
        items.Add(new MenuItem(false, false, MenuCommandIds.Exit, Strings.MenuExit(culture)));
        return items;
    }

    private static MenuItem Separator()
        => new(false, true, MenuCommandIds.None, null);
}
