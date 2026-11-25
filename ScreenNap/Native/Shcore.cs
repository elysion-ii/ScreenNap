using System.Runtime.InteropServices;

namespace ScreenNap.Native;

internal static partial class Shcore
{
    [LibraryImport("shcore.dll", EntryPoint = "GetDpiForMonitor")]
    internal static partial int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
}
