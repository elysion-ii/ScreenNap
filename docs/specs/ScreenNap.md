---
status: active
created: 2026-04-01
---

# ScreenNap Specification

This document specifies the externally observable behavior of ScreenNap.

## Baseline and Precedence

This revision was reconstructed from the current implementation. For this reconstruction,
the implementation is authoritative wherever the previous specification disagreed with
it. After this baseline, behavior changes follow the repository's spec-first workflow.
For table-driven decisions already covered by tests, precedence remains tests, then this
document, then implementation.

## Purpose

ScreenNap protects OLED monitors from burn-in by covering selected monitors with pure
black windows. It provides the visual effect of turning OLED pixels off without powering
off a monitor or changing the Windows display configuration.

## Scope

This specification covers the ScreenNap desktop application, system-tray controls,
global hotkeys, monitor-identification overlays, blackout state, display-change recovery,
localized user-visible text, logging, portable distribution, and installer behavior.

## Users and External Systems

- A Windows desktop user controls ScreenNap through the system tray, global hotkeys, and blackout-window mouse actions.
- Windows provides monitor topology, monitor bounds, display names, stable monitor identity when available, input events, and the current UI culture.
- The local file system stores daily application logs.
- The installer can create user-level shortcuts and a current-user Windows-startup entry.

## Required Behavior

### Application startup and lifetime

- ScreenNap must start without displaying a main window and must remain resident through a system-tray icon and registered global hotkeys.
- Only one ScreenNap process may run at a time. A second launch must display a localized informational message and exit without affecting the running process.
- On normal shutdown, ScreenNap must unregister its global hotkeys, remove its tray icon, close all blackout and identification windows, and release its window resources.
- Blackout choices exist only for the current process lifetime; ScreenNap must not persist blackout state or user configuration across restarts.

### Monitor discovery, names, and numbering

- ScreenNap must enumerate the currently active monitors whenever the tray menu opens, whenever a monitor hotkey is handled, whenever identification overlays are requested, and after a display configuration change settles.
- Monitors must receive one-based numbers in the order returned by the current enumeration. These numbers drive menu accelerators, monitor hotkeys, and identification overlays.
- A monitor name must use the preferred Windows friendly name when available, then a secondary Windows display-device name, then the display device path with a leading `\\.\` prefix removed.
- Each monitor record must include its full monitor bounds, resolution, primary-monitor state, and stable physical identity when Windows supplies that identity.
- Failure to obtain information for one monitor must not prevent other monitors from being returned.

### Tray icon and tooltip

- With no live blackout windows, ScreenNap must use the normal tray icon and the localized tooltip `ScreenNap`.
- With one or more live blackout windows, ScreenNap must use the active tray icon and a localized tooltip containing the live blackout count.
- The tooltip must not exceed the Windows notification-icon limit of 127 characters.
- Either a left-button release or a right-button release on the tray icon must open the context menu at the current cursor position.

### Context menu

- The menu must list every currently enumerated monitor before its action items.
- Each monitor item must contain its one-based accelerator number, friendly name, resolution, localized primary marker when applicable, and localized active marker when blacked out.
- A live blackout monitor item must be checked.
- Selecting a monitor item must toggle the blackout for the monitor represented by the menu snapshot.
- `Release All` must appear only while at least one live blackout window exists and must attempt to close every live blackout window.
- `Exit` must always appear, including when no monitor is available, and must terminate the application through normal shutdown.

### Blackout activation and state

- Activating a monitor must create one blackout window for that monitor. A failed window creation must leave the monitor inactive.
- Selecting an active monitor again must attempt to close its blackout. If closing fails, the monitor must remain active and retain its desired blackout state.
- Multiple monitors may be blacked out simultaneously and controlled independently.
- `Release All` must attempt to close every live blackout and clear restoration intent for every successfully closed window. A window that cannot be closed must remain active and, when it has a stable physical identity, retain only its own desired state.
- The tray state must be updated whenever the live blackout count changes.

### Blackout window behavior

- A blackout window must cover the selected monitor's full bounds, including the taskbar area, with pure black.
- The window must remain above ordinary windows without taking keyboard focus or appearing in the taskbar or Alt+Tab.
- ScreenNap must periodically restore the window's topmost position without moving, resizing, or activating it.
- A left-button double-click or a right-button release anywhere on the blackout window must dismiss it and clear the user's desired blackout state for that physical monitor.
- The cursor must hide after 10 seconds without a position change over the blackout window. A later position change must show the cursor and restart the inactivity period.

### Display configuration changes and restoration

- ScreenNap must wait approximately 500 milliseconds after a display-change notification before reconciling blackout state, so a burst of topology changes is handled as one settled configuration.
- If Windows destroys a blackout window without a user dismissal, ScreenNap must retain the desired blackout state when the monitor has a stable physical identity.
- After reconciliation, ScreenNap must recreate a missing desired blackout on the same physical monitor when that monitor is present, even if its display device path changed.
- A disconnected desired monitor must remain pending and must be restored after a later display change reconnects the same physical monitor.
- A monitor without a stable physical identity must not be restored automatically after its blackout window disappears.
- Reconciliation must remove stale window state, must not duplicate a live blackout, and must ignore monitors that are not desired.

### Global hotkeys

- ScreenNap must register `Ctrl+Shift+Alt+1` through `Ctrl+Shift+Alt+9` as non-repeating global hotkeys.
- A numbered hotkey must toggle the corresponding one-based monitor number in the current enumeration; pressing a number for which no monitor exists must do nothing.
- ScreenNap must register `Ctrl+Shift+Alt+0` as a non-repeating global hotkey that toggles monitor-identification overlays.
- Failure to register one hotkey must be logged and must not prevent registration or use of the remaining hotkeys.

### Monitor-identification overlays

- When no identification overlay is visible, `Ctrl+Shift+Alt+0` must enumerate the current monitors and show a centered overlay on each monitor.
- Each overlay must be a 200 by 150 pixel black, topmost, non-activating tool window containing the monitor's one-based number in large white text.
- Identification overlays must remain above ordinary windows. When the overlay for monitor 1 is created successfully, it must close all identification overlays automatically after approximately three seconds.
- Invoking `Ctrl+Shift+Alt+0` while overlays are visible must close all overlays immediately.
- Failure to create one monitor's overlay must not prevent overlays from being created for other monitors.

### Localization

- ScreenNap must provide English default resources and Japanese resources selected through the current Windows UI culture.
- Localized text must cover menu actions, primary and active monitor markers, tray tooltips, and the duplicate-instance notification.
- If a resource lookup fails, ScreenNap must use its built-in English fallback text.
- Monitor numbers, hotkey combinations, log messages, and identification-overlay numerals are language-independent.

### Logging

- ScreenNap must write logs under `%LocalAppData%\ScreenNap\Logs\` when that directory can be created.
- Log files must use the local date and the name `ScreenNap_yyyyMMdd.log`.
- Each line must use the culture-independent format `yyyy-MM-dd HH:mm:ss.fff [LEVEL] message`.
- Log levels must be `INFO`, `WARN`, and `ERROR`.
- A successful startup must log the three-component application version; application lifetime, monitor enumeration, blackout transitions, and display reconciliation must also produce operational log entries.
- At startup, ScreenNap must make a best-effort attempt to delete matching log files whose last-write time is older than seven days.
- Directory creation, log writes, retention scans, and log deletion failures must not terminate the application.

### Distribution and installer

- The portable distribution must be a self-contained, single-file `ScreenNap.exe` that does not require a separately installed .NET runtime.
- The installer must target x64-compatible Windows and perform a user-level installation without requiring administrator privileges.
- The installer must install `ScreenNap.exe`, create a Start Menu shortcut when that task is selected, and offer optional desktop-shortcut and current-user Windows-startup tasks.
- The interactive installer must offer to launch ScreenNap after installation.
- The installer user interface is English.

## Inputs and Outputs

| Input | Observable output |
|---|---|
| Tray-icon left or right button release | Current-monitor context menu |
| Monitor menu item | Blackout toggled for that menu entry |
| `Release All` | All closable blackouts dismissed |
| `Exit` | Normal application shutdown |
| `Ctrl+Shift+Alt+1` through `9` | Corresponding current monitor toggled when present |
| `Ctrl+Shift+Alt+0` | Identification overlays shown or dismissed |
| Blackout-window left double-click or right-button release | That blackout dismissed by the user |
| Cursor position changes over a blackout | Cursor restored and inactivity timer restarted |
| Display configuration change | Desired blackout state reconciled after debounce |
| Application and operational events | Daily English log entries when logging is available |

## Error Behavior

- A failure during critical process or hidden-window initialization must be logged when logging is available and must stop startup.
- Failure to create a blackout window must not add live or desired blackout state.
- Failure to destroy a blackout window must preserve the corresponding live and desired state.
- Detected failures in tray, menu, hotkey registration, monitor enumeration, blackout topmost maintenance, or identification-window creation must be logged at an appropriate level and must allow unaffected operations to continue.
- Identification-overlay timers, topmost maintenance, dismissal, and cleanup are best-effort operations and do not notify the user when they fail.
- Logging failure must be silent to the user and must never terminate ScreenNap.

## Invariants

- ScreenNap must not change monitor power, display layout, orientation, resolution, or primary-monitor selection.
- At most one live blackout window may exist for a given display device path.
- Every live blackout count change must be reflected in the tray state.
- A user dismissal must clear restoration intent; an operating-system disappearance may retain restoration intent only through a non-default stable monitor identity.
- Normal shutdown must clear all in-memory blackout intent and close all ScreenNap-owned visible windows.

## Non-Functional Requirements

- ScreenNap must operate on x64 Windows systems supported by its self-contained runtime.
- When no blackout or identification overlay is active, ScreenNap must remain idle until it receives user or display events; periodic maintenance is limited to live blackout windows and visible identification overlays.
- User-facing blackout and identification windows must not steal focus.
- The distributed application must have no external runtime package dependency.

## Out of Scope

- Powering monitors off or changing Windows display configuration
- Scheduled blackout or whole-application idle detection
- A clock or other content rendered on a blackout window
- DDC/CI monitor control
- User-configurable hotkeys, cursor timeout, identification duration, or topmost intervals
- Persisting blackout selections or application settings across process restarts

## Requirements-to-Tests Mapping

| Behavior area | Automated decision coverage |
|---|---|
| Application startup and lifetime | No automated Win32 lifecycle test |
| Monitor discovery, names, and numbering | `MonitorNameResolverTests`, `MonitorInfoTests` |
| Tray icon and tooltip | `TrayStateTests` |
| Context menu | `MenuModelTests`, `MonitorInfoTests` |
| Blackout activation and state | `BlackoutManagerTests` |
| Blackout window behavior | `CursorIdleTrackerTests`; remaining behavior requires Win32 integration |
| Display configuration changes and restoration | `BlackoutManagerTests` |
| Global hotkeys | `HotkeyInterpreterTests`; registration requires Win32 integration |
| Monitor-identification overlays | No automated Win32 overlay test |
| Localization | `MonitorInfoTests`, `TrayStateTests` |
| Logging | `LoggerTests` |
| Distribution and installer | No automated installer integration test |
