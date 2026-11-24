namespace ScreenNap.E2ETests.Infrastructure;

internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, nint dwData);
