using System.Runtime.InteropServices;

namespace ScreenNap.Native;

internal sealed class WindowClass
{
    private readonly uint _style;
    private readonly WNDPROC _wndProc;

    internal WindowClass(string name, uint style, WNDPROC wndProc)
    {
        Name = name;
        _style = style;
        _wndProc = wndProc;
    }

    internal string Name { get; }

    internal bool IsRegistered { get; private set; }

    // On failure the caller reads the cause with Marshal.GetLastWin32Error
    internal bool Register(IntPtr hInstance, IntPtr background, IntPtr cursor)
    {
        if (IsRegistered)
            return true;

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            style = _style,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            hbrBackground = background,
            hCursor = cursor,
            lpszClassName = Marshal.StringToHGlobalUni(Name)
        };

        ushort atom = User32.RegisterClassExW(ref wc);
        Marshal.FreeHGlobal(wc.lpszClassName);

        IsRegistered = atom != 0;
        return IsRegistered;
    }

    // On failure the caller reads the cause with Marshal.GetLastWin32Error
    internal bool Unregister(IntPtr hInstance)
    {
        if (!IsRegistered)
            return true;

        if (!User32.UnregisterClassW(Name, hInstance))
            return false;

        IsRegistered = false;
        return true;
    }
}
