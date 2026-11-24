using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    internal int X;
    internal int Y;
}
