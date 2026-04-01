# ScreenNap — Application Specification

## Overview

A system tray resident application that displays fullscreen black windows on specific monitors to prevent OLED burn-in.

Rather than turning off the monitor, it "displays black" to physically turn off OLED pixels while maintaining the Windows display configuration (multi-monitor layout, orientation, and window positions).


## Background / Problem Solved

In a dual-monitor setup with a 4K 32" OLED + FHD 24" IPS, turning off the OLED monitor causes Windows to reconfigure the display layout, disrupting the sub-monitor's orientation (portrait to landscape) and window positions.

OLED is a self-emitting technology — when a pixel displays pure black (#000000), no current flows through it. Therefore, a fullscreen black window provides the same burn-in protection as powering off the monitor.


## Technical Specification

| Item | Detail |
|------|--------|
| Language | C# |
| Runtime | .NET 10.0 LTS |
| UI | Raw Win32 API via P/Invoke |
| UI Framework | None (no WinForms, WPF, or WinUI) |
| Target OS | Windows 10 / 11 (x64) |
| Distribution | Portable self-contained single EXE + Inno Setup installer |
| Runtime dependency | None (bundled in EXE) |
| External NuGet | None (zero dependencies) |
| License | MIT |


## Architecture

### Dependency Direction

```
Program.cs → App/ → Native/
                  → Blackout/ → Native/
```

### Source Structure

| Folder | Responsibility |
|--------|---------------|
| `Native/` | P/Invoke declarations, Win32 constants, native structs. No business logic. |
| `App/` | Tray icon, context menu, monitor enumeration, blackout lifecycle management |
| `Blackout/` | Blackout window class registration, creation, WndProc |
| `Resources/` | i18n string resources (.resx), icon files (.ico) |

### Entry Point

`Program.cs` — Named Mutex for single-instance enforcement, Win32 message loop (`GetMessage` / `TranslateMessage` / `DispatchMessage`).


## Feature List

### 1. System Tray Resident

- On launch, no main window is shown. Only a tray icon appears via `Shell_NotifyIcon`.
- Two icon states: normal (no active blackouts) and active (1+ monitors blacked out).
- Tooltip displays current state (e.g., "ScreenNap", "ScreenNap (1 active)").

### 2. Monitor List Menu

- Left-click or right-click on the tray icon opens a Win32 popup menu (`CreatePopupMenu` + `TrackPopupMenuEx`).
- Monitor list is re-enumerated on every menu open to reflect the current state.
- Each monitor item displays:
  - Friendly name (e.g., "ASUS VG32UQ1B"; falls back to device name if unavailable)
  - Resolution (e.g., 3840x2160)
  - "[Primary]" suffix if it is the primary monitor
  - "(Active)" suffix with checkmark if currently blacked out
- "Exit" item at the bottom.
- "Release All" item appears when 1+ monitors are blacked out.

### 3. Blackout Toggle

- Clicking a monitor item toggles its blackout state.
- Blackout ON: Displays a fullscreen black window covering the target monitor.
- Blackout OFF: Destroys the black window, restoring normal display.
- Multiple monitors can be blacked out simultaneously.

### 4. Single-Instance Enforcement

- Named Mutex prevents multiple instances.
- If a second instance is launched, a Win32 `MessageBox` notification is shown and the process exits.


## Blackout Window Specification

| Item | Specification |
|------|--------------|
| Background color | #000000 (pure black) via `GetStockObject(BLACK_BRUSH)` |
| Window style | `WS_POPUP \| WS_VISIBLE` (borderless fullscreen) |
| Extended style | `WS_EX_TOOLWINDOW \| WS_EX_TOPMOST \| WS_EX_NOACTIVATE` |
| Position | Matches target monitor bounds exactly (`EnumDisplayMonitors` + `GetMonitorInfo` using `rcMonitor`) |
| Topmost | `SetWindowPos(HWND_TOPMOST, ...)` on creation |
| Topmost maintenance | `SetTimer` at 1-second interval to re-apply `SetWindowPos(HWND_TOPMOST)`, preventing taskbar from appearing above |
| Taskbar coverage | Uses full monitor bounds (including taskbar area) |
| Taskbar visibility | Hidden (`WS_EX_TOOLWINDOW`) |
| Alt+Tab visibility | Hidden (`WS_EX_TOOLWINDOW`) |
| Focus stealing | Prevented (`WS_EX_NOACTIVATE`) |
| Dismiss method | Double-click (`WM_LBUTTONDBLCLK`, requires `CS_DBLCLKS` class style) or right-click (`WM_RBUTTONUP`) |
| Cursor auto-hide | Hidden after 10 seconds of no mouse movement; restored on movement. Uses existing TopMost timer (1s) for idle check. `SetCursor(NULL)` to hide, `WM_SETCURSOR` handled to maintain hidden state. |


## Monitor Name Resolution

Friendly monitor names (e.g., "ASUS VG32UQ1B") are resolved in this order:

1. **Primary:** `QueryDisplayConfig` + `DisplayConfigGetDeviceInfo` → `DISPLAYCONFIG_TARGET_DEVICE_NAME`
2. **Fallback 1:** `EnumDisplayDevices` (second-level call) → device string
3. **Fallback 2:** `MONITORINFOEX.szDevice` with `\\.\` prefix removed


## Keyboard Shortcuts

None. All operations are performed through the tray context menu to prevent accidental triggers.


## Internationalization (i18n)

| Item | Detail |
|------|--------|
| Default language | English |
| Included languages | English, Japanese |
| Mechanism | .NET resource files (.resx) with `ResXFileCodeGenerator` |
| Language selection | Automatic based on `CultureInfo.CurrentUICulture` (OS setting) |
| Adding a language | Create `Resources/Strings.xx.resx` — no code changes required |

### Localized Strings

| Key | English | Japanese |
|-----|---------|----------|
| MenuReleaseAll | Release All | すべて解除 |
| MenuExit | Exit | 終了 |
| MenuPrimary | [Primary] | [メイン] |
| MenuActive | (Active) | (暗転中) |
| TooltipNormal | ScreenNap | ScreenNap |
| TooltipActive | ScreenNap ({0} active) | ScreenNap ({0}台 暗転中) |
| NotifyAlreadyRunning | ScreenNap is already running. | ScreenNap は既に起動しています。 |


## Logging

| Item | Detail |
|------|--------|
| Output | `%LocalAppData%\ScreenNap\Logs\` |
| File format | `ScreenNap_yyyyMMdd.log` (daily rotation) |
| Retention | 7 days (purged on startup) |
| Log levels | INFO, WARN, ERROR |
| Line format | `yyyy-MM-dd HH:mm:ss.fff [LEVEL] message` |

### Logged Events

| Level | Events |
|-------|--------|
| INFO | Application start/exit, tray icon lifecycle, blackout create/destroy, monitor enumeration results |
| WARN | Non-critical failures (popup menu) |
| ERROR | Critical P/Invoke failures (RegisterClassExW, CreateWindowExW, Shell_NotifyIcon) with `GetLastWin32Error()` |


## Non-Functional Requirements

| Item | Detail |
|------|--------|
| Memory usage | ~10-15 MB (no UI framework overhead) |
| CPU usage | Near-zero at idle (`GetMessage` blocks when no messages) |
| Startup speed | Instant tray icon appearance |
| External dependency | None (self-contained) |
| Binary size | Minimal (no UI framework bundled) |


## Future Extension Candidates (Not Implemented)

- Windows startup auto-launch (registry HKCU\Run — installer supports this)
- Blackout schedule (auto-blackout after specified idle time)
- Dim clock display mode on blacked-out screen
- DDC/CI monitor power control integration


## Build

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) [Inno Setup 6](https://jrsoftware.org/isdl.php) for creating the installer

### Interactive Build

```
cd Build
Menu.bat
```

### Manual Build

```
dotnet publish ScreenNap/ScreenNap.csproj ^
    -c Release ^
    -f net10.0-windows ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:DebugType=none ^
    -p:DebuggerSupport=false ^
    -o "./Build/ScreenNap"
```

### Installer

The Inno Setup installer (`Build/Setup_ScreenNap.iss`) creates a per-user installation with optional desktop shortcut and Windows startup registration.

| Item | Detail |
|------|--------|
| AppId | `{E4A72F8B-3D19-4C5A-9F61-B8D2E5C7A043}` |
| Default install path | `%LocalAppData%\Programs\ScreenNap` |
| Privileges | User-level (`PrivilegesRequired=lowest`) |
