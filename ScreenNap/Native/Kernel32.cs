using System.Runtime.InteropServices;

namespace ScreenNap.Native;

internal static partial class Kernel32
{
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial IntPtr GetModuleHandleW(string? lpModuleName);
}
