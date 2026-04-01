# Implement Logging System

## Context

Code review identified multiple P/Invoke calls with unchecked return values and no visibility into runtime behavior. A lightweight file-based logger will enable diagnostics without adding external dependencies.

## Design Decisions

| Item | Decision |
|------|----------|
| Output | `%LocalAppData%/ScreenNap/Logs/` |
| Levels | INFO / WARN / ERROR |
| Rotation | Daily — `ScreenNap_yyyyMMdd.log` |
| Retention | 7 days (purge on startup) |
| Format | `yyyy-MM-dd HH:mm:ss.fff [LEVEL] message` |
| Dependencies | None (File.AppendAllText, no NuGet) |

## New File

### `ScreenNap/Logging/Logger.cs`

Static class with:
- `Initialize()` — create log directory, purge old logs, log "Application started"
- `Info(string message)` / `Warn(string message)` / `Error(string message)`
- Thread-safe via `lock`
- Uses `File.AppendAllText` (simple, no handle management)
- `PurgeOldLogs()` — delete `ScreenNap_*.log` files older than 7 days

Dependency position (accessible from all layers):
```
Program.cs → App/     → Logging/
           → Blackout/ → Logging/
           → Logging/
```

## Modified Files — Logging Integration

### `Program.cs`
- Call `Logger.Initialize()` early in Main (after mutex check)
- INFO: "Application started", "Application exiting"
- ERROR: RegisterClassExW / CreateWindowExW failure + GetLastWin32Error

### `TrayIcon.cs`
- INFO: "Tray icon created", "Tray icon removed"
- ERROR: Shell_NotifyIcon NIM_ADD failure

### `BlackoutWindow.cs`
- INFO: "Blackout window created: {devicePath} ({bounds})", "Blackout window destroyed: {devicePath}"
- WARN: Tooltip creation failure
- ERROR: RegisterClassExW / CreateWindowExW failure + GetLastWin32Error

### `BlackoutManager.cs`
- INFO: "Blackout toggled on/off: {friendlyName} ({devicePath})", "Releasing all blackout windows ({count})"

### `MonitorEnumerator.cs`
- INFO: Enumeration summary — count + per-monitor line (friendly name, device path, bounds, primary flag)

### `ContextMenu.cs`
- WARN: CreatePopupMenu failure

## Log Output Example

```
2026-04-01 12:34:56.789 [INFO] Application started
2026-04-01 12:34:56.800 [INFO] Tray icon created
2026-04-01 12:35:00.100 [INFO] Monitors enumerated: 2 found
2026-04-01 12:35:00.101 [INFO]   #1 "DELL U2722D" (0,0 2560x1440) Primary \\.\DISPLAY1
2026-04-01 12:35:00.102 [INFO]   #2 "LG 27GN950" (2560,0 3840x1440) \\.\DISPLAY2
2026-04-01 12:35:10.500 [INFO] Blackout toggled on: LG 27GN950 (\\.\DISPLAY2)
2026-04-01 12:35:10.501 [INFO] Blackout window created: \\.\DISPLAY2 (2560,0 3840x1440)
2026-04-01 12:40:20.300 [INFO] Blackout window destroyed: \\.\DISPLAY2
2026-04-01 12:40:20.301 [INFO] Blackout toggled off: LG 27GN950 (\\.\DISPLAY2)
2026-04-01 12:50:00.000 [INFO] Application exiting
```

## Verification

```bash
dotnet build ScreenNap/ScreenNap.csproj -c Release
```
