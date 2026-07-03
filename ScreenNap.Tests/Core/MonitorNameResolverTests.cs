using ScreenNap.Core;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class MonitorNameResolverTests
{
    [Fact]
    public void Resolve_UsesQdcNameAndIdentity()
    {
        var identity = new MonitorIdentity(1, 2, 3);

        var result = MonitorNameResolver.Resolve("path", new MonitorDisplayInfo("Dell", identity), "Fallback");

        Assert.Equal(("Dell", identity), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Resolve_BlankQdcNameUsesEnumDisplayDevice(string? qdcName)
    {
        MonitorDisplayInfo? qdc = qdcName is null ? null : new MonitorDisplayInfo(qdcName, new MonitorIdentity(1, 2, 3));

        var result = MonitorNameResolver.Resolve("path", qdc, "Fallback");

        Assert.Equal("Fallback", result.FriendlyName);
        Assert.Equal(default, result.Identity);
    }

    [Theory]
    [InlineData(null, @"\\.\DISPLAY1", "DISPLAY1")]
    [InlineData("", @"\\.\DISPLAY1", "DISPLAY1")]
    [InlineData(" ", "DISPLAY1", "DISPLAY1")]
    public void Resolve_MissingNamesUsesDevicePath(string? fallback, string path, string expected)
    {
        var result = MonitorNameResolver.Resolve(path, null, fallback);

        Assert.Equal(expected, result.FriendlyName);
        Assert.Equal(default, result.Identity);
    }
}
