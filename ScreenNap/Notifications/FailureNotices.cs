using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ScreenNap.Resources;

namespace ScreenNap.Notifications;

[SuppressMessage("Performance", "CA1863:Use 'CompositeFormat'", Justification = "The format strings are culture-dependent resources; caching CompositeFormat would pin one culture")]
internal static class FailureNotices
{
    private const string ListSeparator = ", ";

    internal static string BlackoutNotCreated(string monitorName, CultureInfo culture)
        => string.Format(culture, Strings.NotifyBlackoutNotCreated(culture), monitorName);

    internal static string BlackoutNotClosed(string monitorName, CultureInfo culture)
        => string.Format(culture, Strings.NotifyBlackoutNotClosed(culture), monitorName);

    internal static string ReleaseAllIncomplete(int remaining, CultureInfo culture)
        => string.Format(culture, Strings.NotifyReleaseAllIncomplete(culture), remaining);

    internal static string BlackoutNotRestored(string monitorName, CultureInfo culture)
        => string.Format(culture, Strings.NotifyBlackoutNotRestored(culture), monitorName);

    internal static string HotkeysUnavailable(IReadOnlyList<string> combinations, CultureInfo culture)
        => string.Format(culture, Strings.NotifyHotkeysUnavailable(culture), string.Join(ListSeparator, combinations));

    internal static string OverlaysNotCreated(IReadOnlyList<int> monitorNumbers, CultureInfo culture)
    {
        string numbers = string.Join(ListSeparator, monitorNumbers.Select(number => number.ToString(CultureInfo.InvariantCulture)));
        return string.Format(culture, Strings.NotifyOverlaysNotCreated(culture), numbers);
    }
}
