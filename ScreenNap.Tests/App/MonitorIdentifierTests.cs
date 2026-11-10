using ScreenNap.App;
using ScreenNap.Core;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.App;

public sealed class MonitorIdentifierTests
{
    [Fact]
    [Trait("Requirement", "IDN-1")]
    [Trait("Requirement", "IDN-3")]
    [Trait("Requirement", "MON-2")]
    internal void Toggle_NoOverlayShown_ShowsOneNumberedOverlayPerMonitor()
    {
        var overlays = new FakeIdentifyOverlays();
        MonitorInfo first = Monitor("DISPLAY1", 0);
        MonitorInfo second = Monitor("DISPLAY2", 1920);
        var identifier = new MonitorIdentifier(overlays, new FakeUserNotifier(), () => [first, second]);

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
        });
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
        using var scope = new CultureScope("en-US");
        var overlays = new FakeIdentifyOverlays();
        overlays.FailingNumbers.UnionWith([1, 2]);
        var notifier = new FakeUserNotifier();
        var identifier = new MonitorIdentifier(overlays, notifier, () => [Monitor("DISPLAY1", 0), Monitor("DISPLAY2", 1920)]);

        identifier.Toggle();

        Assert.Equal(["Could not show the identification number on monitor 1, 2."], notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "IDN-5")]
    [Trait("Requirement", "ERR-8")]
    internal void Toggle_OneOverlayFails_ShowsTheOthersAndNotifiesItsNumber()
    {
        using var scope = new CultureScope("en-US");
        var overlays = new FakeIdentifyOverlays();
        overlays.FailingNumbers.Add(1);
        var notifier = new FakeUserNotifier();
        var identifier = new MonitorIdentifier(overlays, notifier, () => [Monitor("DISPLAY1", 0), Monitor("DISPLAY2", 1920)]);

        identifier.Toggle();

        Assert.Equal([2], overlays.Shown.Select(shown => shown.DisplayNumber));
        Assert.Equal(["Could not show the identification number on monitor 1."], notifier.Messages);
    }

    [Fact]
    [Trait("Requirement", "ERR-8")]
    internal void Toggle_EveryOverlayShown_DoesNotNotify()
    {
        var notifier = new FakeUserNotifier();
        var identifier = new MonitorIdentifier(new FakeIdentifyOverlays(), notifier, () => [Monitor("DISPLAY1", 0)]);

        identifier.Toggle();

        Assert.Empty(notifier.Messages);
    }

    private static MonitorInfo Monitor(string path, int left)
        => new(path, path, new RECT { Left = left, Right = left + 1920, Bottom = 1080 }, false, default);
}
