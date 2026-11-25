using System.Globalization;
using ScreenNap.Monitors;
using ScreenNap.Resources;

namespace ScreenNap.Tray;

internal static class MenuModelBuilder
{
    internal static IReadOnlyList<MenuItem> Build(
        IReadOnlyList<MonitorInfo> monitors,
        IReadOnlySet<string> activeDevicePaths,
        int liveBlackoutCount,
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

        // A live blackout counts even when its monitor is missing from the enumeration (MON-6)
        if (liveBlackoutCount > 0)
        {
            AddSeparator(items);
            items.Add(new MenuItem(false, false, MenuCommandIds.ReleaseAll, Strings.MenuReleaseAll(culture)));
        }

        AddSeparator(items);
        items.Add(new MenuItem(false, false, MenuCommandIds.Exit, Strings.MenuExit(culture)));
        return items;
    }

    // Never two separators in a row (specification MENU-2)
    private static void AddSeparator(List<MenuItem> items)
    {
        if (!items[^1].IsSeparator)
            items.Add(Separator());
    }

    private static MenuItem Separator()
        => new(false, true, MenuCommandIds.None, null);
}
