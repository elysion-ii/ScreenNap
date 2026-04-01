# ScreenNap Source Code Implementation Plan

## Context

The repository structure, rules, build system, and spec (SPEC.md) are committed. Now we implement all source code to make ScreenNap a functional application. The app uses Raw Win32 API via P/Invoke on .NET 10.0 LTS with zero NuGet dependencies.

## Icon Strategy

The user will create SVG icons and convert them to .ico files manually. For now:
- Generate placeholder .ico files (simple colored squares) via PowerShell so the build works
- Place at: `ScreenNap/Resources/icon-normal.ico` and `ScreenNap/Resources/icon-active.ico`
- Load icons at runtime from embedded resources using a helper method
- User replaces placeholder .ico files with proper designs later

## Implementation Order

Build up from the foundation layer (Native/) to the application layer (App/) to the entry point (Program.cs). Each phase produces compilable code.

### Phase 1: Resource Files + Placeholder Icons
**Files:**
- `ScreenNap/Resources/Strings.resx` — English strings (XML format)
- `ScreenNap/Resources/Strings.ja.resx` — Japanese strings (XML format)
- `ScreenNap/Resources/icon-normal.ico` — Placeholder (gray square, 16x16)
- `ScreenNap/Resources/icon-active.ico` — Placeholder (yellow square, 16x16)

### Phase 2: Native/ Layer (P/Invoke declarations)
**Files:**
- `ScreenNap/Native/WindowStyles.cs` — All Win32 constants
  - Window styles: WS_POPUP, WS_VISIBLE, WS_EX_TOOLWINDOW, WS_EX_TOPMOST, WS_EX_NOACTIVATE
  - Class styles: CS_DBLCLKS, CS_HREDRAW, CS_VREDRAW
  - Messages: WM_DESTROY, WM_PAINT, WM_TIMER, WM_COMMAND, WM_LBUTTONDBLCLK, WM_LBUTTONUP, WM_RBUTTONUP, WM_ERASEBKGND, WM_NULL, WM_CLOSE
  - Tray: NIM_ADD, NIM_MODIFY, NIM_DELETE, NIF_MESSAGE, NIF_ICON, NIF_TIP, NOTIFYICON_VERSION_4
  - Menu: MF_STRING, MF_SEPARATOR, MF_CHECKED, TPM_RIGHTBUTTON, TPM_BOTTOMALIGN
  - SetWindowPos: HWND_TOPMOST, SWP_NOMOVE, SWP_NOSIZE, SWP_NOACTIVATE, SWP_SHOWWINDOW
  - Icon/Image: IMAGE_ICON, LR_DEFAULTSIZE
  - Cursor: IDC_ARROW
  - Brush: GetStockObject constants (BLACK_BRUSH)
  - Custom: WM_TRAYICON (WM_USER + 1)
  - Tooltip: TTS_ALWAYSTIP, TTS_NOPREFIX, TTF_SUBCLASS, TTF_IDISHWND, TTM_ADDTOOL, TTM_DELTOOL, TOOLTIPS_CLASS
  - Common controls: ICC_WIN95_CLASSES

- `ScreenNap/Native/NativeStructs.cs` — All native structs
  - WNDCLASSEXW, MSG, RECT, POINT
  - NOTIFYICONDATAW (Shell_NotifyIcon data)
  - MONITORINFOEXW (with szDevice char[32])
  - MENUITEMINFOW
  - DISPLAYCONFIG_PATH_INFO, DISPLAYCONFIG_MODE_INFO
  - DISPLAYCONFIG_TARGET_DEVICE_NAME (with header)
  - DISPLAYCONFIG_DEVICE_INFO_HEADER
  - DISPLAY_DEVICEW (for EnumDisplayDevices fallback)
  - TOOLINFOW (for tooltip)
  - INITCOMMONCONTROLSEX

- `ScreenNap/Native/User32.cs` — user32.dll P/Invoke
  - Window: RegisterClassExW, UnregisterClassW, CreateWindowExW, DestroyWindow, DefWindowProcW, ShowWindow, SetWindowPos, SetForegroundWindow, PostMessageW, GetModuleHandleW
  - Message loop: GetMessageW, TranslateMessage, DispatchMessageW
  - Menu: CreatePopupMenu, AppendMenuW, TrackPopupMenuEx, DestroyMenu
  - Monitor: EnumDisplayMonitors, GetMonitorInfoW
  - Timer: SetTimer, KillTimer
  - GDI: GetStockObject, LoadImageW, GetCursorPos
  - MessageBox: MessageBoxW
  - Cursor: LoadCursorW
  - Device: EnumDisplayDevicesW
  - SendMessageW (for tooltip)
  - Delegates: WNDPROC, MonitorEnumProc

- `ScreenNap/Native/Shell32.cs` — shell32.dll P/Invoke
  - Shell_NotifyIconW

- `ScreenNap/Native/DisplayConfig.cs` — user32.dll DisplayConfig P/Invoke
  - GetDisplayConfigBufferSizes, QueryDisplayConfig, DisplayConfigGetDeviceInfo
  - Enums: DISPLAYCONFIG_DEVICE_INFO_TYPE, QDC_* flags

### Phase 3: Data Model
**Files:**
- `ScreenNap/App/MonitorInfo.cs` — Immutable record
  - Properties: DevicePath (string), FriendlyName (string), Bounds (RECT), IsPrimary (bool)
  - MenuLabel computed property: "{FriendlyName} {Width}x{Height} [Primary] (Active)"

### Phase 4: Monitor Enumeration
**Files:**
- `ScreenNap/App/MonitorEnumerator.cs` — Static class
  - `EnumerateMonitors()` → `List<MonitorInfo>`
  - Uses EnumDisplayMonitors + GetMonitorInfoW for bounds + device path
  - Uses QueryDisplayConfig + DisplayConfigGetDeviceInfo for friendly names
  - Falls back to EnumDisplayDevices, then szDevice

### Phase 5: Blackout Window
**Files:**
- `ScreenNap/Blackout/BlackoutWindow.cs` — Manages one blackout window
  - Static WNDPROC delegate (prevent GC)
  - RegisterClassExW with CS_DBLCLKS, BLACK_BRUSH
  - CreateWindowExW with WS_POPUP | WS_VISIBLE + WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE
  - SetTimer for TopMost maintenance (1s interval)
  - WM_TIMER → SetWindowPos(HWND_TOPMOST)
  - WM_LBUTTONDBLCLK → DestroyWindow
  - WM_DESTROY → KillTimer + notify BlackoutManager via callback
  - WM_ERASEBKGND → return 1
  - Tooltip: create TOOLTIPS_CLASS child, TTM_ADDTOOL with dismiss hint text
  - Properties: Handle (IntPtr), DevicePath (string)

### Phase 6: Blackout Manager
**Files:**
- `ScreenNap/App/BlackoutManager.cs` — Lifecycle manager
  - Dictionary<string, BlackoutWindow> keyed by device path
  - Toggle(MonitorInfo) — create or destroy blackout for a monitor
  - ReleaseAll() — destroy all blackouts
  - ActiveCount property
  - OnBlackoutDestroyed callback (for WM_DESTROY cleanup)
  - ActiveCountChanged event (for tray icon state update)

### Phase 7: Icon Helper
**Files:**
- `ScreenNap/App/IconHelper.cs` — Load HICON from embedded .ico resources
  - LoadIconFromResource(string resourceName) → IntPtr (HICON)
  - Reads embedded resource stream, parses .ico header, calls CreateIconFromResourceEx

### Phase 8: Tray Icon + Context Menu
**Files:**
- `ScreenNap/App/TrayIcon.cs` — Manages tray icon
  - Create/remove via Shell_NotifyIcon
  - Normal/active icon switching
  - Tooltip update
  - Handles WM_TRAYICON messages (WM_LBUTTONUP / WM_RBUTTONUP → show menu)

- `ScreenNap/App/ContextMenu.cs` — Builds and shows Win32 popup menu
  - ShowMenu(IntPtr hwnd, BlackoutManager manager)
  - Enumerates monitors, builds menu items with labels
  - Adds "Release All" separator + item when ActiveCount > 0
  - Adds separator + "Exit"
  - Handles WM_COMMAND routing:
    - Monitor items: toggle blackout
    - Release All: manager.ReleaseAll()
    - Exit: PostQuitMessage(0)
  - Menu item IDs: monitors = 1000+index, Release All = 2000, Exit = 9999

### Phase 9: Entry Point
**Files:**
- `ScreenNap/Program.cs` — Application entry point
  - Named Mutex ("ScreenNap_SingleInstance") for single-instance check
  - If already running: MessageBoxW with Strings.NotifyAlreadyRunning, then exit
  - Register hidden message window class (WndProc routes WM_TRAYICON and WM_COMMAND)
  - Create hidden HWND_MESSAGE window
  - Initialize TrayIcon, BlackoutManager
  - Wire BlackoutManager.ActiveCountChanged → TrayIcon icon/tooltip update
  - Win32 message loop: GetMessageW / TranslateMessage / DispatchMessageW
  - On WM_DESTROY: cleanup TrayIcon, BlackoutManager.ReleaseAll()
  - InitCommonControlsEx for tooltip support

## File List (14 new files)

| # | File | Phase |
|---|------|-------|
| 1 | Resources/Strings.resx | 1 |
| 2 | Resources/Strings.ja.resx | 1 |
| 3 | Resources/icon-normal.ico | 1 (placeholder) |
| 4 | Resources/icon-active.ico | 1 (placeholder) |
| 5 | Native/WindowStyles.cs | 2 |
| 6 | Native/NativeStructs.cs | 2 |
| 7 | Native/User32.cs | 2 |
| 8 | Native/Shell32.cs | 2 |
| 9 | Native/DisplayConfig.cs | 2 |
| 10 | App/MonitorInfo.cs | 3 |
| 11 | App/MonitorEnumerator.cs | 4 |
| 12 | Blackout/BlackoutWindow.cs | 5 |
| 13 | App/BlackoutManager.cs | 6 |
| 14 | App/IconHelper.cs | 7 |
| 15 | App/TrayIcon.cs | 8 |
| 16 | App/ContextMenu.cs | 8 |
| 17 | Program.cs | 9 |

## Verification

1. `dotnet build ScreenNap/ScreenNap.csproj -c Release` — must compile without errors
2. `dotnet run --project ScreenNap/ScreenNap.csproj` — tray icon appears, context menu lists monitors, blackout toggle works
3. Double-click blackout window — dismisses
4. Launch second instance — MessageBox shown, process exits
5. Tooltip on hover over blackout — "Double-click to dismiss" shown
