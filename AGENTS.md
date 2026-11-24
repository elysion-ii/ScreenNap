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
| Distribution | Native AOT single EXE + Inno Setup installer (`docs/rules/dotnet-client.md`, NATIVEDEP) |
| Runtime dependencies | Ahead-of-time compiled to native code; no .NET runtime, no external NuGet runtime packages |
| License | MIT |

## Applications

| Application | Projects | Rules | Specification |
|---|---|---|---|
| ScreenNap | ScreenNap, ScreenNap.Tests, ScreenNap.E2ETests | `docs/rules/ScreenNap.md` | `docs/specs/ScreenNap.md` |

## Rules and AUDIT

Rules in `docs/rules/` are organized into three tiers:
- **Application rules**: `docs/rules/ScreenNap.md` (this repository's own rules and overrides)
- **Stack rules**: `docs/rules/dotnet.md`, plus every .NET facet rule file under `docs/rules/` (e.g., `dotnet-client.md`) (managed by dev-standards)
- **Shared rules**: Universal engineering standards (`docs/rules/standard.md`, `docs/rules/documentation.md`, `docs/rules/specs.md`, `docs/rules/testing.md`), plus every conditional shared rule file under `docs/rules/` (managed by dev-standards)

**Precedence on conflict**: Application rules > Stack rules > Shared rules.

- **Rule Scope**: which rule files and specification every change complies with — and when to report missing conditional shared rule domains — is stated in `docs/rules/standard.md` (How to Use These Rules). The specification of ScreenNap is `docs/specs/ScreenNap.md`.
- **Loading Rules**: Read this file, every applicable file under `docs/rules/`, `docs/specs/ScreenNap.md`, and `docs/references/code-map.md` before writing code (including when transitioning from a plan to implementation).
- **Completion Gate (AUDIT)**: Before reporting an implementation task as complete, pass the AUDIT gate in `docs/rules/standard.md`.
- **File Ownership**: rule files under `docs/rules/` managed by dev-standards are copies of its pinned tag; repository- and application-specific rules are maintained in `docs/rules/ScreenNap.md`.

## Commands

| Purpose | Command |
|---------|---------|
| Format | `dotnet format ScreenNap.slnx` |
| Format check (must pass before completion) | `dotnet format ScreenNap.slnx --verify-no-changes` |
| Build | `dotnet build ScreenNap/ScreenNap.csproj -c Release` |
| Test | `dotnet test ScreenNap.Tests/ScreenNap.Tests.csproj` |
| End-to-end test | `dotnet test ScreenNap.E2ETests/ScreenNap.E2ETests.csproj` |
| Full build (gates → publish) | `build/Menu.bat` (interactive) or `powershell -ExecutionPolicy Bypass -File build/Build.ps1` |
| Installer | `powershell -ExecutionPolicy Bypass -File build/Installer.ps1` |

## Code Map

Feature folders and shared mechanism homes in ScreenNap are listed in `docs/references/code-map.md` (`docs/rules/documentation.md`, Code Map; `docs/rules/standard.md`, One Home per Piece of Knowledge).

## Directory Layout

### `ScreenNap/`

The main application project contains the source code.

- `Program.cs` contains the application entry point, named mutex, and Win32 message loop
- `Native/` contains P/Invoke declarations, Win32 constants, native structures, and the window-class registration helper
- `Core/` contains shared contracts, domain types, and pure decision logic
- `App/` contains application orchestration, including the tray icon, context menu, monitor enumeration, blackout lifecycle, global hotkeys, and monitor identification
- `Blackout/` contains the blackout-window implementation
- `Logging/` contains best-effort file logging and retention
- `Resources/` contains localized string resources and icon assets

### `ScreenNap.Tests/`

The xUnit unit-test project runs as a gate in `Build.ps1` before publishing.

### `ScreenNap.E2ETests/`

The xUnit end-to-end test project starts the built `ScreenNap.exe` and drives it through
its windows (`docs/adr/0003-end-to-end-tests-drive-the-built-application.md`). It is not
part of `Build.ps1`.

- The tests run on an interactive Windows desktop; during a run, blackout windows cover the monitors and keyboard input is injected
- Every test fails at once while a ScreenNap process is running; exit the resident ScreenNap first
- The distribution tests use `build/ScreenNap/ScreenNap.exe` and `build/Installer/ScreenNap-Setup-{version}.exe`, so `build/Build.ps1` and `build/Installer.ps1` run first
- The installer tests install into a temporary directory and fail at once where ScreenNap is installed for the current user

### `build/`

Build scripts and installer configuration.

- `Build.ps1` runs the configuration-file check, format verification (`dotnet format --verify-no-changes`), requirement-coverage check, and tests, then publishes ScreenNap as a Native AOT single EXE to `build/ScreenNap/`. All checks must pass before publish proceeds
- `Menu.bat` is an interactive menu over `Build.ps1` and `Installer.ps1`
- `Installer.ps1` invokes Inno Setup (ISCC.exe) on `Setup_ScreenNap.iss`; requires `build/ScreenNap/ScreenNap.exe` to exist. Before running ISCC it reads `<Version>` from `Directory.Build.props` (`docs/rules/dotnet.md`, VERSION), injects it via `/DMyAppVersion`, and verifies as its release packaging gate that `CHANGELOG.md` contains a heading for the current version (failing otherwise)
- Output directories: `build/ScreenNap/` (publish output) and `build/Installer/` (installer package), both gitignored (`docs/rules/dotnet.md`, OUTPUT)

### `docs/`

All non-source documents, placed in role-based subfolders (`rules/`, `adr/`, `specs/`,
`guides/`, `references/`, `investigations/`, `notes/`, `plans/`, `inbox/`, `archive/`).
Before creating, changing, moving, renaming, archiving, or deleting any document — or
when unsure where one belongs — read `docs/rules/documentation.md` first; it defines
placement, naming, and front matter.

- `docs/rules/` — rule bodies: universal shared rules (`docs/rules/standard.md`, `docs/rules/documentation.md`, `docs/rules/specs.md`, `docs/rules/testing.md`), `docs/rules/dotnet.md`, the conditional shared and .NET facet rule files distributed to this repository (all managed by dev-standards), and `docs/rules/ScreenNap.md` (application rules)
- `docs/specs/ScreenNap.md` — the ScreenNap specification: what it does
- `docs/references/code-map.md` — feature folders and shared mechanism homes
- `docs/adr/` — Architecture Decision Records; retired ADRs move to `docs/adr/archive/`
- `docs/guides/release.md` — the version bump → assets → tag → GitHub Release procedure
- `docs/plans/` and `docs/archive/plans/` — working area for plans, gitignored (`docs/rules/documentation.md`, Category Notes — `docs/plans/`)
