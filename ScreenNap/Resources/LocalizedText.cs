using System.Globalization;
using System.Resources;

namespace ScreenNap.Resources;

// How a localized text is looked up, and what is shown when the lookup fails (specification L10N-4)
internal static class LocalizedText
{
    internal static string Lookup(ResourceManager manager, string key, CultureInfo culture)
    {
        try
        {
            return manager.GetString(key, culture) ?? key;
        }
        catch (MissingManifestResourceException)
        {
            return key;
        }
    }
}
