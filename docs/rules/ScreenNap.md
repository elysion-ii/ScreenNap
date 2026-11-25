---
status: active
created: 2026-07-18
---

# ScreenNap Rules

Application-specific deltas and overrides against `standard.md` and `dotnet.md` in this
directory. On conflict, this file wins.

## Repository Language

- `AGENTS.md`, this file, and all other agent instruction files in this repository must be written in English
- Keep rule content concise and declarative; reference source files or methods instead of including concrete code examples unless an example is essential

## Architecture

- ScreenNap must use raw Win32 API through P/Invoke; do not introduce WinForms, WPF, WinUI, or another UI framework
- Runtime functionality must use the .NET BCL and Win32 APIs without external NuGet runtime packages; centrally managed build-time analyzers with `PrivateAssets="all"` are permitted
- **Feature folders**: the source of `ScreenNap/` is grouped by feature, as `standard.md` Code Organization requires. The feature folders are `Lifetime/` (startup, the single instance, the hidden message window and the routing of its messages to the other features, and shutdown), `Tray/` (the tray icon, its tooltip, and the context menu), `Blackout/` (blackout windows and their restoration after a display change), `Hotkeys/` (the global hotkeys), and `Identify/` (the monitor-identification overlays). A type that one feature uses lives in that feature's folder. `Program.cs` only creates the Win32 implementations and starts `Lifetime/`
- **Shared homes**: a type that two or more features use lives in a shared home, never in a feature folder. The shared homes are `Monitors/` (monitor enumeration, names, and identity), `Notifications/` (how a failure reaches the user, and its wording), `Versioning/` (the product version), `Logging/`, `Native/` (P/Invoke declarations, Win32 constants, and native structures), and `Resources/` (localized strings and icons). A type moves from a feature folder to a shared home in the change that gives it a second feature
- **Dependency directions**: references between folders follow only these directions, so no feature depends on a feature that depends on it, and a shared home never depends on a feature:

```text
Program.cs     → every folder
Lifetime/      → Tray/, Blackout/, Hotkeys/, Identify/, Monitors/, Versioning/, Logging/, Native/, Resources/
Tray/          → Blackout/, Monitors/, Notifications/, Versioning/, Logging/, Native/, Resources/
Hotkeys/       → Blackout/, Identify/, Monitors/, Notifications/, Logging/, Native/
Identify/      → Monitors/, Notifications/, Logging/, Native/
Blackout/      → Monitors/, Notifications/, Logging/, Native/
Monitors/      → Logging/, Native/
Notifications/ → Resources/
Versioning/    → BCL only
Logging/       → BCL only
Resources/     → BCL only
Native/        → no project dependencies
```

- The named mutex in `Lifetime/` is the only permitted single-instance mechanism

## Win32 and P/Invoke

- Prefer `[LibraryImport]` over `[DllImport]` for new declarations
- Group declarations by DLL in `User32.cs`, `Gdi32.cs`, `Shell32.cs`, `Kernel32.cs`, and `Cfgmgr32.cs`, with the display-configuration functions of user32.dll in `DisplayConfig.cs`
- Use `IntPtr` for HWND, HMENU, HICON, and other Win32 handles
- Native structures must use `[StructLayout(LayoutKind.Sequential)]` or `[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]`
- Check every P/Invoke return value and use `Marshal.GetLastWin32Error()` when the function documents `SetLastError = true`
- **Win32 behind an API interface**: code that decides an outcome the specification states for a failed Win32 call makes that call through the API interface of its feature (e.g., `Tray/ITrayIconApi`) or, for the window functions several features call, through `Native/IWindowApi` — never through the P/Invoke declarations directly — so a unit test can make the call fail and check that outcome (`testing.md` Requirement Coverage). The interface's Win32 implementation (e.g., `Tray/Win32TrayIconApi`) converts arguments and results (native structures, strings, embedded icon resources) and decides no such outcome
- Place Win32 constants in `Native/WindowStyles.cs`
- Display user-facing errors through a tray notification balloon or a Win32 `MessageBox`

## Win32 Resource Cleanup

- Destroy windows with `DestroyWindow`, remove the tray icon with `Shell_NotifyIcon(NIM_DELETE, ...)`, destroy menus with `DestroyMenu`, stop timers with `KillTimer`, and unregister window classes during shutdown
- Perform cleanup in the `WM_DESTROY` handler or the application shutdown path

## Window Procedures

- Keep each WndProc delegate in a field of the `Native/WindowClass` that registers it, and keep that `WindowClass` in an object that lives until the class is unregistered at shutdown, so garbage collection cannot reclaim a delegate Windows can still call. The window procedure is an instance method of that object, so each window's state is found in the object rather than in static state
- Call `DefWindowProc` for every message that is not handled explicitly
- Do not use `async` or `await` inside a WndProc
- The hidden message window handles `WM_TRAYICON`, `WM_COMMAND`, `WM_HOTKEY`, `WM_DISPLAYCHANGE`, the display-change `WM_TIMER`, the registered `TaskbarCreated` message, and `WM_CLOSE`. `WM_CLOSE` ends the message loop without destroying the window, because the shutdown path still needs the window to unregister the hotkeys; the shutdown path destroys it
- The blackout window handles `WM_TIMER`, `WM_MOUSEMOVE`, `WM_SETCURSOR`, `WM_LBUTTONDBLCLK`, `WM_RBUTTONUP`, `WM_CLOSE` (ignored, specification WIN-6), and `WM_DESTROY`
- The identify overlay handles `WM_PAINT`, `WM_TIMER`, and `WM_DESTROY`

## System Tray Implementation

- Call `SetForegroundWindow` on the hidden message window before `TrackPopupMenuEx`
- Post `WM_NULL` to the hidden message window after `TrackPopupMenuEx` returns, as required by Microsoft KB Q135788

## Blackout Window Implementation

- Use `WS_POPUP` with `WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE`
- **Shown without activation**: the blackout window and the identify overlay are created without `WS_VISIBLE` and shown with `SetWindowPos(HWND_TOPMOST, ...)` carrying `SWP_SHOWWINDOW | SWP_NOACTIVATE`, because a window created with `WS_VISIBLE` becomes the foreground window whenever ScreenNap owns the foreground at that moment (specification WIN-2, NFR-3)
- Include `CS_DBLCLKS` in the class style
- Use `GetStockObject(BLACK_BRUSH)` for the class background and let `DefWindowProc` handle `WM_ERASEBKGND`
- Use a one-second `SetTimer` interval to reapply `SetWindowPos(HWND_TOPMOST, ...)`
- Route all blackout teardown through `DestroyWindow`, and notify `BlackoutManager` from `WM_DESTROY`

## Monitor Enumeration Implementation

- Resolve friendly monitor names in this order: `QueryDisplayConfig` with `DisplayConfigGetDeviceInfo`, second-level `EnumDisplayDevices`, then `MONITORINFOEX.szDevice` with the `\\.\` prefix removed
- Identify a physical monitor as `docs/adr/0005-monitor-identity-by-serial-number-or-connection.md` decides: the EDID read from the monitor's device node, or the monitor's device interface path from `DisplayConfigGetDeviceInfo`

## Internationalization Implementation

- Store default English strings in `Resources/Strings.resx` and Japanese strings in `Resources/Strings.ja.resx`
- Use PascalCase resource keys with a category prefix and access values through the `Strings` class in `Resources/Strings.cs`
- Format parameterized resources with `string.Format`
- Build-script output must be written in English

## Logging Override

- **Synchronous log I/O**: this section overrides `standard.md` Synchronous vs Asynchronous and `dotnet.md` ASYNC for `Logging/LogFile.cs`. Log writes, the retention scan, and log deletion are synchronous, because their callers run on the UI thread inside a window procedure or the message loop, where Window Procedures in this file prohibits `async` and `await`
- **The logger's own write failures**: this section overrides `standard.md` Failure Handling and Reporting (Never swallow failures) for the `catch` around a log-line write in `Logging/LogFile.cs`. The `catch` swallows the failure, because the logger has no other channel to report its own failure, and the specification requires a logging failure to stay silent to the user

## Release Constraints

- Every `<Version>` change in `Directory.Build.props` must be published as an annotated tag `v{version}` on `main` and a matching GitHub Release; the procedure is `docs/guides/release.md`
- Every release tag must have a corresponding GitHub Release
- Release assets must be produced with `build/Build.ps1` and `build/Installer.ps1`, and every GitHub Release must include both `build/ScreenNap/ScreenNap.exe` and `build/Installer/ScreenNap-Setup-{version}.exe`
- Only the current version stays published: once a new release is verified, the previous version's tag and GitHub Release must be deleted as a pair, locally and on `origin`, so that no tag outlives its release
