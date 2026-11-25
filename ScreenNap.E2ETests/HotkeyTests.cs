using System.Runtime.InteropServices;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class HotkeyTests
{
    private const int MaximumMonitorDigit = 9;

    public HotkeyTests() => TestAssets.RequireNoRunningScreenNap();

    [Fact]
    [Trait("Requirement", "HK-2")]
    [Trait("Requirement", "MON-1")]
    [Trait("Requirement", "MON-2")]
    internal void NumberHotkey_ExistingMonitor_TogglesItsBlackout()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        RECT first = app.Monitors()[0];

        Keyboard.PressHotkey(1);
        app.WaitForBlackoutCovering(first);
        Keyboard.PressHotkey(1);

        app.WaitForBlackoutCount(0);
    }

    [Fact]
    [Trait("Requirement", "HK-2")]
    internal void NumberHotkey_NoSuchMonitor_DoesNothing()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        int missingDigit = app.Monitors().Count + 1;
        Assert.True(missingDigit <= MaximumMonitorDigit, "Every monitor hotkey has a monitor; no digit is left to press");
        int enumerationsBefore = app.LogLines().Count(line => line.Contains("Monitors enumerated", StringComparison.Ordinal));

        Keyboard.PressHotkey(missingDigit);

        Wait.Until(
            () => app.LogLines().Count(line => line.Contains("Monitors enumerated", StringComparison.Ordinal)) > enumerationsBefore,
            TimeSpan.FromSeconds(10),
            "ScreenNap handles the hotkey");
        Assert.Empty(app.BlackoutWindows());
    }

    [Fact]
    [Trait("Requirement", "HK-4")]
    [Trait("Requirement", "ERR-7")]
    [Trait("Requirement", "ERR-9")]
    internal void Start_OneHotkeyHeldElsewhere_RegistersAndServesTheOthers()
    {
        const int HeldId = 0xB001;
        if (!Win32.RegisterHotKey(IntPtr.Zero, HeldId, Keyboard.HotkeyModifiers, Win32.VK_0 + MaximumMonitorDigit))
            throw new InvalidOperationException($"The test could not hold Ctrl+Shift+Alt+9 (Win32 error: {Marshal.GetLastWin32Error()})");
        bool released;
        try
        {
            using ScreenNapApp app = ScreenNapApp.Start();

            app.WaitForLog("Failed to register hotkey Ctrl+Shift+Alt+9");
            Keyboard.PressHotkey(1);
            app.WaitForBlackoutCovering(app.Monitors()[0]);
        }
        finally
        {
            released = Win32.UnregisterHotKey(IntPtr.Zero, HeldId);
        }

        Assert.True(released, "The test could not release Ctrl+Shift+Alt+9");
    }

    [Fact]
    [Trait("Requirement", "HK-5")]
    internal void Hotkeys_SecondDigitPressedWithTheFirst_OnlyTheFirstActs()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        RECT first = app.Monitors()[0];

        // 0 is pressed while 1 is still held, as a user pressing both together does
        using (Keyboard.HoldHotkey(1))
        {
            app.WaitForBlackoutCovering(first);
            Keyboard.TapDigit(0);
            app.WaitForLog("ignored while another digit is held");
        }

        Assert.Empty(app.Overlays());
    }

    [Fact]
    [Trait("Requirement", "RUN-2")]
    internal void Start_HotkeyReleasedAfterStartup_StaysUnavailable()
    {
        const int HeldId = 0xB002;
        if (!Win32.RegisterHotKey(IntPtr.Zero, HeldId, Keyboard.HotkeyModifiers, Win32.VK_0 + MaximumMonitorDigit))
            throw new InvalidOperationException($"The test could not hold Ctrl+Shift+Alt+9 (Win32 error: {Marshal.GetLastWin32Error()})");
        bool released = false;
        try
        {
            using ScreenNapApp app = ScreenNapApp.Start();
            app.WaitForLog("Failed to register hotkey Ctrl+Shift+Alt+9");
            released = Win32.UnregisterHotKey(IntPtr.Zero, HeldId);
            Assert.True(released, "The test could not release Ctrl+Shift+Alt+9");

            // A settled display change shows ScreenNap has handled events since the release
            app.Post(Win32.WM_DISPLAYCHANGE, 0, 0);
            app.WaitForLog("Display configuration changed (debounced)");

            Assert.False(Keyboard.HotkeyIsHeldElsewhere(MaximumMonitorDigit), "ScreenNap registered Ctrl+Shift+Alt+9 after startup");
        }
        finally
        {
            if (!released)
                _ = Win32.UnregisterHotKey(IntPtr.Zero, HeldId);
        }
    }
}
