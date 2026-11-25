using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_TARGET_INFO
{
    internal LUID adapterId;
    internal uint id;
    internal uint modeInfoIdx;
    internal uint outputTechnology;
    internal uint rotation;
    internal uint scaling;
    internal DISPLAYCONFIG_RATIONAL refreshRate;
    internal uint scanLineOrdering;
    internal int targetAvailable;
    internal uint statusFlags;
}
