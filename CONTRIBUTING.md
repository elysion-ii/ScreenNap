# Contributing to ScreenNap

Thank you for your interest in contributing!

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 or later (x64)
- (Optional) [Inno Setup 6](https://jrsoftware.org/isdl.php) for building the installer

## Getting Started

```
git clone https://github.com/elysion-ii/ScreenNap.git
cd ScreenNap
dotnet build ScreenNap/ScreenNap.csproj -c Release
```

## Architecture Constraints

ScreenNap's architecture constraints (raw Win32 only, no runtime NuGet packages, the
layer dependency directions) are stated in `docs/rules/ScreenNap.md`. Read
`AGENTS.md` first: it lists every rule file under `docs/rules/` and the commands that
verify a change.

## Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b feat/your-feature`)
3. Make your changes
4. Verify the full build passes: `powershell -ExecutionPolicy Bypass -File build/Build.ps1` (format check, requirement coverage, and tests run before publishing)
5. Commit with a clear message using [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `refactor:`, etc.)
6. Open a Pull Request against `main`

CI will automatically verify your PR builds successfully.

## Adding a Language

Create `ScreenNap/Resources/Strings.xx.resx` with translated strings. No code changes required. See existing `.resx` files for the string keys.

## Reporting Issues

Use [GitHub Issues](https://github.com/elysion-ii/ScreenNap/issues). Include:

- Windows version
- ScreenNap version (shown in log file as `Application started (vX.X.X)`)
- Steps to reproduce
- Log file contents (`%LocalAppData%\ScreenNap\Logs\`)
