using System.Diagnostics;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class IdentifyTests
{
    private const int OverlayWidth = 200;
    private const int OverlayHeight = 150;
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    public IdentifyTests() => TestAssets.RequireNoRunningScreenNap();

    [Fact]
    [Trait("Requirement", "HK-3")]
    [Trait("Requirement", "IDN-1")]
    [Trait("Requirement", "IDN-2")]
    [Trait("Requirement", "IDN-3")]
    [Trait("Requirement", "IDN-5")]
    [Trait("Requirement", "ERR-8")]
    [Trait("Requirement", "ERR-10")]
    [Trait("Requirement", "NFR-3")]
    internal void IdentifyHotkey_NoOverlay_ShowsACenteredOverlayOnEachMonitorThenClosesThem()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        IReadOnlyList<RECT> monitors = app.Monitors();
        IntPtr foregroundBefore = Win32.GetForegroundWindow();

        Keyboard.PressHotkey(0);
        Wait.Until(() => app.Overlays().Count == monitors.Count, s_timeout, "an overlay shows on every monitor");
        var shown = Stopwatch.StartNew();

        IReadOnlyList<RECT> overlays = app.Overlays().Select(window => app.Measure(() => Desktop.Bounds(window))).ToList();
        foreach (RECT monitor in monitors)
        {
            int expectedLeft = monitor.Left + (monitor.Width - OverlayWidth) / 2;
            int expectedTop = monitor.Top + (monitor.Height - OverlayHeight) / 2;
            Assert.Contains(overlays, overlay =>
                overlay.Left == expectedLeft && overlay.Top == expectedTop
                && overlay.Width == OverlayWidth && overlay.Height == OverlayHeight);
        }
        Assert.All(app.Overlays(), window =>
        {
            long style = Desktop.ExtendedStyle(window);
            Assert.NotEqual(0, style & Win32.WS_EX_TOPMOST);
            Assert.NotEqual(0, style & Win32.WS_EX_TOOLWINDOW);
            Assert.NotEqual(0, style & Win32.WS_EX_NOACTIVATE);
        });
        Assert.Equal(foregroundBefore, Win32.GetForegroundWindow());

        Wait.Until(() => app.Overlays().Count == 0, s_timeout, "the overlays close by themselves");
        Assert.True(shown.Elapsed >= TimeSpan.FromSeconds(2), $"The overlays closed after {shown.Elapsed.TotalSeconds:0.0}s, well before about three seconds");
    }

    [Fact]
    [Trait("Requirement", "IDN-4")]
    internal void IdentifyHotkey_WhileOverlaysShow_ClosesThemImmediately()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        Keyboard.PressHotkey(0);
        Wait.Until(() => app.Overlays().Count > 0, s_timeout, "the overlays show");
        var pressed = Stopwatch.StartNew();

        Keyboard.PressHotkey(0);

        Wait.Until(() => app.Overlays().Count == 0, s_timeout, "the overlays close");
        Assert.True(pressed.Elapsed < TimeSpan.FromSeconds(2), $"The overlays closed only after {pressed.Elapsed.TotalSeconds:0.0}s");
    }
}
