using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    internal uint type;
    internal INPUTUNION u;
}
