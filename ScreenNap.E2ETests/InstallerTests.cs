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
    private const string SilentUninstallArguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART";

    // The English wizard's control captions as it shows them, without their access-key ampersands
    private const string AcceptLicense = "I accept the agreement";
    private const string NextButton = "Next";
    private const string InstallButton = "Install";
    private const string FinishButton = "Finish";
    private const string CloseApplications = "Automatically close the applications";
    private const string LaunchOption = "Launch ScreenNap";
    private static readonly TimeSpan s_installTimeout = TimeSpan.FromMinutes(2);
    private static readonly DateTime s_untouchedTime = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _scratch;
    private readonly string _installDirectory;
    private readonly SetAsideDirectory _userAppData;

    // Every uninstall deletes ScreenNap's logs in the user's default log directory (DIST-6),
    // so each test sets the user's folder aside and puts it back afterwards
    public InstallerTests()
    {
        TestAssets.RequireNoRunningScreenNap();
        _scratch = TestAssets.CreateScratchDirectory();
        _installDirectory = Path.Combine(_scratch, "ScreenNap");
        _userAppData = new SetAsideDirectory(ScreenNapAppData, Path.Combine(_scratch, "UserAppData"));
    }

    private static string StartMenuShortcut => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ShortcutName);

    private static string DesktopShortcut => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), ShortcutName);

    // ScreenNap's folder under the user's local application data, which holds the default log directory
    private static string ScreenNapAppData => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScreenNap");

    private static string DefaultLogDirectory => Path.Combine(ScreenNapAppData, "Logs");

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

    [Theory]
    [Trait("Requirement", "DIST-6")]
    [InlineData(false)]
    [InlineData(true)]
    internal void Uninstall_LogsInTheDefaultLogDirectory_RemovesScreenNapsLogsAndTheEmptyDirectories(bool otherFilePresent)
    {
        Install("startmenu");
        Directory.CreateDirectory(DefaultLogDirectory);
        File.WriteAllText(Path.Combine(DefaultLogDirectory, "ScreenNap_20000101.log"), "old");
        File.WriteAllText(Path.Combine(DefaultLogDirectory, "ScreenNap_20000102.log"), "newer");
        string otherFile = Path.Combine(DefaultLogDirectory, "notes.txt");
        if (otherFilePresent)
            File.WriteAllText(otherFile, "not ScreenNap's");

        Uninstall();

        if (otherFilePresent)
            Assert.Equal([otherFile], Directory.GetFiles(DefaultLogDirectory));
        else
            Assert.False(Directory.Exists(ScreenNapAppData));
    }

    [Fact]
    [Trait("Requirement", "DIST-7")]
    [Trait("Requirement", "LIFE-5")]
    internal void SilentUninstall_ScreenNapRunning_ClosesItAndContinues()
    {
        Install("startmenu");
        using ScreenNapApp app = ScreenNapApp.Start(InstalledExe);

        Uninstall();

        Assert.True(app.Process.WaitForExit(TimeSpan.FromSeconds(30)), "The uninstaller did not close ScreenNap");
        Assert.False(File.Exists(InstalledExe));
        List<string> shutdown = [.. app.LogLines().SkipWhile(line => !line.EndsWith("Application exiting", StringComparison.Ordinal))];
        Assert.NotEmpty(shutdown);
        Assert.DoesNotContain(shutdown, line => line.Contains("[WARN]", StringComparison.Ordinal) || line.Contains("[ERROR]", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Requirement", "DIST-7")]
    internal void SilentUninstall_ScreenNapDoesNotClose_RemovesNothingAndFails()
    {
        Install("startmenu");
        using ScreenNapApp app = ScreenNapApp.Start(InstalledExe);

        int exitCode;
        using (app.SuspendUiThread())
            exitCode = RunForExitCode(Uninstaller, SilentUninstallArguments);

        Assert.NotEqual(0, exitCode);
        Assert.False(app.Process.HasExited);
        Assert.True(File.Exists(InstalledExe));
        Assert.True(File.Exists(Uninstaller));
        Assert.True(File.Exists(StartMenuShortcut));
    }

    [Fact]
    [Trait("Requirement", "DIST-8")]
    internal void InteractiveUpdate_ScreenNapRunning_OffersToCloseItAndDoesNotStartItAgain()
    {
        Install("startmenu");
        using ScreenNapApp app = ScreenNapApp.Start(InstalledExe);
        using SetupWizard wizard = SetupWizard.Start(TestAssets.Installer);

        // License, tasks, ready, then the page that lists the applications to close
        wizard.Select(AcceptLicense);
        wizard.Press(NextButton);
        wizard.Press(NextButton);
        wizard.Press(InstallButton);

        Assert.True(wizard.IsSelected(CloseApplications), "The wizard does not offer to close ScreenNap by default");
        Assert.Contains("ScreenNap", wizard.MemoText(), StringComparison.OrdinalIgnoreCase);
        Assert.False(app.Process.HasExited, "ScreenNap closed before the user chose to close it");

        wizard.Press(NextButton);
        wizard.WaitForButton(FinishButton);
        Assert.True(app.Process.WaitForExit(TimeSpan.FromSeconds(30)), "The update did not close ScreenNap");

        // Leaves the launch offered after installation (DIST-4) unchosen
        wizard.Uncheck(LaunchOption);
        wizard.PressFinish(FinishButton);
        wizard.WaitForExit();

        Process[] running = Process.GetProcessesByName("ScreenNap");
        foreach (Process process in running)
            process.Dispose();
        Assert.Empty(running);
        Assert.True(File.Exists(InstalledExe));
    }

    [Fact]
    [Trait("Requirement", "DIST-9")]
    internal void SilentUpdate_ScreenNapDoesNotClose_ReplacesNothingAndFails()
    {
        Install("startmenu");
        // A last-write time no installer writes shows whether the update replaced the file
        File.SetLastWriteTimeUtc(InstalledExe, s_untouchedTime);
        using ScreenNapApp app = ScreenNapApp.Start(InstalledExe);

        int exitCode;
        using (app.SuspendUiThread())
            exitCode = RunForExitCode(TestAssets.Installer, SilentInstallArguments("startmenu"));

        Assert.NotEqual(0, exitCode);
        Assert.False(app.Process.HasExited);
        Assert.Equal(s_untouchedTime, File.GetLastWriteTimeUtc(InstalledExe));
    }

    [Fact]
    [Trait("Requirement", "DIST-8")]
    internal void SilentUpdate_ScreenNapRunning_ClosesItWithoutStartingItAgain()
    {
        Install("startmenu");
        using ScreenNapApp app = ScreenNapApp.Start(InstalledExe);

        RunInstaller("startmenu");

        Assert.True(app.Process.WaitForExit(TimeSpan.FromSeconds(30)), "The update did not close ScreenNap");
        Process[] running = Process.GetProcessesByName("ScreenNap");
        foreach (Process process in running)
            process.Dispose();
        Assert.Empty(running);
        Assert.True(File.Exists(InstalledExe));
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
        try
        {
            if (File.Exists(Uninstaller))
                Uninstall();
        }
        finally
        {
            _userAppData.Dispose();
        }
        TestAssets.DeleteScratchDirectory(_scratch);
    }

    // The user's own folder moves aside for the test and returns afterwards, so the user's logs
    // survive the uninstaller deleting logs there
    private sealed class SetAsideDirectory : IDisposable
    {
        private readonly string _path;
        private readonly string _keptAt;

        internal SetAsideDirectory(string path, string keptAt)
        {
            _path = path;
            _keptAt = keptAt;
            if (Directory.Exists(path))
                Directory.Move(path, keptAt);
        }

        public void Dispose()
        {
            if (Directory.Exists(_path))
                Directory.Delete(_path, true);
            if (Directory.Exists(_keptAt))
                Directory.Move(_keptAt, _path);
        }
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
        RunInstaller(tasks);
    }

    private string SilentInstallArguments(string tasks)
        => $"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=\"{_installDirectory}\" /TASKS=\"{tasks}\"";

    private void RunInstaller(string tasks)
    {
        RequireFile(TestAssets.Installer, "Run build/Build.ps1 and build/Installer.ps1 before the installer end-to-end tests.");
        Run(TestAssets.Installer, SilentInstallArguments(tasks));
        Wait.Until(() => File.Exists(InstalledExe), s_installTimeout, "the installer places ScreenNap.exe");
    }

    private void Uninstall()
    {
        Run(Uninstaller, SilentUninstallArguments);
        Wait.Until(
            () => !File.Exists(Uninstaller) && RunEntry() is null && !File.Exists(StartMenuShortcut) && !File.Exists(DesktopShortcut),
            s_installTimeout,
            "the uninstaller removes the installation");
    }

    private static void Run(string executable, string arguments)
    {
        int exitCode = RunForExitCode(executable, arguments);
        if (exitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(executable)} exited with code {exitCode}");
    }

    private static int RunForExitCode(string executable, string arguments)
    {
        using Process process = Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = false })
            ?? throw new InvalidOperationException($"{executable} did not start");
        if (!process.WaitForExit(s_installTimeout))
            throw new TimeoutException($"{Path.GetFileName(executable)} did not finish");
        return process.ExitCode;
    }
}
