using ScreenNap.Blackout;

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
            existing.Destroy();
            // Removal from dictionary happens in OnBlackoutDestroyed callback
        }
        else
        {
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
