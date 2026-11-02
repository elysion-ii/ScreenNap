# Documentation

Part of the shared core rules (`docs/rules/`). Governs the classification, lifecycle,
placement, naming, and authoring of all non-source documents across a repository —
specifications, guides, references, ADRs, investigations, notes, and plans. Every document
operation must strictly follow these rules. Do not classify, name, or place documents from memory.

## Principles

- Folders in normal reference paths contain only documents that are currently valid. Retired documents never mix with active ones.
- Classify by the document's role (rule / decision / procedure / reference / investigation / note), not by the feature it relates to.
- Active documents live directly under their category folder. Never create `current/` or `active/` subfolders whose only meaning is "not archived" — the sole exceptions are categories where state itself is meaningful (`investigations/`, and `adr/archive/`).
- Archiving is not deletion: it removes a document from the normal reference path while keeping history. Documents with no future reference value are deleted, not archived.

## Document responsibilities

- `AGENTS.md` holds facts, commands, the Applications table, and reading instructions only — no rule text. A sentence that points to where a requirement is stated ("pass the AUDIT gate at the end of `standard.md`") is a reading instruction; a sentence that states a requirement itself ("always run the tests before committing") is rule text.
- `docs/rules/` holds development and change rules that cannot be enforced mechanically.
- `docs/specs/` states behavior a system must exhibit, observable from outside it.
- `docs/adr/` records an adopted decision, its reasoning, consequences, rejected alternatives, and revisit conditions.
- `docs/guides/` records a reproducible working procedure.
- `docs/references/` records a durable fact consulted during work.
- `docs/investigations/`, `docs/notes/`, `docs/plans/`, `docs/inbox/`, and `docs/archive/` each have one distinct purpose — see Category notes below; do not blend them.
- Content that defines a correct input/output pair belongs in test code (table-driven), not in a document.

## Writing

Applies to all documents across the repository:

- **State purpose before procedure**: explain *what* and *why* before *how*. Explicitly document all required 5W1H context instead of leaving it to inference.
- **Distinguish criteria from mechanism**: state the required property or criterion first, then the specific mechanism or tool currently satisfying it. Never present temporary mechanisms or tools as the objective itself.
- **Include numbers only as true criteria**: cite numerical values only when they define enforceable boundaries (system limits, version numbers, configurable defaults). Never quote arbitrary examples or illustrations as requirements.
- **Open with the rule, not an essay**: the first sentence of a section declares what the section requires or decides, and the conditional bullets follow immediately. Never open with reflection, self-questioning, or address to the reader.
- **Each item stands alone**: state the requirement and its reason within the item itself. Never justify a requirement by invoking another rule or by analogy.
- **Write contracts, not admonitions**: state required and prohibited behavior as a contract a human and an agent verify the same way. Never write reassurance, scolding, or commentary about the act of writing.
- **One item, one verdict**: each bullet takes the form `**headword**: applicable condition + required action + reason where needed`, and resolves to a single pass or fail. Never chain multiple requirements, exceptions, and reasons into one sentence.
- **Criteria must be checkable**: state what to remove, what to include, and what condition ends an obligation. Never instruct with subjective adjectives alone (e.g., "simple", "short", "readable", "clean").

### Requirement text

Applies to every text that states a requirement — rule files, specifications, acceptance criteria. Two readers who apply a requirement to the same case reach the same verdict without supplying a judgment of their own.

- **Boundary terms are defined by a test**: a term that decides whether a requirement applies (a layer, a "decision", "behavior", "the environment") is defined by an observable test in the text, never left to the reader's sense of the word.
- **Requirements are stated as requirements**: an obligation is never carried only by a rationale or consequence clause ("so ...", "which means ...").
- **Lists say what they are**: a list whose completeness changes a verdict — what a requirement applies to, or what satisfies it — is marked as examples ("e.g.") or as the complete set ("only", "one of").
- **Conditions run both ways**: a permission granted under a condition also states what holds outside the condition, and every later reference to the permission repeats its condition.
- **Scope follows the hazard**: a requirement is scoped by the property that creates the hazard it prevents (where a value goes), not by a property incidental to it (where the value came from).
- **One strength per requirement**: a requirement's lead sentence and its items state the same strength, and "default" in the sense of a strength (what holds unless someone decides otherwise) appears only together with who may deviate and how. The default of a configuration value — what the application uses when the setting is missing — is unaffected.
- **The property, not a proxy**: a requirement states the property it protects, and the property is the checkable criterion. A measurable proxy (e.g., a line count, the presence of a keyword) follows the property only as an example of meeting it, because a proxy can be met while the property is violated. A number is the criterion only when it is the property itself (a system limit, a version), never when it stands in for a quality.
- **Overlaps are resolved in the text**: where two requirements govern the same artifact, at least one of them states how the two combine.
- **Outside facts are located**: a requirement whose verdict depends on a fact held elsewhere (what a framework requires, what a language can declare) names where that fact is recorded, or states what applies where it does not exist.

### Requirement IDs

- **Every requirement has an ID**: each requirement a specification states, in any of its sections, is a heading that begins with its ID and a colon (`### ORD-1: An order without lines is rejected`). The text under that heading, up to the next heading of the same or a higher level, is that requirement. A heading without an ID may group requirements; it states none itself.
- **The ID's form**: a prefix, a hyphen, and a number (`<PREFIX>-<number>`); the prefix starts with an uppercase letter and holds only uppercase letters and digits (`ORD`, `CLI2`).
- **IDs are unique and never reused**: an ID names one requirement across the repository's specifications, and a removed requirement's ID is never given to another.
- **Older specifications**: a specification whose requirements have no IDs gives all of them IDs in the first change that edits it.

## Directory layout

```text
AGENTS.md                   router: repository facts, commands, Applications table,
                            reading instructions — no rule text

docs/
  rules/                    rule bodies: shared core + task-specific rule files +
                            stack rules (managed by dev-standards) + application
                            rules files
  adr/                      active ADRs
    archive/                superseded / rejected / withdrawn ADRs
  specs/                    specifications: what the system must do
  guides/                   reproducible procedures
  references/               durable project-specific facts
  investigations/
    active/                 ongoing investigations
    closed/                 finished investigations still worth reading
  notes/                    lightweight but classified, valid memos
  plans/                    in-progress work plans (working area, gitignored)
  inbox/                    unclassified documents awaiting triage
  archive/                  retired non-ADR documents, mirrored by category
    rules/ specs/ guides/ references/ investigations/ notes/ plans/
```

The category set is extensible: a project may add role subfolders (e.g., `docs/design/`)
when a role clearly fits no existing category and several documents will share it. Every
added category follows the same rules: active documents directly under the folder,
retirement into `docs/archive/<category>/`, standard front matter.

## Application scope

Every category shares one scope rule: a document directly under `docs/<category>/` is
repository-wide; a document at `docs/<category>/<App>.md` or under
`docs/<category>/<App>/` is specific to that application. The single file and the
directory are equivalent scope markers — pick per category by file count; shapes may
differ between categories. Use the same application identifier in every category.

- The identifier is chosen per repository (matching the solution/project name is
  recommended, not required) and is used **verbatim** — it is exempt from the
  kebab-case naming rule
- Application rules (`docs/rules/<App>...`) and the specification
  (`docs/specs/<App>...`) are a mandatory pair, declared with explicit paths in the
  `AGENTS.md` Applications table. App-splitting any other category is optional
- ADR numbering is per directory: `docs/adr/` and `docs/adr/<App>/` hold independent
  sequences (each scope numbers across its active and archived ADRs). Retired
  app-scoped ADRs go to `docs/adr/archive/<App>/`
- When an application is retired, its rules file and specification move to
  `docs/archive/rules/` and `docs/archive/specs/`; dev-standards-managed files are
  never archived

## Placement decision table

| Question | Destination |
|---|---|
| Is it a development or change rule that cannot be enforced mechanically? | `docs/rules/` — repository/application rules go in the application's rules file; shared core and task-specific rule files are managed by dev-standards; stack rules are managed by dev-standards (or repository-authored until promoted) |
| Is it a repository fact, command, or routing instruction (no rule text)? | Root `AGENTS.md` (or a nested `AGENTS.md` for a subtree) |
| Does it define correct results (input → output)? | Test code (table-driven) |
| Does it record why a decision was made? | `docs/adr/` |
| Does it state what the system must do (a specification)? | `docs/specs/` |
| Is it a reproducible working procedure? | `docs/guides/` — unless a distributed task-specific rule file already defines it as a generic procedure (see Category notes) |
| Is it a durable fact referenced during work? | `docs/references/` |
| Does it record how a specific problem was investigated? | `docs/investigations/` |
| Valid, but too light for a formal category? | `docs/notes/` |
| Is it an in-progress work plan? | `docs/plans/` (finished: `docs/archive/plans/`) |
| Placement not yet decided? | `docs/inbox/` |
| Retired non-ADR document worth keeping? | `docs/archive/<category>/` |
| Retired ADR? | `docs/adr/archive/` |
| No future reference value? | Delete |

## Category notes

- **`AGENTS.md` (router)**: repository and application descriptions, technology stack, the Applications table (rules ↔ specification paths), real commands, and reading/AUDIT instructions. No rule text — rule bodies live in `docs/rules/`. Never duplicate a rule across files.
- **`docs/rules/`**: rule bodies organized in three tiers — Core rules (`standard.md`, `documentation.md`, `git.md`, `testing.md`, and the conditional core files `cli.md` and `db.md`), Stack rules (`dotnet.md`, `typescript.md`, `python.md`, ...; managed by dev-standards when distributed, or repository-authored), and Application rules (`<App>.md`, repository-authored deltas and overrides). Precedence: Application rules > Stack rules > Core rules. Never duplicate a rule across files.
- **`docs/specs/`**: states what the system must do — purpose, scope, users and external systems, required behavior, inputs and outputs, error behavior, invariants, non-functional requirements, and what is out of scope. Specifications describe behavior, not implementation: no class structures, libraries, or internal algorithms. Every requirement carries an ID (Requirement IDs above), which the tests that verify it name (`testing.md` Requirement Coverage). (The rule that a spec must be updated before implementing behavior it does not yet cover — spec-first — is a development rule and lives in `standard.md`, not here.)
- **`docs/adr/`**: the adopted decision plus reasoning, constraints, rejected alternatives, and revisit conditions. Body sections (Nygard-style): Context, Decision, Consequences (required); Alternatives considered, Revisit when (when they exist). A superseded ADR names its successor in front matter (`superseded-by`); an ADR stays an ADR — retired ones go to `docs/adr/archive/`, never `docs/archive/`.
- **`docs/guides/`**: repository- or application-specific reproducible procedures. Exception: a generic procedure that a distributed task-specific rule file already defines (e.g., the `gh pr merge` procedure in `git.md`) is not duplicated here — the rule file is the sole source for that procedure. `docs/guides/` holds only what the rule files do not already cover, including a procedure that layers repository-specific decisions on top of one (e.g., a release guide that adds this repository's own release/tag decision on top of the generic Git procedure).
- **`docs/references/`**: durable facts that are hard to read from source alone: external API constraints, auth flows, data mappings, legacy caveats, domain vocabulary. Current-state architecture/design descriptions default here; promote to a dedicated category when numerous.
- **`docs/investigations/`**: state matters — `active/` (ongoing) vs `closed/` (finished, still worth reading; not an archive). When finished, promote durable outcomes: rules → `docs/rules/` (the application rules file), decisions → `docs/adr/`, procedures → `docs/guides/`, facts → `docs/references/`.
- **`docs/notes/` vs `docs/inbox/`**: the line is whether the placement decision has been made — `inbox` holds unclassified documents; `notes` holds documents decided to be lightweight-but-valid. Putting something in `notes` "for now" means it belongs in `inbox`. Promote a note to `references/` when it becomes the authoritative answer you would point someone to.
- **`docs/plans/`**: working area, gitignored and local-only by default (as is `docs/archive/plans/`) — plans stay out of the repository, in progress or archived, unless a project rule deliberately tracks them. In projects that do track plans, clean `docs/plans/` before committing (move finished plans to `docs/archive/plans/`, renaming on collision).

## Front matter

Every document carries front matter with at least `created` and `status`. The
exemptions are: repository-root entry files (`README.md`, `CHANGELOG.md`), which face
external readers and tooling that expect them to open with their content;
in-progress plans under `docs/plans/` (front matter is added when archived);
ADRs, which do **not** carry `created` — the sequence number and git history already
encode chronology, so ADR front matter is `status` only, plus `supersedes`/`superseded-by`
when applicable (never add `created` to an ADR); and dev-standards-managed files under
`docs/rules/`, which carry no front matter — they are distributed verbatim and
version-managed by the pinned dev-standards tag (repository-authored rules files carry
standard front matter).

```yaml
---
status: active
created: 2026-07-15
---
```

### `status` vocabulary

| Document type | Allowed values |
|---|---|
| ADR | `accepted` / `superseded` / `rejected` / `withdrawn` |
| Investigation | `active` / `closed` / `archived` |
| Everything else | `active` / `archived` |

Do not invent other values. Optional fields: `last-reviewed`, `topics`,
`supersedes` / `superseded-by`.

## Naming

- Lowercase kebab-case describing the content: `database-migration.md`, `authentication-flow.md`.
- **Application identifiers are exempt**: `<App>.md` files and `<App>/` directories use the identifier verbatim, exactly as declared in the `AGENTS.md` Applications table (see Application scope).
- Never content-free names (e.g., `memo.md`, `temp.md`, `note1.md`, `misc.md`): a name that does not say what the document is about.
- No sequential numbering for general documents (`0001-`) — chronology belongs in `git log`.
- **ADRs are the exception**, following the community convention: `NNNN-description.md` with a 4-digit zero-padded number. Numbers are immutable IDs allocated append-only: next = highest existing number across `docs/adr/` **and** `docs/adr/archive/` + 1. Never renumber and never fill gaps.
- Infrastructure directories are lowercase (`docs/`, `adr/`, `guides/`); code directories follow the language convention.

## Lifecycle

```text
inbox → (rules | adr | specs | guides | references | investigations | notes) → archive → delete when worthless
investigations: active → closed → archive/investigations
adr:            adr/ → adr/archive/
plans:          docs/plans/ → docs/archive/plans/ (or delete)
```

A document moves only along these arrows, one stage at a time; an investigation that is finished but still worth reading goes to `closed/`, never straight to the archive. A document is retired — and leaves the normal reference path — when it is no longer valid: replaced, or describing a state the system is no longer in.

## Archive vs delete

Archive when the document is invalid or replaced but still traces past incidents or
decision background. Delete when it is duplicated, a spent temporary memo, wrong with no
historical value, or fully covered by Git history.

## Source of truth

When a human-readable reference (e.g., a mapping table) coexists with tests, declare the
source of truth in the document itself. Precedence: table-driven tests > reference
document > current code. Snapshot tests only detect change; they are never a source of
truth.
