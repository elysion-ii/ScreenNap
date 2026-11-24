using System.Runtime.InteropServices;

namespace ScreenNap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct PAINTSTRUCT
{
    internal IntPtr hdc;
    internal int fErase;
    internal RECT rcPaint;
    internal int fRestore;
    internal int fIncUpdate;
    private unsafe fixed byte rgbReserved[32];
}
