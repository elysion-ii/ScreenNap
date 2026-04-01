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

Publishes ScreenNap as a self-contained single-file EXE to `Build/ScreenNap/`.

### Installer.ps1

Invokes Inno Setup (ISCC.exe) on `Setup_ScreenNap.iss`. Requires `Build/ScreenNap/ScreenNap.exe` to exist.

## OUTPUT: Output Directories

| Directory | Contents |
|-----------|----------|
| `Build/ScreenNap/` | ScreenNap.exe (self-contained) |
| `Build/Installer/` | Installer package |

**DO NOT** manually add files to output directories or commit them to git.

## VERSION: Version Management

### Single Source of Truth

Version is defined in `ScreenNap/ScreenNap.csproj` `<Version>` tag.

### Files Requiring Manual Sync

When updating `<Version>`, also update these files to match:
- `Build/Setup_ScreenNap.iss` — `#define MyAppVersion`
- `Build/Installer.ps1` — Write-Host version display

### Versioning Scheme

- Semantic Versioning (MAJOR.MINOR.PATCH)
- During development: `0.x.x` (release as `1.0.0`)
- MINOR: new features
- PATCH: bug fixes and small changes

## VERIFY: Post-Implementation Build Verification

```bash
dotnet build ScreenNap/ScreenNap.csproj -c Release
```
