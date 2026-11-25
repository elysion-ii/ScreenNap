namespace ScreenNap.Versioning;

// How an informational version becomes the product version and its display text
// (specification VER-1)
internal static class ProductVersion
{
    private const string ProductName = "ScreenNap";
    private const char MetadataSeparator = '+';

    // Directory.Build.props keeps the informational version free of build metadata;
    // stripping it here keeps the displayed version at X.Y.Z even in a build that lost that setting
    internal static string Parse(string? informationalVersion)
    {
        string value = informationalVersion ?? string.Empty;
        int metadataStart = value.IndexOf(MetadataSeparator, StringComparison.Ordinal);
        string version = (metadataStart >= 0 ? value[..metadataStart] : value).Trim();

        // The build fails without a <Version>, so an executable without one is a defective build
        if (version.Length == 0)
            throw new InvalidOperationException("The executable carries no product version: Directory.Build.props must define <Version> (docs/rules/dotnet.md, VERSION).");
        return version;
    }

    internal static string DisplayText(string version) => $"{ProductName} {version}";
}
