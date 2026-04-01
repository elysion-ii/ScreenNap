# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Initial project structure and build system
- Repository rules and coding standards

## [0.1.0] - 2026-04-01

### Added
- System tray icon with normal/active states
- Per-monitor blackout toggle via context menu
- Friendly monitor name resolution (QueryDisplayConfig → EnumDisplayDevices → device path fallback)
- Multiple simultaneous blackouts
- Blackout dismiss via double-click or right-click
- Auto-hide cursor on blackout window after 10 seconds of inactivity
- Hover tooltip on blackout windows
- TopMost maintenance timer (1-second interval)
- Single-instance enforcement via named Mutex
- File-based logging (`%LocalAppData%\ScreenNap\Logs\`, daily rotation, 7-day retention)
- Internationalization support (English, Japanese)
- Inno Setup installer with desktop shortcut and Windows startup options
- Portable single-file EXE distribution
