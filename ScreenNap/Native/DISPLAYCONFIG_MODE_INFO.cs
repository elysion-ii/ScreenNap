using System.Runtime.InteropServices;

namespace ScreenNap.Native;

// DISPLAYCONFIG_MODE_INFO is a union; we only need the size for buffer allocation
[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_MODE_INFO
{
    internal uint infoType;
    internal uint id;
    internal LUID adapterId;
    // Union of target/source mode - pad to full struct size
    private unsafe fixed byte modeInfo[64];
}
