using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct CURSORINFO
{
    internal uint cbSize;
    internal uint flags;
    internal IntPtr hCursor;
    internal POINT ptScreenPos;
}
