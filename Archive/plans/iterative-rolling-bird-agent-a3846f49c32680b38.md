# ScreenNap -- Repository Structure and Implementation Plan

## Table of Contents

1. Complete Repository Directory Structure
2. .claude/CLAUDE.md Content
3. .claude/rules/ Files
4. Build/ Folder Contents
5. Solution and Project Structure
6. i18n Design
7. Updated Spec Considerations

---

## 1. Complete Repository Directory Structure

```
ScreenNap/
+-- .claude/
|   +-- CLAUDE.md
|   +-- settings.local.json
|   +-- rules/
|       +-- coding-standards.md
|       +-- build.md
|       +-- git-commits.md
|       +-- screennap.md
+-- .github/
|   +-- copilot-instructions.md
+-- AGENTS.md
+-- GEMINI.md
+-- .gitignore
+-- .gitattributes
+-- LICENSE
+-- README.md
+-- CHANGELOG.md
+-- ScreenNap.slnx
+-- ScreenNap/
|   +-- ScreenNap.csproj
|   +-- Program.cs
|   +-- Resources/
|   |   +-- Strings.resx
|   |   +-- Strings.ja.resx
|   |   +-- icon-normal.ico
|   |   +-- icon-active.ico
|   +-- Native/
|   |   +-- User32.cs
|   |   +-- Shell32.cs
|   |   +-- DisplayConfig.cs
|   |   +-- WindowStyles.cs
|   |   +-- NativeStructs.cs
|   +-- App/
|   |   +-- TrayIcon.cs
|   |   +-- ContextMenu.cs
|   |   +-- MonitorEnumerator.cs
|   |   +-- MonitorInfo.cs
|   |   +-- BlackoutManager.cs
|   +-- Blackout/
|       +-- BlackoutWindow.cs
+-- Build/
|   +-- Menu.bat
|   +-- Build.ps1
|   +-- Installer.ps1
|   +-- Setup_ScreenNap.iss
+-- plans/
```

### File-by-file purpose summary

| File | Purpose |
|------|---------|
| Program.cs | Entry point. Named Mutex for single-instance. Registers hidden message-only window class for tray icon messages. Runs Win32 message loop (GetMessage/DispatchMessage). |
| Native/User32.cs | P/Invoke signatures: RegisterClassEx, CreateWindowEx, DefWindowProc, GetMessage, TranslateMessage, DispatchMessage, DestroyWindow, PostQuitMessage, SetWindowPos, ShowWindow, SetForegroundWindow, GetMonitorInfo, EnumDisplayMonitors, CreatePopupMenu, InsertMenuItem, TrackPopupMenuEx, DestroyMenu, SetTimer, KillTimer, MessageBox |
| Native/Shell32.cs | P/Invoke: Shell_NotifyIcon (add/modify/delete tray icon) |
| Native/DisplayConfig.cs | P/Invoke: QueryDisplayConfig, DisplayConfigGetDeviceInfo for friendly monitor names |
| Native/WindowStyles.cs | Constants: WS_POPUP, WS_VISIBLE, WS_EX_TOOLWINDOW, WS_EX_TOPMOST, WS_EX_NOACTIVATE, HWND_TOPMOST, etc. |
| Native/NativeStructs.cs | Structs: WNDCLASSEX, MSG, RECT, POINT, NOTIFYICONDATA, MENUITEMINFO, MONITORINFOEX, DISPLAYCONFIG_* structs |
| App/TrayIcon.cs | Wraps Shell_NotifyIcon. Creates/updates/removes tray icon. Handles WM_TRAYICON callback. Toggles normal/active icon. Updates tooltip. |
| App/ContextMenu.cs | Builds and shows Win32 popup menu. Refreshes monitor list on each open. Handles WM_COMMAND for menu selections. Adds Release All when any monitor is darkened. Adds Exit at bottom. |
| App/MonitorEnumerator.cs | Uses EnumDisplayMonitors + GetMonitorInfo for bounds/device name. Uses QueryDisplayConfig + DisplayConfigGetDeviceInfo for friendly names. Falls back through spec chain. |
| App/MonitorInfo.cs | Immutable record: DevicePath, FriendlyName, Bounds (RECT), IsPrimary. |
| App/BlackoutManager.cs | Dictionary of active blackout windows keyed by device path. Toggle on/off. Release all. Fires events on count change for tray icon state update. |
| Blackout/BlackoutWindow.cs | Registers unique window class per blackout. Creates WS_POPUP + WS_VISIBLE with WS_EX_TOOLWINDOW + WS_EX_TOPMOST. Paints #000000. Handles WM_LBUTTONDBLCLK for dismiss. SetTimer for TopMost re-application. Tooltip on hover. |

---

## 2. .claude/CLAUDE.md Content

The CLAUDE.md should follow this structure (matching RioOnline pattern):

### Section: Project Structure and Overview

Opening paragraph: ScreenNap is a Windows system tray application that displays fullscreen black windows on selected monitors to protect OLED screens from burn-in. By displaying pure black (#000000), OLED pixels are physically turned off without changing the Windows display configuration.

### Section: Technology Stack

Table with: Language (C#), Framework (.NET 10.0, Raw Win32 API via P/Invoke, no WinForms/WPF/WinUI), Target OS (Windows 10/11 x64), Distribution (Portable self-contained single EXE + Inno Setup installer), License (MIT).

### Section: Directory Layout

Describe each top-level folder:

- **ScreenNap/**: The main (and only) project. Contains all application source code.
  - **Native/**: P/Invoke declarations, Win32 constants, native struct definitions. No business logic. Purely declarative.
  - **App/**: Application-level components (tray icon, context menu, monitor enumeration, blackout lifecycle management).
  - **Blackout/**: The blackout window implementation (window class registration, creation, WndProc message handling).
  - **Resources/**: String resources (.resx) for i18n, and icon files (.ico).
  - **Program.cs**: Application entry point (Mutex, message loop).
- **Build/**: Build scripts and installer configuration.
  - Entry Point: Menu.bat (interactive menu) or Build.ps1
  - Installer: Installer.ps1 + Setup_ScreenNap.iss
  - Rules: See .claude/rules/build.md

### Section: Architecture Rules

**No UI Frameworks**: This application uses raw Win32 API through P/Invoke. Do NOT introduce WinForms, WPF, WinUI, or any other UI framework.

**Dependency Direction**:

    Program.cs -> App/ -> Native/
                       -> Blackout/ -> Native/

Native/ must NEVER reference App/ or Blackout/. Blackout/ must NEVER reference App/.

**No External NuGet Packages**: Zero external dependencies. All functionality through .NET BCL and Win32 P/Invoke.

**Single-Instance Enforcement**: Named Mutex in Program.cs exclusively.

### Section: Rule File Authoring

Rules files must be written in English only. Keep rule content concise and declarative. Do NOT include concrete code examples unless absolutely necessary -- reference the relevant source file/method instead.

---

## 3. .claude/rules/ Files

### 3.1 coding-standards.md

Sections (adapted from RioOnline, removing DB/SQL/ORM/WPF sections):

- **COMMENTS**: English language, simple inline comments, no XML docs. Explain "why" for complex Win32 logic.
- **NAMESPACE**: File-scoped namespaces only.
- **VAR**: Use var for obvious types, explicit for unclear types. Always use explicit types for P/Invoke return values (IntPtr, int, bool).
- **STRING**: Always specify StringComparison. Ordinal for device paths/identifiers.
- **ERROR**: No empty catch blocks. Check Win32 return values after P/Invoke calls. Use Marshal.GetLastWin32Error() when needed. User-facing errors via tray notification balloon.
- **FILEPATH**: Use AppContext.BaseDirectory. Do NOT use Directory.GetCurrentDirectory().
- **CONSTANTS**: No magic numbers. Win32 constants in Native/WindowStyles.cs. App constants as const in owning class.
- **DISPOSE**: DestroyWindow for windows, Shell_NotifyIcon(NIM_DELETE) for tray icon, DestroyMenu for menus, KillTimer for timers. Cleanup in WM_DESTROY or shutdown path.

### 3.2 build.md

Sections:

- **SCRIPTS**: Menu.bat options (1=Build, 2=Installer, 3=Full Build, 9=Exit). Build.ps1 publishes single-file self-contained. Installer.ps1 invokes ISCC.exe.
- **OUTPUT**: Build/ScreenNap/ (EXE), Build/Installer/ (setup EXE). Do not commit output dirs.
- **VERSION**: Single source in ScreenNap.csproj Version tag. Sync to Setup_ScreenNap.iss and Installer.ps1. Semantic versioning. Start at 0.1.0.
- **VERIFY**: dotnet build ScreenNap/ScreenNap.csproj -c Release

### 3.3 git-commits.md

Rules:
- Commit messages entirely in English.
- Clear, concise descriptions.
- Conventional commit format (e.g., feat:, fix:, refactor:, docs:, build:).
- Before committing, ensure plans/ folder is clean. Move plan files to Archive/plan/.

### 3.4 screennap.md

Project-specific rules for Win32/P/Invoke development. Sections:

- **WIN32**: P/Invoke conventions. Prefer [LibraryImport] over [DllImport]. Group by DLL in Native/. Use IntPtr for handles. All structs must have [StructLayout]. Check return values.
- **WNDPROC**: WndProc callbacks must be stored in static field (prevent GC of delegate). Always call DefWindowProc for unhandled messages. No async in WndProc. Document message handling for blackout window (WM_PAINT, WM_LBUTTONDBLCLK, WM_TIMER, WM_ERASEBKGND) and hidden message window (WM_TRAYICON, WM_COMMAND).
- **TRAY**: Shell_NotifyIcon lifecycle. SetForegroundWindow before TrackPopupMenuEx. Post WM_NULL after (KB Q135788). Both left/right click open menu.
- **BLACKOUT**: Window creation specs (styles, position, class style CS_DBLCLKS, BLACK_BRUSH). TopMost maintenance via SetTimer/WM_TIMER. Dismissal via WM_LBUTTONDBLCLK. BlackoutManager notification on destroy.
- **MONITOR**: Friendly name resolution chain (QueryDisplayConfig -> EnumDisplayDevices -> szDevice fallback). Re-enumerate on every menu open. Handle disconnected monitor gracefully.
- **I18N**: Resource file conventions. String keys use PascalCase with category prefix. Access via generated Strings class. Code comments and log messages not localized.

---

## 4. Build/ Folder Contents

### 4.1 Menu.bat

Interactive build menu with 3 options + exit. Simpler than RioOnline (which has 6 options for 3 projects + merge + installer + full). Options:
- 1 = Build (calls Build.ps1)
- 2 = Installer (calls Installer.ps1, requires 1)
- 3 = Full Build (1 then 2)
- 9 = Exit

Same structure as RioOnline: cls, echo menu, set /p choice, goto labels, error handling with pause, loop back to menu.

### 4.2 Build.ps1

Adapted from RioOnline Build-System function. Key differences:
- No -Target parameter (single project)
- Framework: net10.0-windows (not net9.0-windows)
- Project path: ScreenNap/ScreenNap.csproj (one level, not two)
- No appsettings.yaml handling (no config files to copy)
- Output: Build/ScreenNap/

Core publish command flags:
- -c Release
- -f net10.0-windows
- -r win-x64
- --self-contained true
- -p:PublishSingleFile=true
- -p:IncludeNativeLibrariesForSelfExtract=true
- -p:DebugType=none
- -p:DebuggerSupport=false

### 4.3 Setup_ScreenNap.iss

**AppId GUID: {E4A72F8B-3D19-4C5A-9F61-B8D2E5C7A043}**

Key differences from RioOnline installer:
- PrivilegesRequired=lowest (per-user install, no admin needed) instead of admin
- DefaultDirName={autopf}/ScreenNap instead of C:/
- Only one EXE to deploy (single file, no dependencies)
- LicenseFile pointing to MIT LICENSE
- Optional "Start with Windows" task (HKCU Run registry key)
- English task descriptions (public open-source)
- No appsettings.yaml preservation logic (no config files)

Installer features:
- Desktop shortcut (unchecked by default)
- Start Menu shortcut (checked by default)
- Start with Windows (unchecked by default, writes HKCU Run key)
- Optional post-install launch

[Setup] section key values:
- AppId={{E4A72F8B-3D19-4C5A-9F61-B8D2E5C7A043}
- AppName=ScreenNap
- AppVersion=0.1.0
- DefaultDirName={autopf}/ScreenNap
- Compression=lzma, SolidCompression=yes
- ArchitecturesAllowed=x64compatible
- ArchitecturesInstallIn64BitMode=x64compatible
- PrivilegesRequired=lowest
- LicenseFile=..\LICENSE

[Tasks] section:
- desktopicon (unchecked)
- startmenu
- startup (unchecked, "Start with Windows")

[Files] section:
- Source: ScreenNap/ScreenNap.exe -> {app}

[Registry] section (conditional on startup task):
- HKCU Run key for auto-start

[Run] section:
- Post-install launch option

### 4.4 Installer.ps1

Adapted from RioOnline. Simpler validation:
- Check Inno Setup exists at standard path
- Check Build/ScreenNap/ScreenNap.exe exists
- Run ISCC.exe on Setup_ScreenNap.iss
- Report success/failure

---

## 5. Solution and Project Structure

### 5.1 ScreenNap.slnx

XML format solution file at repo root. Single project reference:

    <Solution>
      <Project Path="ScreenNap/ScreenNap.csproj" />
    </Solution>

One level flatter than RioOnline (which has SolutionDir/ProjectDir/file.csproj).

### 5.2 ScreenNap.csproj

Key properties:

| Property | Value | Rationale |
|----------|-------|-----------|
| OutputType | WinExe | Suppresses console window |
| TargetFramework | net10.0-windows | .NET 10 LTS with Windows-specific APIs |
| AllowUnsafeBlocks | true | Required for fixed char buffers in P/Invoke structs |
| Nullable | enable | Null safety |
| ImplicitUsings | enable | Standard .NET implicit usings |
| Version | 0.1.0 | Initial version |
| ApplicationIcon | Resources/icon-normal.ico | EXE icon |
| PublishSingleFile | true | Single-file distribution |
| SelfContained | true | No runtime dependency |
| IncludeNativeLibrariesForSelfExtract | true | Bundle native libs in single file |

What is ABSENT (intentionally):
- No UseWindowsForms (no WinForms)
- No UseWPF (no WPF)
- No PackageReference (zero NuGet dependencies)

EmbeddedResource items for Strings.resx (with ResXFileCodeGenerator) and Strings.ja.resx (DependentUpon Strings.resx).

### 5.3 Namespace Structure

| Folder | Namespace |
|--------|-----------|
| ScreenNap/ | ScreenNap |
| ScreenNap/Native/ | ScreenNap.Native |
| ScreenNap/App/ | ScreenNap.App |
| ScreenNap/Blackout/ | ScreenNap.Blackout |
| ScreenNap/Resources/ | ScreenNap.Resources |

---

## 6. i18n Design

### Approach

Standard .NET Resource Files (.resx). The app has approximately 10-15 user-visible strings. This is the lightest-weight approach requiring zero external dependencies.

### File Structure

    ScreenNap/Resources/
    +-- Strings.resx              (Default = English)
    +-- Strings.Designer.cs       (Auto-generated accessor class)
    +-- Strings.ja.resx           (Japanese)
    +-- (future: Strings.xx.resx)

### String Key Inventory

| Key | English (default) | Japanese | Usage |
|-----|-------------------|----------|-------|
| MenuReleaseAll | Release All | すべて解除 | Context menu item |
| MenuExit | Exit | 終了 | Context menu item |
| MenuPrimary | [Primary] | [メイン] | Primary monitor suffix |
| MenuActive | (Active) | (暗転中) | Darkened monitor suffix |
| TooltipNormal | ScreenNap | ScreenNap | Tray tooltip (idle) |
| TooltipActive | ScreenNap ({0} active) | ScreenNap ({0}台 暗転中) | Tray tooltip with count |
| NotifyAlreadyRunning | ScreenNap is already running. | ScreenNap は既に起動しています。 | Second-instance message |
| BlackoutDismissHint | Double-click to dismiss | ダブルクリックで解除 | Blackout window tooltip |
| NotifyTitle | ScreenNap | ScreenNap | Notification/MessageBox title |

### How It Works

- Strings.resx (English) is the default/fallback resource.
- .NET selects the correct satellite assembly based on CultureInfo.CurrentUICulture.
- No manual culture switching needed; OS language setting determines UI language.
- Access via strongly-typed generated class: Strings.MenuExit, Strings.TooltipNormal, etc.
- Parameterized strings: string.Format(Strings.TooltipActive, count).

### Adding a New Language

1. Create Resources/Strings.xx.resx with translated strings.
2. No code changes required.

### What Is NOT Localized

- Code comments (English)
- Build script output (English)
- Git commit messages (English)
- No logging system in this app.

---

## 7. Updated Spec Considerations

### 7.1 WinForms to Raw Win32 Migration

| Spec Item | WinForms Approach | Raw Win32 Approach |
|-----------|-------------------|-------------------|
| Black window | Form with FormBorderStyle.None | CreateWindowEx with WS_POPUP |
| Background color | BackColor = Color.Black | GetStockObject(BLACK_BRUSH) in WNDCLASS + WM_PAINT |
| TopMost | TopMost = true | WS_EX_TOPMOST + SetWindowPos(HWND_TOPMOST) |
| ShowInTaskbar=false | Property | WS_EX_TOOLWINDOW extended style |
| Double-click dismiss | DoubleClick event | WM_LBUTTONDBLCLK (requires CS_DBLCLKS class style) |
| TopMost timer | System.Windows.Forms.Timer | SetTimer / WM_TIMER |
| Tray icon | NotifyIcon component | Shell_NotifyIcon API |
| Context menu | ContextMenuStrip | CreatePopupMenu + TrackPopupMenuEx |
| Monitor bounds | Screen.Bounds | EnumDisplayMonitors + GetMonitorInfo (rcMonitor) |
| Tooltip | ToolTip component | Win32 TOOLTIPS_CLASS common control |
| Message loop | Application.Run() | GetMessage / TranslateMessage / DispatchMessage |
| Single instance dialog | MessageBox.Show() | User32 MessageBox (still available) |

### 7.2 .NET 9 to .NET 10 Changes

- TFM: net9.0-windows -> net10.0-windows
- Support: STS (18 months) -> LTS (3 years), better for utility app
- [LibraryImport] preferred over [DllImport] for source-generated P/Invoke

### 7.3 Blackout Window Tooltip Design Decision

Recommendation: Win32 Tooltip Common Control (TOOLTIPS_CLASS). Creates a tooltip control associated with the blackout window via TTM_ADDTOOL. Preserves the spec intent, keeps window pure black, standard Win32 pattern. Setup is approximately 20 lines of code.

### 7.4 Second-Instance Notification

Use Win32 MessageBox (User32.MessageBox with MB_OK + MB_ICONINFORMATION) for simplicity. The second instance has no tray icon to show a balloon from.

### 7.5 Memory Expectations

Without WinForms/WPF, expected working set: 10-15MB (primarily .NET runtime). Below the original 20MB target. Near-zero CPU as GetMessage blocks when idle.

### 7.6 WS_EX_NOACTIVATE Consideration

The blackout window should include WS_EX_NOACTIVATE in its extended style. This prevents the blackout window from stealing focus when created or when TopMost is reapplied, which is important when the user is actively working on another monitor.

### 7.7 Monitor Hot-Plug Handling

If a darkened monitor is disconnected, Windows destroys the window automatically. BlackoutManager must handle WM_DESTROY gracefully and clean up its tracking dictionary without crashing.

---

## Implementation Sequence

### Phase 1: Skeleton
Create all directories, config files, .slnx, .csproj, minimal Program.cs with Mutex + empty message loop. Verify dotnet build.

### Phase 2: Native Declarations
All Native/ files with P/Invoke signatures, structs, constants, enums.

### Phase 3: Tray Icon
TrayIcon.cs, hidden message window in Program.cs, basic context menu with Exit only.

### Phase 4: Monitor Enumeration
MonitorEnumerator.cs, MonitorInfo.cs, wire into context menu.

### Phase 5: Blackout Windows
BlackoutWindow.cs, BlackoutManager.cs, toggle via menu, TopMost timer, double-click dismiss.

### Phase 6: State Management
Icon toggling, tooltip updates, menu checkmarks, Release All item.

### Phase 7: i18n Resources
Create .resx files, replace hardcoded strings.

### Phase 8: Tooltip on Blackout
Win32 tooltip common control for dismiss hint.

### Phase 9: Build Scripts
Menu.bat, Build.ps1, Installer.ps1, Setup_ScreenNap.iss.

### Phase 10: Polish
README.md, LICENSE, CHANGELOG.md, final review.

---

## Other Configuration Files

### .gitignore

Standard .NET entries plus:
- Build/ScreenNap/
- Build/Installer/
- Build/publish_temp*/

### .gitattributes

Text normalization for cs, xml, json, md, txt, yaml, resx, iss, csproj. CRLF for bat/cmd/ps1. LF for sh. Binary for ico/png/exe/dll.

### AGENTS.md and GEMINI.md

Both point to .claude/ directory (identical to RioOnline pattern):

    # Agent Instructions
    For project rules and development guidelines, see the .claude/ directory:
    - .claude/CLAUDE.md - Project overview and directory structure
    - .claude/rules/ - Detailed rules by topic
      - build.md - Build and deployment script rules
      - coding-standards.md - C# coding standards
      - git-commits.md - Git commit rules
      - screennap.md - ScreenNap-specific development rules (Win32, P/Invoke)

### .claude/settings.local.json

Permissions allow: dotnet build, dotnet restore, dotnet publish, ls, find, grep.

---

## Summary of Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Solution format | .slnx (XML) | Matches RioOnline convention |
| AppId GUID | {E4A72F8B-3D19-4C5A-9F61-B8D2E5C7A043} | New unique GUID for Inno Setup |
| Installer privilege | PrivilegesRequired=lowest | Per-user install for tray app |
| P/Invoke style | [LibraryImport] preferred | Source-generated, .NET 7+ best practice |
| i18n | .resx resource files | Zero-dependency, standard .NET |
| Default language | English | Public repo |
| Japanese support | Strings.ja.resx | Primary user base |
| Tooltip implementation | Win32 TOOLTIPS_CLASS | Standard Win32, keeps blackout pure black |
| Second-instance notify | MessageBox (Win32) | Simple, no tray icon needed |
| External dependencies | None | Minimal footprint, no supply chain risk |
