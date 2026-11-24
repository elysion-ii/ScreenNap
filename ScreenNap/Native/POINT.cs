using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    internal int X;
    internal int Y;
}
