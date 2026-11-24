using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO
{
    internal uint cbSize;
    internal RECT rcMonitor;
    internal RECT rcWork;
    internal uint dwFlags;
}
