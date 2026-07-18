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
- Dependencies must follow these directions:

```text
Program.cs → App/, Blackout/, Logging/, Native/, Resources/
App/ → Core/, Logging/, Native/, Resources/
Blackout/ → Core/, Logging/, Native/
Core/ → Native/, Resources/
Logging/ → BCL only
Native/ → no project dependencies
```

- `Native/` must not reference another project layer
- `Blackout/` must not reference `App/`
- `Core/` must not reference `App/`, `Blackout/`, or `Logging/`, and it must not call P/Invoke or I/O methods
- The named mutex in `Program.cs` is the only permitted single-instance mechanism

## Win32 and P/Invoke

- Prefer `[LibraryImport]` over `[DllImport]` for new declarations
- Group declarations by DLL in `User32.cs`, `Gdi32.cs`, `Shell32.cs`, and `DisplayConfig.cs`
- Use `IntPtr` for HWND, HMENU, HICON, and other Win32 handles
- Native structures must use `[StructLayout(LayoutKind.Sequential)]` or `[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]`
- Check every P/Invoke return value and use `Marshal.GetLastWin32Error()` when the function documents `SetLastError = true`
- Use explicit types for P/Invoke return values, overriding `dotnet.md` VAR when the type would otherwise be considered obvious
- Place Win32 constants in `Native/WindowStyles.cs`
- Display user-facing errors through a tray notification balloon or a Win32 `MessageBox`

## Win32 Resource Cleanup

- Destroy windows with `DestroyWindow`, remove the tray icon with `Shell_NotifyIcon(NIM_DELETE, ...)`, destroy menus with `DestroyMenu`, stop timers with `KillTimer`, and unregister window classes during shutdown
- Perform cleanup in the `WM_DESTROY` handler or the application shutdown path

## Window Procedures

- Store WndProc delegates in static fields so garbage collection cannot reclaim them
- Call `DefWindowProc` for every message that is not handled explicitly
- Do not use `async` or `await` inside a WndProc
- The hidden message window handles `WM_TRAYICON`, `WM_COMMAND`, `WM_HOTKEY`, `WM_DISPLAYCHANGE`, the display-change `WM_TIMER`, and `WM_DESTROY`
- The blackout window handles `WM_TIMER`, `WM_MOUSEMOVE`, `WM_SETCURSOR`, `WM_LBUTTONDBLCLK`, `WM_RBUTTONUP`, and `WM_DESTROY`
- The identify overlay handles `WM_PAINT`, `WM_TIMER`, and `WM_DESTROY`

## System Tray Implementation

- Call `SetForegroundWindow` on the hidden message window before `TrackPopupMenuEx`
- Post `WM_NULL` to the hidden message window after `TrackPopupMenuEx` returns, as required by Microsoft KB Q135788

## Blackout Window Implementation

- Use `WS_POPUP | WS_VISIBLE` with `WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE`
- Include `CS_DBLCLKS` in the class style
- Use `GetStockObject(BLACK_BRUSH)` for the class background and let `DefWindowProc` handle `WM_ERASEBKGND`
- Use a one-second `SetTimer` interval to reapply `SetWindowPos(HWND_TOPMOST, ...)`
- Route all blackout teardown through `DestroyWindow`, and notify `BlackoutManager` from `WM_DESTROY`

## Monitor Enumeration Implementation

- Resolve friendly monitor names in this order: `QueryDisplayConfig` with `DisplayConfigGetDeviceInfo`, second-level `EnumDisplayDevices`, then `MONITORINFOEX.szDevice` with the `\\.\` prefix removed
- Use EDID manufacturer ID, product code, and connector instance as the stable identity for restoring blackout state across display-path changes

## Internationalization Implementation

- Store default English strings in `Resources/Strings.resx` and Japanese strings in `Resources/Strings.ja.resx`
- Use PascalCase resource keys with a category prefix and access values through the generated `Strings` class
- Format parameterized resources with `string.Format`
- Build-script output must be written in English

## Logging Override

- This section overrides `dotnet.md` LOGGING: write logs directly to `{Environment.GetFolderPath(SpecialFolder.LocalApplicationData)}\ScreenNap\Logs\`, not to `AppContext.BaseDirectory`

## Release Constraints

- Every release tag must have a corresponding GitHub Release
- Release assets must be produced with `build/Build.ps1` and `build/Installer.ps1`, and every GitHub Release must include both `build/ScreenNap/ScreenNap.exe` and `build/Installer/ScreenNap-Setup-{version}.exe`
