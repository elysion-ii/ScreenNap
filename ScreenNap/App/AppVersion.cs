using System.Reflection;

namespace ScreenNap.App;

internal static class AppVersion
{
    internal const string ProductName = "ScreenNap";
    internal const string Unknown = "unknown";
    private const char MetadataSeparator = '+';

    internal static string Current { get; } = Normalize(
        typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    internal static string DisplayText => Format(Current);

    // Directory.Build.props keeps the informational version free of build metadata;
    // stripping it here keeps the displayed version at X.Y.Z even in a build that lost that setting
    internal static string Normalize(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
            return Unknown;

        int metadataStart = informationalVersion.IndexOf(MetadataSeparator);
        string version = metadataStart >= 0
            ? informationalVersion[..metadataStart]
            : informationalVersion;

        version = version.Trim();
        return version.Length > 0 ? version : Unknown;
    }

    internal static string Format(string version) => $"{ProductName} {version}";
}
