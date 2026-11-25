using ScreenNap.Native;

namespace ScreenNap.Identify;

internal readonly record struct OverlayLayout(int X, int Y, int Width, int Height, int FontHeight)
{
    // The overlay's size at 100% scaling (specification IDN-2)
    private const int BaseWidth = 200;
    private const int BaseHeight = 150;
    private const int BaseFontHeight = 96;

    internal static OverlayLayout For(RECT monitor, uint dpi)
    {
        int width = Scale(BaseWidth, dpi);
        int height = Scale(BaseHeight, dpi);
        return new OverlayLayout(
            monitor.Left + (monitor.Width - width) / 2,
            monitor.Top + (monitor.Height - height) / 2,
            width,
            height,
            Scale(BaseFontHeight, dpi));
    }

    private static int Scale(int value, uint dpi)
        => (int)(((long)value * dpi + WindowStyles.USER_DEFAULT_SCREEN_DPI / 2) / WindowStyles.USER_DEFAULT_SCREEN_DPI);
}
