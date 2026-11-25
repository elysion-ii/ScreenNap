using System.Reflection;

namespace ScreenNap.Versioning;

internal static class AppVersion
{
    internal const string ProductName = "ScreenNap";
    private const char MetadataSeparator = '+';

    internal static string Current { get; } = Normalize(
        typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    internal static string DisplayText => Format(Current);

    // Directory.Build.props keeps the informational version free of build metadata;
    // stripping it here keeps the displayed version at X.Y.Z even in a build that lost that setting
    internal static string Normalize(string? informationalVersion)
    {
        string value = informationalVersion ?? string.Empty;
        int metadataStart = value.IndexOf(MetadataSeparator, StringComparison.Ordinal);
        string version = (metadataStart >= 0 ? value[..metadataStart] : value).Trim();

        // The build fails without a <Version>, so an executable without one is a defective build
        if (version.Length == 0)
            throw new InvalidOperationException("The executable carries no product version: Directory.Build.props must define <Version> (docs/rules/dotnet.md, VERSION).");
        return version;
    }

    internal static string Format(string version) => $"{ProductName} {version}";
}
