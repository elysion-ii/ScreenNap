using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
{
    internal DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    internal fixed char viewGdiDeviceName[32];
}
