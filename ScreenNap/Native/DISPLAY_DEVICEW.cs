using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAY_DEVICEW
{
    internal uint cb;
    internal fixed char DeviceName[32];
    internal fixed char DeviceString[128];
    internal uint StateFlags;
    internal fixed char DeviceID[128];
    internal fixed char DeviceKey[128];
}
