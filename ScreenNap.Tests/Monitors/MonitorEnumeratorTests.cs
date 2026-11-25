using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Monitors;

public sealed class MonitorEnumeratorTests
{
    [Fact]
    [Trait("Requirement", "MON-2")]
    [Trait("Requirement", "MON-4")]
    internal void EnumerateMonitors_EveryMonitorAnswers_ListsThemInEnumerationOrder()
    {
        FakeMonitorApi api = ThreeMonitors();

        IReadOnlyList<MonitorInfo> monitors = new MonitorEnumerator(api).EnumerateMonitors();

        Assert.Equal([@"\\.\DISPLAY1", @"\\.\DISPLAY2", @"\\.\DISPLAY3"], monitors.Select(monitor => monitor.DevicePath));
        Assert.Equal([true, false, false], monitors.Select(monitor => monitor.IsPrimary));
        Assert.Equal(1920, monitors[1].Bounds.Left);
    }

    [Fact]
    [Trait("Requirement", "MON-5")]
    internal void EnumerateMonitors_OneMonitorFailsToAnswer_LeavesOnlyThatMonitorOut()
    {
        FakeMonitorApi api = ThreeMonitors();
        api.MonitorsFailingInformation.Add(1);
        using var log = new LogCapture();

        IReadOnlyList<MonitorInfo> monitors = new MonitorEnumerator(api).EnumerateMonitors();

        Assert.Equal([@"\\.\DISPLAY1", @"\\.\DISPLAY3"], monitors.Select(monitor => monitor.DevicePath));
        LogAssert.Logged(log, "WARN", "GetMonitorInfoW failed; skipping the monitor");
    }

    [Theory]
    [Trait("Requirement", "MON-6")]
    [InlineData(0)]
    [InlineData(2)]
    internal void EnumerateMonitors_EnumerationFails_UsesTheMonitorsFoundBeforeTheFailureAndLogsIt(int foundBeforeFailure)
    {
        FakeMonitorApi api = ThreeMonitors();
        api.EnumerationFailsAfter = foundBeforeFailure;
        using var log = new LogCapture();

        IReadOnlyList<MonitorInfo> monitors = new MonitorEnumerator(api).EnumerateMonitors();

        Assert.Equal(
            api.Monitors.Take(foundBeforeFailure).Select(monitor => monitor.DevicePath),
            monitors.Select(monitor => monitor.DevicePath));
        LogAssert.Logged(log, "WARN", $"EnumDisplayMonitors failed; using the monitors found before the failure (Win32 error: {FakeMonitorApi.FailureError})");
    }

    // Two monitors of the same model; the first lacks a friendly name and the third has no display path
    [Fact]
    [Trait("Requirement", "MON-3")]
    [Trait("Requirement", "MON-4")]
    [Trait("Requirement", "MON-7")]
    internal void EnumerateMonitors_DisplayTargetsReported_NamesAndIdentifiesEachMonitor()
    {
        FakeMonitorApi api = ThreeMonitors();
        api.Targets[@"\\.\DISPLAY1"] = new DisplayTarget(" ", "port 1", EdidTests.Build(0xAC10, 0xA0DB, 0, "SERIAL-A"));
        api.Targets[@"\\.\DISPLAY2"] = new DisplayTarget("DELL P2417H", "port 2", EdidTests.Build(0xAC10, 0xA0DB, 0, "SERIAL-B"));
        api.DisplayDeviceNames[@"\\.\DISPLAY1"] = "Generic PnP Monitor";

        IReadOnlyList<MonitorInfo> monitors = new MonitorEnumerator(api).EnumerateMonitors();

        Assert.Equal(["Generic PnP Monitor", "DELL P2417H", "DISPLAY3"], monitors.Select(monitor => monitor.FriendlyName));
        Assert.Equal(
            [
                MonitorIdentity.For(api.Targets[@"\\.\DISPLAY1"].Edid, "port 1"),
                MonitorIdentity.For(api.Targets[@"\\.\DISPLAY2"].Edid, "port 2"),
                null,
            ],
            monitors.Select(monitor => monitor.Identity));
        Assert.NotEqual(monitors[0].Identity, monitors[1].Identity);
    }

    private static FakeMonitorApi ThreeMonitors()
    {
        var api = new FakeMonitorApi();
        api.Monitors.Add((new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 }, true, @"\\.\DISPLAY1"));
        api.Monitors.Add((new RECT { Left = 1920, Top = 0, Right = 3840, Bottom = 1080 }, false, @"\\.\DISPLAY2"));
        api.Monitors.Add((new RECT { Left = -1920, Top = 0, Right = 0, Bottom = 1080 }, false, @"\\.\DISPLAY3"));
        return api;
    }
}
