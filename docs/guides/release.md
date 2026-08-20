---
status: active
created: 2026-07-30
---

# ScreenNap Release Procedure

Publishing a new ScreenNap version: the version bump, the tag, and the GitHub Release.

Pushing the tag is what publishes. `.github/workflows/release.yml` triggers on `v*`,
builds both release assets on a runner, and creates the GitHub Release with this
version's `CHANGELOG.md` section as its notes. Nothing here uploads assets by hand.

The entry state is a clean `main` holding every change the release contains, with
`Directory.Build.props` still carrying the previous version. The goal state is the
version commit merged into `origin/main`, an annotated tag `v{version}` on `origin`, a
GitHub Release carrying both release assets, and the previous version's tag and release
gone — only the current version stays published.

## Prerequisites

- The repository root — the directory containing `ScreenNap.slnx` — is the working
  directory for every command below unless a step names another one
- Visual Studio Build Tools carry the C++ desktop workload
  (`Microsoft.VisualStudio.Component.VC.Tools.x86.x64`); the Native AOT publish in
  `build/Build.ps1` links with MSVC
- `C:\Program Files (x86)\Microsoft Visual Studio\Installer` is on `PATH`; the MSVC
  environment script invokes `vswhere` by bare name, and the publish fails with
  `MSB3073` when the directory is missing from `PATH`
- Inno Setup 6 is installed at `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`;
  `build/Installer.ps1` aborts when that path is absent
- `gh auth status` reports an account with push access to `elysion-ii/ScreenNap`

## Phase 1 — Land the version on `main`

### 1. Branch from a clean `main`

Run `git switch main` and `git pull`, then `git switch -c {branch}`. `main` is
protected, so the version commit is made on a branch and reaches `main` only through a
pull request; `docs/rules/git.md` governs the branch name. When the version bump travels
with the change being released, that change's own branch is the one.

Confirmation: `git status --porcelain` prints nothing, and `git branch --show-current`
prints `{branch}`.

### 2. Set the new version

In `Directory.Build.props`, set the `<Version>` element to the new version. It is the
only version definition in the repository; the EXE inherits it and
`build/Installer.ps1` reads it.

Confirmation: `Directory.Build.props` contains `<Version>{version}</Version>` and no
other file in the repository defines a version.

### 3. Add the changelog section

In `CHANGELOG.md`, add a `## [{version}] - YYYY-MM-DD` heading directly below
`## [Unreleased]`, with `### Added` / `### Changed` / `### Fixed` entries describing the
release from a user's perspective. This section becomes the published release notes
verbatim, so write it for the people downloading the release.

Confirmation: the heading matches `^## \[{version}\]` exactly — both
`build/Installer.ps1` and the release workflow's notes extraction fail without it.

### 4. Commit both files together

Commit `Directory.Build.props` and `CHANGELOG.md` in one commit, either on their own or
together with the change being released. The pre-commit hook rejects a commit until
AUDIT is confirmed; once the AUDIT procedure at the end of `docs/rules/standard.md` has
run, commit with `git -c audit.ok=true commit`.

Confirmation: `git show --stat HEAD` lists both files in the same commit.

### 5. Merge the branch into `main`

Push the branch, open a pull request, and merge it by the Merge Procedure in
`docs/rules/git.md` — squash merge with an explicit subject and body, then delete the
branch on both the remote and the local clone. Return to `main` and run `git pull`.

Confirmation: `gh pr view {number} --json state` reports `MERGED`,
`git branch --show-current` prints `main`, and `git log --oneline -1` shows the squash
commit.

## Phase 2 — Rehearse the publish

The pull request's CI builds and tests, but it never publishes. Phase 3 does, and it
cannot be undone once the tag is on `origin` — so the same two scripts the workflow runs
are run locally first, on `main` at the squash commit. Their output is a rehearsal; the
published assets come from the workflow.

### 6. Build the portable EXE

Run `powershell -ExecutionPolicy Bypass -File build/Build.ps1`. It checks the
configuration files, verifies formatting, runs the test suite, and publishes the
Native AOT single EXE; a failure in any gate stops it before publishing.

Confirmation: the command exits 0, `build/ScreenNap/ScreenNap.exe` is the only file in
`build/ScreenNap/`, and
`(Get-Item build/ScreenNap/ScreenNap.exe).VersionInfo.ProductVersion` prints the new
version.

### 7. Build the installer

Run `powershell -ExecutionPolicy Bypass -File build/Installer.ps1`. It reads `<Version>`
from `Directory.Build.props`, verifies the changelog heading, and injects the version
into `build/Setup_ScreenNap.iss`.

Confirmation: the command exits 0 and
`build/Installer/ScreenNap-Setup-{version}.exe` exists.

`build/Menu.bat` option 3 runs steps 6 and 7 in sequence and stops at the first failure.

## Phase 3 — Publish

Pushing the tag publishes the release, and a published tag or release can only be
withdrawn manually. Run this phase only after Phase 2 succeeded.

### 8. Create and push the tag

On `main` at the squash commit from step 5, run `git tag -a v{version} -m "v{version}"`,
then `git push origin v{version}`. The push starts `release.yml`, which builds both
assets and creates the GitHub Release.

Confirmation: `gh run list --workflow=release.yml --limit 1` reports the run for
`v{version}` as `success`.

A tag must never outlive its release: when the run fails, delete the tag on `origin` and
locally (`git push origin :refs/tags/v{version}` and `git tag -d v{version}`), fix the
cause, and start Phase 3 again.

## Phase 4 — Verify the published release

### 9. Check the release contents

Run `gh release view v{version} --json tagName,isDraft,assets,body`.

Confirmation: `tagName` is `v{version}`, `isDraft` is `false`, `assets` lists both
`ScreenNap.exe` and `ScreenNap-Setup-{version}.exe`, and `body` is this version's
`CHANGELOG.md` section.

### 10. Check the download page

Open `https://github.com/elysion-ii/ScreenNap/releases`.

Confirmation: the new release is marked `Latest`, and both assets are downloadable from
it.

## Phase 5 — Retire the previous release

Only the current version stays published, so the previous version's tag and its GitHub
Release are removed once the new release is verified. Deleting them is irreversible and
their assets cannot be recovered — run this phase only after Phase 4 confirmed the new
release carries both assets. When `v{version}` is the only published version, this phase
ends at step 11.

### 11. Identify what is still published

Run `gh release list` and `git ls-remote --tags origin`.

Confirmation: both list `v{version}`, and whether a previous `v{previous}` remains is now
known. With no `v{previous}` in either listing, the release is complete.

### 12. Delete the previous release and its tag

A tag must never outlive its release, so remove both in one step:

```powershell
gh release delete v{previous} --yes --cleanup-tag
git tag -d v{previous}
```

Confirmation: `gh release list` shows only `v{version}`, `git ls-remote --tags origin`
lists only `refs/tags/v{version}`, and `git tag -l` no longer prints `v{previous}`.
