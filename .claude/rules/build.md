# Build Folder Rules

## SCRIPTS: Build Scripts

### Menu.bat

User-facing entry point for builds with the following options:

| Option | Name | Description |
|--------|------|-------------|
| 1 | Build | Build ScreenNap → `Build.ps1` |
| 2 | Installer | Create installer → `Installer.ps1` (requires 1) |
| 3 | Full Build | Run 1→2 sequentially |
| 9 | Exit | Exit menu |

### Build.ps1

Runs format verification (`dotnet format --verify-no-changes`) and tests (`ScreenNap.Tests`), then publishes ScreenNap as a self-contained single-file EXE to `build/ScreenNap/`. Both must pass before publish proceeds.

Additionally, `TreatWarningsAsErrors` in `Directory.Build.props` turns warnings into errors in every build (Build.ps1, Visual Studio, direct `dotnet build`). See ANALYZERS in `.claude/rules/coding-standards.md` for the relaxation rules.

### Installer.ps1

Invokes Inno Setup (ISCC.exe) on `Setup_ScreenNap.iss`. Requires `build/ScreenNap/ScreenNap.exe` to exist.

## OUTPUT: Output Directories

| Directory | Contents |
|-----------|----------|
| `build/ScreenNap/` | ScreenNap.exe (self-contained) |
| `build/Installer/` | Installer package |

**DO NOT** manually add files to output directories or commit them to git.

## VERSION: Version Management

### Single Source of Truth

Version is defined in `ScreenNap/ScreenNap.csproj` `<Version>` tag.

### Files Requiring Manual Sync

When updating `<Version>`, also update these files to match:

- `build/Setup_ScreenNap.iss` — `#define MyAppVersion`
- `CHANGELOG.md` — move the `[Unreleased]` section's entries into a new `## [<Version>] - <date>` section (Keep a Changelog format)

### Versioning Scheme

- Semantic Versioning (MAJOR.MINOR.PATCH)
- During development: `0.x.x` (release as `1.0.0`)
- MINOR: new features
- PATCH: bug fixes and small changes

## VERIFY: Post-Implementation Build Verification

### Format
```bash
dotnet format ScreenNap.slnx --verify-no-changes
```

### Build
```bash
dotnet build ScreenNap/ScreenNap.csproj -c Release
```

### Tests
```bash
dotnet test ScreenNap.Tests/ScreenNap.Tests.csproj
```
