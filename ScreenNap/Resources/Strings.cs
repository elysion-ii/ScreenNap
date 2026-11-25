using System.Globalization;
using System.Resources;

namespace ScreenNap.Resources;

internal static class Strings
{
    private static ResourceManager? s_resourceManager;

    private static ResourceManager ResourceManager =>
        s_resourceManager ??= new ResourceManager("ScreenNap.Resources.Strings",
            typeof(Strings).Assembly);

    internal static string MenuReleaseAll(CultureInfo culture) => Lookup(nameof(MenuReleaseAll), culture);

    internal static string MenuExit(CultureInfo culture) => Lookup(nameof(MenuExit), culture);

    internal static string MenuPrimary(CultureInfo culture) => Lookup(nameof(MenuPrimary), culture);

    internal static string MenuActive(CultureInfo culture) => Lookup(nameof(MenuActive), culture);

    internal static string TooltipNormal(CultureInfo culture) => Lookup(nameof(TooltipNormal), culture);

    internal static string TooltipActive(CultureInfo culture) => Lookup(nameof(TooltipActive), culture);

    internal static string NotifyAlreadyRunning(CultureInfo culture) => Lookup(nameof(NotifyAlreadyRunning), culture);

    internal static string NotifyTitle(CultureInfo culture) => Lookup(nameof(NotifyTitle), culture);

    internal static string NotifyStartFailed(CultureInfo culture) => Lookup(nameof(NotifyStartFailed), culture);

    internal static string NotifyTrayFailed(CultureInfo culture) => Lookup(nameof(NotifyTrayFailed), culture);

    internal static string NotifyStopped(CultureInfo culture) => Lookup(nameof(NotifyStopped), culture);

    internal static string NotifyBlackoutNotCreated(CultureInfo culture) => Lookup(nameof(NotifyBlackoutNotCreated), culture);

    internal static string NotifyBlackoutNotClosed(CultureInfo culture) => Lookup(nameof(NotifyBlackoutNotClosed), culture);

    internal static string NotifyReleaseAllIncomplete(CultureInfo culture) => Lookup(nameof(NotifyReleaseAllIncomplete), culture);

    internal static string NotifyBlackoutNotRestored(CultureInfo culture) => Lookup(nameof(NotifyBlackoutNotRestored), culture);

    internal static string NotifyHotkeysUnavailable(CultureInfo culture) => Lookup(nameof(NotifyHotkeysUnavailable), culture);

    internal static string NotifyOverlaysNotCreated(CultureInfo culture) => Lookup(nameof(NotifyOverlaysNotCreated), culture);

    private static string Lookup(string key, CultureInfo culture) => LocalizedText.Lookup(ResourceManager, key, culture);
}
