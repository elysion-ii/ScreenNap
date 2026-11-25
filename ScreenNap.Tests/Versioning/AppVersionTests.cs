using ScreenNap.Versioning;
using Xunit;

namespace ScreenNap.Tests.Versioning;

public sealed class AppVersionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("+abc1234")]
    [Trait("Requirement", "VER-1")]
    internal void Normalize_NoVersion_Throws(string? informationalVersion)
    {
        Assert.Throws<InvalidOperationException>(() => AppVersion.Normalize(informationalVersion));
    }

    [Theory]
    [InlineData("1.4.0", "1.4.0")]
    [InlineData("  1.4.0  ", "1.4.0")]
    [InlineData("1.4.0+abc1234", "1.4.0")]
    [InlineData("1.4.0-beta.1", "1.4.0-beta.1")]
    [Trait("Requirement", "VER-1")]
    internal void Normalize_AnyVersion_ReturnsItWithoutMetadata(string informationalVersion, string expected)
    {
        Assert.Equal(expected, AppVersion.Normalize(informationalVersion));
    }

    [Fact]
    [Trait("Requirement", "VER-1")]
    internal void Format_AnyVersion_PrefixesProductName()
    {
        Assert.Equal("ScreenNap 1.4.0", AppVersion.Format("1.4.0"));
    }

    [Fact]
    [Trait("Requirement", "VER-1")]
    internal void Current_BuiltAssembly_ReturnsVersionWithoutMetadata()
    {
        Assert.DoesNotContain('+', AppVersion.Current);
    }
}
