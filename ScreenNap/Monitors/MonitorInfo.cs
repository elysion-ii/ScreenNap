using ScreenNap.Native;

namespace ScreenNap.Monitors;

internal sealed record MonitorInfo(string DevicePath, string FriendlyName, RECT Bounds, bool IsPrimary, MonitorIdentity Identity);
