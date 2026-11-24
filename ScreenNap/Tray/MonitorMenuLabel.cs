using System.Globalization;
using ScreenNap.Monitors;
using ScreenNap.Resources;

namespace ScreenNap.Tray;

internal static class MonitorMenuLabel
{
    internal static string Build(MonitorInfo monitor, int number, bool isActive, CultureInfo culture)
    {
        string label = string.Create(CultureInfo.InvariantCulture, $"&{number}  {monitor.FriendlyName}  {monitor.Bounds.Width}x{monitor.Bounds.Height}");

        if (monitor.IsPrimary)
            label += $"  {Strings.MenuPrimary(culture)}";

        if (isActive)
            label += $"  {Strings.MenuActive(culture)}";

        return label;
    }
}
