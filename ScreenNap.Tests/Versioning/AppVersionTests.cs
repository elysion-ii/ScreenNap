using ScreenNap.Versioning;
using Xunit;

namespace ScreenNap.Tests.Versioning;

public sealed class AppVersionTests
{
    [Fact]
    [Trait("Requirement", "VER-1")]
    internal void Current_BuiltAssembly_ReturnsVersionWithoutMetadata()
    {
        Assert.DoesNotContain('+', AppVersion.Current);
    }

    [Fact]
    [Trait("Requirement", "VER-1")]
    internal void DisplayText_BuiltAssembly_NamesTheProductAndItsVersion()
    {
        Assert.Equal($"ScreenNap {AppVersion.Current}", AppVersion.DisplayText);
    }
}
