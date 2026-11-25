using System.Globalization;
using ScreenNap.Identify;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Identify;

public sealed class MonitorIdentifierTests
{
    private static readonly CultureInfo s_english = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    [Trait("Requirement", "IDN-1")]
    [Trait("Requirement", "IDN-3")]
    [Trait("Requirement", "MON-2")]
    internal void Toggle_NoOverlayShown_ShowsOneNumberedOverlayPerMonitor()
    {
        var overlays = new FakeIdentifyOverlays();
        MonitorInfo first = Monitor("DISPLAY1", 0);
        MonitorInfo second = Monitor("DISPLAY2", 1920);
        var identifier = new MonitorIdentifier(overlays, new FakeUserNotifier(), () => [first, second], s_english);

        identifier.Toggle();

        Assert.Equal([(1, first.Bounds, true), (2, second.Bounds, false)], overlays.Shown);
    }

    [Fact]
    [Trait("Requirement", "IDN-4")]
    internal void Toggle_OverlaysShown_DismissesThemWithoutShowingAgain()
    {
        var overlays = new FakeIdentifyOverlays();
        int enumerations = 0;
        var identifier = new MonitorIdentifier(overlays, new FakeUserNotifier(), () =>
        {
            enumerations++;
            return [Monitor("DISPLAY1", 0)];
        }, s_english);
        identifier.Toggle();

        identifier.Toggle();

        Assert.Equal(1, overlays.DismissCalls);
        Assert.Empty(overlays.Shown);
        Assert.Equal(1, enumerations);
    }

    [Fact]
    [Trait("Requirement", "ERR-8")]
    internal void Toggle_EveryOverlayFails_NotifiesEveryMonitorNumber()
    {
        var overlays = new FakeIdentifyOverlays();
        overlays.FailingNumbers.UnionWith([1, 2]);
        var notifier = new FakeUserNotifier();
        var identifier = new MonitorIdentifier(overlays, notifier, () => [Monitor("DISPLAY1", 0), Monitor("DISPLAY2", 1920)], s_english);

        identifier.Toggle();

        Assert.Equal(["Could not show the identification number on monitor 1, 2."], notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "IDN-5")]
    [Trait("Requirement", "ERR-8")]
    internal void Toggle_OneOverlayFails_ShowsTheOthersAndNotifiesItsNumber()
    {
        var overlays = new FakeIdentifyOverlays();
        overlays.FailingNumbers.Add(1);
        var notifier = new FakeUserNotifier();
        var identifier = new MonitorIdentifier(overlays, notifier, () => [Monitor("DISPLAY1", 0), Monitor("DISPLAY2", 1920)], s_english);

        identifier.Toggle();

        Assert.Equal([2], overlays.Shown.Select(shown => shown.DisplayNumber));
        Assert.Equal(["Could not show the identification number on monitor 1."], notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "ERR-8")]
    internal void Toggle_OneOverlayMissingAndOneWithoutItsNumber_NotifiesBothNumbersOnce()
    {
        var overlays = new FakeIdentifyOverlays();
        overlays.FailingNumbers.Add(1);
        overlays.NumberFailingNumbers.Add(3);
        var notifier = new FakeUserNotifier();
        var identifier = new MonitorIdentifier(
            overlays, notifier, () => [Monitor("DISPLAY1", 0), Monitor("DISPLAY2", 1920), Monitor("DISPLAY3", 3840)], s_english);

        identifier.Toggle();

        Assert.Equal([2, 3], overlays.Shown.Select(shown => shown.DisplayNumber));
        Assert.Equal(["Could not show the identification number on monitor 1, 3."], notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "ERR-8")]
    internal void Toggle_EveryOverlayShown_DoesNotNotify()
    {
        var notifier = new FakeUserNotifier();
        var identifier = new MonitorIdentifier(new FakeIdentifyOverlays(), notifier, () => [Monitor("DISPLAY1", 0)], s_english);

        identifier.Toggle();

        Assert.Empty(notifier.Messages);
    }

    [Theory]
    [Trait("Requirement", "IDN-3")]
    [InlineData(false, true)]
    [InlineData(true, false)]
    internal void Toggle_FirstOverlayCannotStartTheTimer_SecondOverlayStartsIt(bool firstFails, bool firstShown)
    {
        var overlays = new FakeIdentifyOverlays();
        if (firstFails)
            overlays.FailingNumbers.Add(1);
        else
            overlays.TimerFailingNumbers.Add(1);
        MonitorInfo first = Monitor("DISPLAY1", 0);
        MonitorInfo second = Monitor("DISPLAY2", 1920);
        MonitorInfo third = Monitor("DISPLAY3", 3840);
        var identifier = new MonitorIdentifier(overlays, new FakeUserNotifier(), () => [first, second, third], s_english);

        identifier.Toggle();

        Assert.Equal(firstShown, overlays.Shown.Contains((1, first.Bounds, true)));
        Assert.Contains((2, second.Bounds, true), overlays.Shown);
        Assert.Contains((3, third.Bounds, false), overlays.Shown);
    }

    [Fact]
    [Trait("Requirement", "IDN-3")]
    internal void Toggle_NoOverlayCanStartTheTimer_OffersItToEveryOverlay()
    {
        var overlays = new FakeIdentifyOverlays();
        overlays.TimerFailingNumbers.UnionWith([1, 2]);
        MonitorInfo first = Monitor("DISPLAY1", 0);
        MonitorInfo second = Monitor("DISPLAY2", 1920);
        var identifier = new MonitorIdentifier(overlays, new FakeUserNotifier(), () => [first, second], s_english);

        identifier.Toggle();

        Assert.Equal([(1, first.Bounds, true), (2, second.Bounds, true)], overlays.Shown);
    }

    [Fact]
    [Trait("Requirement", "IDN-6")]
    [Trait("Requirement", "MON-6")]
    internal void Toggle_NoMonitor_ShowsNothingAndDoesNotNotify()
    {
        var overlays = new FakeIdentifyOverlays();
        var notifier = new FakeUserNotifier();
        var identifier = new MonitorIdentifier(overlays, notifier, () => [], s_english);

        identifier.Toggle();

        Assert.Empty(overlays.Shown);
        Assert.Empty(notifier.Messages);
    }

    private static MonitorInfo Monitor(string path, int left)
        => new(path, path, new RECT { Left = left, Right = left + 1920, Bottom = 1080 }, false, default);
}
