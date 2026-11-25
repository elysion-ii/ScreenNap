using ScreenNap.Identify;
using ScreenNap.Logging;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Identify;

public sealed class IdentifyOverlayTests
{
    private const string OverlayClass = "ScreenNap_Identify";
    private static readonly RECT s_monitor = new() { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };

    [Theory]
    [Trait("Requirement", "IDN-2")]
    [InlineData(96u, 860, 465, 200, 150)]
    [InlineData(144u, 810, 427, 300, 225)]
    internal void Show_MonitorDpiKnown_SizesTheOverlayByTheScaleFactor(uint dpi, int x, int y, int width, int height)
    {
        var api = new FakeWindowApi { Dpi = dpi };

        Assert.Equal(new OverlayShowResult(true, true, true), new IdentifyOverlay(api, new FakeOverlayPaintApi()).Show(1, s_monitor, startsClosingTimer: true));

        Assert.Contains(api.Positions, position => (position.X, position.Y, position.Width, position.Height) == (x, y, width, height));
    }

    [Fact]
    [Trait("Requirement", "IDN-2")]
    internal void Show_DpiCannotBeRead_UsesTheHundredPercentSizeAndLogsIt()
    {
        var api = new FakeWindowApi { Dpi = 0 };
        using var log = new LogCapture();

        Assert.Equal(new OverlayShowResult(true, true, true), new IdentifyOverlay(api, new FakeOverlayPaintApi()).Show(2, s_monitor, startsClosingTimer: true));

        Assert.Contains(api.Positions, position => (position.X, position.Y, position.Width, position.Height) == (860, 465, 200, 150));
        LogAssert.Logged(log, "WARN", "GetDpiForWindow failed for the identify overlay on monitor 2; showing it at 100% size");
    }

    [Fact]
    [Trait("Requirement", "IDN-2")]
    internal void Show_Monitor_DrawsItsNumberAsTheOverlayAppears()
    {
        var paint = new FakeOverlayPaintApi();

        OverlayShowResult result = new IdentifyOverlay(new FakeWindowApi(), paint).Show(3, s_monitor, startsClosingTimer: true);

        Assert.True(result.NumberDrawn);
        Assert.Equal(["3"], paint.DrawnTexts);
    }

    [Theory]
    [Trait("Requirement", "ERR-8")]
    [InlineData(nameof(IOverlayPaintApi.BeginPaint), "BeginPaint failed for the identify overlay on monitor 2")]
    [InlineData(nameof(IOverlayPaintApi.CreateFont), "CreateFontW failed for the identify overlay on monitor 2")]
    [InlineData(nameof(IOverlayPaintApi.SelectObject), "SelectObject failed for the identify overlay font on monitor 2")]
    [InlineData(nameof(IOverlayPaintApi.SetTextColor), "SetTextColor failed for the identify overlay on monitor 2")]
    [InlineData(nameof(IOverlayPaintApi.DrawText), "DrawTextW failed for the identify overlay on monitor 2")]
    internal void Show_NumberCannotBeDrawn_ReportsItLogsItAndKeepsTheOverlay(string failingCall, string logged)
    {
        var paint = new FakeOverlayPaintApi();
        paint.FailingCalls.Add(failingCall);
        var overlays = new IdentifyOverlay(new FakeWindowApi(), paint);
        using var log = new LogCapture();

        OverlayShowResult result = overlays.Show(2, s_monitor, startsClosingTimer: true);

        Assert.Equal(new OverlayShowResult(true, true, false), result);
        Assert.True(overlays.AnyShown);
        LogAssert.Logged(log, "WARN", logged);
    }

    [Fact]
    [Trait("Requirement", "ERR-8")]
    internal void Show_WindowCannotBePaintedNow_ReportsTheNumberAsNotDrawn()
    {
        var api = new FakeWindowApi { UpdateWindowFails = true };
        using var log = new LogCapture();

        OverlayShowResult result = new IdentifyOverlay(api, new FakeOverlayPaintApi()).Show(1, s_monitor, startsClosingTimer: true);

        Assert.False(result.NumberDrawn);
        LogAssert.Logged(log, "WARN", "UpdateWindow failed for the identify overlay on monitor 1");
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void Show_TransparentBackgroundCannotBeSet_StillCountsTheNumberAsDrawn()
    {
        var paint = new FakeOverlayPaintApi();
        paint.FailingCalls.Add(nameof(IOverlayPaintApi.SetTransparentBackground));
        using var log = new LogCapture();

        OverlayShowResult result = new IdentifyOverlay(new FakeWindowApi(), paint).Show(1, s_monitor, startsClosingTimer: true);

        Assert.True(result.NumberDrawn);
        LogAssert.Logged(log, "WARN", "SetBkMode failed for the identify overlay on monitor 1");
    }

    [Fact]
    [Trait("Requirement", "ERR-8")]
    [Trait("Requirement", "ERR-10")]
    internal void LaterRedraw_NumberCannotBeDrawn_IsOnlyLogged()
    {
        var api = new FakeWindowApi();
        var paint = new FakeOverlayPaintApi();
        var overlays = new IdentifyOverlay(api, paint);
        Assert.True(overlays.Show(1, s_monitor, startsClosingTimer: true).NumberDrawn);
        IntPtr overlay = api.Windows.Single().Key;
        paint.FailingCalls.Add(nameof(IOverlayPaintApi.DrawText));
        using var log = new LogCapture();

        api.Send(overlay, WindowStyles.WM_PAINT);

        Assert.True(overlays.AnyShown);
        LogAssert.Logged(log, "WARN", "DrawTextW failed for the identify overlay on monitor 1");
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void Show_ClosingTimerCannotStart_LogsItAndShowsTheOverlayWithoutTheTimer()
    {
        var api = new FakeWindowApi();
        api.TimersFailingToStart.Add(IdentifyOverlay.DismissTimerId);
        var overlays = new IdentifyOverlay(api, new FakeOverlayPaintApi());
        using var log = new LogCapture();

        Assert.Equal(new OverlayShowResult(true, false, true), overlays.Show(1, s_monitor, startsClosingTimer: true));

        Assert.True(overlays.AnyShown);
        LogAssert.Logged(log, "WARN", "SetTimer failed for identify overlay dismissal on monitor 1");
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void Show_TopmostTimerCannotStart_LogsItAndShowsTheOverlay()
    {
        var api = new FakeWindowApi();
        api.TimersFailingToStart.Add(IdentifyOverlay.TopmostTimerId);
        var overlays = new IdentifyOverlay(api, new FakeOverlayPaintApi());
        using var log = new LogCapture();

        Assert.Equal(new OverlayShowResult(true, true, true), overlays.Show(1, s_monitor, startsClosingTimer: true));

        Assert.True(overlays.AnyShown);
        LogAssert.Logged(log, "WARN", "SetTimer failed for identify overlay topmost maintenance on monitor 1");
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void TopmostTimer_PositionCannotBeRestored_LogsItAndKeepsTheOverlay()
    {
        var api = new FakeWindowApi();
        var overlays = new IdentifyOverlay(api, new FakeOverlayPaintApi());
        overlays.Show(1, s_monitor, startsClosingTimer: true);
        IntPtr overlay = api.Windows.Single().Key;
        api.SetWindowPosFails = true;
        using var log = new LogCapture();

        api.Send(overlay, WindowStyles.WM_TIMER, IdentifyOverlay.TopmostTimerId);

        Assert.True(overlays.AnyShown);
        LogAssert.Logged(log, "WARN", "SetWindowPos failed for identify overlay");
    }

    [Fact]
    [Trait("Requirement", "IDN-3")]
    internal void ClosingTimer_Elapses_ClosesEveryOverlay()
    {
        var api = new FakeWindowApi();
        var overlays = new IdentifyOverlay(api, new FakeOverlayPaintApi());
        overlays.Show(1, s_monitor, startsClosingTimer: true);
        overlays.Show(2, s_monitor, startsClosingTimer: false);
        IntPtr first = api.Windows.First().Key;

        api.Send(first, WindowStyles.WM_TIMER, IdentifyOverlay.DismissTimerId);

        Assert.False(overlays.AnyShown);
        Assert.Empty(api.LiveWindows(OverlayClass));
        Assert.Empty(api.Timers);
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void DismissAll_OverlayCannotBeClosed_LogsItAndKeepsTheOverlay()
    {
        var api = new FakeWindowApi();
        var overlays = new IdentifyOverlay(api, new FakeOverlayPaintApi());
        overlays.Show(1, s_monitor, startsClosingTimer: true);
        api.ClassesFailingDestruction.Add(OverlayClass);
        using var log = new LogCapture();

        overlays.DismissAll();

        Assert.True(overlays.AnyShown);
        LogAssert.Logged(log, "WARN", "DestroyWindow failed for identify overlay");
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void DismissAll_TimersCannotBeStopped_LogsItAndClosesTheOverlay()
    {
        var api = new FakeWindowApi();
        var overlays = new IdentifyOverlay(api, new FakeOverlayPaintApi());
        overlays.Show(1, s_monitor, startsClosingTimer: true);
        api.TimersFailingToStop.Add(IdentifyOverlay.DismissTimerId);
        api.TimersFailingToStop.Add(IdentifyOverlay.TopmostTimerId);
        using var log = new LogCapture();

        overlays.DismissAll();

        Assert.False(overlays.AnyShown);
        LogAssert.Logged(log, "WARN", "KillTimer failed for identify overlay dismissal");
        LogAssert.Logged(log, "WARN", "KillTimer failed for identify overlay topmost maintenance");
    }
}
