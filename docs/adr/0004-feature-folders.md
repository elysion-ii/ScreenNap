---
status: accepted
---

# Group the Source by Feature

## Context

ScreenNap's source was grouped by layer: `App/` held the orchestration of every feature,
`Core/` held the decisions and contracts of every feature, and `Blackout/` held the
blackout window. A change to one feature — the context menu, for one — touched `App/` and
`Core/` alike, and nothing in the layout said which types belonged to which feature.

`docs/rules/standard.md` Code Organization requires code that changes together for one
feature to sit in that feature's folder, with mechanisms shared across features in homes
of their own. Separating decisions from I/O (`standard.md` Functional Core, Imperative
Shell) is a property of each type — a decision takes its inputs as arguments — and does
not need a folder of its own to hold.

## Decision

- The source of `ScreenNap/` is grouped into four feature folders — `Tray/`, `Blackout/`,
  `Hotkeys/`, and `Identify/` — and the shared homes `Monitors/`, `Notifications/`,
  `Versioning/`, `Logging/`, `Native/`, and `Resources/`
- The folders, what belongs in each, and the permitted dependency directions are stated
  in `docs/rules/ScreenNap.md` Architecture
- The unit-test project mirrors the same folders; a test double one feature's tests use
  sits in that feature's test folder, and doubles several features use stay in
  `ScreenNap.Tests/TestDoubles/`

## Consequences

- A feature's types — its window, its decisions, its manager — are found in one folder
- A type that a second feature starts to use moves to a shared home in that change
- The text of a monitor's menu item is menu knowledge and lives in `Tray/`, apart from
  the monitor record in `Monitors/`

## Alternatives Considered

### Keep the layer folders and state them as an override of Code Organization

Rejected: the layer folders split each feature across `App/` and `Core/`, which is the
cost Code Organization exists to remove, and the decision/I/O separation they stood for
is checked per type by `standard.md` either way.
