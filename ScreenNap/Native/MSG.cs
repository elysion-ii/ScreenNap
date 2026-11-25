using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct MSG
{
    internal IntPtr hwnd;
    internal uint message;
    internal nuint wParam;
    internal nint lParam;
    internal uint time;
    internal POINT pt;
}
