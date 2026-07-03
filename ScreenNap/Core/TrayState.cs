using ScreenNap.Resources;

namespace ScreenNap.Core;

internal readonly record struct TrayState(bool UseActiveIcon, string TipText)
{
    private const int MaximumTipLength = 127;

    internal static TrayState For(int activeCount)
    {
        return activeCount > 0
            ? new TrayState(true, string.Format(Strings.TooltipActive, activeCount))
            : new TrayState(false, Strings.TooltipNormal);
    }

    internal static string TruncateTip(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length <= MaximumTipLength ? text : text[..MaximumTipLength];
    }
}
