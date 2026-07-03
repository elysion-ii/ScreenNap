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
- **`Core/`**: Shared contracts, domain types, and pure decision logic. P/Invoke and I/O calls are prohibited; references to native structs and constants are allowed.
- **`App/`**: Application-level orchestration (tray icon, context menu, monitor enumeration, `BlackoutManager`, global hotkey management, monitor identify overlay).
- **`Blackout/`**: The blackout window implementation (window class registration, creation, WndProc message handling).
- **`Logging/`**: Best-effort file logging and retention. Logging failures must never terminate the application.
- **`Resources/`**: String resources (.resx) for i18n, and icon files (.ico).
- **Rules:** Developers MUST read and strictly adhere to `.claude/rules/coding-standards.md` (shared) and `.claude/rules/screennap.md` (project-specific) during development.

### `ScreenNap.Tests/`

xUnit test project. Runs as a gate in `Build.ps1` before publish.

- **Rules:** See `.claude/rules/testing.md`

### `build/`

Build scripts and installer configuration.

- **Entry Point:** `Menu.bat` (interactive menu) or `Build.ps1`
- **Installer:** `Installer.ps1` + `Setup_ScreenNap.iss`
- **Rules:** See `.claude/rules/build.md`

### `docs/`

Specifications and design documents. Place documents that do not fit Rule, Test, or ADR categories here.

### `adr/`

Architecture Decision Records. Record the reasoning, alternatives considered, and applicability conditions of design decisions.

### `plans/`

Working area for in-progress plans. Contents are gitignored. Move files to `archive/plans/` before commit per `.claude/rules/git-commits.md`.

### `archive/plans/`

Completed plan files preserved for reference.

## Architecture Rules

### No UI Frameworks

This application uses raw Win32 API through P/Invoke. Do NOT introduce WinForms, WPF, WinUI, or any other UI framework.

### No External NuGet Packages

All functionality is provided through .NET BCL and Win32 P/Invoke. Do NOT add NuGet package references.

### Dependency Direction

Dependencies must flow in one direction only:

```
Program.cs → App/, Blackout/, Logging/, Native/, Resources/
App/ → Core/, Logging/, Native/, Resources/
Blackout/ → Core/, Logging/, Native/
Core/ → Native/, Resources/
Logging/ → BCL only
Native/ → no project dependencies
```

- `Native/` must NEVER reference other project layers.
- `Blackout/` must NEVER reference `App/`.
- `Core/` must NEVER reference `App/`, `Blackout/`, or `Logging/`, and must NEVER call P/Invoke or I/O methods.

### Single-Instance Enforcement

Named Mutex in `Program.cs` exclusively. No other single-instance mechanism.

## Rule File Authoring

Rules files (`.claude/CLAUDE.md` and all files under `.claude/rules/`) MUST be written in **English only**.

Keep rule content concise and declarative. Do NOT include concrete code examples unless absolutely necessary — reference the relevant source file/method instead. This saves context window budget.
