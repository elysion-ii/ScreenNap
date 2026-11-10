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
| Distribution | Native AOT single EXE + Inno Setup installer |
| Runtime dependencies | Ahead-of-time compiled to native code; no .NET runtime, no external NuGet runtime packages |
| License | MIT |

## Applications

| Application | Projects | Rules | Specification |
|---|---|---|---|
| ScreenNap | ScreenNap, ScreenNap.Tests, ScreenNap.E2ETests | `docs/rules/ScreenNap.md` | `docs/specs/ScreenNap.md` |

## Rules and AUDIT

Rules in `docs/rules/` are organized into three tiers:
- **Application rules**: `docs/rules/ScreenNap.md` (this repository's own rules and overrides)
- **Stack rules**: `docs/rules/dotnet.md` (.NET stack rules, managed by dev-standards)
- **Core rules**: Shared engineering standards (`standard.md`, `documentation.md`, `git.md`, `specs.md`, `testing.md`, and the conditional core files `cli.md` and `db.md`; managed by dev-standards)

**Precedence on conflict**: Application rules > Stack rules > Core rules.

- **Rule Scope**: which rule files and specification every change complies with, and when `cli.md` and `db.md` apply, is stated in `docs/rules/standard.md` (How to use these rules). The specification of ScreenNap is `docs/specs/ScreenNap.md`.
- **Loading Rules**: Read this file and every file under `docs/rules/` before writing code (including when transitioning from a plan to implementation).
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
| Full build (format gate → tests → publish) | `build/Menu.bat` (interactive) or `powershell -ExecutionPolicy Bypass -File build/Build.ps1` |
| Installer | `powershell -ExecutionPolicy Bypass -File build/Installer.ps1` |

## Shared Knowledge

Where each piece of shared knowledge lives in ScreenNap (`docs/rules/standard.md` One Home per Piece of Knowledge).

| Knowledge | Home |
|---|---|
| Which directory log files go to, how a log line is formatted and written, how a Win32 error code is appended to a log message, and which old log files are deleted | `Logger` (`ScreenNap/Logging/Logger.cs`) |
| How a localized user-facing string is looked up, and what is shown when the lookup fails | `Strings` (`ScreenNap/Resources/Strings.cs`) |
| The wording of every failure notification shown to the user | `FailureNotices` (`ScreenNap/Core/FailureNotices.cs`) |
| How an operation's failure reaches the user | `IUserNotifier` (`ScreenNap/Core/IUserNotifier.cs`) |
| How the product version is read and shown as `ScreenNap X.Y.Z` | `AppVersion` (`ScreenNap/App/AppVersion.cs`) |
| How the active monitors are enumerated with their bounds, primary state, name, and identity | `MonitorEnumerator.EnumerateMonitors` (`ScreenNap/App/MonitorEnumerator.cs`) |
| The order in which a monitor's friendly name falls back | `MonitorNameResolver` (`ScreenNap/Core/MonitorNameResolver.cs`) |
| What identifies a physical monitor across display-path changes | `MonitorIdentity` (`ScreenNap/Core/MonitorIdentity.cs`) |
| The global hotkey IDs, the action each ID selects, and how a hotkey combination is named | `HotkeyInterpreter` (`ScreenNap/Core/HotkeyInterpreter.cs`) |
| The context-menu command IDs | `MenuCommandIds` (`ScreenNap/Core/MenuModel.cs`) |
| Which overlays the identify hotkey shows or closes, and which failures it reports | `MonitorIdentifier` (`ScreenNap/App/MonitorIdentifier.cs`) |
| How a window class is registered once and unregistered at shutdown | `WindowClass` (`ScreenNap/Native/WindowClass.cs`) |
| How an embedded `.ico` resource becomes a tray icon handle | `IconHelper.LoadIconFromResource` (`ScreenNap/App/IconHelper.cs`) |
| The P/Invoke declarations of one Win32 DLL | that DLL's class in `ScreenNap/Native/` (`User32`, `Gdi32`, `Shell32`, `Kernel32`, `DisplayConfigApi`) |
| Win32 constants | `WindowStyles` (`ScreenNap/Native/WindowStyles.cs`) |
| How a unit test sets the culture and restores it | `CultureScope` (`ScreenNap.Tests/TestDoubles/CultureScope.cs`) |
| How a unit test substitutes blackout windows | `FakeBlackoutWindowFactory` (`ScreenNap.Tests/TestDoubles/FakeBlackoutWindowFactory.cs`) |
| How a unit test substitutes identification overlays | `FakeIdentifyOverlays` (`ScreenNap.Tests/TestDoubles/FakeIdentifyOverlays.cs`) |
| How a unit test captures user notifications | `FakeUserNotifier` (`ScreenNap.Tests/TestDoubles/FakeUserNotifier.cs`) |
| How an end-to-end test starts the built ScreenNap with a log directory of its own, drives its message window and menu, and stops it | `ScreenNapApp` (`ScreenNap.E2ETests/Infrastructure/ScreenNapApp.cs`) |
| Where an end-to-end test finds the built executable, the repository root, and the release assets, and how it creates and removes its scratch directories | `TestAssets` (`ScreenNap.E2ETests/Infrastructure/TestAssets.cs`) |
| How an end-to-end test waits for an observable condition with a time limit | `Wait` (`ScreenNap.E2ETests/Infrastructure/Wait.cs`) |
| How an end-to-end test reads windows, monitors, pixels, and the cursor in the DPI awareness of the window it measures | `Desktop` (`ScreenNap.E2ETests/Infrastructure/Desktop.cs`) |
| How an end-to-end test presses a global hotkey and probes whether one is held | `Keyboard` (`ScreenNap.E2ETests/Infrastructure/Keyboard.cs`) |
| The Win32 declarations and constants the end-to-end tests use | `Win32` (`ScreenNap.E2ETests/Infrastructure/Win32.cs`) |

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

The directory contains build scripts and installer configuration.

- `Build.ps1` runs the configuration-file check, format verification, the requirement-coverage check, and tests, then publishes ScreenNap as a Native AOT single EXE to `build/ScreenNap/`; every check must pass before publish proceeds
- `Installer.ps1` invokes Inno Setup on `Setup_ScreenNap.iss`; it reads `<Version>` from `Directory.Build.props`, injects `/DMyAppVersion`, and verifies the matching `CHANGELOG.md` heading
- `build/ScreenNap/` and `build/Installer/` are generated, gitignored output directories

### `docs/`

All non-source documents live in role-based subfolders. Before creating, changing,
moving, renaming, archiving, or deleting a document, read
`docs/rules/documentation.md` first. It defines placement, naming, and front matter.

- `docs/rules/` contains `standard.md`, `documentation.md`, `git.md`, `specs.md`, `testing.md`, `cli.md`, `db.md`, and `dotnet.md` managed by dev-standards, plus `ScreenNap.md` (application rules)
- `docs/specs/ScreenNap.md` defines ScreenNap behavior
- `docs/adr/` contains active Architecture Decision Records; retired ADRs move to `docs/adr/archive/`
- `docs/guides/release.md` is the version bump → assets → tag → GitHub Release procedure
- `docs/plans/` and `docs/archive/plans/` are gitignored working areas (`docs/rules/documentation.md`, `docs/plans/`)
