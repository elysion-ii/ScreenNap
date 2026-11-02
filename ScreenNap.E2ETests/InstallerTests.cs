using System.Diagnostics;
using Microsoft.Win32;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class InstallerTests : IDisposable
{
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{E4A72F8B-3D19-4C5A-9F61-B8D2E5C7A043}_is1";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "ScreenNap";
    private const string ShortcutName = "ScreenNap.lnk";
    private static readonly TimeSpan s_installTimeout = TimeSpan.FromMinutes(2);

    private readonly string _scratch;
    private readonly string _installDirectory;

    public InstallerTests()
    {
        TestAssets.RequireNoRunningScreenNap();
        _scratch = TestAssets.CreateScratchDirectory();
        _installDirectory = Path.Combine(_scratch, "ScreenNap");
    }

    private static string StartMenuShortcut => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ShortcutName);

    private static string DesktopShortcut => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), ShortcutName);

    private string InstalledExe => Path.Combine(_installDirectory, "ScreenNap.exe");

    private string Uninstaller => Path.Combine(_installDirectory, "unins000.exe");

    [Fact]
    [Trait("Requirement", "DIST-1")]
    [Trait("Requirement", "NFR-4")]
    internal void PublishedExecutable_OnItsOwn_IsTheWholeDistributionAndRuns()
    {
        RequireFile(TestAssets.PublishedExe, "Run build/Build.ps1 before the distribution end-to-end tests.");

        string[] published = Directory.GetFiles(Path.GetDirectoryName(TestAssets.PublishedExe)!, "*", SearchOption.AllDirectories);
        Assert.Equal([TestAssets.PublishedExe], published.Select(Path.GetFullPath));

        using ScreenNapApp app = ScreenNapApp.Start(TestAssets.PublishedExe);
        app.Exit();
    }

    [Fact]
    [Trait("Requirement", "DIST-2")]
    [Trait("Requirement", "DIST-3")]
    [Trait("Requirement", "DIST-6")]
    internal void SilentInstall_EveryTask_InstallsPerUserAndUninstallRemovesEverything()
    {
        Install("desktopicon,startmenu,startup");

        Assert.True(File.Exists(InstalledExe));
        Assert.True(File.Exists(StartMenuShortcut));
        Assert.True(File.Exists(DesktopShortcut));
        Assert.Equal($"\"{InstalledExe}\"", RunEntry());
        using (RegistryKey? perUser = Registry.CurrentUser.OpenSubKey(UninstallKey))
            Assert.NotNull(perUser);

        Uninstall();

        Assert.False(File.Exists(InstalledExe));
        Assert.False(File.Exists(StartMenuShortcut));
        Assert.False(File.Exists(DesktopShortcut));
        Assert.Null(RunEntry());
    }

    [Fact]
    [Trait("Requirement", "DIST-7")]
    internal void SilentUninstall_ScreenNapRunning_ClosesItAndContinues()
    {
        Install("startmenu");
        using ScreenNapApp app = ScreenNapApp.Start(InstalledExe);

        Uninstall();

        Assert.True(app.Process.WaitForExit(TimeSpan.FromSeconds(30)), "The uninstaller did not close ScreenNap");
        Assert.False(File.Exists(InstalledExe));
    }

    [Fact]
    [Trait("Requirement", "DIST-4")]
    [Trait("Requirement", "DIST-5")]
    internal void InstallerScript_Wizard_OffersALaunchAndShowsOnlyEnglish()
    {
        string[] script = File.ReadAllLines(TestAssets.InstallerScript);

        string[] languages = Section(script, "Languages");
        Assert.Single(languages);
        Assert.StartsWith("Name: \"english\"", languages[0], StringComparison.Ordinal);
        Assert.Contains(Section(script, "Run"), line => line.Contains("postinstall", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (File.Exists(Uninstaller))
            Uninstall();
        TestAssets.DeleteScratchDirectory(_scratch);
    }

    private static void RequireNotInstalled()
    {
        using RegistryKey? installed = Registry.CurrentUser.OpenSubKey(UninstallKey);
        if (installed is not null || RunEntry() is not null || File.Exists(StartMenuShortcut) || File.Exists(DesktopShortcut))
            throw new InvalidOperationException("ScreenNap is installed for this user. Run the installer end-to-end tests where ScreenNap is not installed (docs/adr/0003-end-to-end-tests-drive-the-built-application.md).");
    }

    private static void RequireFile(string path, string remedy)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"{path} does not exist. {remedy}");
    }

    private static string? RunEntry()
    {
        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunKey);
        return run?.GetValue(RunValue) as string;
    }

    private static string[] Section(string[] script, string name)
        => script
            .SkipWhile(line => !line.Trim().Equals($"[{name}]", StringComparison.Ordinal))
            .Skip(1)
            .TakeWhile(line => !line.TrimStart().StartsWith('['))
            .Where(line => line.Trim().Length > 0 && !line.TrimStart().StartsWith(';'))
            .ToArray();

    private void Install(string tasks)
    {
        RequireNotInstalled();
        RequireFile(TestAssets.Installer, "Run build/Build.ps1 and build/Installer.ps1 before the installer end-to-end tests.");
        Run(TestAssets.Installer, $"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=\"{_installDirectory}\" /TASKS=\"{tasks}\"");
        Wait.Until(() => File.Exists(InstalledExe), s_installTimeout, "the installer places ScreenNap.exe");
    }

    private void Uninstall()
    {
        Run(Uninstaller, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART");
        Wait.Until(
            () => !File.Exists(Uninstaller) && RunEntry() is null && !File.Exists(StartMenuShortcut) && !File.Exists(DesktopShortcut),
            s_installTimeout,
            "the uninstaller removes the installation");
    }

    private static void Run(string executable, string arguments)
    {
        using Process process = Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = false })
            ?? throw new InvalidOperationException($"{executable} did not start");
        if (!process.WaitForExit(s_installTimeout))
            throw new TimeoutException($"{Path.GetFileName(executable)} did not finish");
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(executable)} exited with code {process.ExitCode}");
    }
}
