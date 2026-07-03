namespace ScreenNap.Core;

internal static class IcoParser
{
    private const int MinimumIcoLength = 22;
    private const int ImageSizeOffset = 14;
    private const int ImageDataOffset = 18;

    internal static bool TryGetFirstImage(byte[] icoData, out int offset, out int size)
    {
        ArgumentNullException.ThrowIfNull(icoData);
        offset = 0;
        size = 0;

        if (icoData.Length < MinimumIcoLength)
            return false;

        int candidateSize = BitConverter.ToInt32(icoData, ImageSizeOffset);
        int candidateOffset = BitConverter.ToInt32(icoData, ImageDataOffset);
        if (candidateSize <= 0 || candidateOffset < 0 ||
            candidateOffset > icoData.Length - candidateSize)
        {
            return false;
        }

        offset = candidateOffset;
        size = candidateSize;
        return true;
    }
}
