using System.Reflection;
using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;

namespace ScreenNap.App;

internal static class IconHelper
{
    private const uint IconResourceVersion = 0x00030000;
    private const int TrayIconSize = 16;

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
        if (!IcoParser.TryGetFirstImage(icoData, out int imageOffset, out int imageSize))
            return IntPtr.Zero;

        IntPtr buffer = Marshal.AllocHGlobal(imageSize);
        try
        {
            Marshal.Copy(icoData, imageOffset, buffer, imageSize);

            IntPtr hIcon = User32.CreateIconFromResourceEx(
                buffer, (uint)imageSize, true, IconResourceVersion,
                TrayIconSize, TrayIconSize, WindowStyles.LR_DEFAULTCOLOR);

            if (hIcon == IntPtr.Zero)
                Logger.Warn($"CreateIconFromResourceEx failed (Win32 error: {Marshal.GetLastWin32Error()})");

            return hIcon;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
