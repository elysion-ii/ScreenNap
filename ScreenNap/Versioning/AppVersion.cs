using System.Reflection;

namespace ScreenNap.Versioning;

// The product version this executable was built with
internal static class AppVersion
{
    internal static string Current { get; } = ProductVersion.Parse(
        typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    internal static string DisplayText => ProductVersion.DisplayText(Current);
}
