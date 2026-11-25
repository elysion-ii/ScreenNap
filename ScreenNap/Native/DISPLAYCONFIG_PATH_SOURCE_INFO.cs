using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_SOURCE_INFO
{
    internal LUID adapterId;
    internal uint id;
    internal uint modeInfoIdx;
    internal uint statusFlags;
}
