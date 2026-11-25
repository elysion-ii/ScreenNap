using System.Globalization;
using ScreenNap.Monitors;
using ScreenNap.Resources;

namespace ScreenNap.Tray;

internal static class MonitorMenuLabel
{
    internal static string Build(MonitorInfo monitor, int number, bool isActive, CultureInfo culture)
    {
        // A menu shows "&&" as one "&" instead of taking the next character as an access key (MENU-4)
        string name = monitor.FriendlyName.Replace("&", "&&", StringComparison.Ordinal);
        string label = string.Create(CultureInfo.InvariantCulture, $"&{number}  {name}  {monitor.Bounds.Width}x{monitor.Bounds.Height}");

        if (monitor.IsPrimary)
            label += $"  {Strings.MenuPrimary(culture)}";

        if (isActive)
            label += $"  {Strings.MenuActive(culture)}";

        return label;
    }
}
