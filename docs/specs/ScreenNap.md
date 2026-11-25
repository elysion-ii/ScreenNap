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

#### LIFE-2: Only one ScreenNap process runs at a time in a Windows session

A second launch in the same Windows session (the desktop a user signed in to) displays a
localized informational message and exits without affecting the running process, also
when the two launches run with different privileges (for example one of them elevated)
and the second cannot open what the first holds. Each Windows session (another user, or
another sign-in through Remote Desktop or user switching) runs its own ScreenNap. When
the check itself fails, for example because another program holds an object of the same
name, ScreenNap reports a startup failure (ERR-1).
When this message, or a startup-failure message (ERR-1, ERR-2), cannot be shown,
ScreenNap logs that failure and ends as it would have after the message.

#### LIFE-3: Normal shutdown releases everything ScreenNap holds

On normal shutdown, ScreenNap unregisters its global hotkeys, removes its tray icon,
closes all blackout and identification windows, and releases its window resources.
When a blackout window cannot be closed at that point, ScreenNap logs the failure, shows
no notification, and ends; Windows removes the windows of the ended process.

#### LIFE-4: Nothing persists across restarts

Blackout choices exist only for the current process lifetime; ScreenNap persists neither
blackout state nor user configuration across restarts.

#### LIFE-5: A close request from another process shuts ScreenNap down normally

When another process sends ScreenNap's windows a close request (as the installer does
before an update or uninstall), ScreenNap shuts down normally (LIFE-3), so the update or
uninstall can proceed without force-closing it (DIST-7 to DIST-9). The shutdown releases
what ScreenNap holds in the same way and logs no failure for what it released.

### Processing and timing

#### RUN-1: ScreenNap handles one event at a time

ScreenNap handles every menu command, hotkey, mouse action, timer, and display change
one at a time, in the order Windows delivers them, so no event changes blackout, overlay,
or tray state while ScreenNap decides another. Only one ScreenNap process runs (LIFE-2).

#### RUN-2: Startup values stay fixed while ScreenNap runs

The Windows UI language, the `SCREENNAP_LOG_DIR` value, and the set of hotkeys that could
be registered are read once at startup. A change to any of them takes effect at the next
start; a hotkey another application held at startup stays unavailable until then.

#### RUN-3: Every input from the user's session is the user's action

The tray-icon and menu messages ScreenNap acts on may come from any process of the
user's Windows session, and ScreenNap treats each as the user's action. The
`SCREENNAP_LOG_DIR` value comes from the environment the user or an administrator sets.
No input decides an identity, a permission, or another effect the user may not choose.

### Monitor discovery, names, and numbering

#### MON-1: Monitors are enumerated when they are needed

ScreenNap enumerates the currently active monitors whenever the tray menu opens, whenever
a monitor item of the menu is selected for a monitor without a live blackout (MENU-6),
whenever a monitor hotkey is handled, whenever identification overlays are requested, and
after a display configuration change settles.

#### MON-2: Monitors are numbered from one in enumeration order

Monitors receive one-based numbers in the order returned by the current enumeration.
These numbers drive menu accelerators, monitor hotkeys, and identification overlays.

#### MON-3: Monitor names fall back in a fixed order

A monitor name is the preferred Windows friendly name when available, then a secondary
Windows display-device name, then the display device path with a leading `\\.\` prefix
removed.

#### MON-4: A monitor record carries bounds, resolution, primary state, and identity

Each monitor record includes its full monitor bounds, resolution, primary-monitor state,
and physical identity (MON-7) when Windows reports the monitor's display path.

#### MON-7: A physical monitor is identified by its serial number, or by its connection

Two monitor records denote the same physical monitor when their EDID manufacturer ID,
product code, and serial number are equal, for a monitor whose EDID carries a serial
number. A monitor whose EDID carries no serial number, or whose EDID cannot be read, is
identified by the display connection it is attached to. Two monitors of the same model
are therefore told apart, and a monitor with a serial number keeps its identity when it
is attached to another connection. The serial number is the EDID's serial-number text
when the EDID carries one, and otherwise its numeric serial number when that is not zero.
A monitor has a physical identity whenever Windows reports its display path, whether or
not Windows also reports a friendly name. The reasons are in
`docs/adr/0005-monitor-identity-by-serial-number-or-connection.md`.

#### MON-5: One monitor's failure does not hide the others

Failure to obtain information for one monitor does not prevent other monitors from being
returned. The monitor whose information cannot be obtained is left out of the
enumeration, and the monitors after it take the following one-based numbers (MON-2).

#### MON-6: A failed enumeration uses the monitors found before the failure

When the enumeration itself fails, ScreenNap uses the monitors it obtained before the
failure, possibly none, logs the failure, and shows no notification. With no monitor, the
menu holds only the version item, a separator, and `Exit` (MENU-8), monitor hotkeys and
the identify hotkey do nothing (HK-2, IDN-6), and reconciliation keeps every desired
blackout pending (DISP-4).

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
When the cursor position cannot be obtained, the menu opens at screen coordinates
(0, 0), and the failure is logged.

#### TRAY-5: The tray icon returns after the taskbar is recreated

When the Windows taskbar is recreated (for example after Explorer restarts), ScreenNap
adds its tray icon again, with the icon and tooltip of the current live blackout count.
When ScreenNap cannot register at startup for the taskbar's notification, it logs the
failure and keeps running without adding the icon again.

#### TRAY-6: A notification fits the Windows limits

A tray notification's text is cut to its first 255 characters and its title to its first
63 characters, the Windows notification-icon limits.

#### TRAY-7: The tray icon is sharp at the display scaling

The tray icon is drawn from the icon image whose size is the one Windows uses for small
icons at the primary monitor's scaling when ScreenNap starts (16 pixels at 100%, 24 at
150%, 32 at 200%), or from the next larger image when the icon has no image of exactly
that size, so Windows never stretches a smaller image. Like the other startup values
(RUN-2), a later change of the scaling takes effect at the next start. When the scaling
cannot be read, the icon is drawn at its 100% size and the failure is logged.

### Context menu

#### MENU-1: The menu opens with the version item and a separator

The menu opens with a disabled version item, followed by a separator, before the monitor
items.

#### MENU-2: The menu never shows two consecutive separators

The menu never contains two consecutive separators.

#### MENU-3: Every monitor is listed before the actions

The menu lists every currently enumerated monitor before its action items.

#### MENU-4: A monitor item describes its monitor

Each monitor item contains its one-based accelerator number, friendly name, resolution
in physical pixels, localized primary marker when applicable, and localized active marker
when blacked out. The friendly name appears exactly as Windows reports it; an `&` in it
is shown as an `&` and never becomes an access key.

#### MENU-5: A blacked-out monitor's item is checked

A live blackout monitor item is checked.

#### MENU-6: Selecting a monitor item toggles that monitor

Selecting a monitor item toggles the blackout for the monitor the item represented when
the menu opened. When that monitor has a live blackout, the blackout closes. Otherwise
the blackout covers that monitor as the current enumeration reports it, so a display
change while the menu was open never leaves the blackout at the monitor's earlier bounds
(WIN-1); when the current enumeration no longer lists that monitor, selecting the item
does nothing, as for a hotkey without a monitor (HK-2).

#### MENU-7: Release All appears only while a blackout is live

`Release All` appears only while at least one live blackout window exists, and selecting
it attempts to close every live blackout window.

#### MENU-8: Exit always appears and shuts ScreenNap down

`Exit` always appears, including when no monitor is available, and terminates the
application through normal shutdown.

#### MENU-9: Monitors beyond the ninth keep their numbers

With ten or more monitors, every monitor is listed with its one-based number; the
accelerator of a number with two or more digits is its first digit. A menu command that
matches no item of the menu snapshot does nothing.

### Version display

#### VER-1: The version item shows the product version

The context menu's version item reads `ScreenNap X.Y.Z`, using the product version the
application was built with, with no build metadata.

#### VER-2: The version item is disabled

The version item is visible, and selecting it does nothing.

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
every successfully closed window. It also clears the desired state of every disconnected
monitor waiting for restoration (DISP-4), so no monitor is blacked out again when it
reconnects. A window that cannot be closed remains active and, when it has a physical
identity, retains only its own desired state.

#### BLK-5: The tray follows the live blackout count

The tray state is updated whenever the live blackout count changes.
When Windows rejects an icon, tooltip, or notification update, ScreenNap logs the
failure and the tray keeps what it showed before; the next change of the live blackout
count tries the update again.

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

#### WIN-7: A blackout of the primary monitor counts as a full-screen application

Because a blackout covers its whole monitor above every ordinary window (WIN-1, WIN-2),
Windows treats a blackout of the primary monitor as a full-screen application and holds
back notifications while it is live, as it does for any full-screen application; this
includes ScreenNap's own failure notifications (ERR-3 to ERR-8). A blackout of another
monitor does not change how Windows shows notifications.

#### WIN-6: Only the user and ScreenNap close a blackout

A blackout window ignores a close request another process sends to it. A blackout closes
only through the user's actions (WIN-4, MENU-6, MENU-7, HK-2), reconciliation for a
disconnected monitor (DISP-2), and ScreenNap's shutdown (LIFE-3, LIFE-5), so the monitor
stays protected as long as the user wants it blacked out.

### Display configuration changes and restoration

#### DISP-1: Display changes are debounced

ScreenNap waits approximately 500 milliseconds after a display-change notification
before reconciling blackout state, so a burst of topology changes is handled as one
settled configuration.
When the debounce timer cannot be started, ScreenNap logs the failure and does not
reconcile for that notification; the next display-change notification starts the
debounce again.

#### DISP-2: A blackout whose monitor is disconnected closes and stays desired

When a physical monitor with a live blackout is no longer present after reconciliation,
ScreenNap closes that blackout, so Windows never moves it onto another monitor, and the
monitor stays desired when it has a physical identity (DISP-4). Live blackouts are kept
per physical monitor (MON-7), so a display device path that moves to another monitor
never carries a blackout with it. When the blackout cannot be closed, ScreenNap logs the
failure, shows no notification, and tries again at the next reconciliation.

#### DISP-3: A desired blackout follows its physical monitor

After reconciliation, ScreenNap recreates a missing desired blackout on the same physical
monitor when that monitor is present, even if its display device path changed.

#### DISP-4: A disconnected desired monitor is restored on reconnection

A disconnected desired monitor remains pending and is restored after a later display
change reconnects the same physical monitor.

#### DISP-5: A monitor without a stable identity is not restored

A monitor without a stable physical identity is not restored automatically after its
blackout window disappears.

#### DISP-6: Reconciliation never duplicates a blackout

Reconciliation does not create a second blackout for a monitor that has a live one, and
ignores monitors that are not desired.

#### DISP-7: A live blackout follows its monitor's bounds

After reconciliation, every live blackout covers the current full bounds of its monitor,
so a change of the monitor's resolution or position never leaves part of it uncovered
(WIN-1). When a blackout cannot be moved, ScreenNap logs the failure, shows no
notification, and tries again at the next reconciliation.

### Global hotkeys

#### HK-1: Ctrl+Shift+Alt+1 through 9 are registered

ScreenNap registers `Ctrl+Shift+Alt+1` through `Ctrl+Shift+Alt+9` as non-repeating
global hotkeys. The digits of every ScreenNap hotkey (HK-1, HK-3) are the digit keys of
the keyboard's main row; the numeric keypad's digit keys are not hotkeys, because Windows
turns a keypad digit pressed with Shift into a navigation key.

#### HK-2: A numbered hotkey toggles its monitor

A numbered hotkey toggles the corresponding one-based monitor number in the current
enumeration; pressing a number for which no monitor exists does nothing.

#### HK-3: Ctrl+Shift+Alt+0 toggles identification overlays

ScreenNap registers `Ctrl+Shift+Alt+0` as a non-repeating global hotkey that toggles
monitor-identification overlays.

#### HK-4: One hotkey's registration failure leaves the others working

Failure to register one hotkey does not prevent registration or use of the remaining
hotkeys.

#### HK-5: A second digit pressed with the first is ignored

While the digit key of the ScreenNap hotkey that ScreenNap last acted on is still held,
every other ScreenNap hotkey does nothing, so pressing two digits together acts only on
the digit pressed first.

### Monitor-identification overlays

#### IDN-1: The identify hotkey shows an overlay on each monitor

When no identification overlay is visible, `Ctrl+Shift+Alt+0` enumerates the current
monitors and shows a centered overlay on each monitor.

#### IDN-2: An overlay shows the monitor number

Each overlay is a black, topmost, non-activating tool window containing the monitor's
one-based number in large white text. It is 200 by 150 pixels at 100% scaling and grows
with the monitor's scale factor (for example 300 by 225 pixels at 150%).
When the scale factor cannot be read, the overlay uses its 100% size and the failure is
logged.

#### IDN-3: Overlays close themselves after about three seconds

Identification overlays remain above ordinary windows. All identification overlays close
automatically approximately three seconds after the first overlay that could start the
closing timer appeared. When no overlay can start the timer, the overlays stay until the
identify hotkey closes them (IDN-4).

#### IDN-4: The identify hotkey closes visible overlays

Invoking `Ctrl+Shift+Alt+0` while overlays are visible closes all overlays immediately.

#### IDN-5: One overlay's failure does not stop the others

Failure to create one monitor's overlay does not prevent overlays from being created for
other monitors.

#### IDN-6: No monitor means no overlay

When the enumeration finds no monitor, the identify hotkey shows nothing and shows no
notification.

#### IDN-7: Overlays appear above blackouts

An identification overlay on a blacked-out monitor appears above the blackout for as
long as the overlay is shown, so a blacked-out monitor can be identified.

### Localization

#### L10N-1: English and Japanese are provided

ScreenNap provides English default resources and Japanese resources selected through the
current Windows UI culture.

#### L10N-2: Other cultures receive English

A UI culture with no resources of its own receives the English default resources.

#### L10N-3: All user-facing text is localized

Localized text covers menu actions, primary and active monitor markers, tray tooltips,
the duplicate-instance notification, and every failure notification.

#### L10N-4: A failed resource lookup shows the resource key

When the lookup of a localized text fails — no resource set for the UI culture or its
parents can be loaded, or the resource set found holds no value for the text's key —
ScreenNap shows the text's resource key in its place.

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

#### LOG-9: With no usable log directory, ScreenNap runs without logs

When neither the `SCREENNAP_LOG_DIR` directory nor the default directory can be created,
ScreenNap writes no log and shows no notification.

#### LOG-10: Log files are UTF-8 text with CRLF line ends

Log files are UTF-8 without a byte order mark, and each line ends with CRLF. Each line is
appended with one write, so a process ended during that write can leave its last line
incomplete.

#### LOG-11: An unset log directory is reported once

When `SCREENNAP_LOG_DIR` is not set or is empty, ScreenNap writes one `INFO` entry at
startup that names the variable, states that it is not set, and names the default
directory applied.

### Distribution and installer

#### DIST-1: The portable distribution is one self-contained executable

The portable distribution is a self-contained, single-file `ScreenNap.exe` that does not
require a separately installed .NET runtime.

#### DIST-2: The installer installs per user on x64

The installer installs on x64 Windows only and refuses to install on any other
architecture, including Arm64 Windows, where ScreenNap would run under emulation. It
performs a user-level installation without requiring administrator privileges.

#### DIST-3: The installer installs the executable and the selected shortcuts

The installer installs `ScreenNap.exe`, creates a Start Menu shortcut when that task is
selected, and offers optional desktop-shortcut and current-user Windows-startup tasks.

#### DIST-4: The interactive installer offers to launch ScreenNap

The interactive installer offers to launch ScreenNap after installation.

#### DIST-5: The installer user interface is English

The installer user interface is English.

#### DIST-6: Uninstalling removes what the installer created

Uninstalling removes the installed files, the shortcuts, and the Windows-startup entry
the installer created. It also removes ScreenNap's log files (LOG-3) from the default log
directory (LOG-1), and that directory and its `ScreenNap` parent when they are left
empty, because no retention runs once ScreenNap is gone (LOG-7). A directory named by
`SCREENNAP_LOG_DIR` is left untouched.

#### DIST-7: Uninstalling handles a running ScreenNap

When ScreenNap is running, the interactive uninstaller offers to close it and retry, to
retry after the user closes it, or to cancel; it never force-closes ScreenNap. A silent
uninstall asks ScreenNap to close without force-closing it and continues once ScreenNap
has exited; when ScreenNap is still running after that request, the silent uninstall
removes nothing and ends with a non-zero exit code.

#### DIST-8: Updating closes a running ScreenNap without restarting it

When ScreenNap is running, the interactive installer offers to close it before replacing
its files, and ScreenNap is not started again after the update unless the user chooses
the launch offered by DIST-4.

#### DIST-9: A silent update never force-closes ScreenNap

A silent update asks a running ScreenNap to close without force-closing it and continues
once ScreenNap has exited. When ScreenNap is still running after that request, the silent
update replaces nothing and ends with a non-zero exit code, so a deployment can be run
again once ScreenNap has closed.

## Inputs and Outputs

| Input | Observable output | Requirements |
|---|---|---|
| Tray-icon left or right button release | Context menu with the version item and the current monitors | TRAY-4, MENU-1 to MENU-8, VER-1, VER-2 |
| Monitor menu item | Blackout toggled for that menu entry | MENU-6, BLK-1, BLK-2 |
| `Release All` | All closable blackouts dismissed | MENU-7, BLK-4 |
| `Exit` | Normal application shutdown | MENU-8, LIFE-3 |
| `Ctrl+Shift+Alt+1` through `9` | Corresponding current monitor toggled when present | HK-1, HK-2, HK-5 |
| `Ctrl+Shift+Alt+0` | Identification overlays shown or dismissed | HK-3, IDN-1 to IDN-4 |
| Blackout-window left double-click or right-button release | That blackout dismissed by the user | WIN-4 |
| Cursor position changes over a blackout | Cursor restored and inactivity timer restarted | WIN-5 |
| Display configuration change | Desired blackout state reconciled after debounce | DISP-1 to DISP-7 |
| Taskbar recreated | Tray icon added again with the current state | TRAY-5 |
| Close request from another process | Normal application shutdown; a blackout window ignores it | LIFE-5, WIN-6 |
| `SCREENNAP_LOG_DIR` | Log directory | LOG-1, LOG-2 |
| Application and operational events | Daily English log entries when logging is available | LOG-3 to LOG-6 |

## Error Behavior

### Failures that stop ScreenNap

#### ERR-1: A startup failure is shown to the user

A failure during critical process or hidden-window initialization is logged when logging
is available, is reported to the user in a localized message box, and stops startup.

#### ERR-2: A missing tray icon is shown to the user and stops ScreenNap

When the normal or active tray icon image cannot be loaded at startup, or the tray icon
cannot be added within 60 seconds of startup, ScreenNap logs the failure, reports it to
the user in a localized message box, and shuts down normally, because without a visible
tray icon the user has no way to open the menu or exit. When the icon cannot be added
at once, as when ScreenNap starts at sign-in before the taskbar is ready, ScreenNap
logs it and tries again every second and whenever Windows reports the taskbar created
(TRAY-5), registering its hotkeys once the icon is added; when it cannot start the
one-second retry, it reports the failure at once.

#### ERR-12: Losing Windows' messages is shown to the user and stops ScreenNap

When ScreenNap can no longer receive messages from Windows, it logs the failure, reports
it to the user in a localized message box, and shuts down normally, because it can no
longer react to the tray icon, the hotkeys, or the blackout windows.

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

#### ERR-8: Overlays that cannot show their number are reported

When an identification overlay cannot be created, or its number cannot be drawn as it
appears, for one or more monitors, one localized tray notification names those monitor
numbers. An overlay whose number cannot be drawn stays until the overlays close (IDN-3,
IDN-4). A later redraw of an overlay that fails is logged only.

### Failures that are logged only

#### ERR-9: Detected failures are logged and unaffected operations continue

Detected failures in tray, menu, hotkey registration, monitor enumeration, blackout
topmost maintenance, or identification-window creation are logged at an appropriate
level, and unaffected operations continue.

#### ERR-10: Maintenance and cleanup failures are logged without notification

Identification-overlay timers, topmost maintenance, a later redraw of an overlay,
dismissal, and cleanup are best-effort operations: their failures are logged and do not
notify the user.

#### ERR-11: Logging failures stay silent

A logging failure is silent to the user and never terminates ScreenNap.

## Invariants

#### INV-1: ScreenNap never changes the display configuration

ScreenNap does not change monitor power, display layout, orientation, resolution, or
primary-monitor selection.

#### INV-2: At most one blackout exists per monitor

At most one live blackout window exists for a physical monitor (MON-7) and, for a monitor
without a physical identity, for its display device path.

#### INV-3: The tray always reflects the live blackout count

Every live blackout count change is reflected in the tray state.

#### INV-4: Only the user clears restoration intent

Only the user's actions clear a monitor's desired state: a dismissal (WIN-4), selecting
its item or hotkey again (MENU-6, HK-2), and Release All (BLK-4). A blackout closed for a
disconnected monitor keeps it desired (DISP-2) when the monitor has a physical identity.

#### INV-5: Shutdown leaves nothing behind

Normal shutdown clears all in-memory blackout intent and closes all ScreenNap-owned
visible windows.

## Non-Functional Requirements

#### NFR-1: ScreenNap runs on supported x64 Windows

ScreenNap operates on x64 Windows systems supported by its self-contained runtime;
Arm64 Windows is not supported (DIST-2).

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
- Keyboard access to the tray icon and its menu; they respond to the mouse (TRAY-4)
- What happens when the Windows session ends (sign-out, restart, shutdown): Windows ends
  ScreenNap there, and whether ScreenNap's normal shutdown runs is unspecified; nothing
  is lost, because nothing persists (LIFE-4)
- Covering the secure desktop (the lock screen and elevation prompts) and full-screen
  applications that take exclusive control of a display; Windows draws no ordinary
  window there
