using ScreenNap.Versioning;
using Xunit;

namespace ScreenNap.Tests.Versioning;

public sealed class ProductVersionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("+abc1234")]
    [Trait("Requirement", "VER-1")]
    internal void Parse_NoVersion_Throws(string? informationalVersion)
    {
        Assert.Throws<InvalidOperationException>(() => ProductVersion.Parse(informationalVersion));
    }

    [Theory]
    [InlineData("1.4.0", "1.4.0")]
    [InlineData("  1.4.0  ", "1.4.0")]
    [InlineData("1.4.0+abc1234", "1.4.0")]
    [InlineData("1.4.0-beta.1", "1.4.0-beta.1")]
    [Trait("Requirement", "VER-1")]
    internal void Parse_AnyVersion_ReturnsItWithoutMetadata(string informationalVersion, string expected)
    {
        Assert.Equal(expected, ProductVersion.Parse(informationalVersion));
    }

    [Fact]
    [Trait("Requirement", "VER-1")]
    internal void DisplayText_AnyVersion_PrefixesProductName()
    {
        Assert.Equal("ScreenNap 1.4.0", ProductVersion.DisplayText("1.4.0"));
    }
}
