using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace ScreenNap.E2ETests.Infrastructure;

// Drives the interactive Inno Setup wizard through UI Automation, which invokes a control
// whichever window is active. The controls of every page exist at once; only the current
// page's controls are on screen.
internal sealed class SetupWizard : IDisposable
{
    // The installer and the .tmp process it extracts and runs the wizard in share this prefix
    private const string SetupProcessPrefix = "ScreenNap-Setup-";
    private const string WizardTitlePrefix = "Setup - ";
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    // In the test's build output, which no commit takes
    private static readonly string s_failureFolder = Path.Combine(AppContext.BaseDirectory, "SetupWizardFailures");

    private readonly Process _setup;
    private readonly UIA3Automation _automation = new();
    private readonly Window? _wizard;

    private SetupWizard(Process setup)
    {
        _setup = setup;
        _wizard = Until(
            () => SetupWindows().FirstOrDefault(window => window.Title.StartsWith(WizardTitlePrefix, StringComparison.Ordinal)),
            "Setup shows its wizard");
    }

    private Window Wizard => _wizard ?? throw new InvalidOperationException("The wizard has not been found");

    internal static SetupWizard Start(string installer)
    {
        Process setup = Process.Start(new ProcessStartInfo(installer) { UseShellExecute = false })
            ?? throw new InvalidOperationException($"{installer} did not start");
        try
        {
            return new SetupWizard(setup);
        }
        catch
        {
            StopSetupProcesses();
            setup.Dispose();
            throw;
        }
    }

    internal void WaitForButton(string name) => Button(name);

    // Presses the button and waits until the page changes, so the next step acts on the next page
    internal void Press(string name)
    {
        string before = PageText();
        Button(name).Invoke();
        Until(() => PageText() != before ? this : null, $"the wizard leaves the page after \"{name}\"");
    }

    // The last button closes the wizard instead of changing its page
    internal void PressFinish(string name) => Button(name).Invoke();

    internal void Select(string name)
    {
        RadioButton radio = Radio(name);
        radio.IsChecked = true;
        Until(() => radio.IsChecked ? radio : null, $"the option \"{name}\" is selected");
    }

    internal bool IsSelected(string name) => Radio(name).IsChecked;

    internal string MemoText()
        => Until(() => OnScreen(ControlType.Edit).FirstOrDefault()?.AsTextBox(), "the wizard's memo").Text;

    // Inno Setup's check list reports its items' state through MSAA but takes neither UI
    // Automation's toggle nor MSAA's default action, so the item is clicked as a user does
    internal void Uncheck(string name)
    {
        AutomationElement item = Until(() => OnScreen(ControlType.CheckBox).FirstOrDefault(c => Matches(c, name)), $"the check box \"{name}\"");
        if (IsMsaaChecked(item))
        {
            Wizard.SetForeground();
            item.Click(moveMouse: true);
        }
        Until(() => IsMsaaChecked(item) ? null : item, $"the check box \"{name}\" is cleared");
    }

    private static bool IsMsaaChecked(AutomationElement item)
        => (item.Patterns.LegacyIAccessible.Pattern.State.Value & AccessibilityState.STATE_SYSTEM_CHECKED) != 0;

    internal void WaitForExit()
    {
        if (!_setup.WaitForExit(s_timeout))
            throw new TimeoutException("Setup did not finish");
    }

    public void Dispose()
    {
        StopSetupProcesses();
        _setup.Dispose();
        _automation.Dispose();
    }

    private Button Button(string name)
        => Until(
            () => OnScreen(ControlType.Button).FirstOrDefault(button => button.IsEnabled && Matches(button, name))?.AsButton(),
            $"the wizard enables \"{name}\"");

    private RadioButton Radio(string name)
        => Until(() => OnScreen(ControlType.RadioButton).FirstOrDefault(radio => Matches(radio, name))?.AsRadioButton(), $"the option \"{name}\"");

    private List<AutomationElement> OnScreen(ControlType type)
    {
        try
        {
            return [.. Wizard.FindAllDescendants(cf => cf.ByControlType(type)).Where(element => !element.Properties.IsOffscreen.ValueOrDefault)];
        }
        catch (COMException)
        {
            // The wizard changed its page while it was being read
            return [];
        }
    }

    private string PageText() => string.Join(" | ", OnScreen(ControlType.Text).Select(text => text.Name));

    private List<Window> SetupWindows()
    {
        var windows = new List<Window>();
        foreach (int processId in SetupProcessIds())
        {
            try
            {
                windows.AddRange(_automation.GetDesktop()
                    .FindAllChildren(cf => cf.ByProcessId(processId).And(cf.ByControlType(ControlType.Window)))
                    .Select(window => window.AsWindow()));
            }
            catch (COMException)
            {
                // A window that closed while it was being read
            }
        }
        return windows;
    }

    private static List<int> SetupProcessIds()
    {
        Process[] processes = Process.GetProcesses();
        try
        {
            return [.. processes.Where(p => p.ProcessName.StartsWith(SetupProcessPrefix, StringComparison.OrdinalIgnoreCase)).Select(p => p.Id)];
        }
        finally
        {
            foreach (Process process in processes)
                process.Dispose();
        }
    }

    // Stops the wizard a failed test leaves open
    private static void StopSetupProcesses()
    {
        foreach (int processId in SetupProcessIds())
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                process.Kill(entireProcessTree: true);
                _ = process.WaitForExit(s_timeout);
            }
            catch (ArgumentException)
            {
                // A process that already ended
            }
        }
    }

    // A control's name as UI Automation reports it may keep the caption's access-key ampersand
    private static bool Matches(AutomationElement element, string name)
        => element.Name.Replace("&", string.Empty, StringComparison.Ordinal) == name;

    private T Until<T>(Func<T?> probe, string description) where T : class
    {
        T? found = null;
        try
        {
            Wait.Until(() => (found = probe()) is not null, s_timeout, description);
        }
        catch (TimeoutException timeout)
        {
            throw new TimeoutException($"{timeout.Message}. The screen and the wizard's controls at that moment are in {RecordScreen()}.*", timeout);
        }
        return found!;
    }

    // Saves the whole screen, so a message box over the wizard shows too, and every control of
    // the wizard with its state
    private string RecordScreen()
    {
        Directory.CreateDirectory(s_failureFolder);
        string stem = Path.Combine(s_failureFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));
        using (CaptureImage screen = Capture.Screen())
            screen.ToFile(stem + ".png");

        var lines = new List<string>();
        try
        {
            if (_wizard is not null)
            {
                lines.AddRange(_wizard.FindAllDescendants().Select(element =>
                    $"{element.Properties.ControlType.ValueOrDefault} \"{element.Properties.Name.ValueOrDefault}\" enabled={element.Properties.IsEnabled.ValueOrDefault} offscreen={element.Properties.IsOffscreen.ValueOrDefault}"));
            }
            lines.AddRange(SetupWindows().Select(window => $"window \"{window.Title}\""));
        }
        catch (COMException exception)
        {
            lines.Add($"Reading the controls failed: {exception.Message}");
        }
        File.WriteAllLines(stem + ".txt", lines);
        return stem;
    }
}
