using ScreenNap.Core;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class IcoParserTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void TryGetFirstImage_TooShortReturnsFalse(int length)
    {
        Assert.False(IcoParser.TryGetFirstImage(new byte[length], out _, out _));
    }

    [Theory]
    [InlineData(0, 22, 22, false)]
    [InlineData(4, 22, 26, true)]
    [InlineData(-1, 22, 22, false)]
    [InlineData(1, -1, 22, false)]
    [InlineData(20, 10, 22, false)]
    public void TryGetFirstImage_ValidatesImageRange(
        int imageSize,
        int imageOffset,
        int dataLength,
        bool expected)
    {
        byte[] data = Ico(imageSize, imageOffset, dataLength);

        bool result = IcoParser.TryGetFirstImage(data, out int offset, out int size);

        Assert.Equal(expected, result);
        Assert.Equal(expected ? imageOffset : 0, offset);
        Assert.Equal(expected ? imageSize : 0, size);
    }

    private static byte[] Ico(int size, int offset, int length)
    {
        var data = new byte[length];
        BitConverter.GetBytes(size).CopyTo(data, 14);
        BitConverter.GetBytes(offset).CopyTo(data, 18);
        return data;
    }
}
