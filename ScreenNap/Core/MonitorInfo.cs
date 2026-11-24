using System.Globalization;
using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.Core;

internal sealed record MonitorInfo(string DevicePath, string FriendlyName, RECT Bounds, bool IsPrimary, MonitorIdentity Identity)
{
    internal string BuildMenuLabel(int index, bool isActive, CultureInfo culture)
    {
        string label = string.Create(CultureInfo.InvariantCulture, $"&{index}  {FriendlyName}  {Bounds.Width}x{Bounds.Height}");

        if (IsPrimary)
            label += $"  {Strings.MenuPrimary(culture)}";

        if (isActive)
            label += $"  {Strings.MenuActive(culture)}";

        return label;
    }
}
