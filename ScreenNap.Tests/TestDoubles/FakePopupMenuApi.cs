using ScreenNap.Native;
using ScreenNap.Tray;

namespace ScreenNap.Tests.TestDoubles;

// Stands in for Windows' menu functions: it records the menu the code under test builds and
// where it opens it, and fails the calls a test names
internal sealed class FakePopupMenuApi : IPopupMenuApi
{
    internal const int FailureError = 1401;
    private static readonly IntPtr s_menu = new(0x700);

    public int LastError { get; private set; }

    internal POINT? CursorPosition { get; set; } = new POINT { X = 640, Y = 480 };
    internal bool DestroyMenuFails { get; set; }
    internal List<(uint Flags, nuint ItemId, string? Text)> Items { get; } = [];
    internal List<(int X, int Y)> OpenedAt { get; } = [];
    internal int DestroyedMenus { get; private set; }

    public IntPtr CreatePopupMenu() => s_menu;

    public bool AppendMenu(IntPtr menu, uint flags, nuint itemId, string? text)
    {
        Items.Add((flags, itemId, text));
        return true;
    }

    public bool SetForegroundWindow(IntPtr hwnd) => true;

    public bool GetCursorPos(out POINT point)
    {
        point = CursorPosition ?? new POINT { X = -1, Y = -1 };
        if (CursorPosition is null)
            LastError = FailureError;
        return CursorPosition is not null;
    }

    public bool TrackPopupMenu(IntPtr menu, uint flags, int x, int y, IntPtr hwnd)
    {
        OpenedAt.Add((x, y));
        return true;
    }

    public bool PostMessage(IntPtr hwnd, uint message, nuint wParam, nint lParam) => true;

    public bool DestroyMenu(IntPtr menu)
    {
        if (DestroyMenuFails)
        {
            LastError = FailureError;
            return false;
        }
        DestroyedMenus++;
        return true;
    }
}
