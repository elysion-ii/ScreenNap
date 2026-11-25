using System.Runtime.InteropServices;
using ScreenNap.Native;

namespace ScreenNap.Hotkeys;

internal sealed class Win32HotkeyApi : IHotkeyApi
{
    public int LastError => Marshal.GetLastWin32Error();

    public bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey) => User32.RegisterHotKey(hwnd, id, modifiers, virtualKey);

    public bool UnregisterHotKey(IntPtr hwnd, int id) => User32.UnregisterHotKey(hwnd, id);

    public bool IsKeyDown(uint virtualKey) => (User32.GetAsyncKeyState((int)virtualKey) & WindowStyles.KEY_DOWN_MASK) != 0;
}
