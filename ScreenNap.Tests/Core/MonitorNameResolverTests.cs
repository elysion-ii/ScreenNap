using ScreenNap.Core;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class MonitorNameResolverTests
{
    [Fact]
    [Trait("Requirement", "MON-3")]
    [Trait("Requirement", "MON-4")]
    internal void Resolve_QdcNameAvailable_UsesQdcNameAndIdentity()
    {
        var identity = new MonitorIdentity(1, 2, 3);

        (string FriendlyName, MonitorIdentity Identity) result = MonitorNameResolver.Resolve("path", new MonitorDisplayInfo("Dell", identity), "Fallback");

        Assert.Equal(("Dell", identity), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [Trait("Requirement", "MON-3")]
    internal void Resolve_BlankQdcName_UsesEnumDisplayDeviceName(string? qdcName)
    {
        MonitorDisplayInfo? qdc = qdcName is null ? null : new MonitorDisplayInfo(qdcName, new MonitorIdentity(1, 2, 3));

        (string FriendlyName, MonitorIdentity Identity) result = MonitorNameResolver.Resolve("path", qdc, "Fallback");

        Assert.Equal("Fallback", result.FriendlyName);
        Assert.Equal(default, result.Identity);
    }

    [Theory]
    [InlineData(null, @"\\.\DISPLAY1", "DISPLAY1")]
    [InlineData("", @"\\.\DISPLAY1", "DISPLAY1")]
    [InlineData(" ", "DISPLAY1", "DISPLAY1")]
    [Trait("Requirement", "MON-3")]
    internal void Resolve_NoNamesAvailable_UsesDevicePath(string? fallback, string path, string expected)
    {
        (string FriendlyName, MonitorIdentity Identity) result = MonitorNameResolver.Resolve(path, null, fallback);

        Assert.Equal(expected, result.FriendlyName);
        Assert.Equal(default, result.Identity);
    }
}
