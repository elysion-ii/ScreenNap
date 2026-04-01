# Project Structure and Overview

ScreenNap is a Windows system tray application that displays fullscreen black windows on selected monitors to protect OLED screens from burn-in. By displaying pure black (#000000), OLED pixels are physically turned off without changing the Windows display configuration.

## Technology Stack

| Item | Detail |
|------|--------|
| Language | C# |
| Runtime | .NET 10.0 LTS (net10.0-windows) |
| UI | Raw Win32 API via P/Invoke (no WinForms, WPF, or WinUI) |
| Target OS | Windows 10 / 11 (x64) |
| Distribution | Portable self-contained single EXE + Inno Setup installer |
| External Dependencies | None (zero NuGet packages) |
| License | MIT |

## Directory Layout

### `ScreenNap/`

The main (and only) project. Contains all application source code.

- **`Program.cs`**: Application entry point. Named Mutex for single-instance enforcement. Win32 message loop.
- **`Native/`**: P/Invoke declarations, Win32 constants, native struct definitions. Purely declarative — no business logic.
- **`App/`**: Application-level components (tray icon, context menu, monitor enumeration, blackout lifecycle management, global hotkey management).
- **`Blackout/`**: The blackout window implementation (window class registration, creation, WndProc message handling).
- **`Resources/`**: String resources (.resx) for i18n, and icon files (.ico).
- **Rules:** Developers MUST read and strictly adhere to `.claude/rules/coding-standards.md` (shared) and `.claude/rules/screennap.md` (project-specific) during development.

### `Build/`

Build scripts and installer configuration.

- **Entry Point:** `Menu.bat` (interactive menu) or `Build.ps1`
- **Installer:** `Installer.ps1` + `Setup_ScreenNap.iss`
- **Rules:** See `.claude/rules/build.md`

## Architecture Rules

### No UI Frameworks

This application uses raw Win32 API through P/Invoke. Do NOT introduce WinForms, WPF, WinUI, or any other UI framework.

### No External NuGet Packages

All functionality is provided through .NET BCL and Win32 P/Invoke. Do NOT add NuGet package references.

### Dependency Direction

Dependencies must flow in one direction only:

```
Program.cs → App/ → Native/
                  → Blackout/ → Native/
```

- `Native/` must NEVER reference `App/` or `Blackout/`.
- `Blackout/` must NEVER reference `App/`.

### Single-Instance Enforcement

Named Mutex in `Program.cs` exclusively. No other single-instance mechanism.

## Rule File Authoring

Rules files (`.claude/CLAUDE.md` and all files under `.claude/rules/`) MUST be written in **English only**.

Keep rule content concise and declarative. Do NOT include concrete code examples unless absolutely necessary — reference the relevant source file/method instead. This saves context window budget.
