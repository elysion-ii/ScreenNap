using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAYCONFIG_TARGET_DEVICE_NAME
{
    internal DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    internal uint flags;
    internal uint outputTechnology;
    internal ushort edidManufactureId;
    internal ushort edidProductCodeId;
    internal uint connectorInstance;
    internal fixed char monitorFriendlyDeviceName[64];
    internal fixed char monitorDevicePath[128];
}
