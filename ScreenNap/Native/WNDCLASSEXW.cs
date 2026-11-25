using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct WNDCLASSEXW
{
    internal uint cbSize;
    internal uint style;
    internal IntPtr lpfnWndProc;
    internal int cbClsExtra;
    internal int cbWndExtra;
    internal IntPtr hInstance;
    internal IntPtr hIcon;
    internal IntPtr hCursor;
    internal IntPtr hbrBackground;
    internal IntPtr lpszMenuName;
    internal IntPtr lpszClassName;
    internal IntPtr hIconSm;
}
