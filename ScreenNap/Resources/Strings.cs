using System.Globalization;
using System.Resources;

namespace ScreenNap.Resources;

internal static class Strings
{
    private static ResourceManager? s_resourceManager;

    private static ResourceManager ResourceManager =>
        s_resourceManager ??= new ResourceManager("ScreenNap.Resources.Strings",
            typeof(Strings).Assembly);

    internal static string MenuReleaseAll => Lookup(nameof(MenuReleaseAll));

    internal static string MenuExit => Lookup(nameof(MenuExit));

    internal static string MenuPrimary => Lookup(nameof(MenuPrimary));

    internal static string MenuActive => Lookup(nameof(MenuActive));

    internal static string TooltipNormal => Lookup(nameof(TooltipNormal));

    internal static string TooltipActive => Lookup(nameof(TooltipActive));

    internal static string NotifyAlreadyRunning => Lookup(nameof(NotifyAlreadyRunning));

    internal static string NotifyTitle => Lookup(nameof(NotifyTitle));

    internal static string NotifyStartFailed => Lookup(nameof(NotifyStartFailed));

    internal static string NotifyTrayFailed => Lookup(nameof(NotifyTrayFailed));

    internal static string NotifyBlackoutNotCreated => Lookup(nameof(NotifyBlackoutNotCreated));

    internal static string NotifyBlackoutNotClosed => Lookup(nameof(NotifyBlackoutNotClosed));

    internal static string NotifyReleaseAllIncomplete => Lookup(nameof(NotifyReleaseAllIncomplete));

    internal static string NotifyBlackoutNotRestored => Lookup(nameof(NotifyBlackoutNotRestored));

    internal static string NotifyHotkeysUnavailable => Lookup(nameof(NotifyHotkeysUnavailable));

    internal static string NotifyOverlaysNotCreated => Lookup(nameof(NotifyOverlaysNotCreated));

    internal static string Lookup(ResourceManager manager, string key)
    {
        try
        {
            return manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }
        catch (MissingManifestResourceException)
        {
            return key;
        }
    }

    private static string Lookup(string key) => Lookup(ResourceManager, key);
}
