using ScreenNap.Monitors;
using Xunit;

namespace ScreenNap.Tests.Monitors;

public sealed class MonitorNameResolverTests
{
    [Theory]
    [Trait("Requirement", "MON-3")]
    [InlineData("Dell", "Fallback", @"\\.\DISPLAY1", "Dell")]
    [InlineData(null, "Fallback", @"\\.\DISPLAY1", "Fallback")]
    [InlineData("", "Fallback", @"\\.\DISPLAY1", "Fallback")]
    [InlineData(" ", "Fallback", @"\\.\DISPLAY1", "Fallback")]
    [InlineData(null, null, @"\\.\DISPLAY1", "DISPLAY1")]
    [InlineData("", "", @"\\.\DISPLAY1", "DISPLAY1")]
    [InlineData(" ", " ", "DISPLAY1", "DISPLAY1")]
    internal void Resolve_AnyNames_FallsBackInOrder(string? friendlyName, string? displayDeviceName, string devicePath, string expected)
    {
        Assert.Equal(expected, MonitorNameResolver.Resolve(devicePath, friendlyName, displayDeviceName));
    }
}
