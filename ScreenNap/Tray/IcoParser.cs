using System.Buffers.Binary;

namespace ScreenNap.Tray;

// Which image of an .ico file draws an icon of a given size (specification TRAY-7)
internal static class IcoParser
{
    private const int HeaderLength = 6;
    private const int CountOffset = 4;
    private const int EntryLength = 16;
    private const int EntryImageSizeOffset = 8;
    private const int EntryImageDataOffset = 12;
    private const int ByteValueOfLargestWidth = 256;

    // The image of exactly the size, else the smallest larger one, else the largest one;
    // false when the file holds no image whose data lies inside it
    internal static bool TryGetImage(byte[] icoData, int pixelSize, out int offset, out int size)
    {
        ArgumentNullException.ThrowIfNull(icoData);
        offset = 0;
        size = 0;
        if (icoData.Length < HeaderLength)
            return false;

        int count = BinaryPrimitives.ReadUInt16LittleEndian(icoData.AsSpan(CountOffset));
        int bestWidth = 0;
        for (int i = 0; i < count; i++)
        {
            int entry = HeaderLength + (i * EntryLength);
            if (entry > icoData.Length - EntryLength)
                return false;

            // A width of 0 stands for 256 pixels
            int width = icoData[entry] == 0 ? ByteValueOfLargestWidth : icoData[entry];
            int imageSize = BinaryPrimitives.ReadInt32LittleEndian(icoData.AsSpan(entry + EntryImageSizeOffset));
            int imageOffset = BinaryPrimitives.ReadInt32LittleEndian(icoData.AsSpan(entry + EntryImageDataOffset));
            if (imageSize <= 0 || imageOffset < 0 || imageOffset > icoData.Length - imageSize)
                continue;

            if (bestWidth == 0 || Better(width, bestWidth, pixelSize))
            {
                bestWidth = width;
                offset = imageOffset;
                size = imageSize;
            }
        }

        return bestWidth != 0;
    }

    private static bool Better(int candidate, int current, int target)
    {
        bool candidateFits = candidate >= target;
        bool currentFits = current >= target;
        if (candidateFits != currentFits)
            return candidateFits;
        return candidateFits ? candidate < current : candidate > current;
    }
}
