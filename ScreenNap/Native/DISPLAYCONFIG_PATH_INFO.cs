using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_INFO
{
    internal DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
    internal DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
    internal uint flags;
}
