# Git

Part of the shared core rules (`docs/rules/`). Governs Git write operations, branch
lifecycle, commit composition, and pull requests. Every Git operation must strictly
adhere to these rules.

## Confirming Scope

The extent of Git operations is decided by the user, never assumed by the agent. Always agree on the stopping stage before the first Git write operation:

1. **Commit only**: stage and commit changes locally.
2. **Commit and push**: commit and push the feature branch to the remote.
3. **Through PR creation**: push and open a pull request.
4. **Full lifecycle**: PR creation, squash merge, return to default branch, and delete the feature branch.

Stop strictly at the agreed stage and report. Never proceed beyond the agreed stage autonomously; unpushed or unmerged branches are valid resting states.

- **What counts as agreement**: the user names a stage, or gives an instruction that maps to exactly one stage. An instruction that maps to more than one (e.g., "finish this") is confirmed with the user before the first Git write operation.
- **Agreed once per task**: the agreement covers every Git operation of the task it was made for; a new task needs a new agreement.
- **What counts as a write**: a Git write operation is any operation from the stages above — staging, committing, pushing, opening or merging a PR, deleting a branch. Fetching and creating a local branch are not.

## Operation Language

Write all Git-related text in English: commit messages, branch names, PR titles and descriptions, tag names and tag messages, and release notes. This prevents shell encoding issues, garbled terminal output, and cross-environment inconsistencies.

## Commit Format

- Use clear, concise descriptions of what was changed
- Follow the conventional commit format: `feat:`, `fix:`, `refactor:`, `docs:`, `build:`, `chore:`, etc.

## Branches

- Treat the default branch as protected, whether or not the repository actually enforces it: never commit on it. Create a branch first, from the remote default branch's latest state (fetch before branching; a local default branch behind the remote is not its current state). Work on the default branch only when the user explicitly asks for it
- Branch names are kebab-case and name the change, at least by the area it changes (e.g., `login-screen`); a type prefix mirroring the commit type (`feat/`, `fix/`, `docs/`, `chore/`, ...) is recommended, not required
- A branch carries as many commits as the work needs — one PR does not mean one commit
- Delete branches per Branch Cleanup below once their PR is merged

## Pull Requests

- Landing a branch on the default branch always goes through a PR; never merge a branch locally
- Give every PR a clear title and a description of what changed and why
- Before opening a PR, and again after every later push to it, run every end-to-end test command `AGENTS.md` names; each exits 0 (`testing.md` When Tests Run)
- Before relying on a PR's diff, composing a merge message, or merging, confirm the PR still reflects the intended change — re-check with `gh pr view` if commits were pushed after opening

## Merge Strategy

- Merge PRs with `--squash` by default; use `--merge` / `--rebase` only when explicitly instructed

## Squash Commit Message Composition

When squash-merging, always specify the final commit message explicitly. Never rely on
the tool's defaults, the PR title, the PR body, or auto-generated co-author text.

- **Subject**: a concise statement of what the change does, in the Operation Language
- **Body**: summarize what changed and why at a level useful when reading the history during a future review. Not a copy of the PR description — distill it
- **Nothing useful beyond the subject**: set an explicitly empty body instead of letting the tool generate one

## Pre-merge Verification

- Check the PR state with `gh pr view` before merging: approvals, CI checks, merge conflicts
- If checks are failing, report and stop — do not merge over a red CI without the user's explicit instruction

## Merge Procedure

1. Determine the strategy per Merge Strategy above (squash unless overridden)
2. Compose the subject and body from the PR's actual diff and commits, following Squash Commit Message Composition. Pass them explicitly with `--subject` and `--body` (or `--body-file`); use `--body ""` when nothing useful exists beyond the subject
3. Merge, naming the strategy explicitly: `gh pr merge <number> --squash --subject "..." --body "..."` (substitute the chosen strategy's flag when Merge Strategy calls for `--merge` or `--rebase`)

## Post-merge Verification

- Verify the PR state is `MERGED` (`gh pr view`) and report the final commit message

## Branch Cleanup

- After a PR is merged, delete the source branch on both the remote and the local clone (`gh pr merge --delete-branch` does both in one step)
- Under squash merging, `git branch -d` does not recognize the branch as merged — the default branch received a different commit SHA. After confirming the PR is merged, delete with `git branch -D <branch>`
- Prune stale remote-tracking refs with `git fetch --prune`
