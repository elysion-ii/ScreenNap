# Shared Engineering Standards (core)

The stack-agnostic core of the shared engineering standards, distributed into each
repository at `docs/rules/`. Stack rules live beside it (`dotnet.md`, `typescript.md`,
`python.md`, ...); application rules live in the rules file the repository's `AGENTS.md`
Applications table lists.

## How to use these rules

Rules in `docs/rules/` form three tiers:
- **Application rules**: `docs/rules/<App>.md`
- **Stack rules**: `dotnet.md`, `typescript.md`, `python.md`, ...
- **Core rules**: `standard.md`, `documentation.md`, `git.md`, `testing.md`, and the conditional core files `cli.md` and `db.md`

**Precedence on conflict**: Application rules > Stack rules > Core rules. `AGENTS.md` provides routing and commands only.

- **No conflicts within the core**: a contradiction between two core files is a defect in the rules. Report it to the user and continue only on the user's decision — never by choosing one.
- **Rule scope**: every change complies with every file under `docs/rules/` and with the specification under `docs/specs/` of the application it changes.
- **Required reading**: every file under `docs/rules/` is read before implementing any change, except a conditional core file whose condition is false.
- **Conditional core files**: each opens with its condition, a fact about the application that its code settles — never whether the task touches the file's subject. A change that makes the condition true (the first command-line option, the first database access) is subject to the file.
- **Stack conformance**: a repository applies the stack rules of its technology stack, verified by the commands in `AGENTS.md`.
- **Enforcement matrix**: each stack rule file opens with a matrix classifying every rule as mechanically enforced (analyzers, build gates) or verified by AUDIT.
- **Completion requirement**: no task is reported complete before the AUDIT gate at the end of this file passes.

---

## Code Design

### Separation of Concerns

- Code is layered by responsibility (e.g., UI → business logic → infrastructure)
- UI event handlers delegate to logic and contain none
- What the UI shows is decision logic too: a choice among alternatives — a color, whether a control is enabled or visible, which message appears — made from data or from an operation's outcome is made in code a test reaches without rendering anything (e.g., a view model that maps an operation's outcome to a message, a value converter). A value that never differs involves no choice

### Dependency Direction

- Dependencies flow in one direction only (e.g., UI → Service → Common → Database). A change that would need a reverse or circular dependency is redesigned before it is implemented

### Minimal Visibility

A declaration visible wider than its callers claims callers that do not exist, and invites the first one to depend on it.

- **Visible only as widely as its callers**: each type and member is visible to the narrowest scope containing every caller it has. Visibility widens when a caller that needs it is written, never in advance
- **The declaration states the reach it has**: a member is never declared wider than its containing type lets it reach, unless a framework that calls the member requires the wider declaration
- **A framework that calls code is a caller**: a test runner, a serializer, a UI binding, and generated code reach code by their own rules, and the visibility they require is the visibility the code needs. Configuring a framework to read past its default visibility opens every member of that visibility to it, not only the intended ones
- **Tests never widen visibility**: a test reaches code it cannot see through the stack's test-access mechanism, or through code it can see
- Stack rules give the modifiers, the mechanisms that enforce this, and which frameworks require a wider declaration

### Functional Core, Imperative Shell

- Decisions are made on data already read, never while reading from a database, files, or the network: a thin I/O shell fetches, the decision runs, the shell writes
- Decision logic takes data as arguments and returns results (pure functions), so a test reaches it without infrastructure
- Decision logic may write a log entry, which changes no result; every other write (a database, files, a message) stays in the shell
- Time, randomness, and environment values are inputs too, passed as arguments. A clock abstraction or DI is introduced only where one argument cannot carry the value — the code reads it more than once or after the call starts (e.g., measuring a duration), or a framework calls the code with a fixed signature

### Declared Mutation

A call changes only what its name and signature show it changes, and state changes only where something must change it. Any other change is found only by reading the body, and a value anything could have changed has to be traced before it can be trusted.

- **Arguments change only when declared**: a function changes an object it receives only when its name says what happens to the argument (sorting it, filling it) or the language marks the parameter as modified. A name for another effect (save, calculate) declares nothing about the arguments. Every other result leaves through the return value
- **Fixed unless something must change it**: where the language has a form for it, a variable or field that nothing assigns after initialization is declared unassignable, and an object whose state nothing changes after construction is declared unchangeable. The two are judged separately: a field that always refers to the same list is unassignable while the list's contents change
- **A framework that writes is a writer**: state a framework can set only after creating the object (a UI binding, an object-relational mapper) stays changeable. A framework that can set it while creating the object (e.g., a deserializer that fills a constructor) is configured to, whether or not that is its default, and the state stays fixed

### Declarative Where It Removes Machinery

Inside a function, the form that leaves less machinery to read is used. Across function boundaries, Functional Core, Imperative Shell governs.

- **Declarative pipelines** (LINQ, map/filter) where they remove mutable accumulators, manual index tracking, nested loops, or manual list appending (e.g., filtering, mapping, grouping, sorting, aggregating)
- **Imperative loops** where a pipeline would need tuple accumulators mimicking mutable state, extra traversals, or convoluted folds, and for complex control flow: early exits with reasons, per-item error handling, cancellation checks, dependence on earlier elements
- **Performance claims require measurement**: a harder-to-read form is justified only by profiler data, never by an assumption

### Snapshot Before Mutate

- Never modify a data source while lazily iterating over it — a directory listing (e.g., C# `Directory.EnumerateFiles`, Python `os.scandir`, POSIX `readdir`), a collection under `foreach`, a database cursor, a lazy query or generator. Some environments throw; others silently skip entries, repeat them, or end the loop early
- Materialize the full item list first (e.g., `.ToList()`, `Directory.GetFiles`, `list(...)`, fetch-all), then run the loop that modifies the source

### No Lasagna Code

Every layer adds behavior.

- **No pass-through methods**: a method is pass-through when its callers would get the same result by calling what it calls, with the same arguments. Logging, metrics, or tracing added to it do not change that — they belong in the layer that does the work or in a mechanism that spans layers
- An adapter is not pass-through: a method that implements an interface the code depends on, so that a test can substitute the implementation, adds that substitution point
- Logic extracted into a new service is called directly by **all** its callers
- No wrapper method stays behind on the original class

### Shared Algorithm, Local Data

- Deduplication extracts only the algorithm (the steps identical everywhere); each call site keeps its own data (queries, mappings, registrations)
- Sharing that would need flags or parameters growing with each new caller stops

### Change as Addition

- **One branch point per set of variations**: code branches on a set of variations (screens, formats, identifiers) in one place only — one branch, or a registration of units. A second place branching on the same set is the one missed when the next variation is added

### Leave No Vestiges

Code reads as though the current implementation were the only one that ever existed.

- **Delete replaced code completely**: superseded implementations, unreachable fields, parameters, and branches, obsolete feature flags, and temporary wrappers go. Past implementations live in Git history, never as commented-out code, dead branches, or duplicate classes
- **Avoid relational naming**: never name a symbol after its predecessor (`_old`, `_new`, `V2`, `Legacy`, `Ex`); the replacement takes the canonical name. A version in a published interface's name (an API path, a package's major version) names that interface, not a predecessor
- **External compatibility exception**: a superseded shape stays only while a dependent the same change cannot update — any code outside the repository — still uses it (e.g., through a published API, a wire protocol, stored data). Callers within the repository are updated in the change itself, never kept on a compatibility wrapper
- **Document retained compatibility paths**: a comment at the retained path names the dependent that requires it and the condition that ends the obligation

### Enforce Invariants in Code

- Prefer compile-time impossibility: where practical, express the rule in types (stage-specific return types for call order, required constructor arguments, narrowed parameter types) so violations do not compile
- A narrowed type checks its rule wherever an instance is created or changed. One that can hold an invalid value — through an unchecked constructor, a setter, or a default the language creates — only renames the unchecked value
- Code that receives a narrowed type does not check its rule again, by a branch or by an assertion: no instance can fail it
- A rule that can be broken silently (wrong call order, configuration after startup, out-of-range values) fails fast instead of relying on documentation
- The error message states the rule itself

### Writes That Hold Together

Applies to every write to durable state — a database, files, a message sent to another system.

- **Atomic units**: writes valid only together (a header and its rows, a file and the record pointing at it) are applied as one unit that completes or leaves no trace (e.g., a database transaction, a temporary file moved into place). A process that stops between separate writes leaves a half-applied state nothing repairs.
- **Safe to re-run**: a process that can stop partway (a crash, a killed job, a request sent twice) reaches the same final state when run again as when it runs once to completion. Whoever re-runs it cannot see how far the first run got.
- **Concurrent updates**: where two users or processes can change the same data, a write checks that the state it was based on is still current, and a write whose basis changed is reported instead of applied (e.g., a version column compared on update). Without the check, the later write silently discards the earlier one.

### Stored Time

- **Instants are UTC**: a point in time that is stored or sent to another system is UTC or carries its UTC offset, converted to a local time zone only for display. A local time without an offset changes meaning when the machine, the server's location, or daylight saving time changes.
- **Dates are not instants**: a calendar date with no time of day (a due date, a delivery date) is stored as a date, never as midnight of some time zone — which is a different date in another.

### Synchronous vs Asynchronous

- I/O-bound work (network, database, files) is async wherever the platform offers an asynchronous API; CPU-bound and pure logic stay synchronous
- Async propagates end-to-end: never block on async code (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`)
- No method is marked async without asynchronous work to await
- Stack rules refine this per stack and take precedence

---

## Code Comments

### Principle

A comment exists only where code cannot carry the context — a clearer name, an extracted function, a type, a test, or a fail-fast check comes first. A valid comment explains **why the current shape, condition, guard, or order is required now**.

- **One constraint per comment**: a comment states one constraint — one fact the code must satisfy — and its reason, and nothing more, at the code the constraint shapes. The reason says only why the constraint holds; a statement about other code or later processing is more than the reason. Each exception to a constraint counts as a constraint of its own; constraints that shape different code are separate comments, each at its own code
- **Offload the rest**: anything more — how a mechanism works, several constraints on the same code, a decision's rationale — goes to an ADR or `docs/references/`, leaving a one-line conclusion and the file path in the comment
- **Current constraints only**: work history — what changed or was removed, who asked, what a review said — belongs in Git history or the PR description, never in code

### Good Comments

- Business rules that are not obvious from the code
- External constraints: API limits, protocol requirements, legacy system behavior
- Invariants, ordering requirements, exceptional states, and guards required only in specific runtime states
- A shape a reader would not have chosen, with the measurement that rejected their choice — the number makes the comment checkable, and deletable once it stops holding

```csharp
// config is null only on first launch (before the settings file is generated)
if (config != null) { ... }

// The contractual matching key is aaa only; extra conditions drop update history (rationale: docs/adr/0002-order-matching-key.md)
var sql = "SELECT ... FROM [Order_TBL] WHERE aaa = @bbb";

// Three passes over 200k rows cost 400ms; one pass keeps the report inside its 1s budget
var count = 0; var sum = 0m; var max = decimal.MinValue;
```

### Do Not Write

- What was changed or removed, and notes like "X removed", "X excluded", "changed after review", or "aligned with screen Y"
- Commented-out code, conditions, fields, or parameters
- A restatement of the next line of code
- A prohibition without its current reason
- A performance claim with nothing measured behind it ("for performance", "this is faster") — it can be neither checked nor refuted, and no one dares delete it

### Where to Put What

| Information | Location |
|---|---|
| Why the current shape, order, or guard is required; external constraints, business rules, invariants | Code comment, only when code cannot express it |
| What changed, and why | Commit message, PR description, CHANGELOG |
| Why something is prohibited; implementation decisions | ADR (`docs/adr/`), with a brief current reason at the site if needed |
| Context beyond one constraint and its reason | ADR or `docs/references/`, with a one-line conclusion and the path at the site |

---

## Specifications

- **Spec-first**: a change that needs behavior the specification lacks (`docs/specs/...`, listed in the `AGENTS.md` Applications table) updates the specification before implementing — never retro-fitted afterwards. What a specification contains is defined in `documentation.md`

---

## Version Display

The first thing to establish about a reported problem is which build produced it.

- Every application exposes the version it was built with as `<AppName> X.Y.Z` (e.g., `MyApp 1.22.3`)
- `<AppName>` is the application's own name as it spells it; the GUI and the CLI print the identical string
- The version is the bare product version — no build hash, build date, or other metadata
- A GUI displays it somewhere always reachable (title bar, about screen, footer)
- A CLI provides `--version` / `-V`; the concrete requirements are in `cli.md`
- The value comes from the project's single version definition (the stack rule's version management), never from a hardcoded string

---

## External Input

Applies to data the application receives while it runs — user input, imported files,
requests, and responses from other systems, including generated output such as a
language model's. Values an operator sets are Configuration Values below.

- **Validate at the boundary**: input is checked against its expected shape and range where it enters the program, and only checked data travels inward. A check repeated at each use site is missing from at least one of them.
- **Canonicalize, then validate**: before the check, the canonicalizations the specification of the input's format defines are applied (e.g., trimming surrounding whitespace, unifying line endings, Unicode normalization) and the variants it permits are removed (e.g., thousands separators) — nothing else. Guessing what an invalid value was meant to be is not canonicalization.
- **Reject, never repair**: input that fails the check is rejected — whole, or only the failing items of a set of independent items — and the rejection states what was wrong. Input is never altered into something that passes: it has no known-good value to fall back on, and a repaired input produces a result nobody submitted.

---

## Values Never Become Code

- **Passed as data**: a value not written in the code itself — input, a configuration value, a stored value — is never concatenated into text that something executes or interprets (SQL, a shell command, HTML, a query language), whatever its source. It goes through the mechanism that keeps it data (e.g., bound query parameters, an argument array, encoding by the rendering layer); a concatenated value can rewrite the command it was placed in.
- **Chosen where no mechanism exists**: where the position has no such mechanism (e.g., a table or column name in SQL), the value selects one of a fixed set of alternatives written in the code, and a value matching none is rejected.

---

## Configuration Values

Applies to every value an operator sets to configure the application — configuration
files, environment variables, and any other external store.

- **Every configurable value has a built-in default in code**, used whenever the external value is missing, unparsable, or out of range. Such a value never fails startup
- **Required values fail fast**: a value no default can stand in for (a credential, the address of the only database the application uses) is checked once at startup, and the application stops with a message naming it before doing any work — not on the first operation that needs it, behind an unrelated failure
- **The target environment is configuration**: which database, service address, or directory the application works against is never a constant in code. A target fixed in code makes the build that is tested differ from the build that runs in production
- **Validate on read**: invalid values are replaced or clamped while the configuration loads, so no later code defends against them
- **Report every substitution once** in the application's log, naming the value, why the external value was not used (missing, unparsable, or out of range), and the default applied. A silent fallback turns an operator's typo into an invisible change of behavior
- **Externalizing a value is a choice**: a value nobody adjusts in the field stays a named constant in code (the stack rule's constants rule). Making it configurable adds the obligations above
- This does not relax Enforce Invariants in Code, which governs values the program itself produces: there a violation is a defect and fails fast, while an operator's value falls back to a known-good one and is reported
- Precedence between sources, for applications that expose a command line, is in `cli.md` (CONFIG)

### Configuration Files in the Repository

Applies to every configuration file that ships with the application — application
settings, deployment descriptors, workflow definitions, and the like.

- **The repository tracks a placeholder template, never a working configuration**: the tracked file is `<name>.template.<ext>` with placeholder values only; the file the application reads is derived from it and stays untracked. A working configuration carries its author's credentials, host names, and paths into every other environment
- **A developer's own configuration stays local**: derived from the template, kept out of version control, and never a source of truth for anyone else
- **What ships is derived from the template by the build**, so distribution never picks up a configuration file that happened to be in a working tree
- **Installation never overwrites an existing configuration**: the derived file is written only when none is present
- **The template is complete in shape**: every key the application reads appears in it, so it documents what is configurable

---

## Failure Reporting

- **Unattended failures reach a person**: a process that runs with nobody watching (a scheduled job, a service, a background task) reports a failure where a person sees it without going to look (e.g., a notification, a monitored event log, an exit code the scheduler alerts on). A failure recorded only in a log file is found when someone notices the missing result.
- **Attended failures reach the user**: an operation a user started that fails tells that user it failed and what they can do next. An operation that ends silently, or a failure that terminates the application, leaves the user believing it succeeded or unable to tell what was lost.

---

## Procedure Design

Applies to every ordered step sequence written for humans or agents: guides (`docs/guides/`), release steps, setup instructions, and procedure lists in PRs or tasks.

### Design Before Writing

- **Define boundaries first**: the entry state and the goal state are fixed before any step is drafted.
- **Redesign on failure**: when the design breaks midway, the flow is reordered, never patched with ad-hoc steps.

### Phases and Steps

- **Two-layer structure**: a long procedure is organized into phases (headings), each one concern, and steps, each one operation.
- **Narrative sequence**: the phase headings alone tell the end-to-end lifecycle (e.g., prepare → change → verify → clean up).
- **Proportional granularity**: one logical operation stays one step; a short procedure needs no phases.

### Motivated Order

- **Group related operations**: operations of one kind stay adjacent unless sequencing or verification demands interleaving.
- **Justifiable positioning**: every step's position is explained by a prerequisite, a verification need, or locality.

### Step Contract

- **Explicit prerequisites**: each step relies only on explicit inputs or outputs of earlier steps, never on an implicit precondition or a later step.
- **Observable confirmations**: each step ends with a verifiable outcome (e.g., "command exits 0", "service responds on port 8080"), never a vague "done".
- **Structured format**: a substantial procedure writes each step as Precondition → Operation → Confirmation.

### Name the Place

- **Explicit operational context**: each step starts with its exact location (URL, screen, working directory, window).
- **Explicit transitions**: moving between environments, directories, or screens is a step of its own.
- **Ground truth verification**: stated locations are checked against the real system, never assumed from earlier steps.

### Automation Boundary

- **Automate mechanical tasks**: repetitive or mechanical steps go into scripts and build tooling where available.
- **Preserve human gates**: irreversible actions and critical judgment calls stay explicit manual steps, tagged (e.g., "requires manual confirmation", "irreversible").

---

## AUDIT

Run this procedure after completing an implementation task. No task is reported complete
until AUDIT passes.

1. **Reload the rules**: re-read the root `AGENTS.md` and every nested `AGENTS.md` covering a touched directory, every file under `docs/rules/`, and the specification under `docs/specs/` of each touched application. A file read in full earlier in the same session and unchanged since needs no re-reading.
2. **Identify target files**: Git changes decide which files are targets, not which lines. With no input, the targets are all uncommitted changes (`git status --porcelain`: modified, added, untracked); with a file path, that file; with a folder path, every file in it. The entire content of every target file is verified: a pre-existing violation in a touched file weighs the same as a new one, and no target file is sampled or skipped.
3. **Verify the change against rules and specifications**:
   - **Core rules, in every target file**: zero violations of `standard.md`, `documentation.md`, `testing.md`, and each conditional core file (`cli.md`, `db.md`) whose condition holds for the application after the change.
   - **Git operations, in the change itself**: zero violations of `git.md`. Its object is the change's commit split, commit messages, branch name, and pull request, never a file's content.
   - **Stack rules, in every target file**: zero violations of every rule its enforcement matrix marks **AUDIT**, and of every Enforced rule whose mechanism the repository lacks.
   - **Application rules, in every target file**: zero violations of `docs/rules/<App>.md` and any other repository-authored rules under `docs/rules/`.
   - **Specification**: implemented behavior matches the application specification under `docs/specs/` exactly. A needed specification change is committed before or with the implementation, never afterwards.
4. **Run mechanical gates**: run the commands defined in `AGENTS.md`; each must execute and exit 0 — the formatter in check mode, a zero-warning build, and the unit and boundary tests. The end-to-end tests run before the pull request instead (`testing.md` When Tests Run).
5. **Fix and report**: fix every violation found and re-run from step 2. Report completion only when clean, stating the files checked, the violations found and fixed (file / line / rule) or "no violations", the gate results with exit codes, and anything deliberately left unresolved.
