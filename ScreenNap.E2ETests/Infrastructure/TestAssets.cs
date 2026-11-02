using System.Diagnostics;
using System.Reflection;

namespace ScreenNap.E2ETests.Infrastructure;

internal static class TestAssets
{
    internal static string ScreenNapExe => Path.GetFullPath(Metadata("ScreenNapExe"));

    internal static string RepositoryRoot => Path.GetFullPath(Metadata("RepositoryRoot"));

    internal static string PublishedExe => Path.Combine(RepositoryRoot, "build", "ScreenNap", "ScreenNap.exe");

    internal static string Installer => Path.Combine(RepositoryRoot, "build", "Installer", $"ScreenNap-Setup-{Version}.exe");

    internal static string InstallerScript => Path.Combine(RepositoryRoot, "build", "Setup_ScreenNap.iss");

    // The test assembly inherits <Version> from the same Directory.Build.props as ScreenNap
    internal static string Version =>
        typeof(TestAssets).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? throw new InvalidOperationException("The test assembly carries no informational version");

    internal static void RequireNoRunningScreenNap()
    {
        Process[] running = Process.GetProcessesByName("ScreenNap");
        foreach (Process process in running)
            process.Dispose();
        if (running.Length > 0)
            throw new InvalidOperationException("ScreenNap is already running. Exit the resident ScreenNap before running the end-to-end tests (docs/adr/0003-end-to-end-tests-drive-the-built-application.md).");
    }

    internal static string CreateScratchDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ScreenNapE2E_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    internal static void DeleteScratchDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    private static string Metadata(string key)
        => typeof(TestAssets).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == key)?.Value
            ?? throw new InvalidOperationException($"The test assembly carries no {key} metadata");
}
