---
name: merge-pr
description: Procedure for merging a GitHub pull request via gh pr merge — squash strategy and explicit squash-commit message composition per the Git section of docs/rules/standard.md. Read BEFORE running any gh pr merge command, whenever asked to merge a PR.
---

# merge-pr

How to merge a GitHub pull request with the `gh` CLI.

## Strategy and message conventions

The merge strategy (squash by default), the squash-commit message composition rules, and
branch cleanup are defined in the Git section of `docs/rules/standard.md` (distributed
into this repository by dev-standards). Read that section before composing the message.
This skill holds only the `gh` operating procedure.

## Procedure

1. Check the PR state with `gh pr view`: approvals, CI checks, merge conflicts. If checks
   are failing, report and stop — do not merge over a red CI without the user's explicit
   instruction.
2. Determine the strategy per the Git section of `docs/rules/standard.md` (squash unless overridden).
3. Compose the subject and body from the PR's actual diff and commits, following the
   Squash Commit Message Composition rules in `docs/rules/standard.md`. Pass them explicitly with
   `--subject` and `--body` (or `--body-file`); use `--body ""` when nothing useful
   exists beyond the subject.
4. Merge: `gh pr merge <number> --squash --subject "..." --body "..."`.
5. Delete the source branch per the Branch Cleanup rules in `docs/rules/standard.md` (prefer
   `--delete-branch` on the merge; under squash, local deletion needs `git branch -D`
   after confirming the PR is MERGED).
6. Verify the PR state is MERGED (`gh pr view`) and report the final commit message.
