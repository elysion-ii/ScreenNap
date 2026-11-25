using ScreenNap.Logging;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Logging;

public sealed class LoggerTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("ja-JP")]
    internal void Warn_Win32Error_AppendsTheErrorCodeInAnyCulture(string culture)
    {
        using var scope = new CultureScope(culture);
        using var log = new LogCapture();

        Logger.Warn("SetTimer failed", 1400);

        Assert.Equal([new LogCapture.Entry("WARN", "SetTimer failed (Win32 error: 1400)")], log.Entries);
    }

    [Theory]
    [Trait("Requirement", "LOG-5")]
    [InlineData("INFO")]
    [InlineData("WARN")]
    [InlineData("ERROR")]
    internal void Write_EachLevel_ReachesTheSinkWithThatLevel(string level)
    {
        using var log = new LogCapture();

        switch (level)
        {
            case "INFO":
                Logger.Info("Started");
                break;
            case "WARN":
                Logger.Warn("Started");
                break;
            default:
                Logger.Error("Started");
                break;
        }

        Assert.Equal([new LogCapture.Entry(level, "Started")], log.Entries);
    }

    [Fact]
    [Trait("Requirement", "LOG-9")]
    internal void Write_NoSink_LogsNothingAndDoesNotThrow()
    {
        Logger.Use(null);

        Logger.Error("Nowhere to go", 5);
    }

    [Theory]
    [Trait("Requirement", "L10N-5")]
    [InlineData("en-US")]
    [InlineData("sv-SE")]
    [InlineData("ar-SA")]
    internal void InvariantLogMessage_AnyCulture_FormatsValuesInvariantly(string culture)
    {
        using var scope = new CultureScope(culture);
        int left = -1920;
        double ratio = 1.5;

        InvariantLogMessage message = $"at {left} scale {ratio} id {255:X4}";

        Assert.Equal("at -1920 scale 1.5 id 00FF", message.ToStringAndClear());
    }
}
