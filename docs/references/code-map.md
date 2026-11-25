---
status: active
created: 2026-11-24
---

# ScreenNap Code Map

Feature folders and homes of cross-cutting mechanisms in ScreenNap (`docs/rules/documentation.md`, Code Map; `docs/rules/standard.md`, One Home per Piece of Knowledge). Keep this map current in the same change whenever a feature folder or shared mechanism home is added, moved, or removed.

| Path | Knowledge |
|---|---|
| `ScreenNap/Lifetime/` | Feature: startup, the single instance, the hidden message window and which feature each of its messages goes to, and shutdown |
| `ScreenNap/Tray/` | Feature: the tray icon and its tooltip, tray notifications, and the context menu — its items, the text of each monitor's item, and what each command does |
| `ScreenNap/Blackout/` | Feature: blackout windows, which monitors are blacked out and which should be restored, waiting for display changes to settle and restoring blackouts after them, and hiding the idle cursor |
| `ScreenNap/Hotkeys/` | Feature: the global hotkeys, their registration, and the action each one selects |
| `ScreenNap/Identify/` | Feature: the monitor-identification overlays and which failures showing them reports |
| `ScreenNap/Native/` | The P/Invoke declarations, one class per Win32 DLL (`User32`, `Gdi32`, `Shell32`, `Kernel32`) plus `DisplayConfig` for the display-configuration functions of user32.dll, and the native structures and delegates they take, one file per type |
| `ScreenNap/app.manifest` | That ScreenNap is aware of each monitor's DPI, so it works in physical pixels |
| `ScreenNap/Native/WindowStyles.cs` | Win32 constants |
| `ScreenNap/Native/IWindowApi.cs` | The window functions several features call (creation, destruction, position, timers, DPI), behind one interface so unit tests can make them fail; `Win32WindowApi.cs` beside it is the Win32 implementation. Each feature's own Win32 calls sit behind its own `I*Api` interface in its folder |
| `ScreenNap/Native/WindowClass.cs` | How a window class is registered once, keeps its window procedure's delegate alive, and is unregistered at shutdown |
| `ScreenNap/Logging/Logger.cs` | Which directory log files go to and the notice about that choice, how a log line is formatted and written, how a Win32 error code is appended to a log message, and which old log files are deleted |
| `ScreenNap/Logging/LogCapture.cs` | How a unit test takes the entries the code it calls logs, instead of the log file |
| `ScreenNap/Logging/InvariantLogMessage.cs` | How the values interpolated into a log message are formatted independently of the user's culture |
| `ScreenNap/Resources/Strings.cs` | How a localized user-facing string is looked up, and what is shown when the lookup fails |
| `ScreenNap/Notifications/FailureNotices.cs` | The wording of every failure notification shown to the user |
| `ScreenNap/Notifications/IUserNotifier.cs` | How an operation's failure reaches the user |
| `ScreenNap/Versioning/AppVersion.cs` | How the product version is read and shown as `ScreenNap X.Y.Z` |
| `ScreenNap/Monitors/MonitorEnumerator.cs` | How the active monitors are enumerated with their bounds, primary state, name, and identity, and which monitors a failed enumeration still yields |
| `ScreenNap/Monitors/MonitorNameResolver.cs` | The order in which a monitor's friendly name falls back |
| `ScreenNap/Monitors/MonitorIdentity.cs` | What identifies a physical monitor across display-path changes |
| `ScreenNap.Tests/<feature>/` | A feature's unit tests and the test doubles only its tests use (e.g., `Blackout/FakeBlackoutWindowFactory.cs`, `Identify/FakeIdentifyOverlays.cs`) |
| `ScreenNap.Tests/TestDoubles/` | Test doubles several features' tests use: how a unit test captures user notifications (`FakeUserNotifier`), sets the culture and restores it (`CultureScope`), checks a logged entry (`LogAssert`), and stands in for the window, tray, and menu functions while failing the calls it names (`FakeWindowApi`, `FakeTrayIconApi`, `FakePopupMenuApi`) |
| `ScreenNap.E2ETests/Infrastructure/ScreenNapApp.cs` | How an end-to-end test starts the built ScreenNap with a log directory of its own, drives its message window and menu, measures and suspends its UI thread, and stops it |
| `ScreenNap.E2ETests/Infrastructure/SetupWizard.cs` | How an end-to-end test drives the interactive installer wizard through its controls |
| `ScreenNap.E2ETests/Infrastructure/ThreadSuspension.cs` | How an end-to-end test keeps a thread of another process from answering messages |
| `ScreenNap.E2ETests/Infrastructure/TestAssets.cs` | Where an end-to-end test finds the built executable, the repository root, and the release assets, and how it creates and removes its scratch directories |
| `ScreenNap.E2ETests/Infrastructure/Wait.cs` | How an end-to-end test waits for an observable condition with a time limit |
| `ScreenNap.E2ETests/Infrastructure/Desktop.cs` | How an end-to-end test reads windows, monitors, pixels, and the cursor in the DPI awareness of the window it measures |
| `ScreenNap.E2ETests/Infrastructure/Keyboard.cs` | How an end-to-end test presses a global hotkey and probes whether one is held |
| `ScreenNap.E2ETests/Infrastructure/Win32.cs` | The Win32 function declarations and constants the end-to-end tests use; the native structures and delegates they take sit beside it, one file per type |
