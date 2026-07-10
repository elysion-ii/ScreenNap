# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
