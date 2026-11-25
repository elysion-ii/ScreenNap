using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct SECURITY_ATTRIBUTES
{
    internal uint nLength;
    internal IntPtr lpSecurityDescriptor;
    internal int bInheritHandle;
}
