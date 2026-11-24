---
status: active
created: 2026-11-24
---

# ScreenNap Code Map

Feature folders and homes of cross-cutting mechanisms in ScreenNap (`docs/rules/documentation.md`, Code Map; `docs/rules/standard.md`, One Home per Piece of Knowledge). Keep this map current in the same change whenever a feature folder or shared mechanism home is added, moved, or removed.

| Path | Knowledge |
|---|---|
| `ScreenNap/App/` | Application orchestration: the tray icon, the context menu, the blackout lifecycle, global hotkeys, monitor identification, and display-change handling |
| `ScreenNap/Blackout/` | The blackout window |
| `ScreenNap/Core/` | Shared contracts, domain types, and pure decision logic |
| `ScreenNap/Native/` | The P/Invoke declarations, one class per Win32 DLL (`User32`, `Gdi32`, `Shell32`, `Kernel32`) plus `DisplayConfig` for the display-configuration functions of user32.dll, and the native structures and delegates they take, one file per type |
| `ScreenNap/Native/WindowStyles.cs` | Win32 constants |
| `ScreenNap/Native/WindowClass.cs` | How a window class is registered once and unregistered at shutdown |
| `ScreenNap/Logging/Logger.cs` | Which directory log files go to, how a log line is formatted and written, how a Win32 error code is appended to a log message, and which old log files are deleted |
| `ScreenNap/Logging/InvariantLogMessage.cs` | How the values interpolated into a log message are formatted independently of the user's culture |
| `ScreenNap/Resources/Strings.cs` | How a localized user-facing string is looked up, and what is shown when the lookup fails |
| `ScreenNap/Core/FailureNotices.cs` | The wording of every failure notification shown to the user |
| `ScreenNap/Core/IUserNotifier.cs` | How an operation's failure reaches the user |
| `ScreenNap/App/AppVersion.cs` | How the product version is read and shown as `ScreenNap X.Y.Z` |
| `ScreenNap/App/MonitorEnumerator.cs` | How the active monitors are enumerated with their bounds, primary state, name, and identity |
| `ScreenNap/Core/MonitorIdentity.cs` | What identifies a physical monitor across display-path changes |
| `ScreenNap.Tests/TestDoubles/` | Unit-test substitutes for blackout windows, identification overlays, and user notifications, and how a unit test sets the culture and restores it (`CultureScope`) |
| `ScreenNap.E2ETests/Infrastructure/ScreenNapApp.cs` | How an end-to-end test starts the built ScreenNap with a log directory of its own, drives its message window and menu, and stops it |
| `ScreenNap.E2ETests/Infrastructure/TestAssets.cs` | Where an end-to-end test finds the built executable, the repository root, and the release assets, and how it creates and removes its scratch directories |
| `ScreenNap.E2ETests/Infrastructure/Wait.cs` | How an end-to-end test waits for an observable condition with a time limit |
| `ScreenNap.E2ETests/Infrastructure/Desktop.cs` | How an end-to-end test reads windows, monitors, pixels, and the cursor in the DPI awareness of the window it measures |
| `ScreenNap.E2ETests/Infrastructure/Keyboard.cs` | How an end-to-end test presses a global hotkey and probes whether one is held |
| `ScreenNap.E2ETests/Infrastructure/Win32.cs` | The Win32 function declarations and constants the end-to-end tests use; the native structures and delegates they take sit beside it, one file per type |
