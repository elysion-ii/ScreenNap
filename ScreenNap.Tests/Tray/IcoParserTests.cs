using ScreenNap.Tray;
using Xunit;

namespace ScreenNap.Tests.Tray;

public sealed class IcoParserTests
{
    // The image sizes ScreenNap's icons carry; each image's data is 10 bytes long
    private static readonly int[] s_shippedSizes = [16, 24, 32, 48, 64, 72, 96, 128, 256];

    [Theory]
    [Trait("Requirement", "TRAY-7")]
    [InlineData(16, 16)]
    [InlineData(20, 24)]
    [InlineData(24, 24)]
    [InlineData(32, 32)]
    [InlineData(40, 48)]
    [InlineData(256, 256)]
    [InlineData(300, 256)]
    [InlineData(1, 16)]
    internal void TryGetImage_AnySize_TakesThatSizeOrTheNextLarger(int requested, int expectedWidth)
    {
        byte[] ico = Ico(s_shippedSizes);

        Assert.True(IcoParser.TryGetImage(ico, requested, out int offset, out int size));

        Assert.Equal(ImageOffset(s_shippedSizes, expectedWidth), offset);
        Assert.Equal(10, size);
    }

    [Fact]
    [Trait("Requirement", "TRAY-7")]
    internal void TryGetImage_ImagesInAnyOrder_StillTakesTheBestFit()
    {
        int[] sizes = [256, 32, 24, 16];

        Assert.True(IcoParser.TryGetImage(Ico(sizes), 20, out int offset, out _));

        Assert.Equal(ImageOffset(sizes, 24), offset);
    }

    [Theory]
    [Trait("Requirement", "ERR-2")]
    [InlineData(0)]
    [InlineData(5)]
    internal void TryGetImage_DataTooShort_ReturnsFalse(int length)
    {
        Assert.False(IcoParser.TryGetImage(new byte[length], 16, out _, out _));
    }

    [Fact]
    [Trait("Requirement", "ERR-2")]
    internal void TryGetImage_DirectoryLongerThanTheData_ReturnsFalse()
    {
        byte[] ico = Ico([16, 32]);

        Assert.False(IcoParser.TryGetImage(ico[..20], 16, out _, out _));
    }

    [Theory]
    [Trait("Requirement", "ERR-2")]
    [InlineData(0, 22)]
    [InlineData(-1, 22)]
    [InlineData(10, -1)]
    [InlineData(10, 1000)]
    internal void TryGetImage_ImageOutsideTheData_SkipsIt(int imageSize, int imageOffset)
    {
        byte[] ico = Ico([16, 32]);
        BitConverter.GetBytes(imageSize).CopyTo(ico, 6 + 8);
        BitConverter.GetBytes(imageOffset).CopyTo(ico, 6 + 12);

        Assert.True(IcoParser.TryGetImage(ico, 16, out int offset, out _));

        Assert.Equal(ImageOffset([16, 32], 32), offset);
    }

    // An .ico file with one 10-byte image per size, the images stored after the directory in its order
    private static byte[] Ico(int[] widths)
    {
        int dataStart = 6 + (16 * widths.Length);
        var ico = new byte[dataStart + (10 * widths.Length)];
        BitConverter.GetBytes((ushort)1).CopyTo(ico, 2);
        BitConverter.GetBytes((ushort)widths.Length).CopyTo(ico, 4);
        for (int i = 0; i < widths.Length; i++)
        {
            int entry = 6 + (16 * i);
            ico[entry] = (byte)(widths[i] == 256 ? 0 : widths[i]);
            ico[entry + 1] = ico[entry];
            BitConverter.GetBytes(10).CopyTo(ico, entry + 8);
            BitConverter.GetBytes(dataStart + (10 * i)).CopyTo(ico, entry + 12);
        }
        return ico;
    }

    private static int ImageOffset(int[] widths, int width) => 6 + (16 * widths.Length) + (10 * Array.IndexOf(widths, width));
}
