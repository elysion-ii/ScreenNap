# ScreenNap

A lightweight Windows system tray application that displays fullscreen black windows on selected monitors to protect OLED screens from burn-in.

## Why?

OLED monitors use self-emitting pixels. When displaying pure black (#000000), no current flows through the pixels, effectively turning them off. ScreenNap leverages this by covering selected monitors with a fullscreen black window, providing the same burn-in protection as turning off the monitor -- without disrupting the Windows display configuration (monitor layout, orientation, window positions).

## Features

- System tray resident with normal/active state icons
- Per-monitor blackout toggle via context menu
- Friendly monitor names (e.g., "ASUS VG32UQ1B" instead of "\\\\.\\DISPLAY1")
- Multiple monitors can be blacked out simultaneously
- Double-click or right-click on black window to dismiss
- Auto-hide cursor on blackout window after 10 seconds of inactivity
- Single-instance enforcement
- File-based logging with automatic rotation and retention
- Zero external dependencies

## Requirements

- Windows 10 or later (x64)

## Installation

### Portable

Download `ScreenNap.exe` from [Releases](https://github.com/luxon-45/ScreenNap/releases) and run it. No installation required.

### Installer

Download `ScreenNap-Setup-x.x.x.exe` from [Releases](https://github.com/luxon-45/ScreenNap/releases) and follow the setup wizard. Optionally register for Windows startup.

Default install path: `%LocalAppData%\Programs\ScreenNap`

## Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) [Inno Setup 6](https://jrsoftware.org/isdl.php) for creating the installer

### Build

```
cd Build
Menu.bat
```

Or directly:

```
dotnet publish ScreenNap/ScreenNap.csproj -c Release -f net10.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Usage

### Dismiss a Blackout

- **Double-click** anywhere on the black window
- **Right-click** anywhere on the black window (safety fallback for when the main monitor is blacked out)

### Logs

Log files are written to `%LocalAppData%\ScreenNap\Logs\` with daily rotation and 7-day retention.

| Level | Description |
|-------|-------------|
| INFO | Application lifecycle, blackout events, monitor enumeration |
| WARN | Non-critical failures (tooltip, menu) |
| ERROR | Critical P/Invoke failures with Win32 error codes |

### Adding a Language

Create `ScreenNap/Resources/Strings.xx.resx` (where `xx` is the language code, e.g., `fr`, `de`, `ko`) with translated strings. No code changes required — .NET picks the resource file automatically based on the OS language setting.

## Technology

- **C#** on **.NET 10.0 LTS**
- **Raw Win32 API** via P/Invoke (no WinForms, WPF, or WinUI)
- Zero NuGet dependencies

## License

[MIT](LICENSE)
