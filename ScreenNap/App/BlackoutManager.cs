using ScreenNap.Blackout;
using ScreenNap.Logging;

namespace ScreenNap.App;

internal sealed class BlackoutManager
{
    private readonly Dictionary<string, BlackoutWindow> _windows = new(StringComparer.Ordinal);

    internal int ActiveCount => _windows.Count;
    internal event Action? ActiveCountChanged;

    internal bool IsActive(string devicePath)
    {
        return _windows.ContainsKey(devicePath);
    }

    internal void Toggle(MonitorInfo monitor)
    {
        if (_windows.TryGetValue(monitor.DevicePath, out BlackoutWindow? existing))
        {
            Logger.Info($"Blackout toggled off: {monitor.FriendlyName} ({monitor.DevicePath})");
            existing.Destroy();
            // Removal from dictionary happens in OnBlackoutDestroyed callback
        }
        else
        {
            Logger.Info($"Blackout toggled on: {monitor.FriendlyName} ({monitor.DevicePath})");
            var window = new BlackoutWindow(monitor.DevicePath, monitor.Bounds);
            if (window.Handle == IntPtr.Zero)
                return;

            window.OnDestroyed = OnBlackoutDestroyed;
            _windows[monitor.DevicePath] = window;
            ActiveCountChanged?.Invoke();
        }
    }

    internal void ReleaseAll()
    {
        if (_windows.Count > 0)
            Logger.Info($"Releasing all blackout windows ({_windows.Count})");

        // Snapshot keys to avoid modification during enumeration
        var paths = _windows.Keys.ToList();
        foreach (string path in paths)
        {
            if (_windows.TryGetValue(path, out BlackoutWindow? window))
                window.Destroy();
        }
    }

    private void OnBlackoutDestroyed(BlackoutWindow window)
    {
        _windows.Remove(window.DevicePath);
        ActiveCountChanged?.Invoke();
    }
}
