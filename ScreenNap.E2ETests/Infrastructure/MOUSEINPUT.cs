using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    internal int dx;
    internal int dy;
    internal uint mouseData;
    internal uint dwFlags;
    internal uint time;
    internal nuint dwExtraInfo;
}
