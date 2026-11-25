using ScreenNap.Monitors;
using Xunit;

namespace ScreenNap.Tests.Monitors;

public sealed class MonitorIdentityTests
{
    private const string FirstPort = @"\\?\DISPLAY#DELA0DB#5&1f3a8ef1&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
    private const string SecondPort = @"\\?\DISPLAY#DELA0DB#5&1f3a8ef1&0&UID4353#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";

    [Fact]
    [Trait("Requirement", "MON-7")]
    internal void For_SameModelWithDifferentSerialNumbers_AreDifferentMonitors()
    {
        Assert.NotEqual(
            MonitorIdentity.For(EdidTests.Build(0xAC10, 0xA0DB, 0, "ABC123"), FirstPort),
            MonitorIdentity.For(EdidTests.Build(0xAC10, 0xA0DB, 0, "ABC124"), SecondPort));
    }

    [Fact]
    [Trait("Requirement", "MON-7")]
    internal void For_SerialNumberOnAnotherConnection_IsTheSameMonitor()
    {
        Assert.Equal(
            MonitorIdentity.For(EdidTests.Build(0xAC10, 0xA0DB, 7, null), FirstPort),
            MonitorIdentity.For(EdidTests.Build(0xAC10, 0xA0DB, 7, null), SecondPort));
    }

    [Fact]
    [Trait("Requirement", "MON-7")]
    internal void For_SameSerialNumberOfAnotherModel_IsAnotherMonitor()
    {
        Assert.NotEqual(
            MonitorIdentity.For(EdidTests.Build(0xAC10, 0xA0DB, 7, null), FirstPort),
            MonitorIdentity.For(EdidTests.Build(0xAC10, 0xA0DC, 7, null), FirstPort));
    }

    [Theory]
    [Trait("Requirement", "MON-7")]
    [InlineData(true)]
    [InlineData(false)]
    internal void For_NoSerialNumberOrNoEdid_IsIdentifiedByItsConnection(bool hasEdid)
    {
        byte[]? edid = hasEdid ? EdidTests.Build(0xAC10, 0xA0DB, 0, null) : null;

        Assert.NotEqual(MonitorIdentity.For(edid, FirstPort), MonitorIdentity.For(edid, SecondPort));
        Assert.Equal(MonitorIdentity.For(edid, FirstPort), MonitorIdentity.For(edid, FirstPort));
    }

    [Fact]
    [Trait("Requirement", "MON-7")]
    internal void For_UnreadableEdid_IsIdentifiedByItsConnection()
    {
        Assert.Equal(MonitorIdentity.For(null, FirstPort), MonitorIdentity.For([1, 2, 3], FirstPort));
    }
}
