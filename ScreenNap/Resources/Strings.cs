using System.Globalization;
using System.Resources;

namespace ScreenNap.Resources;

internal static class Strings
{
    private static ResourceManager? s_resourceManager;

    private static ResourceManager ResourceManager =>
        s_resourceManager ??= new ResourceManager("ScreenNap.Resources.Strings",
            typeof(Strings).Assembly);

    internal static string MenuReleaseAll => Lookup("MenuReleaseAll", "Release All");

    internal static string MenuExit => Lookup("MenuExit", "Exit");

    internal static string MenuPrimary => Lookup("MenuPrimary", "[Primary]");

    internal static string MenuActive => Lookup("MenuActive", "(Active)");

    internal static string TooltipNormal => Lookup("TooltipNormal", "ScreenNap");

    internal static string TooltipActive => Lookup("TooltipActive", "ScreenNap ({0} active)");

    internal static string NotifyAlreadyRunning => Lookup("NotifyAlreadyRunning", "ScreenNap is already running.");

    internal static string NotifyTitle => Lookup("NotifyTitle", "ScreenNap");

    internal static string NotifyStartFailed => Lookup("NotifyStartFailed", "ScreenNap could not start. See the log for details.");

    internal static string NotifyTrayFailed => Lookup("NotifyTrayFailed", "ScreenNap could not create its tray icon and will exit.");

    internal static string NotifyBlackoutNotCreated => Lookup("NotifyBlackoutNotCreated", "Could not black out {0}.");

    internal static string NotifyBlackoutNotClosed => Lookup("NotifyBlackoutNotClosed", "Could not release the blackout on {0}.");

    internal static string NotifyReleaseAllIncomplete => Lookup("NotifyReleaseAllIncomplete", "{0} blackout(s) could not be released.");

    internal static string NotifyBlackoutNotRestored => Lookup("NotifyBlackoutNotRestored", "Could not restore the blackout on {0}.");

    internal static string NotifyHotkeysUnavailable => Lookup("NotifyHotkeysUnavailable", "These hotkeys are unavailable: {0}");

    internal static string NotifyOverlaysNotCreated => Lookup("NotifyOverlaysNotCreated", "Could not show the identification number on monitor {0}.");

    internal static string Lookup(ResourceManager manager, string key, string fallback)
    {
        try
        {
            return manager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
        }
        catch (MissingManifestResourceException)
        {
            return fallback;
        }
    }

    private static string Lookup(string key, string fallback) => Lookup(ResourceManager, key, fallback);
}
