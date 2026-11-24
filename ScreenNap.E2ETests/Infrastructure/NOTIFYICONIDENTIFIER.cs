using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

[StructLayout(LayoutKind.Sequential)]
internal struct NOTIFYICONIDENTIFIER
{
    internal uint cbSize;
    internal IntPtr hWnd;
    internal uint uID;
    internal Guid guidItem;
}
