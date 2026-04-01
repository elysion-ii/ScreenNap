# Contributing to ScreenNap

Thank you for your interest in contributing!

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 or later (x64)
- (Optional) [Inno Setup 6](https://jrsoftware.org/isdl.php) for building the installer

## Getting Started

```
git clone https://github.com/luxon-45/ScreenNap.git
cd ScreenNap
dotnet build ScreenNap/ScreenNap.csproj -c Release
```

## Architecture Constraints

This project follows strict rules — please review before making changes:

- **No UI frameworks** — Raw Win32 API via P/Invoke only. No WinForms, WPF, or WinUI.
- **No NuGet packages** — All functionality through .NET BCL and Win32 P/Invoke.
- **Dependency direction** — `Program.cs → App/ → Native/`, `Blackout/ → Native/`. Never reverse.

## Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b feat/your-feature`)
3. Make your changes
4. Verify the build passes: `dotnet build ScreenNap/ScreenNap.csproj -c Release`
5. Commit with a clear message using [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `refactor:`, etc.)
6. Open a Pull Request against `main`

CI will automatically verify your PR builds successfully.

## Adding a Language

Create `ScreenNap/Resources/Strings.xx.resx` with translated strings. No code changes required. See existing `.resx` files for the string keys.

## Reporting Issues

Use [GitHub Issues](https://github.com/luxon-45/ScreenNap/issues). Include:

- Windows version
- ScreenNap version (shown in log file as `Application started (vX.X.X)`)
- Steps to reproduce
- Log file contents (`%LocalAppData%\ScreenNap\Logs\`)
