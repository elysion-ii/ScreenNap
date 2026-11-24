---
status: active
created: 2026-11-24
---

# ScreenNap Violating Behavior

Behavior the ScreenNap implementation has that breaks a rule under `docs/rules/`, kept here
instead of in `docs/specs/ScreenNap.md` until the fix lands (`docs/rules/specs.md`,
Ledgers and Handling Each Class). Each entry is a fact about the current code, not
intended behavior. The change that fixes an entry removes it; this file is deleted when it
holds no entry.

## The version item when the product version cannot be read

- **Decision**: the text of the context menu's version item, chosen by whether the
  executable carries a product version (its `AssemblyInformationalVersionAttribute`)
- **Question**: OUTCOMES (the class "no product version, or one that is empty or holds only
  build metadata")
- **Code's answer**: the item reads `ScreenNap unknown`, and ScreenNap otherwise runs
  normally
- **Code**: `AppVersion.Normalize` and `AppVersion.Current` in
  `ScreenNap/App/AppVersion.cs`
- **Declared by**: a test — `AppVersionTests.Normalize_AnyInformationalVersion_ReturnsVersionWithoutMetadata`
  pins the `unknown` rows. The specification stated it as VER-3 until that requirement
  moved here
- **Rule broken**: `docs/rules/standard.md` Version Display (Single version definition):
  when version metadata is absent, the application fails fast instead of substituting a
  hardcoded fallback constant
- **Answers the rules admit** (not yet chosen by the user): the build fails when the
  version is missing or empty, so no executable without a version is produced; or ScreenNap
  stops at startup with a localized message box and a log entry, as ERR-1 states for other
  startup failures; or both

## No log entry when `SCREENNAP_LOG_DIR` is not set

- **Decision**: the log directory, chosen by the `SCREENNAP_LOG_DIR` environment variable
  (LOG-1, LOG-2)
- **Question**: OUTCOMES (the class "the variable is not set, or set to an empty string")
- **Code's answer**: ScreenNap logs to `%LocalAppData%\ScreenNap\Logs\` and writes no
  entry that says the default directory replaced a missing setting. LOG-2 covers only a
  value that is set but rejected
- **Code**: `Logger.Initialize` in `ScreenNap/Logging/Logger.cs` (the
  `string.IsNullOrEmpty(configuredDirectory)` branch)
- **Declared by**: nothing — no comment, test, or guard states it
- **Rule broken**: `docs/rules/configuration.md` Configuration Values (Log every default
  substitution once): a stored preference that falls back to its default, including a
  missing one, gets one log entry naming the setting, the reason, and the default applied
- **Answer the fix will implement**: at startup, when `SCREENNAP_LOG_DIR` is not set or is
  empty, ScreenNap writes one `INFO` entry naming `SCREENNAP_LOG_DIR`, the reason
  (`not set`), and the default directory; the specification states it next to LOG-2
