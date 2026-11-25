using ScreenNap.Native;

namespace ScreenNap.Identify;

internal sealed class Win32OverlayPaintApi : IOverlayPaintApi
{
    public IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT paint) => User32.BeginPaint(hwnd, out paint);

    // EndPaint always returns nonzero
    public void EndPaint(IntPtr hwnd, ref PAINTSTRUCT paint) => _ = User32.EndPaint(hwnd, ref paint);

    public IntPtr CreateFont(int height, int weight, string faceName)
        => Gdi32.CreateFontW(height, 0, 0, 0, weight, 0, 0, 0, 0, 0, 0, 0, 0, faceName);

    public IntPtr SelectObject(IntPtr hdc, IntPtr gdiObject) => Gdi32.SelectObject(hdc, gdiObject);

    public bool SetTextColor(IntPtr hdc, uint color) => Gdi32.SetTextColor(hdc, color) != WindowStyles.CLR_INVALID;

    public bool SetTransparentBackground(IntPtr hdc) => Gdi32.SetBkMode(hdc, WindowStyles.TRANSPARENT_BK) != 0;

    public bool DrawText(IntPtr hdc, string text, ref RECT rect, uint format) => User32.DrawTextW(hdc, text, -1, ref rect, format) != 0;

    public bool DeleteObject(IntPtr gdiObject) => Gdi32.DeleteObject(gdiObject);
}
