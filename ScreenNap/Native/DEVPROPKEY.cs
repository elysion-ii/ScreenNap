using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct DEVPROPKEY
{
    internal Guid fmtid;
    internal uint pid;
}
