using System.Runtime.InteropServices;

namespace ScreenNap.Native;

internal static partial class Cfgmgr32
{
    [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_Interface_PropertyW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint CM_Get_Device_Interface_PropertyW(
        string pszDeviceInterface,
        in DEVPROPKEY propertyKey,
        out uint propertyType,
        [Out] byte[] propertyBuffer,
        ref uint propertyBufferSize,
        uint flags);
}
