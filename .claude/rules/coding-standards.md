# Shared Coding Standards

Common technical standards for ScreenNap (C# / .NET 10 / Raw Win32).

For project-specific rules, see `screennap.md`.

---

## FORMAT: Code Formatting

- **Style definition:** `.editorconfig` at the repository root is the single source of formatting rules. To change a style, edit `.editorconfig` — never deviate per file
- **Always run before completing implementation:** `dotnet format ScreenNap.slnx`
- Do NOT report completion unless `dotnet format ScreenNap.slnx --verify-no-changes` passes
- `Build.ps1` runs the same verification and fails the build on unformatted code

---

## ANALYZERS: Static Analysis

- `Directory.Build.props` enables the built-in Roslyn analyzers (`AnalysisLevel = latest-recommended`, `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`). **Warnings are build errors in every project**
- **Never disable or lower `TreatWarningsAsErrors` or `AnalysisLevel`.** To tolerate a specific warning, suppress it at the narrowest possible scope:

| Scope | Mechanism |
|---|---|
| A specific line | `#pragma warning disable <ID>` followed by `restore`. Reason comment required |
| A specific file/folder | `.editorconfig` section with `dotnet_diagnostic.<ID>.severity = none`. Reason comment required |
| A whole project | Add the ID to `<NoWarn>` in that csproj. Reason comment required |

- The reason comment states why this warning is acceptable here as a current constraint ("for now" or "fix later" is not a reason)

---

## COMMENTS: Code Comments

- **Language:** English
- **Style:** Simple inline comments (`//`). No XML documentation.
- **Simple Code:** Brief one-line "what" description
- **Complex Logic:** Explain "why"

---

## NAMESPACE: Namespaces

- **File-scoped namespaces only** (`namespace X;`)

---

## VAR: Type Inference

- **Use `var`** for `new` expressions and other obvious types
- **Use explicit types** when the type is not clear from the right-hand side

---

## STRING: String Comparison

- **Always specify `StringComparison`** for string methods (`StartsWith`, `EndsWith`, `IndexOf`, `Contains`, `Equals`)
- **Use `StringComparison.Ordinal`** for technical comparisons (paths, identifiers)
- **Use `StringComparison.OrdinalIgnoreCase`** when case-insensitivity is needed

---

## ERROR: Error Handling

- **No empty catch blocks:** Every `catch` must handle or log the exception
- **No raw exception messages in output:** Never display `ex.Message` directly to users
- **Don't log and rethrow:** The layer that handles an exception (catches without rethrowing) is responsible for logging it. A layer that rethrows must NOT log — each failure is logged exactly once

---

## FILEPATH: File Paths

- **Use `AppContext.BaseDirectory`** for paths relative to the application executable
- **Do NOT use `Directory.GetCurrentDirectory()`** — it changes based on how the app is launched

---

## LOGGING: Log Output Location

- **Location:** `{Environment.GetFolderPath(SpecialFolder.LocalApplicationData)}\ScreenNap\Logs\` (the app may be installed to Program Files, which is not writable at runtime)
- **File name:** `ScreenNap_yyyyMMdd.log` (daily rotation, 7-day retention)
- **Log messages MUST be written in English**
- **Logging must never crash the application:** write failures are swallowed

---

## TEMPWORK: File Operations in %TEMP%

When I/O targets may be network paths, perform all file reads/writes in local `%TEMP%`.

- **Input**: Copy source files from network to `%TEMP%` before processing
- **Output**: Create artifacts in `%TEMP%`, then `File.Copy` / `File.Move` to the final destination
- **Naming**: Flat under `Path.GetTempPath()` as `{FeatureName}_{Guid.NewGuid():N}.{ext}`
- **Cleanup**: Always delete in `finally`. Deletion failure is best-effort (log and continue)

---

## CONSTANTS: Constants

- **No magic numbers/strings:** Extract hardcoded values to named constants
- **Application constants** as `const` in the owning class
