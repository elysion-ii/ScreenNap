using System.Runtime.InteropServices;

namespace ScreenNap.Native;

internal static partial class Gdi32
{
    [LibraryImport("gdi32.dll", EntryPoint = "GetStockObject")]
    internal static partial IntPtr GetStockObject(int i);

    [LibraryImport("gdi32.dll", EntryPoint = "CreateFontW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr CreateFontW(
        int cHeight, int cWidth, int cEscapement, int cOrientation, int cWeight,
        uint bItalic, uint bUnderline, uint bStrikeOut,
        uint iCharSet, uint iOutPrecision, uint iClipPrecision,
        uint iQuality, uint iPitchAndFamily, string pszFaceName);

    [LibraryImport("gdi32.dll", EntryPoint = "SelectObject")]
    internal static partial IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [LibraryImport("gdi32.dll", EntryPoint = "SetTextColor")]
    internal static partial uint SetTextColor(IntPtr hdc, uint color);

    [LibraryImport("gdi32.dll", EntryPoint = "SetBkMode")]
    internal static partial int SetBkMode(IntPtr hdc, int mode);

    [LibraryImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteObject(IntPtr ho);
}
