using ScreenNap.Native;

namespace ScreenNap.Identify;

// The painting functions the identify overlay calls, behind one interface so a unit test can
// make any of them fail (docs/rules/ScreenNap.md, Win32 and P/Invoke)
internal interface IOverlayPaintApi
{
    // Zero when painting cannot begin
    IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT paint);

    void EndPaint(IntPtr hwnd, ref PAINTSTRUCT paint);

    // Zero when the font cannot be created
    IntPtr CreateFont(int height, int weight, string faceName);

    // The object selected before; zero when the selection fails
    IntPtr SelectObject(IntPtr hdc, IntPtr gdiObject);

    bool SetTextColor(IntPtr hdc, uint color);

    bool SetTransparentBackground(IntPtr hdc);

    bool DrawText(IntPtr hdc, string text, ref RECT rect, uint format);

    bool DeleteObject(IntPtr gdiObject);
}
