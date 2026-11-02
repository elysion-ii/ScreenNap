using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

internal static class Keyboard
{
    internal const uint HotkeyModifiers = Win32.MOD_CONTROL | Win32.MOD_SHIFT | Win32.MOD_ALT;

    internal static void PressHotkey(int digit)
    {
        ushort digitKey = (ushort)(Win32.VK_0 + digit);
        INPUT[] inputs =
        [
            Key(Win32.VK_CONTROL, false),
            Key(Win32.VK_SHIFT, false),
            Key(Win32.VK_MENU, false),
            Key(digitKey, false),
            Key(digitKey, true),
            Key(Win32.VK_MENU, true),
            Key(Win32.VK_SHIFT, true),
            Key(Win32.VK_CONTROL, true),
        ];

        uint sent = Win32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
            throw new InvalidOperationException($"SendInput sent {sent} of {inputs.Length} key events (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    // A hotkey another process holds cannot be registered again; registering and releasing it probes that
    internal static bool HotkeyIsHeldElsewhere(int digit)
    {
        const int ProbeId = 0xB000;
        if (Win32.RegisterHotKey(IntPtr.Zero, ProbeId, HotkeyModifiers, (uint)(Win32.VK_0 + digit)))
        {
            if (!Win32.UnregisterHotKey(IntPtr.Zero, ProbeId))
                throw new InvalidOperationException($"UnregisterHotKey failed for the probe (Win32 error: {Marshal.GetLastWin32Error()})");
            return false;
        }

        int error = Marshal.GetLastWin32Error();
        if (error != Win32.ERROR_HOTKEY_ALREADY_REGISTERED)
            throw new InvalidOperationException($"RegisterHotKey failed for the probe (Win32 error: {error})");
        return true;
    }

    private static INPUT Key(ushort virtualKey, bool up) => new()
    {
        type = Win32.INPUT_KEYBOARD,
        u = new INPUTUNION { ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = up ? Win32.KEYEVENTF_KEYUP : 0 } },
    };
}
