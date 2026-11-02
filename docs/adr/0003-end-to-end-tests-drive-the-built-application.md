---
status: accepted
supersedes: archive/0002-arch-functional-core-shell.md
---

# End-to-End Tests Drive the Built Application Alongside a Functional Core

## Context

ADR 0002 separated deterministic decisions into `ScreenNap/Core/` so that unit tests
reach them without windows, timers, or files, and it rejected tests that create real
windows because CI agents may not offer a stable interactive desktop.

`docs/rules/testing.md` now requires an end-to-end layer once the specification states a
requirement whose outcome is observable through the application's external entry point.
Most ScreenNap requirements are observable through its windows: the context menu, the
blackout windows, the identification overlays, the global hotkeys, and the log files it
writes. A unit test of the decision behind such a requirement leaves the path through
the entry point unverified.

Three facts constrain an end-to-end test of ScreenNap:

- ScreenNap enforces a single instance through a named mutex, so a resident ScreenNap
  makes a test-launched one exit at once
- ScreenNap's log directory was fixed under the user profile, so a test could not give
  the application a log directory of its own
- The installer installs per user under a fixed AppId, so installing and uninstalling
  in a test replaces and then removes an existing installation

## Decision

- Deterministic decisions stay in `ScreenNap/Core/` and are unit-tested there, as ADR
  0002 decided. Every input case of a decision is tested in the unit layer
- `ScreenNap.E2ETests` starts the application as built from the repository, as its own
  process, and drives it only through its windows: it posts the tray callback and
  command messages to the hidden message window, injects the global hotkeys with
  `SendInput`, posts mouse messages to the blackout windows, and reads the open context
  menu through its menu handle. It observes outcomes where they land: the windows
  ScreenNap creates, the hotkeys it holds, and the log files it writes
- The `SCREENNAP_LOG_DIR` environment variable is an operator setting the specification
  states (LOG-1, LOG-2). End-to-end tests point it at a directory the test run creates
- The installer end-to-end test installs silently into a directory the test run creates
  and uninstalls at the end
- An end-to-end test that finds a running ScreenNap, or an existing ScreenNap
  installation for the installer test, fails at once and names the condition, rather
  than replacing state the user owns
- End-to-end tests run on an interactive Windows desktop before a pull request is
  opened, as `docs/rules/testing.md` When Tests Run states; they are not part of
  `build/Build.ps1` or the AUDIT gate

## Consequences

- An end-to-end run covers the real monitors with blackout windows for moments at a
  time, and injects keyboard input; the desktop is not usable for other work during the
  run
- A developer exits the resident ScreenNap before running the end-to-end tests, and runs
  the installer end-to-end test only where ScreenNap is not installed
- Outcomes the shell owns and no API reports back — the tray tooltip text, a tray
  notification's text — are verified by unit tests of the decision that produces them
- Outcomes that depend on an input a test cannot set — the UI culture of the launched
  process, a display topology change, a Win32 call failing — are verified by unit tests

## Alternatives Considered

### Keep the functional core as the only test layer (ADR 0002)

Rejected: the unit layer never runs the message loop, the window procedures, or the
P/Invoke declarations, so a defect there passes every gate.

### A UI automation library

Rejected: ScreenNap's windows are raw Win32 windows without automation peers, and
the menu handle, window classes, and hotkey registration are reachable through Win32
calls directly, without adding a test dependency.

### A test-only mutex name or log directory switch

Rejected: `docs/rules/testing.md` forbids a setting that only tests use. The log
directory is an operator setting the specification states; the mutex name stays fixed.

## Revisit When

Revisit when CI agents provide an interactive desktop on which these tests can run
unattended, or when ScreenNap gains an entry point other than its windows.
