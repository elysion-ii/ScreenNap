namespace ScreenNap.Hotkeys;

// The hotkey and keyboard functions the hotkeys call, behind one interface so a unit test
// never registers a real global hotkey (docs/rules/ScreenNap.md, Win32 and P/Invoke)
internal interface IHotkeyApi
{
    // The Win32 error code the most recent call left
    int LastError { get; }

    bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

    bool UnregisterHotKey(IntPtr hwnd, int id);

    bool IsKeyDown(uint virtualKey);
}
