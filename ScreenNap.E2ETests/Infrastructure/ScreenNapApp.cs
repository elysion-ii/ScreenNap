using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScreenNap.E2ETests.Infrastructure;

internal sealed class ScreenNapApp : IDisposable
{
    internal const uint ExitCommand = 9999;
    internal const uint ReleaseAllCommand = 2000;
    internal const uint MonitorCommandBase = 1000;
    internal const string BlackoutClass = "ScreenNap_Blackout";
    internal const string OverlayClass = "ScreenNap_Identify";

    private const string MessageWindowClass = "ScreenNap_MessageWindow";
    private const string MenuWindowClass = "#32768";
    private const string DialogClass = "#32770";
    private const uint TrayIconId = 1;
    private const int MenuTextCapacity = 256;
    private const int S_OK = 0;

    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private readonly bool _ownsLogDirectory;

    private ScreenNapApp(Process process, string logDirectory, bool ownsLogDirectory)
    {
        Process = process;
        LogDirectory = logDirectory;
        _ownsLogDirectory = ownsLogDirectory;
    }

    internal Process Process { get; }
    internal string LogDirectory { get; }
    internal IntPtr MessageWindow { get; private set; }

    internal static ScreenNapApp Start(string? executable = null, string? logDirectory = null)
    {
        bool ownsLogDirectory = logDirectory is null;
        string directory = logDirectory ?? TestAssets.CreateScratchDirectory();
        var startInfo = new ProcessStartInfo(executable ?? TestAssets.ScreenNapExe) { UseShellExecute = false };
        startInfo.Environment["SCREENNAP_LOG_DIR"] = directory;

        Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("ScreenNap did not start");
        var app = new ScreenNapApp(process, directory, ownsLogDirectory);
        try
        {
            app.MessageWindow = Wait.ForWindow(
                () => Desktop.First(Desktop.TopLevelWindows(process.Id, MessageWindowClass)),
                s_timeout,
                "ScreenNap creates its message window");
            Wait.Until(app.TrayIconExists, s_timeout, "ScreenNap adds its tray icon");
            return app;
        }
        catch
        {
            app.Dispose();
            throw;
        }
    }

    internal Process StartSecondInstance()
    {
        var startInfo = new ProcessStartInfo(TestAssets.ScreenNapExe) { UseShellExecute = false };
        startInfo.Environment["SCREENNAP_LOG_DIR"] = LogDirectory;
        return Process.Start(startInfo) ?? throw new InvalidOperationException("The second ScreenNap did not start");
    }

    internal T Measure<T>(Func<T> measurement) => Desktop.MeasuredAs(MessageWindow, measurement);

    internal void Measure(Action action) => Desktop.MeasuredAs(MessageWindow, () =>
    {
        action();
        return true;
    });

    internal IReadOnlyList<RECT> Monitors() => Measure(Desktop.Monitors);

    internal static IntPtr WaitForMessageBox(Process process)
        => Wait.ForWindow(
            () => Desktop.First(Desktop.VisibleTopLevelWindows(process.Id, DialogClass)),
            s_timeout,
            "the process shows a message box");

    internal bool TrayIconExists()
    {
        var identifier = new NOTIFYICONIDENTIFIER
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
            hWnd = MessageWindow,
            uID = TrayIconId,
        };
        return Win32.Shell_NotifyIconGetRect(ref identifier, out _) == S_OK;
    }

    internal IReadOnlyList<MenuEntry> OpenMenu(uint mouseButtonMessage = Win32.WM_RBUTTONUP)
    {
        // Windows grants this only while the test process owns the foreground; the menu opens either way
        _ = Win32.AllowSetForegroundWindow((uint)Process.Id);
        Post(Win32.WM_TRAYICON, 0, (nint)mouseButtonMessage);

        IntPtr menuWindow = Wait.ForWindow(
            () => Desktop.First(Desktop.VisibleTopLevelWindows(Process.Id, MenuWindowClass)),
            s_timeout,
            "ScreenNap opens its context menu");
        IntPtr menu = Win32.SendMessageW(menuWindow, Win32.MN_GETHMENU, 0, 0);
        if (menu == IntPtr.Zero)
            throw new InvalidOperationException("MN_GETHMENU returned no menu");

        IReadOnlyList<MenuEntry> entries = ReadMenu(menu);

        Post(Win32.WM_CANCELMODE, 0, 0);
        Wait.Until(() => Desktop.VisibleTopLevelWindows(Process.Id, MenuWindowClass).Count == 0, s_timeout, "the context menu closes");
        return entries;
    }

    // ScreenNap interprets a menu command against the monitors it listed when the menu opened
    internal void Select(uint commandId)
    {
        OpenMenu();
        Post(Win32.WM_COMMAND, commandId, 0);
    }

    internal void Post(uint message, nuint wParam, nint lParam)
    {
        if (!Win32.PostMessageW(MessageWindow, message, wParam, lParam))
            throw new InvalidOperationException($"PostMessageW failed (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    // Explorer drops every tray icon when it restarts; deleting ScreenNap's icon reproduces that
    internal void RemoveTrayIcon()
    {
        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = MessageWindow,
            uID = TrayIconId,
        };
        if (!Win32.Shell_NotifyIconW(Win32.NIM_DELETE, ref data))
            throw new InvalidOperationException($"Shell_NotifyIconW(NIM_DELETE) failed (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    internal void SendTaskbarCreated()
    {
        uint message = Win32.RegisterWindowMessageW(Win32.TaskbarCreatedMessage);
        if (message == 0)
            throw new InvalidOperationException($"RegisterWindowMessageW failed (Win32 error: {Marshal.GetLastWin32Error()})");
        Post(message, 0, 0);
    }

    // The thread that owns ScreenNap's windows, which runs its message loop
    internal uint UiThreadId => Win32.GetWindowThreadProcessId(MessageWindow, out _);

    internal ThreadSuspension SuspendUiThread() => ThreadSuspension.Suspend(UiThreadId);

    // A thread waiting for messages runs no CPU cycles; a timer or any other message wakes it
    internal ulong UiThreadCycles()
    {
        IntPtr thread = Win32.OpenThread(Win32.THREAD_QUERY_LIMITED_INFORMATION, false, UiThreadId);
        if (thread == IntPtr.Zero)
            throw new InvalidOperationException($"OpenThread failed for ScreenNap's UI thread (Win32 error: {Marshal.GetLastWin32Error()})");
        try
        {
            if (!Win32.QueryThreadCycleTime(thread, out ulong cycles))
                throw new InvalidOperationException($"QueryThreadCycleTime failed (Win32 error: {Marshal.GetLastWin32Error()})");
            return cycles;
        }
        finally
        {
            _ = Win32.CloseHandle(thread);
        }
    }

    // Waits until ScreenNap's UI thread has run no cycle for a whole quiet period
    internal void WaitForIdleUiThread(TimeSpan quietPeriod, TimeSpan timeout)
    {
        ulong last = UiThreadCycles();
        Stopwatch unchanged = Stopwatch.StartNew();
        Wait.Until(() =>
        {
            ulong now = UiThreadCycles();
            if (now != last)
            {
                last = now;
                unchanged.Restart();
            }
            return unchanged.Elapsed >= quietPeriod;
        }, timeout, $"ScreenNap's UI thread stays idle for {quietPeriod.TotalSeconds:0.#}s");
    }

    internal IReadOnlyList<IntPtr> BlackoutWindows() => Desktop.VisibleTopLevelWindows(Process.Id, BlackoutClass);

    internal IReadOnlyList<IntPtr> Overlays() => Desktop.VisibleTopLevelWindows(Process.Id, OverlayClass);

    internal IntPtr WaitForBlackoutCovering(RECT monitor)
        => Wait.ForWindow(
            () => BlackoutWindows().FirstOrDefault(window => SameRect(Measure(() => Desktop.Bounds(window)), monitor)),
            s_timeout,
            $"a blackout window covers the monitor at {monitor.Left},{monitor.Top}");

    internal void WaitForBlackoutCount(int count)
        => Wait.Until(() => BlackoutWindows().Count == count, s_timeout, $"{count} blackout window(s) are live");

    internal void Exit()
    {
        Select(ExitCommand);
        if (!Process.WaitForExit(s_timeout))
            throw new TimeoutException("ScreenNap did not exit after the Exit command");
    }

    internal IReadOnlyList<string> LogLines()
    {
        var lines = new List<string>();
        foreach (string file in Directory.GetFiles(LogDirectory, "ScreenNap_*.log").Order(StringComparer.Ordinal))
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is string line)
                lines.Add(line);
        }
        return lines;
    }

    internal void WaitForLog(string fragment)
        => Wait.Until(() => LogLines().Any(line => line.Contains(fragment, StringComparison.Ordinal)), s_timeout, $"the log contains \"{fragment}\"");

    internal static bool SameRect(RECT a, RECT b)
        => a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom;

    public void Dispose()
    {
        if (!Process.HasExited)
        {
            Process.Kill(entireProcessTree: true);
            if (!Process.WaitForExit(s_timeout))
                throw new TimeoutException($"ScreenNap (process {Process.Id}) did not stop; stop it before the next run");
        }
        Process.Dispose();
        if (_ownsLogDirectory)
            TestAssets.DeleteScratchDirectory(LogDirectory);
    }

    private static List<MenuEntry> ReadMenu(IntPtr menu)
    {
        int count = Win32.GetMenuItemCount(menu);
        if (count < 0)
            throw new InvalidOperationException($"GetMenuItemCount failed (Win32 error: {Marshal.GetLastWin32Error()})");

        var entries = new List<MenuEntry>(count);
        for (int position = 0; position < count; position++)
        {
            uint state = Win32.GetMenuState(menu, (uint)position, Win32.MF_BYPOSITION);
            entries.Add(new MenuEntry(
                Win32.GetMenuItemID(menu, position),
                MenuText(menu, position),
                (state & Win32.MF_SEPARATOR) != 0,
                (state & Win32.MF_CHECKED) != 0,
                (state & (Win32.MF_GRAYED | Win32.MF_DISABLED)) != 0));
        }
        return entries;
    }

    private static unsafe string MenuText(IntPtr menu, int position)
    {
        char* buffer = stackalloc char[MenuTextCapacity];
        int length = Win32.GetMenuStringW(menu, (uint)position, buffer, MenuTextCapacity, Win32.MF_BYPOSITION);
        return new string(buffer, 0, length);
    }
}
