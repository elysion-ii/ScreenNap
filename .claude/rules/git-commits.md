# Git Commit Rules

When the user grants permission to commit, follow these rules:

- **Commit messages must be written entirely in English.** Do not use Japanese in commit messages.
- Use clear, concise descriptions of what was changed.
- Follow the conventional commit format: `feat:`, `fix:`, `refactor:`, `docs:`, `build:`, `chore:`, etc.
- **Before committing, ensure the `plans/` folder is clean.** Move any remaining plan files to `archive/plans/` before creating the commit. If a filename conflicts with an existing file in `archive/plans/`, rename the file to avoid the collision before moving.

## Release Rules

When the user requests a release:

1. **Tag**: Create an annotated or lightweight tag (e.g., `v1.2.0`) on the release commit.
2. **GitHub Release**: Always create a GitHub Release via `gh release create` for every tag. A tag alone is not sufficient.
3. **Release assets**: Build via `build/Build.ps1` then `build/Installer.ps1`, and attach **both** to the GitHub Release using `gh release upload`:
   - Portable EXE: `build/ScreenNap/ScreenNap.exe`
   - Installer: `build/Installer/ScreenNap-Setup-{version}.exe`
