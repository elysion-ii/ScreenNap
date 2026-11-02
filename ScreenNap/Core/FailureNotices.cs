using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ScreenNap.Resources;

namespace ScreenNap.Core;

[SuppressMessage("Performance", "CA1863:Use 'CompositeFormat'", Justification = "The format strings are culture-dependent resources; caching CompositeFormat would pin one culture")]
internal static class FailureNotices
{
    private const string ListSeparator = ", ";

    internal static string BlackoutNotCreated(string monitorName)
        => string.Format(CultureInfo.CurrentCulture, Strings.NotifyBlackoutNotCreated, monitorName);

    internal static string BlackoutNotClosed(string monitorName)
        => string.Format(CultureInfo.CurrentCulture, Strings.NotifyBlackoutNotClosed, monitorName);

    internal static string ReleaseAllIncomplete(int remaining)
        => string.Format(CultureInfo.CurrentCulture, Strings.NotifyReleaseAllIncomplete, remaining);

    internal static string BlackoutNotRestored(string monitorName)
        => string.Format(CultureInfo.CurrentCulture, Strings.NotifyBlackoutNotRestored, monitorName);

    internal static string HotkeysUnavailable(IReadOnlyList<string> combinations)
        => string.Format(CultureInfo.CurrentCulture, Strings.NotifyHotkeysUnavailable, string.Join(ListSeparator, combinations));

    internal static string OverlaysNotCreated(IReadOnlyList<int> monitorNumbers)
    {
        string numbers = string.Join(ListSeparator, monitorNumbers.Select(number => number.ToString(CultureInfo.InvariantCulture)));
        return string.Format(CultureInfo.CurrentCulture, Strings.NotifyOverlaysNotCreated, numbers);
    }
}
