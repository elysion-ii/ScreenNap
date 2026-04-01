# ScreenNap Repository Structure Plan

## Context

ScreenNap is a public Windows tray app that displays fullscreen black windows to protect OLED monitors from burn-in. The current spec exists as `ScreenNap_仕様書.md` but lacks repo structure, rules, and build system. This plan defines the complete repository layout, coding rules, build infrastructure, and i18n strategy -- reverse-engineered from the deliverables (portable EXE + installer) and modeled after the RioOnline reference repo's organizational patterns.

## Key Decisions

| Decision | Choice |
|----------|--------|
| Framework | Raw Win32 API via P/Invoke (no WinForms/WPF/WinUI) |
| .NET | 10.0 LTS (net10.0-windows) |
| Distribution | Portable single EXE + Inno Setup installer |
| License | MIT |
| Language | English-only (comments, docs, commits) |
| i18n | .resx resource files (English default, Japanese included) |
| External NuGet | None (zero dependencies) |

---

## 1. Repository Directory Structure

```
ScreenNap/                         (repo root)
├── .claude/
│   ├── CLAUDE.md                  # Project overview, directory layout, architecture rules
│   └── rules/
│       ├── coding-standards.md    # Shared C# coding standards
│       ├── build.md               # Build system rules
│       ├── git-commits.md         # Git commit conventions
│       └── screennap.md           # Win32/P/Invoke project-specific rules
├── .github/
│   └── copilot-instructions.md    # Points to .claude/ (for GitHub Copilot)
├── AGENTS.md                      # Points to .claude/ (for Codex/other agents)
├── GEMINI.md                      # Points to .claude/ (for Gemini)
├── .gitignore
├── .gitattributes
├── LICENSE                        # MIT License
├── README.md
├── CHANGELOG.md
├── ScreenNap.slnx                 # Solution file (XML format, at root)
├── ScreenNap/                     # Project folder (1 level, not 2 like RioOnline)
│   ├── ScreenNap.csproj
│   ├── Program.cs                 # Entry point: Mutex, message loop
│   ├── Native/                    # P/Invoke declarations only (no business logic)
│   │   ├── User32.cs
│   │   ├── Shell32.cs
│   │   ├── DisplayConfig.cs
│   │   ├── WindowStyles.cs        # Win32 constants (WS_POPUP, WS_EX_TOOLWINDOW, etc.)
│   │   └── NativeStructs.cs       # WNDCLASSEX, MSG, RECT, NOTIFYICONDATA, etc.
│   ├── App/                       # Application logic
│   │   ├── TrayIcon.cs            # Shell_NotifyIcon wrapper, icon state toggle
│   │   ├── ContextMenu.cs         # Win32 popup menu, monitor list, commands
│   │   ├── MonitorEnumerator.cs   # EnumDisplayMonitors + DisplayConfig friendly names
│   │   ├── MonitorInfo.cs         # Immutable record: name, bounds, isPrimary
│   │   └── BlackoutManager.cs     # Active blackout tracking, toggle, release all
│   ├── Blackout/                  # Blackout window implementation
│   │   └── BlackoutWindow.cs      # WS_POPUP window, #000000, TopMost timer, dismiss
│   └── Resources/                 # i18n string resources + icons
│       ├── Strings.resx           # English (default)
│       ├── Strings.ja.resx        # Japanese
│       ├── icon-normal.ico
│       └── icon-active.ico
└── Build/
    ├── Menu.bat                   # Interactive build menu (3 options)
    ├── Build.ps1                  # dotnet publish orchestrator
    ├── Installer.ps1              # Inno Setup invoker
    └── Setup_ScreenNap.iss        # Inno Setup script (AppId GUID included)
```

### Comparison with RioOnline

| Aspect | RioOnline | ScreenNap |
|--------|-----------|-----------|
| Solutions | 3 (.sln, .slnx) | 1 (.slnx) |
| Project depth | `AppName/AppName/AppName.csproj` | `ScreenNap/ScreenNap.csproj` |
| Build menu | 6 options (3 projects + merge + installer + full) | 3 options (build + installer + full) |
| Merge step | Required (3 projects → 1 folder) | Not needed (single project) |
| Database | SQL Server + SQLite | None |
| NuGet packages | Multiple | Zero |

---

## 2. .claude/CLAUDE.md Outline

Following RioOnline's structure:

1. **Project overview** -- What ScreenNap is, OLED burn-in prevention mechanism
2. **Technology stack** -- .NET 10 LTS, Raw Win32, no UI frameworks
3. **Directory layout** -- Each folder's purpose and contents
4. **Architecture rules**:
   - Dependency direction: `Program.cs → App/ → Native/`, `Blackout/ → Native/`
   - No UI frameworks, no NuGet packages
   - Single-instance via named Mutex in Program.cs only
5. **Rule file authoring** -- English only, concise, declarative

---

## 3. .claude/rules/ Files

### coding-standards.md
Adapted from RioOnline (removed: DB/SQL/ORM/WPF/DTO/Customer sections):
- **COMMENTS**: English, inline `//`, explain "why" for complex Win32 logic
- **NAMESPACE**: File-scoped only
- **VAR**: `var` for obvious types, explicit for P/Invoke returns (IntPtr, int, bool)
- **STRING**: Always specify `StringComparison`
- **ERROR**: No empty catch blocks. Check Win32 return values. `Marshal.GetLastWin32Error()` when needed
- **FILEPATH**: `AppContext.BaseDirectory` only
- **CONSTANTS**: No magic numbers. Win32 constants in `Native/WindowStyles.cs`
- **DISPOSE**: DestroyWindow, Shell_NotifyIcon(NIM_DELETE), DestroyMenu, KillTimer

### build.md
- **SCRIPTS**: Menu.bat (3 options), Build.ps1, Installer.ps1
- **OUTPUT**: `Build/ScreenNap/` (EXE), `Build/Installer/` (setup). Do not commit
- **VERSION**: Single source in .csproj `<Version>`. Sync to Setup_ScreenNap.iss + Installer.ps1
- **VERIFY**: `dotnet build ScreenNap/ScreenNap.csproj -c Release`

### git-commits.md
- English only, conventional commits (feat:, fix:, docs:, build:, etc.)

### screennap.md
Project-specific Win32/P/Invoke rules:
- **WIN32**: Prefer `[LibraryImport]` over `[DllImport]`. Group by DLL. `[StructLayout]` on all structs
- **WNDPROC**: Store delegates in static fields (prevent GC). Always call DefWindowProc for unhandled messages
- **TRAY**: SetForegroundWindow before TrackPopupMenuEx. Post WM_NULL after (KB Q135788)
- **BLACKOUT**: WS_POPUP + WS_EX_TOOLWINDOW + WS_EX_TOPMOST + WS_EX_NOACTIVATE. CS_DBLCLKS for dismiss. TopMost re-apply via SetTimer
- **MONITOR**: Friendly name chain: QueryDisplayConfig → EnumDisplayDevices → szDevice fallback
- **I18N**: PascalCase keys with category prefix. Access via generated `Strings` class

---

## 4. Build/ Folder

### AppId GUID (Inno Setup)
```
{E4A72F8B-3D19-4C5A-9F61-B8D2E5C7A043}
```

### Menu.bat
| Option | Action |
|--------|--------|
| 1 | Build → `Build.ps1` |
| 2 | Installer → `Installer.ps1` (requires 1) |
| 3 | Full Build → 1 then 2 |
| 9 | Exit |

### Build.ps1
Single-project publish:
```
dotnet publish ScreenNap/ScreenNap.csproj -c Release -f net10.0-windows -r win-x64
    --self-contained true -p:PublishSingleFile=true
    -p:IncludeNativeLibrariesForSelfExtract=true
    -p:DebugType=none -p:DebuggerSupport=false -o Build/ScreenNap
```

### Setup_ScreenNap.iss
- `PrivilegesRequired=lowest` (per-user install, no admin)
- `DefaultDirName={autopf}\ScreenNap`
- Tasks: desktop shortcut, start menu, start with Windows (via HKCU\Run)
- LicenseFile: `../LICENSE`

---

## 5. Project Configuration (.csproj)

| Property | Value | Rationale |
|----------|-------|-----------|
| OutputType | WinExe | No console window |
| TargetFramework | net10.0-windows | .NET 10 LTS |
| AllowUnsafeBlocks | true | P/Invoke fixed-size char buffers |
| Nullable | enable | Null safety |
| Version | 0.1.0 | Initial version |
| ApplicationIcon | Resources\icon-normal.ico | EXE icon |

**Intentionally absent**: UseWindowsForms, UseWPF, PackageReference

---

## 6. i18n Design

### String Resources (lightweight, ~10 keys)

| Key | English (default) | Japanese |
|-----|-------------------|----------|
| MenuReleaseAll | Release All | すべて解除 |
| MenuExit | Exit | 終了 |
| MenuPrimary | [Primary] | [メイン] |
| MenuActive | (Active) | (暗転中) |
| TooltipNormal | ScreenNap | ScreenNap |
| TooltipActive | ScreenNap ({0} active) | ScreenNap ({0}台 暗転中) |
| NotifyAlreadyRunning | ScreenNap is already running. | ScreenNap は既に起動しています。 |
| BlackoutDismissHint | Double-click to dismiss | ダブルクリックで解除 |

### How it works
- `Strings.resx` = English default. `Strings.ja.resx` = Japanese
- .NET auto-selects based on `CultureInfo.CurrentUICulture` (OS language)
- Adding a new language = add `Strings.xx.resx`, zero code changes
- Access: `Strings.MenuExit`, `string.Format(Strings.TooltipActive, count)`

---

## 7. Spec Updates (WinForms → Raw Win32)

| Item | WinForms | Raw Win32 |
|------|----------|-----------|
| Black window | `Form` + `FormBorderStyle.None` | `CreateWindowEx` + `WS_POPUP` |
| Background | `BackColor = Color.Black` | `GetStockObject(BLACK_BRUSH)` |
| TopMost | `TopMost = true` | `WS_EX_TOPMOST` + `SetWindowPos(HWND_TOPMOST)` |
| Hide from taskbar | `ShowInTaskbar = false` | `WS_EX_TOOLWINDOW` |
| Hide from Alt+Tab | `WS_EX_TOOLWINDOW` override | Same (`WS_EX_TOOLWINDOW`) |
| Focus prevention | N/A | `WS_EX_NOACTIVATE` (new addition) |
| Double-click dismiss | `DoubleClick` event | `WM_LBUTTONDBLCLK` (`CS_DBLCLKS`) |
| TopMost timer | `System.Windows.Forms.Timer` | `SetTimer` / `WM_TIMER` |
| Tray icon | `NotifyIcon` component | `Shell_NotifyIcon` API |
| Context menu | `ContextMenuStrip` | `CreatePopupMenu` + `TrackPopupMenuEx` |
| Monitor bounds | `Screen.Bounds` | `EnumDisplayMonitors` + `GetMonitorInfo` |
| Dismiss tooltip | `ToolTip` component | `TOOLTIPS_CLASS` common control |
| Message loop | `Application.Run()` | `GetMessage` / `TranslateMessage` / `DispatchMessage` |
| Memory target | ~20MB | ~10-15MB (no framework overhead) |

### New considerations
- **WS_EX_NOACTIVATE**: Prevents blackout from stealing focus when created/re-topped
- **Monitor hot-unplug**: Windows destroys the window; BlackoutManager must clean up gracefully

---

## Deliverables (what this plan will produce)

1. `.claude/CLAUDE.md` -- Repository documentation
2. `.claude/rules/*.md` (4 files) -- Development rules
3. `Build/` folder (4 files) -- Build scripts + installer config with GUID
4. `ScreenNap.slnx` -- Solution file
5. `.gitignore`, `.gitattributes`, `LICENSE`, `AGENTS.md`, `GEMINI.md`, `.github/copilot-instructions.md`
6. Updated `ScreenNap_仕様書.md` -- Reflecting new tech decisions

**NOT included in this phase**: Source code implementation (Program.cs, Native/, App/, Blackout/, Resources/). That is a separate implementation phase.

## Verification

After creating the deliverables:
1. `dotnet new sln` equivalent via .slnx validates
2. `dotnet build` will fail (no source yet) but .csproj structure is correct
3. Build scripts are syntactically valid (Menu.bat, Build.ps1, Installer.ps1)
4. All rule files are English-only and follow RioOnline conventions
