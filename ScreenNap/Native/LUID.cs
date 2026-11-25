using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct LUID
{
    internal uint LowPart;
    internal int HighPart;
}
