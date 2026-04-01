using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.App;

internal sealed record MonitorInfo(string DevicePath, string FriendlyName, RECT Bounds, bool IsPrimary)
{
    internal string BuildMenuLabel(int index, bool isActive)
    {
        var label = $"&{index}  {FriendlyName}  {Bounds.Width}x{Bounds.Height}";

        if (IsPrimary)
            label += $"  {Strings.MenuPrimary}";

        if (isActive)
            label += $"  {Strings.MenuActive}";

        return label;
    }
}
