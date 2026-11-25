using ScreenNap.Hotkeys;

namespace ScreenNap.Tests.Lifetime;

// Stands in for global hotkey registration so a test never takes a real hotkey
internal sealed class FakeHotkeyApi : IHotkeyApi
{
    public int LastError => 0;

    internal HashSet<int> Registered { get; } = [];

    public bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey) => Registered.Add(id);

    public bool UnregisterHotKey(IntPtr hwnd, int id) => Registered.Remove(id);

    public bool IsKeyDown(uint virtualKey) => false;
}
