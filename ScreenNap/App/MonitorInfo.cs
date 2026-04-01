using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.App;

internal sealed record MonitorInfo(string DevicePath, string FriendlyName, RECT Bounds, bool IsPrimary)
{
    internal string BuildMenuLabel(bool isActive)
    {
        var label = $"{FriendlyName}  {Bounds.Width}x{Bounds.Height}";

        if (IsPrimary)
            label += $"  {Strings.MenuPrimary}";

        if (isActive)
            label += $"  {Strings.MenuActive}";

        return label;
    }
}
