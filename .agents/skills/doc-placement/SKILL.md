---
name: doc-placement
description: Placement, classification, naming, front matter, and archiving procedure for all non-source documents (specs, guides, references, investigations, notes, ADRs). Use BEFORE creating, moving, renaming, or archiving any document, when deciding where a new document belongs, or when unsure how to name one or what front matter to add. Do not guess placement from memory — read this skill first.
---

# doc-placement

Classification and placement procedure for every non-source document in a repository.

## Principles

- Folders in normal reference paths contain only documents that are currently valid. Retired documents never mix with active ones.
- Classify by the document's role (rule / decision / procedure / reference / investigation / note), not by the feature it relates to.
- Active documents live directly under their category folder. Never create `current/` or `active/` subfolders whose only meaning is "not archived" — the sole exceptions are categories where state itself is meaningful (`investigations/`, and `adr/archive/`).
- Archiving is not deletion: it removes a document from the normal reference path while keeping history. Documents with no future reference value are deleted, not archived.

## Directory layout

```text
AGENTS.md                   router: repository facts, commands, Applications table,
                            reading instructions — no rule text

docs/
  rules/                    rule bodies: shared core + language files (managed by
                            dev-standards) + application rules files
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
| Is it a development or change rule that cannot be enforced mechanically? | `docs/rules/` — repository/application rules go in the application's rules file; shared core and language files are managed by dev-standards |
| Is it a repository fact, command, or routing instruction (no rule text)? | Root `AGENTS.md` (or a nested `AGENTS.md` for a subtree) |
| Does it define correct results (input → output)? | Test code (table-driven) |
| Does it record why a decision was made? | `docs/adr/` |
| Does it state what the system must do (a specification)? | `docs/specs/` |
| Is it a reproducible working procedure? | `docs/guides/` |
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
- **`docs/rules/`**: rule bodies in three layers — shared core (`standard.md`) and language files are distributed verbatim by dev-standards (never edit them; retrofit updates them from a pinned tag and reports local edits); application rules files are repository-authored and hold only deltas and overrides against the shared rules. The more specific file wins on conflict.
- **`docs/adr/`**: the adopted decision plus reasoning, constraints, rejected alternatives, and revisit conditions. Body sections (Nygard-style): Context, Decision, Consequences (required); Alternatives considered, Revisit when (when they exist). A superseded ADR names its successor in front matter (`superseded-by`); an ADR stays an ADR — retired ones go to `docs/adr/archive/`, never `docs/archive/`.
- **`docs/references/`**: durable facts that are hard to read from source alone: external API constraints, auth flows, data mappings, legacy caveats, domain vocabulary. Current-state architecture/design descriptions default here; promote to a dedicated category when numerous.
- **`docs/investigations/`**: state matters — `active/` (ongoing) vs `closed/` (finished, still worth reading; not an archive). When finished, promote durable outcomes: rules → `docs/rules/` (the application rules file), decisions → `docs/adr/`, procedures → `docs/guides/`, facts → `docs/references/`.
- **`docs/notes/` vs `docs/inbox/`**: the line is whether the placement decision has been made — `inbox` holds unclassified documents; `notes` holds documents decided to be lightweight-but-valid. Putting something in `notes` "for now" means it belongs in `inbox`. Promote a note to `references/` when it becomes the authoritative answer you would point someone to.
- **`docs/plans/`**: working area, gitignored and local-only by default (as is `docs/archive/plans/`) — plans stay out of the repository, in progress or archived, unless a project rule deliberately tracks them. In projects that do track plans, clean `docs/plans/` before committing (move finished plans to `docs/archive/plans/`, renaming on collision).

## Front matter

Every document carries front matter with at least `created` and `status`. Three
exemptions: in-progress plans under `docs/plans/` (front matter is added when archived);
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
- Never content-free names: `memo.md`, `temp.md`, `note1.md`, `misc.md`.
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

## Archive vs delete

Archive when the document is invalid or replaced but still traces past incidents or
decision background. Delete when it is duplicated, a spent temporary memo, wrong with no
historical value, or fully covered by Git history.

## Source of truth

When a human-readable reference (e.g., a mapping table) coexists with tests, declare the
source of truth in the document itself. Precedence: table-driven tests > reference
document > current code. Snapshot tests only detect change; they are never a source of
truth.
