using System.Globalization;
using ScreenNap.Hotkeys;
using ScreenNap.Identify;
using ScreenNap.Lifetime;
using ScreenNap.Logging;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tray;

namespace ScreenNap;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Logger.Use(LogFile.Open(
            Environment.GetEnvironmentVariable(LogDirectoryChoice.DirectoryVariable),
            LogFile.DefaultDirectory,
            DateTimeOffset.Now));

        // The Windows UI language selects the localized text and stays fixed for the process
        CultureInfo culture = CultureInfo.CurrentUICulture;
        var monitors = new MonitorEnumerator(new Win32MonitorApi());

        new AppLifetime(
            new Win32LifetimeApi(),
            new Win32WindowApi(),
            new Win32TrayIconApi(),
            new Win32PopupMenuApi(),
            new Win32HotkeyApi(),
            new Win32OverlayPaintApi(),
            monitors.EnumerateMonitors,
            culture).Run();
    }
}
