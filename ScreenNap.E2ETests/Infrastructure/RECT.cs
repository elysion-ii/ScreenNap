using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;

    internal readonly int Width => Right - Left;
    internal readonly int Height => Bottom - Top;
}
