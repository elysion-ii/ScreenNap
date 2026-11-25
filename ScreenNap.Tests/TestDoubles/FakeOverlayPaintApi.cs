using ScreenNap.Identify;
using ScreenNap.Native;

namespace ScreenNap.Tests.TestDoubles;

// Stands in for the painting functions: it records the text drawn and fails the calls a test names
internal sealed class FakeOverlayPaintApi : IOverlayPaintApi
{
    private static readonly IntPtr s_deviceContext = new(0x900);
    private static readonly IntPtr s_font = new(0x901);
    private static readonly IntPtr s_previousFont = new(0x902);

    // The names of the IOverlayPaintApi methods that fail
    internal HashSet<string> FailingCalls { get; } = new(StringComparer.Ordinal);
    internal List<string> DrawnTexts { get; } = [];

    public IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT paint)
    {
        paint = default;
        return FailingCalls.Contains(nameof(BeginPaint)) ? IntPtr.Zero : s_deviceContext;
    }

    public void EndPaint(IntPtr hwnd, ref PAINTSTRUCT paint)
    {
    }

    public IntPtr CreateFont(int height, int weight, string faceName)
        => FailingCalls.Contains(nameof(CreateFont)) ? IntPtr.Zero : s_font;

    public IntPtr SelectObject(IntPtr hdc, IntPtr gdiObject)
        => FailingCalls.Contains(nameof(SelectObject)) ? IntPtr.Zero : s_previousFont;

    public bool SetTextColor(IntPtr hdc, uint color) => !FailingCalls.Contains(nameof(SetTextColor));

    public bool SetTransparentBackground(IntPtr hdc) => !FailingCalls.Contains(nameof(SetTransparentBackground));

    public bool DrawText(IntPtr hdc, string text, ref RECT rect, uint format)
    {
        if (FailingCalls.Contains(nameof(DrawText)))
            return false;
        DrawnTexts.Add(text);
        return true;
    }

    public bool DeleteObject(IntPtr gdiObject) => !FailingCalls.Contains(nameof(DeleteObject));
}
