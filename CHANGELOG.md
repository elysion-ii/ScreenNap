# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.7.1] - 2026-11-10

### Changed
- A text whose localized resource cannot be found now shows its resource key instead of built-in English text
- Converged the repository onto dev-standards v4.31.0, including the specification rules, the shared-knowledge table, and a build error for APIs that resolve paths against the working directory

### Fixed
- A blackout window no longer takes the keyboard focus from the active window when it appears, and neither does an identification overlay
- Pressing the identify hotkey now reports every monitor whose identification number cannot be shown, also when no overlay can be created at all

## [1.7.0] - 2026-11-02

### Added
- ScreenNap now tells you when something fails: a message box when it cannot start or cannot create its tray icon, and a tray notification when a blackout cannot be created, released, or restored, when hotkeys are taken by another application, or when an identification number cannot be shown
- The `SCREENNAP_LOG_DIR` environment variable chooses the log directory

### Changed
- Log lines carry their UTC offset (for example `2026-11-02 15:30:12.345 +09:00`)
- A log file that cannot be deleted during cleanup is now logged, and the remaining expired logs are still deleted
- Converged the repository onto dev-standards v4.28.0, including end-to-end tests that drive the built application

### Fixed
- A blackout window that Windows destroyed is now removed from the tray count even when no blackout is waiting to be restored
- A missing resource set now falls back to the built-in English text instead of failing

## [1.6.0] - 2026-09-04

### Changed
- Updating an installed ScreenNap while it is running now closes it and starts it again after the install, instead of leaving files locked
- Uninstalling while ScreenNap is running now offers to close it and retry, so an uninstall no longer leaves a half-removed installation behind. ScreenNap is never force-closed: when it does not shut down gracefully, the choice returns to you
- Converged the repository onto dev-standards v4.16.0: a second enumeration of a LINQ query now fails the build, and the shared rules gained the deferred-execution, running-application, and declarative-code sections

## [1.5.0] - 2026-08-20

### Changed
- Publish ScreenNap with Native AOT: the portable EXE is compiled to native code and no longer carries the .NET runtime
- Converged the repository onto dev-standards v4.12.0: the publish shape moved from `Build.ps1` into the csproj, a self-extracting single-file publish now fails the build, `Build.ps1` gained the configuration-file check, and the installer ships the whole publish directory
- The release workflow is the single publisher: pushing a `v*` tag builds both assets and creates the GitHub Release with that version's changelog section as its notes

## [1.4.0] - 2026-07-30

### Added
- Application version display as a disabled item at the top of the tray context menu (`ScreenNap X.Y.Z`)

### Changed
- Converged the repository onto dev-standards v4.8.0: added the `cli.md` rule file, updated the shared rule bodies, excluded build metadata from the informational version, and applied `Method_Scenario_ExpectedResult` naming across the test suite
- Startup log and menu version display read the same single version source

## [1.3.2] - 2026-07-10

### Added
- `.editorconfig` and `Directory.Build.props` as the single source of formatting and analyzer configuration
- Format verification (`dotnet format --verify-no-changes`) as a `Build.ps1` gate

### Fixed
- Cleared the Roslyn analyzer warning backlog (CA1822, CA1806, CA1305, CA1863) and enabled `TreatWarningsAsErrors`

## [1.3.1] - 2026-07-03

### Added
- Unit tests for blackout state transitions, monitor and menu decisions, hotkeys, cursor idle behavior, icon parsing, tray state, and logging rules

### Changed
- Refactored decision logic into a functional core to improve testability without changing application behavior
- Updated project ownership and repository links to elysion-ii
- Made log timestamps culture-independent

## [1.3.0] - 2026-04-02

### Added
- Auto-restore blackout windows after display configuration change (RDP connect/disconnect, dock/undock)
- Stable monitor identity using EDID + connector instance for reliable matching across display reconfigurations
- WM_DISPLAYCHANGE handling with debounce for display change detection

## [1.2.1] - 2026-04-02

### Fixed
- Standardize hotkey notation to Ctrl+Shift+Alt (conventional modifier order)

## [1.2.0] - 2026-04-02

### Added
- Monitor identify overlay: Ctrl+Shift+Alt+0 shows monitor numbers on screen (auto-dismisses after 3 seconds)

## [1.1.0] - 2026-04-01

### Added
- Global hotkey support: Ctrl+Shift+Alt+1~9 to toggle blackout per monitor
- Monitor number display in context menu

## [1.0.0] - 2026-04-01

### Changed
- Removed tooltip from blackout window

### Added
- Version display in startup log
- CI/release GitHub Actions workflows
- CONTRIBUTING.md

## [0.1.0] - 2026-04-01

### Added
- System tray icon with normal/active states
- Per-monitor blackout toggle via context menu
- Friendly monitor name resolution (QueryDisplayConfig → EnumDisplayDevices → device path fallback)
- Multiple simultaneous blackouts
- Blackout dismiss via double-click or right-click
- Auto-hide cursor on blackout window after 10 seconds of inactivity
- TopMost maintenance timer (1-second interval)
- Single-instance enforcement via named Mutex
- File-based logging (`%LocalAppData%\ScreenNap\Logs\`, daily rotation, 7-day retention)
- Internationalization support (English, Japanese)
- Inno Setup installer with desktop shortcut and Windows startup options
- Portable single-file EXE distribution
