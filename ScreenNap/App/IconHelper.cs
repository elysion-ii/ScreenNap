using System.Reflection;
using System.Runtime.InteropServices;
using ScreenNap.Native;

namespace ScreenNap.App;

internal static class IconHelper
{
    /// <summary>
    /// Load an HICON from an embedded .ico resource.
    /// Resource names follow the pattern "ScreenNap.Resources.{fileName}".
    /// </summary>
    internal static IntPtr LoadIconFromResource(string resourceFileName)
    {
        string resourceName = $"ScreenNap.Resources.{resourceFileName}";
        var assembly = Assembly.GetExecutingAssembly();

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return IntPtr.Zero;

        byte[] icoData = new byte[stream.Length];
        stream.ReadExactly(icoData);

        return CreateIconFromIcoData(icoData);
    }

    private static IntPtr CreateIconFromIcoData(byte[] icoData)
    {
        // .ico format: 6-byte ICONDIR + 16-byte ICONDIRENTRY per image + image data
        // We read the first image entry
        if (icoData.Length < 22)
            return IntPtr.Zero;

        int imageOffset = BitConverter.ToInt32(icoData, 18);
        int imageSize = BitConverter.ToInt32(icoData, 14);

        if (imageOffset + imageSize > icoData.Length)
            return IntPtr.Zero;

        IntPtr buffer = Marshal.AllocHGlobal(imageSize);
        try
        {
            Marshal.Copy(icoData, imageOffset, buffer, imageSize);

            // 0x00030000 = version required by CreateIconFromResourceEx
            IntPtr hIcon = User32.CreateIconFromResourceEx(
                buffer, (uint)imageSize, true, 0x00030000,
                16, 16, WindowStyles.LR_DEFAULTCOLOR);

            return hIcon;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
