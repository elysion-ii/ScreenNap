# ScreenNap — Agent Instructions

ScreenNap is a raw Win32 system tray application targeting `net10.0-windows`.

`CLAUDE.md` at the repository root is a one-line import of this file. This file is the
repository's router — facts, commands, and reading instructions; it holds no rule text.
Rule bodies live under `docs/rules/`. Edit this file, never `CLAUDE.md`.

## Technology Stack

| Item | Detail |
|------|--------|
| Language | C# |
| Runtime | `net10.0-windows` (.NET 10 LTS) |
| UI | Raw Win32 API via P/Invoke; no UI framework |
| Target OS | Windows 10 / 11 (x64) |
| Distribution | Self-contained single EXE + Inno Setup installer |
| Runtime dependencies | .NET runtime bundled in the EXE; no external NuGet runtime packages |
| License | MIT |

## Applications

| Application | Projects | Rules | Specification |
|---|---|---|---|
| ScreenNap | ScreenNap, ScreenNap.Tests | `docs/rules/ScreenNap.md` | `docs/specs/ScreenNap.md` |

## Rules and AUDIT

- **Before implementing any change**, read in order: `docs/rules/standard.md` (shared core), `docs/rules/dotnet.md` (.NET rules), then the application's rules file and specification from the Applications table. On conflict the more specific file wins (application > language > core)
- **Before creating, changing, moving, renaming, archiving, or deleting any document**, also read `docs/rules/documentation.md`
- **Before any Git write operation or PR operation** (commit, branch, push, PR creation, update, or merge), also read `docs/rules/git.md`
- When a change requires behavior not in the specification, update the specification **before** implementing (spec-first — see the Specifications section of `docs/rules/standard.md`)
- **When transitioning from a plan to implementation**, re-read this file (root and any nested `AGENTS.md` covering the work area) and the rules files first, so all rules are loaded before code is written
- **Before reporting an implementation task as complete**, run the AUDIT procedure at the end of `docs/rules/standard.md`
- `docs/rules/standard.md`, `docs/rules/documentation.md`, `docs/rules/git.md`, and `docs/rules/dotnet.md` are managed by dev-standards — never edit them; repository- and application-specific rules go in `docs/rules/ScreenNap.md`

## Commands

| Purpose | Command |
|---------|---------|
| Format | `dotnet format ScreenNap.slnx` |
| Format check (must pass before completion) | `dotnet format ScreenNap.slnx --verify-no-changes` |
| Build | `dotnet build ScreenNap/ScreenNap.csproj -c Release` |
| Test | `dotnet test ScreenNap.Tests/ScreenNap.Tests.csproj` |
| Full build (format gate → tests → publish) | `build/Menu.bat` (interactive) or `powershell -ExecutionPolicy Bypass -File build/Build.ps1` |
| Installer | `powershell -ExecutionPolicy Bypass -File build/Installer.ps1` |

## Directory Layout

### `ScreenNap/`

The main application project contains the source code.

- `Program.cs` contains the application entry point, named mutex, and Win32 message loop
- `Native/` contains P/Invoke declarations, Win32 constants, and native structures
- `Core/` contains shared contracts, domain types, and pure decision logic
- `App/` contains application orchestration, including the tray icon, context menu, monitor enumeration, blackout lifecycle, global hotkeys, and monitor identification
- `Blackout/` contains the blackout-window implementation
- `Logging/` contains best-effort file logging and retention
- `Resources/` contains localized string resources and icon assets

### `ScreenNap.Tests/`

The xUnit test project runs as a gate in `Build.ps1` before publishing.

### `build/`

The directory contains build scripts and installer configuration.

- `Build.ps1` runs format verification and tests, then publishes ScreenNap as a self-contained single-file EXE to `build/ScreenNap/`
- `Installer.ps1` invokes Inno Setup on `Setup_ScreenNap.iss`; it reads `<Version>` from `Directory.Build.props`, injects `/DMyAppVersion`, and verifies the matching `CHANGELOG.md` heading
- `build/ScreenNap/` and `build/Installer/` are generated, gitignored output directories

### `docs/`

All non-source documents live in role-based subfolders. Before creating, changing,
moving, renaming, archiving, or deleting a document, read
`docs/rules/documentation.md` first. It defines placement, naming, and front matter.
Do not classify or name documents from memory.

- `docs/rules/` contains `standard.md`, `documentation.md`, `git.md`, and `dotnet.md` managed by dev-standards, plus the ScreenNap-specific rule body
- `docs/specs/ScreenNap.md` defines ScreenNap behavior
- `docs/adr/` contains active Architecture Decision Records; retired ADRs move to `docs/adr/archive/`
- `docs/plans/` and `docs/archive/plans/` are gitignored working areas; plans never enter the repository
