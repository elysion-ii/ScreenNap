using ScreenNap.App;
using Xunit;

namespace ScreenNap.Tests.App;

public sealed class AppVersionTests
{
    [Theory]
    [InlineData(null, "unknown")]
    [InlineData("", "unknown")]
    [InlineData("   ", "unknown")]
    [InlineData("+abc1234", "unknown")]
    [InlineData("1.4.0", "1.4.0")]
    [InlineData("  1.4.0  ", "1.4.0")]
    [InlineData("1.4.0+abc1234", "1.4.0")]
    [InlineData("1.4.0-beta.1", "1.4.0-beta.1")]
    public void Normalize_AnyInformationalVersion_ReturnsVersionWithoutMetadata(string? informationalVersion, string expected)
    {
        Assert.Equal(expected, AppVersion.Normalize(informationalVersion));
    }

    [Fact]
    public void Format_AnyVersion_PrefixesProductName()
    {
        Assert.Equal("ScreenNap 1.4.0", AppVersion.Format("1.4.0"));
    }

    [Fact]
    public void Current_BuiltAssembly_ReturnsVersionWithoutMetadata()
    {
        Assert.DoesNotContain('+', AppVersion.Current);
        Assert.NotEqual(AppVersion.Unknown, AppVersion.Current);
    }
}
