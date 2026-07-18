---
status: accepted
---

# Functional Core and Imperative Shell

## Context

ScreenNap coordinates Win32 windows, timers, monitor state, and file-system logging. If deterministic decisions are mixed with those operations, tests must create operating-system resources or depend on an interactive desktop session. That would make the build gate dependent on the execution environment and would leave decision logic difficult to test in isolation.

## Decision

ScreenNap separates deterministic decisions from Win32 and file-system operations. New decision logic belongs in `ScreenNap/Core/` and receives time or environment-dependent values as arguments. Application, blackout-window, and logging code act as thin shells that acquire inputs, invoke the core logic, and apply the result.

Tests target the functional core without creating windows, timers, or files in user profile directories.

## Consequences

- New features must place deterministic decision logic in `Core/` from the outset.
- Win32 declarations remain in `Native/`, while P/Invoke and file-system operations remain in the imperative shell.
- Tests can verify decisions without creating windows, timers, or files in user profile directories.
- Environment-dependent values must be converted into data and passed into the functional core, which may require additional contracts between the core and shell.

## Alternatives Considered

### Win32 UI automation

UI automation frameworks were rejected because they require external NuGet packages and depend on an interactive desktop session.

### Integration tests using real windows

Tests that create real blackout windows were rejected because CI agents may not provide a stable interactive Windows session. Such tests would make the build gate environment-dependent.

## Revisit When

Revisit this decision if required behavior cannot be represented as deterministic inputs and outputs and a stable, supported Windows integration-test environment becomes available.
