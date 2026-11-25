using System.Reflection;
using System.Runtime.InteropServices;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.Tray;

internal static class IconHelper
{
    private const uint IconResourceVersion = 0x00030000;

    internal static IntPtr LoadIconFromResource(string resourceFileName, int pixelSize)
    {
        string resourceName = $"ScreenNap.Resources.{resourceFileName}";
        Assembly assembly = Assembly.GetExecutingAssembly();

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            Logger.Warn($"Icon resource not found: {resourceName}");
            return IntPtr.Zero;
        }

        byte[] icoData = new byte[stream.Length];
        stream.ReadExactly(icoData);

        return CreateIconFromIcoData(icoData, pixelSize);
    }

    private static IntPtr CreateIconFromIcoData(byte[] icoData, int pixelSize)
    {
        if (!IcoParser.TryGetImage(icoData, pixelSize, out int imageOffset, out int imageSize))
            return IntPtr.Zero;

        IntPtr buffer = Marshal.AllocHGlobal(imageSize);
        try
        {
            Marshal.Copy(icoData, imageOffset, buffer, imageSize);

            IntPtr hIcon = User32.CreateIconFromResourceEx(
                buffer, (uint)imageSize, true, IconResourceVersion,
                pixelSize, pixelSize, WindowStyles.LR_DEFAULTCOLOR);

            if (hIcon == IntPtr.Zero)
                Logger.Warn("CreateIconFromResourceEx failed", Marshal.GetLastWin32Error());

            return hIcon;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
