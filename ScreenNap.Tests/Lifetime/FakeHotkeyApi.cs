using ScreenNap.Hotkeys;

namespace ScreenNap.Tests.Lifetime;

// Stands in for global hotkey registration so a test never takes a real hotkey
internal sealed class FakeHotkeyApi : IHotkeyApi
{
    public int LastError => 0;

    // The virtual key of each registered hotkey, by hotkey ID
    internal Dictionary<int, uint> Registered { get; } = [];

    public bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey) => Registered.TryAdd(id, virtualKey);

    public bool UnregisterHotKey(IntPtr hwnd, int id) => Registered.Remove(id);

    // The ID ScreenNap registered for the hotkey of a digit, as WM_HOTKEY carries it
    internal nuint IdFor(int digit) => (nuint)Registered.Single(hotkey => hotkey.Value == '0' + (uint)digit).Key;

    public bool IsKeyDown(uint virtualKey) => false;
}
