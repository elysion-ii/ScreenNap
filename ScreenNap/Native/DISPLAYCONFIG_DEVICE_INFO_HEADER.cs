using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_DEVICE_INFO_HEADER
{
    internal uint type;
    internal uint size;
    internal LUID adapterId;
    internal uint id;
}
