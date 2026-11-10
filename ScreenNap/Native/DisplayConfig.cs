using System.Runtime.InteropServices;

namespace ScreenNap.Native;

internal static partial class DisplayConfigApi
{
    [LibraryImport("user32.dll", EntryPoint = "GetDisplayConfigBufferSizes", SetLastError = true)]
    internal static partial int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [LibraryImport("user32.dll", EntryPoint = "QueryDisplayConfig", SetLastError = true)]
    internal static partial int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [In, Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements,
        [In, Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        IntPtr currentTopologyId);

    [LibraryImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo", SetLastError = true)]
    internal static partial int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

    [LibraryImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo", SetLastError = true)]
    internal static partial int DisplayConfigGetSourceDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);
}
