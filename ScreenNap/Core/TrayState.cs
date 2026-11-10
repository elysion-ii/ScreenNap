using System.Globalization;
using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.Core;

internal readonly record struct TrayState(bool UseActiveIcon, string TipText)
{
    // The buffers hold the terminating null as well
    private const int MaximumTipLength = WindowStyles.NotifyIconTipCapacity - 1;
    private const int MaximumInfoLength = WindowStyles.NotifyIconInfoCapacity - 1;

    internal static TrayState For(int activeCount)
    {
        return activeCount > 0
#pragma warning disable CA1863 // format string is a culture-dependent resource; caching CompositeFormat would pin one culture
            ? new TrayState(true, string.Format(CultureInfo.CurrentCulture, Strings.TooltipActive, activeCount))
#pragma warning restore CA1863
            : new TrayState(false, Strings.TooltipNormal);
    }

    internal static string TruncateTip(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length <= MaximumTipLength ? text : text[..MaximumTipLength];
    }

    internal static string TruncateInfo(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length <= MaximumInfoLength ? text : text[..MaximumInfoLength];
    }
}
