---
status: active
created: 2026-11-24
---

# ScreenNap Deferred Violations

Rule violations in ScreenNap's code and documents that the repository's audit found and
the maintainer chose to fix later (`docs/rules/standard.md`, AUDIT step 5). Violations of
a decision's behavior are in `docs/references/ScreenNap/violating-behavior.md` instead.
The change that fixes an entry removes it; this file is deleted when it holds no entry.

## Requirements without a verifying test

- **Where**: `docs/specs/ScreenNap.md` and the test projects
- **Rule**: `docs/rules/testing.md` Requirement Coverage (Verify every IDed requirement;
  A verifying test fails if behavior breaks)
- **Violation**: each requirement below is named by a test, if at all, that still passes
  when the behavior it states is removed:
  - **WIN-3** (a blackout periodically reasserts its topmost position): the end-to-end test
    reads the `WS_EX_TOPMOST` style, which a window keeps without the periodic reassertion
  - **NFR-2** (ScreenNap stays idle when nothing is shown): no test observes idleness or
    the absence of periodic work
  - **LOG-2** (a rejected log directory is reported once): the unit test checks only which
    values are rejected, not the `WARN` entry naming the rejected value and the default
    directory
  - **ERR-1** and **ERR-2** (startup failures and a missing tray icon are shown in a message
    box): the tests check only the resource text, not that a message box appears and
    startup stops
  - **ERR-10** (maintenance and cleanup failures are logged without notification): no test
    makes a timer, topmost, dismissal, or cleanup call fail
  - **DIST-7**, its silent-uninstall clause (when ScreenNap is still running after the
    close request, the silent uninstall removes nothing and exits non-zero): the end-to-end
    test covers only a ScreenNap that closes; no test keeps ScreenNap running against a
    graceful close request without a code path for tests alone
- **The fix needs**: for each requirement, a test that fails without the behavior — at the
  end-to-end layer where the outcome is observable through ScreenNap's windows, logs, or
  the installer, and at the unit layer where it depends on an input an end-to-end test
  cannot set (a failing Win32 call), which may need the decision moved into code a unit
  test reaches
