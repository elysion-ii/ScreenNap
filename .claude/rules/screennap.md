# ScreenNap Project Rules

Project-specific rules for Win32/P/Invoke development in ScreenNap.

---

## WIN32: P/Invoke Conventions

- **Prefer `[LibraryImport]`** (source-generated) over `[DllImport]` for all new declarations.
- **Group by DLL:** `User32.cs`, `Gdi32.cs`, `Shell32.cs`, `DisplayConfig.cs`. One file per DLL.
- **Handle types:** Use `IntPtr` for window handles (HWND), menu handles (HMENU), icon handles (HICON), and other Win32 handles.
- **Struct layout:** All native structs MUST have `[StructLayout(LayoutKind.Sequential)]` or `[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]`.
- **Return value checking:** Always check return values after P/Invoke calls. Use `Marshal.GetLastWin32Error()` for functions that document `SetLastError = true`.

---

## WNDPROC: Window Procedure

- **Delegate pinning:** WndProc callback delegates MUST be stored in a static field to prevent garbage collection.
- **Default handling:** Always call `DefWindowProc` for messages not explicitly handled.
- **No async:** Do NOT use `async`/`await` inside WndProc. Win32 message handling is synchronous.
- **Message routing:**
  - Hidden message window: `WM_TRAYICON` (custom), `WM_COMMAND`, `WM_HOTKEY`
  - Blackout window: `WM_PAINT`, `WM_LBUTTONDBLCLK`, `WM_TIMER`, `WM_ERASEBKGND`, `WM_DESTROY`
  - Identify overlay: `WM_PAINT`, `WM_TIMER`, `WM_DESTROY`

---

## TRAY: System Tray Icon

- Call `SetForegroundWindow` on the hidden message window before `TrackPopupMenuEx`. This is required for the menu to dismiss correctly when the user clicks outside it.
- Post `WM_NULL` to the hidden message window after `TrackPopupMenuEx` returns (Microsoft KB Q135788).
- Both left-click and right-click open the context menu.
- Icon state: normal (no active blackouts) and active (1+ active blackouts). Update icon and tooltip via `Shell_NotifyIcon(NIM_MODIFY, ...)`.

---

## BLACKOUT: Blackout Window

- **Window styles:** `WS_POPUP | WS_VISIBLE` with extended styles `WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE`.
- **Class style:** Include `CS_DBLCLKS` to receive `WM_LBUTTONDBLCLK` for dismiss.
- **Background:** `GetStockObject(BLACK_BRUSH)` as the class background brush. Return `1` from `WM_ERASEBKGND`.
- **TopMost maintenance:** Use `SetTimer` (1-second interval) to call `SetWindowPos(HWND_TOPMOST, ...)` periodically.
- **Dismiss:** Handle `WM_LBUTTONDBLCLK` → `DestroyWindow`. Notify `BlackoutManager` from `WM_DESTROY`.
- **Focus:** `WS_EX_NOACTIVATE` prevents focus stealing when the window is created or re-topped.

---

## MONITOR: Monitor Enumeration

Friendly name resolution chain (try in order, fall back on failure):

1. `QueryDisplayConfig` + `DisplayConfigGetDeviceInfo` → `DISPLAYCONFIG_TARGET_DEVICE_NAME`
2. `EnumDisplayDevices` (second-level call) → device string
3. `MONITORINFOEX.szDevice` with `\\.\` prefix removed

Re-enumerate monitors on every context menu open. Handle disconnected monitors gracefully (if a darkened monitor is unplugged, Windows destroys the window; `BlackoutManager` must clean up its tracking state without crashing).

---

## I18N: Internationalization

- **Resource files:** `Resources/Strings.resx` (English default), `Resources/Strings.ja.resx` (Japanese).
- **String keys:** PascalCase with category prefix (e.g., `MenuExit`, `TooltipActive`, `NotifyAlreadyRunning`).
- **Access:** Via the generated `Strings` class (e.g., `Strings.MenuExit`). Parameterized strings use `string.Format`.
- **Not localized:** Code comments, log messages, build script output, git commit messages.
- **Adding a language:** Create `Resources/Strings.xx.resx` with translated strings. No code changes required.
