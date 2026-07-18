# Shared Engineering Standards (core)

Shared engineering standards, distributed into each repository at `docs/rules/`. This
file is the language-agnostic core. Language-specific rules live in sibling files in the
same directory (`dotnet.md`, ...); application-specific rules live in the application's
rules file listed in the repository's `AGENTS.md` Applications table.

## How to use these rules

1. Read this file.
2. Read every language file in this directory that matches the project's technology
   stack (a .NET repository reads `dotnet.md`; a multi-stack repository reads all
   matching files). The repository's `AGENTS.md` states the stack and the concrete
   commands (formatter, build, test).
3. Identify the application being changed in the `AGENTS.md` Applications table, then
   read its rules file and its specification (`docs/specs/...`).
4. Each language file begins with an **enforcement matrix** stating which rules the
   project scaffold enforces mechanically (analyzers, build gates) and which must be
   checked by AUDIT.
5. Precedence on conflict: the more specific file wins — application rules > language
   rules > this file. `AGENTS.md` holds no rule text; it only routes.
6. Before reporting an implementation task as complete, run **AUDIT** (bottom of this
   file).

---

## Code Design

### Separation of Concerns

- Structure code in layers with distinct responsibilities (e.g., UI → business logic → infrastructure)
- Keep the UI layer thin: event handlers delegate to logic, they do not contain it

### Dependency Direction

- Dependencies must flow in one direction only (e.g., UI → Service → Common → Database)
- Never introduce reverse or circular dependencies. If a change would require one, reconsider the design before implementing

### Functional Core, Imperative Shell

- Separate decisions from I/O: do not make business decisions while reading from DB/files/network — read the data first, then decide on the data already read
- Decision logic takes data as arguments and returns results (pure functions), so it can be tested without infrastructure
- Keep I/O in a thin shell that orchestrates: fetch → decide (pure) → write
- Time, randomness, and environment values are inputs too: pass them as arguments; introduce clock abstractions or DI only when argument-passing is not enough

### Snapshot Before Mutate

- Never modify a data source while lazily iterating over it. This applies to any deferred/streaming enumeration in any language: directory listings (e.g., C# `Directory.EnumerateFiles`, Python `os.scandir`, POSIX `readdir`), collections under `foreach`, DB cursors, lazy queries (LINQ, generators)
- Some environments throw immediately; others fail silently — entries get skipped, processed twice, or the loop ends early. Absence of an exception does not mean absence of the bug
- Snapshot first, then mutate: materialize the full item list (e.g., `.ToList()`, `Directory.GetFiles`, `list(...)`, fetch-all) before the loop that modifies the source
- This is the same principle as Functional Core, Imperative Shell applied to iteration: read everything first, then act on the data already read

### No Lasagna Code

- Do not create pass-through delegation methods that just call another service with the same arguments
- When extracting shared logic into a new service, update **all** callers to use the new service directly
- Do not leave wrapper methods on the original class

### Shared Algorithm, Local Data

- When deduplicating similar code, extract only the algorithm (the steps identical everywhere); keep each call site's data (queries, mappings, registrations) local
- If sharing would require flags or parameters that grow with each new caller, stop sharing

### Change as Addition

- Prefer structures where adding a variation (new screen, format, identifier) means adding a new unit plus a registration, not editing branches across existing code
- Health check: count the files that must be edited to add one variation

### Enforce Invariants in Code

- Prefer compile-time impossibility over runtime checks: where practical, express the rule in types (stage-specific interface return types for call order, required constructor arguments, narrowed parameter types) so violations do not compile
- When a rule can be broken silently (wrong call order, configuration after startup, out-of-range values), make the code fail fast with a clear message instead of relying on documentation
- The error message should state the rule itself

### Synchronous vs Asynchronous

- Use async for I/O-bound work (network, DB, file access); keep CPU-bound and pure logic synchronous
- Async propagates end-to-end: never block on async code (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`)
- Do not mark a method async when nothing in it awaits
- Language files refine this per language/framework; they take precedence

---

## Code Comments

### Principle

Comments are generally unnecessary. Prefer clear names, simple structure, and code that explains itself.

Write a comment only when the code cannot express the necessary context by itself. A useful comment explains **why the current shape, condition, guard, or order must be this way now**.

Even a justified comment is capped at a short conclusion (1–2 lines):

- Check Enforce Invariants in Code first — a constraint that types or a fail-fast check can enforce needs neither a comment nor an ADR
- When an ADR covers the context, the comment states only the conclusion plus the ADR path
- A comment that would exceed ~3 lines is the signal that its body belongs in an ADR or `docs/references/` — move the body there and leave a one-line pointer

Before writing a comment, ask:

- Can a better name, extracted function, type, or test make this clear? If yes, do that instead.
- Is this about what changed, what was removed, who requested it, or how it was aligned? If yes, put it in Git history, a PR, a ticket, or a design document, not in code.
- Can the comment be phrased as "this must be this way because..." in terms of the current behavior? If not, do not write it.

### Good Comments

Good comments explain current constraints that code cannot show:

- Business rules that are not obvious from the code
- External constraints such as API limits, protocol requirements, or legacy system behavior
- Invariants, ordering requirements, and exceptional states
- Non-obvious guards that are required only in specific runtime states

```csharp
// config is null only on first launch (before the settings file is generated)
if (config != null) { ... }

// The contractual matching key is aaa only; extra conditions drop update history (rationale: docs/adr/0002-order-matching-key.md)
var sql = "SELECT ... FROM [Order_TBL] WHERE aaa = @bbb";
```

### Do Not Write

Do not write comments that explain the work history instead of the current constraint:

- What was changed or removed
- Commented-out code, conditions, fields, or parameters
- Notes like "X removed", "X excluded", "changed after review", or "aligned with screen Y"
- Comments that only restate the next line of code
- Bare prohibitions without the current reason

If the current code is clear without a comment, write no comment. If the reason matters, explain the reason as a current rule or constraint without preserving removed implementation details.

### Where to Put What

| Information Type | Location |
|-----------------|----------|
| Why the current shape/order/guard is required now | Code comment, only when code cannot express it |
| External constraints, business rules, invariants | Code comment |
| What was changed or removed | Git history / ChangeLog / Release notes |
| Why code was changed | Git commit message / PR description |
| Why something is prohibited | ADR (`docs/adr/`), plus a brief current reason at the usage site if needed |
| Context needing more than 1–2 lines to explain | ADR (`docs/adr/`) or `docs/references/`, with a one-line conclusion + path comment at the site |
| Implementation decisions | ADR (`docs/adr/`) or Docs (`docs/`) |
| Author's work process (review fixes, what was aligned with) | Nowhere in code — that is what PR / commit history is for |

---

## Testing

### Environment Independence

- Tests must not depend on the execution environment: OS culture/locale, timezone, machine state, pre-existing database contents, or network availability
- When behavior depends on culture or time, pin them explicitly inside the test

### Self-Sufficient Test Data

- Each test creates the minimal data it needs (e.g., via test data builders with sensible defaults)
- Do not depend on shared fixture databases or data prepared outside the test — shared mutable test data rots; per-test data keeps tests independent

### Table-Driven Tests

- When a function's spec is "input → output", write tests as a specification table (parameterized tests), not as near-identical separate test methods
- Order rows consistently: null/none → empty → minimal → main cases → boundaries
- Make input values self-describing so the table reads as documentation

---

## Specifications

- Each application has a specification (`docs/specs/...`, listed in the `AGENTS.md` Applications table) stating what the system must do: purpose, scope, users and external systems, required behavior, inputs and outputs, error behavior, invariants, non-functional requirements, and what is out of scope
- Specifications describe behavior, not implementation: no class structures, libraries, or internal algorithms
- **Spec-first**: when a change requires behavior not in the specification, update the specification before implementing. Never retro-fit the spec to the implementation afterwards
- Requirement IDs are optional; when present, tests reference them so conformance to the specification is verifiable

---

## Git

### Operation Language

- Write all Git-related text in English by default: commit messages, branch names, PR titles and descriptions, tags, and the like
- If the application rules file specifies another language (e.g., Japanese), follow it instead
- When no language is specified, English is the default

### Commit Format

- Use clear, concise descriptions of what was changed
- Follow the conventional commit format: `feat:`, `fix:`, `refactor:`, `docs:`, `build:`, `chore:`, etc.

### Merge Strategy

- Merge PRs with `--squash` by default; use `--merge` / `--rebase` only when explicitly instructed or specified by the application rules file

### Squash Commit Message Composition

When squash-merging, always specify the final commit message explicitly. Never rely on
the tool's defaults, the PR title, the PR body, or auto-generated co-author text.

- **Subject**: a concise statement of what the change does, in the Operation Language
- **Body**: summarize what changed and why at a level useful when reading the history during a future review. Not a copy of the PR description — distill it
- **Nothing useful beyond the subject**: set an explicitly empty body instead of letting the tool generate one

### Branch Cleanup

- After a PR is merged, delete the source branch on both the remote and the local clone (`gh pr merge --delete-branch` does both in one step)
- Under squash merging, `git branch -d` does not recognize the branch as merged — the default branch received a different commit SHA. After confirming the PR is merged, delete with `git branch -D <branch>`
- Prune stale remote-tracking refs with `git fetch --prune`

---

## AUDIT

Run this procedure after completing an implementation task and **before reporting it as
complete**. Do not report completion until AUDIT passes.

### 1. Reload the rules

- Re-read the root `AGENTS.md`, and any nested `AGENTS.md` covering the directories you touched
- Re-read this file, every applicable language file in this directory, and the rules files of the applications you touched (skim is enough if already read this session and unchanged)

### 2. Identify target files

Git changes determine which **files** are targets — not which lines.

| Input | Targets |
|-------|---------|
| (none specified) | All uncommitted changes: `git status --porcelain` (modified, added, untracked) |
| A file path | Only that file |
| A folder path | Files in that folder only |

Check ALL target files. Do not skip any. Do not sample.

### 3. Check file contents

Once a file is a target, **every line in that file** is subject to all rules — do not
distinguish between changed and pre-existing lines; a violation is a violation regardless
of when it was introduced. Check each target file against:

- The core principles in this file
- Every language-file rule whose enforcement matrix row says **AUDIT** (skip rules marked as mechanically enforced — the build gate covers them; but if the repository lacks the enforcing mechanism, treat those rules as AUDIT items too)
- The application-specific rules from the application's rules file (and any other repository-authored files under `docs/rules/`)
- The application's specification (`docs/specs/...`): implemented behavior must match it, and any needed spec change must have happened before the implementation, never as an after-the-fact edit

### 4. Run the mechanical gates

Run the commands defined in `AGENTS.md`: the formatter in check mode, the build
(zero warnings), and the tests. All must pass.

### 5. Fix and report

Fix violations and re-run from step 2. Report completion only when clean, including:

- Files checked
- Violations found and fixed (file / line / rule), or "no violations"
- Gate results with exit codes
- Anything deliberately left unresolved, stated explicitly
