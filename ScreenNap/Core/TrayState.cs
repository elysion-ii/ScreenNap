using System.Globalization;
using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.Core;

internal readonly record struct TrayState(bool UseActiveIcon, string TipText)
{
    // The buffers hold the terminating null as well
    private const int MaximumTipLength = WindowStyles.NotifyIconTipCapacity - 1;
    private const int MaximumInfoLength = WindowStyles.NotifyIconInfoCapacity - 1;
    private const int MaximumInfoTitleLength = WindowStyles.NotifyIconInfoTitleCapacity - 1;

    internal static TrayState For(int activeCount, CultureInfo culture)
    {
        return activeCount > 0
#pragma warning disable CA1863 // format string is a culture-dependent resource; caching CompositeFormat would pin one culture
            ? new TrayState(true, string.Format(culture, Strings.TooltipActive(culture), activeCount))
#pragma warning restore CA1863
            : new TrayState(false, Strings.TooltipNormal(culture));
    }

    internal static string TruncateTip(string text) => Truncate(text, MaximumTipLength);

    internal static string TruncateInfo(string text) => Truncate(text, MaximumInfoLength);

    internal static string TruncateInfoTitle(string text) => Truncate(text, MaximumInfoTitleLength);

    private static string Truncate(string text, int maximumLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length <= maximumLength ? text : text[..maximumLength];
    }
}
