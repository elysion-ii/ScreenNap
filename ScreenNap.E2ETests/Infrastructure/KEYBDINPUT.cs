using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    internal ushort wVk;
    internal ushort wScan;
    internal uint dwFlags;
    internal uint time;
    internal nuint dwExtraInfo;
}
