---
status: active
created: 2026-04-01
---

# ScreenNap Specification

This document specifies the externally observable behavior of ScreenNap.

## Baseline and Precedence

Behavior changes follow the repository's spec-first workflow. For table-driven decisions
already covered by tests, precedence is tests, then this document, then implementation.

Every requirement is a heading that begins with its ID. A heading without an ID groups
requirements and states none itself. The tests that verify a requirement name its ID.

## Purpose

ScreenNap protects OLED monitors from burn-in by covering selected monitors with pure
black windows. It provides the visual effect of turning OLED pixels off without powering
off a monitor or changing the Windows display configuration.

## Scope

This specification covers the ScreenNap desktop application, system-tray controls,
global hotkeys, monitor-identification overlays, blackout state, display-change recovery,
localized user-visible text, failure notification, logging, portable distribution, and
installer behavior.

## Users and External Systems

- A Windows desktop user controls ScreenNap through the system tray, global hotkeys, and blackout-window mouse actions.
- An operator may set the `SCREENNAP_LOG_DIR` environment variable to choose the log directory.
- Windows provides monitor topology, monitor bounds, display names, stable monitor identity when available, input events, and the current UI culture.
- The local file system stores daily application logs.
- The installer can create user-level shortcuts and a current-user Windows-startup entry.

## Required Behavior

### Application startup and lifetime

#### LIFE-1: ScreenNap starts without a main window

ScreenNap starts without displaying a main window and remains resident through a
system-tray icon and registered global hotkeys.

#### LIFE-2: Only one ScreenNap process runs at a time

A second launch displays a localized informational message and exits without affecting
the running process.

#### LIFE-3: Normal shutdown releases everything ScreenNap holds

On normal shutdown, ScreenNap unregisters its global hotkeys, removes its tray icon,
closes all blackout and identification windows, and releases its window resources.

#### LIFE-4: Nothing persists across restarts

Blackout choices exist only for the current process lifetime; ScreenNap persists neither
blackout state nor user configuration across restarts.

### Monitor discovery, names, and numbering

#### MON-1: Monitors are enumerated when they are needed

ScreenNap enumerates the currently active monitors whenever the tray menu opens, whenever
a monitor hotkey is handled, whenever identification overlays are requested, and after a
display configuration change settles.

#### MON-2: Monitors are numbered from one in enumeration order

Monitors receive one-based numbers in the order returned by the current enumeration.
These numbers drive menu accelerators, monitor hotkeys, and identification overlays.

#### MON-3: Monitor names fall back in a fixed order

A monitor name is the preferred Windows friendly name when available, then a secondary
Windows display-device name, then the display device path with a leading `\\.\` prefix
removed.

#### MON-4: A monitor record carries bounds, resolution, primary state, and identity

Each monitor record includes its full monitor bounds, resolution, primary-monitor state,
and stable physical identity when Windows supplies that identity.

#### MON-5: One monitor's failure does not hide the others

Failure to obtain information for one monitor does not prevent other monitors from being
returned.

### Tray icon and tooltip

#### TRAY-1: The idle tray shows the normal icon and tooltip

With no live blackout windows, ScreenNap uses the normal tray icon and the localized
tooltip `ScreenNap`.

#### TRAY-2: The active tray shows the active icon and the blackout count

With one or more live blackout windows, ScreenNap uses the active tray icon and a
localized tooltip containing the live blackout count.

#### TRAY-3: The tooltip fits the Windows limit

The tooltip does not exceed the Windows notification-icon limit of 127 characters.

#### TRAY-4: Either mouse button opens the context menu

Either a left-button release or a right-button release on the tray icon opens the
context menu at the current cursor position.

### Context menu

#### MENU-1: The menu opens with the version item and a separator

The menu opens with a disabled version item, followed by a separator, before the monitor
items.

#### MENU-2: The menu never shows two consecutive separators

The menu never contains two consecutive separators.

#### MENU-3: Every monitor is listed before the actions

The menu lists every currently enumerated monitor before its action items.

#### MENU-4: A monitor item describes its monitor

Each monitor item contains its one-based accelerator number, friendly name, resolution,
localized primary marker when applicable, and localized active marker when blacked out.

#### MENU-5: A blacked-out monitor's item is checked

A live blackout monitor item is checked.

#### MENU-6: Selecting a monitor item toggles that monitor

Selecting a monitor item toggles the blackout for the monitor represented by the menu
snapshot.

#### MENU-7: Release All appears only while a blackout is live

`Release All` appears only while at least one live blackout window exists, and selecting
it attempts to close every live blackout window.

#### MENU-8: Exit always appears and shuts ScreenNap down

`Exit` always appears, including when no monitor is available, and terminates the
application through normal shutdown.

### Version display

#### VER-1: The version item shows the product version

The context menu's version item reads `ScreenNap X.Y.Z`, using the product version the
application was built with, with no build metadata.

#### VER-2: The version item is disabled

The version item is visible, and selecting it does nothing.

#### VER-3: An unknown version reads as unknown

When the product version cannot be determined, the item reads `ScreenNap unknown`.

#### VER-4: The context menu is the only version display

There is no about window, and ScreenNap accepts no command-line options.

### Blackout activation and state

#### BLK-1: Activating a monitor creates one blackout window

Activating a monitor creates one blackout window for that monitor. A failed window
creation leaves the monitor inactive.

#### BLK-2: Selecting an active monitor again closes its blackout

Selecting an active monitor again attempts to close its blackout. If closing fails, the
monitor remains active and retains its desired blackout state.

#### BLK-3: Monitors are blacked out independently

Multiple monitors may be blacked out simultaneously and controlled independently.

#### BLK-4: Release All closes every blackout it can

`Release All` attempts to close every live blackout and clears restoration intent for
every successfully closed window. A window that cannot be closed remains active and, when
it has a stable physical identity, retains only its own desired state.

#### BLK-5: The tray follows the live blackout count

The tray state is updated whenever the live blackout count changes.

### Blackout window behavior

#### WIN-1: A blackout covers its whole monitor in black

A blackout window covers the selected monitor's full bounds, including the taskbar area,
with pure black.

#### WIN-2: A blackout stays on top without taking focus

The window remains above ordinary windows without taking keyboard focus or appearing in
the taskbar or Alt+Tab.

#### WIN-3: A blackout periodically reasserts its topmost position

ScreenNap periodically restores the window's topmost position without moving, resizing,
or activating it.

#### WIN-4: Double-click or right-click dismisses a blackout

A left-button double-click or a right-button release anywhere on the blackout window
dismisses it and clears the user's desired blackout state for that physical monitor.

#### WIN-5: The cursor hides over an idle blackout

The cursor hides after 10 seconds without a position change over the blackout window. A
later position change shows the cursor and restarts the inactivity period.

### Display configuration changes and restoration

#### DISP-1: Display changes are debounced

ScreenNap waits approximately 500 milliseconds after a display-change notification
before reconciling blackout state, so a burst of topology changes is handled as one
settled configuration.

#### DISP-2: A blackout Windows destroyed stays desired

If Windows destroys a blackout window without a user dismissal, ScreenNap retains the
desired blackout state when the monitor has a stable physical identity.

#### DISP-3: A desired blackout follows its physical monitor

After reconciliation, ScreenNap recreates a missing desired blackout on the same physical
monitor when that monitor is present, even if its display device path changed.

#### DISP-4: A disconnected desired monitor is restored on reconnection

A disconnected desired monitor remains pending and is restored after a later display
change reconnects the same physical monitor.

#### DISP-5: A monitor without a stable identity is not restored

A monitor without a stable physical identity is not restored automatically after its
blackout window disappears.

#### DISP-6: Reconciliation removes stale state without duplicating blackouts

Reconciliation removes stale window state, whether or not any blackout is desired, does
not duplicate a live blackout, and ignores monitors that are not desired.

### Global hotkeys

#### HK-1: Ctrl+Shift+Alt+1 through 9 are registered

ScreenNap registers `Ctrl+Shift+Alt+1` through `Ctrl+Shift+Alt+9` as non-repeating
global hotkeys.

#### HK-2: A numbered hotkey toggles its monitor

A numbered hotkey toggles the corresponding one-based monitor number in the current
enumeration; pressing a number for which no monitor exists does nothing.

#### HK-3: Ctrl+Shift+Alt+0 toggles identification overlays

ScreenNap registers `Ctrl+Shift+Alt+0` as a non-repeating global hotkey that toggles
monitor-identification overlays.

#### HK-4: One hotkey's registration failure leaves the others working

Failure to register one hotkey does not prevent registration or use of the remaining
hotkeys.

### Monitor-identification overlays

#### IDN-1: The identify hotkey shows an overlay on each monitor

When no identification overlay is visible, `Ctrl+Shift+Alt+0` enumerates the current
monitors and shows a centered overlay on each monitor.

#### IDN-2: An overlay shows the monitor number

Each overlay is a 200 by 150 pixel black, topmost, non-activating tool window containing
the monitor's one-based number in large white text.

#### IDN-3: Overlays close themselves after about three seconds

Identification overlays remain above ordinary windows. When the overlay for monitor 1 is
created successfully, all identification overlays close automatically after
approximately three seconds.

#### IDN-4: The identify hotkey closes visible overlays

Invoking `Ctrl+Shift+Alt+0` while overlays are visible closes all overlays immediately.

#### IDN-5: One overlay's failure does not stop the others

Failure to create one monitor's overlay does not prevent overlays from being created for
other monitors.

### Localization

#### L10N-1: English and Japanese are provided

ScreenNap provides English default resources and Japanese resources selected through the
current Windows UI culture.

#### L10N-2: Other cultures receive English

A UI culture with no resources of its own receives the English default resources.

#### L10N-3: All user-facing text is localized

Localized text covers menu actions, primary and active monitor markers, tray tooltips,
the duplicate-instance notification, and every failure notification.

#### L10N-4: A failed resource lookup falls back to English

If a resource lookup fails, ScreenNap uses its built-in English fallback text.

#### L10N-5: Numbers, hotkeys, versions, and logs are language-independent

Monitor numbers, hotkey combinations, the version item, log messages, and
identification-overlay numerals are language-independent.

### Logging

#### LOG-1: Logs go to the configured or the default directory

ScreenNap writes logs to the directory named by the `SCREENNAP_LOG_DIR` environment
variable when it names an absolute directory path that can be created, and otherwise to
`%LocalAppData%\ScreenNap\Logs\` when that directory can be created.

#### LOG-2: A rejected log directory is reported once

When `SCREENNAP_LOG_DIR` is set but is not an absolute path, or names a directory that
cannot be created, ScreenNap uses the default directory and writes one `WARN` entry that
names the rejected value and the default directory applied.

#### LOG-3: Log files are named by local date

Log files use the local date and the name `ScreenNap_yyyyMMdd.log`.

#### LOG-4: Log lines carry the local time with its UTC offset

Each line uses the culture-independent format
`yyyy-MM-dd HH:mm:ss.fff zzz [LEVEL] message`, where the time is local and `zzz` is its
offset from UTC (for example `+09:00`).

#### LOG-5: Log levels are INFO, WARN, and ERROR

Log levels are `INFO`, `WARN`, and `ERROR`.

#### LOG-6: Operational events are logged

A successful startup logs the three-component application version; application
lifetime, monitor enumeration, blackout transitions, and display reconciliation also
produce operational log entries.

#### LOG-7: Logs older than seven days are deleted at startup

At startup, ScreenNap makes a best-effort attempt to delete matching log files whose
last-write time is older than seven days. A file that cannot be deleted is logged with a
`WARN` entry, and the remaining files are still deleted.

#### LOG-8: Logging failures never stop ScreenNap

Directory creation, log writes, retention scans, and log deletion failures do not
terminate the application.

### Distribution and installer

#### DIST-1: The portable distribution is one self-contained executable

The portable distribution is a self-contained, single-file `ScreenNap.exe` that does not
require a separately installed .NET runtime.

#### DIST-2: The installer installs per user on x64

The installer targets x64-compatible Windows and performs a user-level installation
without requiring administrator privileges.

#### DIST-3: The installer installs the executable and the selected shortcuts

The installer installs `ScreenNap.exe`, creates a Start Menu shortcut when that task is
selected, and offers optional desktop-shortcut and current-user Windows-startup tasks.

#### DIST-4: The interactive installer offers to launch ScreenNap

The interactive installer offers to launch ScreenNap after installation.

#### DIST-5: The installer user interface is English

The installer user interface is English.

#### DIST-6: Uninstalling removes what the installer created

Uninstalling removes the installed files, the shortcuts, and the Windows-startup entry
the installer created.

#### DIST-7: Uninstalling handles a running ScreenNap

When ScreenNap is running, the interactive uninstaller offers to close it and retry, to
retry after the user closes it, or to cancel; it never force-closes ScreenNap. A silent
uninstall closes ScreenNap and continues.

## Inputs and Outputs

| Input | Observable output | Requirements |
|---|---|---|
| Tray-icon left or right button release | Context menu with the version item and the current monitors | TRAY-4, MENU-1 to MENU-8, VER-1 to VER-3 |
| Monitor menu item | Blackout toggled for that menu entry | MENU-6, BLK-1, BLK-2 |
| `Release All` | All closable blackouts dismissed | MENU-7, BLK-4 |
| `Exit` | Normal application shutdown | MENU-8, LIFE-3 |
| `Ctrl+Shift+Alt+1` through `9` | Corresponding current monitor toggled when present | HK-1, HK-2 |
| `Ctrl+Shift+Alt+0` | Identification overlays shown or dismissed | HK-3, IDN-1 to IDN-4 |
| Blackout-window left double-click or right-button release | That blackout dismissed by the user | WIN-4 |
| Cursor position changes over a blackout | Cursor restored and inactivity timer restarted | WIN-5 |
| Display configuration change | Desired blackout state reconciled after debounce | DISP-1 to DISP-6 |
| `SCREENNAP_LOG_DIR` | Log directory | LOG-1, LOG-2 |
| Application and operational events | Daily English log entries when logging is available | LOG-3 to LOG-6 |

## Error Behavior

### Failures that stop ScreenNap

#### ERR-1: A startup failure is shown to the user

A failure during critical process or hidden-window initialization is logged when logging
is available, is reported to the user in a localized message box, and stops startup.

#### ERR-2: A missing tray icon is shown to the user and stops ScreenNap

When the tray icon cannot be created, ScreenNap logs the failure, reports it to the user
in a localized message box, and shuts down normally, because without the tray icon the
user has no way to open the menu or exit.

### Failures of an operation that continue

#### ERR-3: A blackout that cannot be created is reported

When a blackout window cannot be created for a monitor the user activated, no live or
desired blackout state is added, and a localized tray notification names the monitor.

#### ERR-4: A blackout that cannot be closed is reported

When a blackout window cannot be closed after the user selects its monitor again, the
corresponding live and desired state is preserved, and a localized tray notification
names the monitor.

#### ERR-5: Release All reports the blackouts it could not close

When `Release All` cannot close one or more blackout windows, a localized tray
notification states how many remain.

#### ERR-6: A blackout that cannot be restored is reported

When reconciliation cannot recreate a desired blackout on a present monitor, a localized
tray notification names the monitor.

#### ERR-7: Hotkeys that cannot be registered are reported

When one or more hotkeys cannot be registered at startup, one localized tray notification
lists the hotkeys that are unavailable.

#### ERR-8: Overlays that cannot be created are reported

When an identification overlay cannot be created for one or more monitors, a localized
tray notification names the monitor numbers without an overlay.

### Failures that are logged only

#### ERR-9: Detected failures are logged and unaffected operations continue

Detected failures in tray, menu, hotkey registration, monitor enumeration, blackout
topmost maintenance, or identification-window creation are logged at an appropriate
level, and unaffected operations continue.

#### ERR-10: Maintenance and cleanup failures are logged without notification

Identification-overlay timers, topmost maintenance, dismissal, and cleanup are
best-effort operations: their failures are logged and do not notify the user.

#### ERR-11: Logging failures stay silent

A logging failure is silent to the user and never terminates ScreenNap.

## Invariants

#### INV-1: ScreenNap never changes the display configuration

ScreenNap does not change monitor power, display layout, orientation, resolution, or
primary-monitor selection.

#### INV-2: At most one blackout exists per display device

At most one live blackout window exists for a given display device path.

#### INV-3: The tray always reflects the live blackout count

Every live blackout count change is reflected in the tray state.

#### INV-4: Only an operating-system disappearance keeps restoration intent

A user dismissal clears restoration intent; an operating-system disappearance retains
restoration intent only through a non-default stable monitor identity.

#### INV-5: Shutdown leaves nothing behind

Normal shutdown clears all in-memory blackout intent and closes all ScreenNap-owned
visible windows.

## Non-Functional Requirements

#### NFR-1: ScreenNap runs on supported x64 Windows

ScreenNap operates on x64 Windows systems supported by its self-contained runtime.

#### NFR-2: ScreenNap stays idle when nothing is shown

When no blackout or identification overlay is active, ScreenNap remains idle until it
receives user or display events; periodic maintenance is limited to live blackout
windows and visible identification overlays.

#### NFR-3: ScreenNap's windows never steal focus

User-facing blackout and identification windows do not steal focus.

#### NFR-4: The distribution has no external runtime package

The distributed application has no external runtime package dependency.

## Out of Scope

- Powering monitors off or changing Windows display configuration
- Scheduled blackout or whole-application idle detection
- A clock or other content rendered on a blackout window
- DDC/CI monitor control
- User-configurable hotkeys, cursor timeout, identification duration, or topmost intervals
- Persisting blackout selections or application settings across process restarts
