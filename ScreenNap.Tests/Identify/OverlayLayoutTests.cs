using ScreenNap.Identify;
using ScreenNap.Native;
using Xunit;

namespace ScreenNap.Tests.Identify;

public sealed class OverlayLayoutTests
{
    [Theory]
    [Trait("Requirement", "IDN-2")]
    [InlineData(96u, 860, 465, 200, 150, 96)]
    [InlineData(120u, 835, 446, 250, 188, 120)]
    [InlineData(144u, 810, 427, 300, 225, 144)]
    [InlineData(192u, 760, 390, 400, 300, 192)]
    internal void For_AnyScale_CentersTheScaledOverlayOnTheMonitor(uint dpi, int x, int y, int width, int height, int fontHeight)
    {
        var monitor = new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };

        Assert.Equal(new OverlayLayout(x, y, width, height, fontHeight), OverlayLayout.For(monitor, dpi));
    }

    [Fact]
    [Trait("Requirement", "IDN-2")]
    internal void For_MonitorLeftOfThePrimary_CentersOnThatMonitor()
    {
        var monitor = new RECT { Left = -2560, Top = -360, Right = 0, Bottom = 1080 };

        OverlayLayout layout = OverlayLayout.For(monitor, 96);

        Assert.Equal((-1380, 285), (layout.X, layout.Y));
    }
}
