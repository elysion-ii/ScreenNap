# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
