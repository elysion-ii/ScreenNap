using ScreenNap.Native;

namespace ScreenNap.Tests.TestDoubles;

// Stands in for Windows' window functions: it keeps the windows, classes, and timers the code
// under test creates, delivers messages to the registered window procedures as Windows does,
// and fails the calls a test names
internal sealed class FakeWindowApi : IWindowApi
{
    internal const int FailureError = 1400;
    internal static readonly IntPtr ModuleHandle = new(0x400000);

    private readonly Dictionary<string, WNDPROC> _classes = new(StringComparer.Ordinal);
    private long _nextHandle = 0x1000;

    public int LastError { get; private set; }

    internal Dictionary<IntPtr, FakeWindow> Windows { get; } = [];
    // The interval of each running timer; a test tells timers apart by window and interval,
    // which is what Windows knows of them
    internal Dictionary<(IntPtr Window, nuint TimerId), uint> Timers { get; } = [];
    internal List<(IntPtr Window, IntPtr InsertAfter, int X, int Y, int Width, int Height, uint Flags)> Positions { get; } = [];
    internal List<string> UnregisteredClasses { get; } = [];

    internal bool ModuleHandleFails { get; set; }
    internal HashSet<string> ClassesFailingRegistration { get; } = new(StringComparer.Ordinal);
    internal HashSet<string> ClassesFailingCreation { get; } = new(StringComparer.Ordinal);
    internal HashSet<string> ClassesFailingDestruction { get; } = new(StringComparer.Ordinal);
    internal HashSet<string> ClassesFailingUnregistration { get; } = new(StringComparer.Ordinal);
    // Whether starting or stopping the timer of an ID and interval fails
    internal Func<nuint, uint, bool> TimerStartFails { get; set; } = (_, _) => false;
    internal Func<nuint, uint, bool> TimerStopFails { get; set; } = (_, _) => false;
    internal bool SetWindowPosFails { get; set; }
    internal bool UpdateWindowFails { get; set; }
    // Zero stands for a DPI that cannot be read
    internal uint Dpi { get; set; } = WindowStyles.USER_DEFAULT_SCREEN_DPI;

    internal IEnumerable<FakeWindow> LiveWindows(string className)
        => Windows.Values.Where(window => window.IsAlive && window.ClassName == className);

    internal nint Send(IntPtr hwnd, uint message, nuint wParam = 0, nint lParam = 0)
        => _classes[Windows[hwnd].ClassName](hwnd, message, wParam, lParam);

    // The ID of the window's running timer whose interval matches
    private nuint TimerOf(IntPtr hwnd, Func<uint, bool>? interval)
        => Timers.Single(timer => timer.Key.Window == hwnd && (interval is null || interval(timer.Value))).Key.TimerId;

    // Delivers WM_TIMER for the window's running timer whose interval matches
    internal nint Tick(IntPtr hwnd, Func<uint, bool>? interval = null)
        => Send(hwnd, WindowStyles.WM_TIMER, TimerOf(hwnd, interval));

    public IntPtr GetModuleHandle() => ModuleHandleFails ? Fail(IntPtr.Zero) : ModuleHandle;

    public bool RegisterClass(string className, uint classStyle, WNDPROC wndProc, IntPtr hInstance, IntPtr background, IntPtr cursor)
    {
        if (ClassesFailingRegistration.Contains(className))
            return Fail(false);
        _classes[className] = wndProc;
        return true;
    }

    public bool UnregisterClass(string className, IntPtr hInstance)
    {
        if (ClassesFailingUnregistration.Contains(className))
            return Fail(false);
        UnregisteredClasses.Add(className);
        return _classes.Remove(className);
    }

    public IntPtr CreateWindow(uint extendedStyle, string className, string title, uint style, int x, int y, int width, int height, IntPtr hInstance)
    {
        if (ClassesFailingCreation.Contains(className) || !_classes.ContainsKey(className))
            return Fail(IntPtr.Zero);

        var handle = new IntPtr(_nextHandle++);
        Windows[handle] = new FakeWindow(className, extendedStyle, x, y, width, height);
        return handle;
    }

    public bool DestroyWindow(IntPtr hwnd)
    {
        if (!Windows.TryGetValue(hwnd, out FakeWindow? window) || !window.IsAlive || ClassesFailingDestruction.Contains(window.ClassName))
            return Fail(false);

        Send(hwnd, WindowStyles.WM_DESTROY);
        window.IsAlive = false;
        foreach ((IntPtr Window, nuint TimerId) timer in Timers.Keys.Where(timer => timer.Window == hwnd).ToList())
            Timers.Remove(timer);
        return true;
    }

    public bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags)
    {
        if (SetWindowPosFails)
            return Fail(false);
        Positions.Add((hwnd, insertAfter, x, y, width, height, flags));
        if ((flags & WindowStyles.SWP_SHOWWINDOW) != 0)
            Windows[hwnd].IsVisible = true;
        return true;
    }

    // Windows sends WM_PAINT to the window's procedure before UpdateWindow returns
    public bool UpdateWindow(IntPtr hwnd)
    {
        if (UpdateWindowFails)
            return Fail(false);
        Send(hwnd, WindowStyles.WM_PAINT);
        return true;
    }

    public bool SetTimer(IntPtr hwnd, nuint timerId, uint intervalMs)
    {
        if (TimerStartFails(timerId, intervalMs))
            return Fail(false);
        Timers[(hwnd, timerId)] = intervalMs;
        return true;
    }

    public bool KillTimer(IntPtr hwnd, nuint timerId)
    {
        if (!Timers.TryGetValue((hwnd, timerId), out uint intervalMs) || TimerStopFails(timerId, intervalMs))
            return Fail(false);
        return Timers.Remove((hwnd, timerId));
    }

    public uint GetDpiForWindow(IntPtr hwnd) => Dpi;

    // Windows' DefWindowProc destroys a window asked to close
    public nint DefWindowProc(IntPtr hwnd, uint message, nuint wParam, nint lParam)
    {
        if (message == WindowStyles.WM_CLOSE)
            DestroyWindow(hwnd);
        return 0;
    }

    public IntPtr GetStockObject(int stockObject) => new(0x10 + stockObject);

    public IntPtr LoadSystemCursor(int cursorId) => new(cursorId);

    public void SetCursor(IntPtr cursor)
    {
    }

    private T Fail<T>(T result)
    {
        LastError = FailureError;
        return result;
    }

    internal sealed class FakeWindow(string className, uint extendedStyle, int x, int y, int width, int height)
    {
        internal string ClassName { get; } = className;
        internal uint ExtendedStyle { get; } = extendedStyle;
        internal (int X, int Y, int Width, int Height) CreatedBounds { get; } = (x, y, width, height);
        internal bool IsAlive { get; set; } = true;
        internal bool IsVisible { get; set; }
    }
}
