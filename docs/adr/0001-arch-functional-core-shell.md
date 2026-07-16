---
status: accepted
---

# Functional Core and Imperative Shell

## Decision

ScreenNap separates deterministic decisions from Win32 and file-system operations. New decision logic belongs in `ScreenNap/Core/` and receives time or environment-dependent values as arguments. Application, blackout-window, and logging code act as thin shells that acquire inputs, invoke the core logic, and apply the result.

Tests target the functional core without creating windows, timers, or files in user profile directories.

## Alternatives Considered

### Win32 UI automation

UI automation frameworks were rejected because they require external NuGet packages and depend on an interactive desktop session.

### Integration tests using real windows

Tests that create real blackout windows were rejected because CI agents may not provide a stable interactive Windows session. Such tests would make the build gate environment-dependent.

## Applicability

New features must place their decision logic in `Core/` from the outset. Win32 declarations remain in `Native/`, while P/Invoke calls remain in the imperative shell.
