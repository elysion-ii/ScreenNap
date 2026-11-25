using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Explicit)]
internal struct INPUTUNION
{
    [FieldOffset(0)] internal MOUSEINPUT mi;
    [FieldOffset(0)] internal KEYBDINPUT ki;
}
