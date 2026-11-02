# Database Standards

**Applies when** the application reads or writes a database: a store it queries through a
database engine, whether the engine runs as a server, a hosted service, or a file the
application opens.

A conditional core file of the shared core rules (`docs/rules/`). Governs who changes a
database schema, which invariants the schema itself enforces, how schema changes are
written, recorded, and deployed, and the shape of the queries the application sends. Stack-agnostic: the migration tool, the database
engine, and the data-access library are the stack's or the application's choice.

Rules that bind every durable store and every input, databases included, are in
`standard.md`: atomic and concurrency-safe writes (Writes That Hold Together) and bound
parameters (Values Never Become Code). Tests against a database the test run builds from
the repository's schema are in `testing.md` (Boundary Tests).

## Enforcement matrix

No rule in this file has a mechanical guard: every rule is an **AUDIT** item. A stack or
an application that adds a guard (e.g., a check that fails when a migration file that was
already applied changes) records it in its own rules file.

| ID | Rule | Mechanism | Check |
|----|------|-----------|-------|
| OWNER | Each schema has one owning repository, and only the owner changes it | — | AUDIT |
| CONSTRAINTS | Uniqueness, references, and required values are declared as database constraints | — | AUDIT |
| MIGRATION | The owner changes the schema only through migration scripts committed to its repository | — | AUDIT |
| APPLIED | The database records which migrations it has applied | — | AUDIT |
| FORWARD | Migrations are applied forward only; a pushed migration is never edited or deleted | — | AUDIT |
| MEANING | A migration that changes what stored data means converts the stored rows | — | AUDIT |
| DEPLOY | Each migration stays compatible with every code version running against the database while it is applied | — | AUDIT |
| QUERY | Set work runs in the query; never one query per row | — | AUDIT |

## OWNER: Schema Ownership

- **One owner per schema**: each schema (each database, or each set of tables one team changes, with their columns, constraints, indexes, and views) is owned by exactly one repository, and only that repository's migrations change it. Two repositories that both change one schema each hold a definition the other does not know about.
- **Other users treat the schema as external**: an application that reads or writes a schema it does not own changes nothing in it. A schema change it needs is made by the owner's migration, and the application states its dependency on that change in its own specification.

## CONSTRAINTS: Database Constraints

- **The schema enforces what it can express**: a value that must be unique, a reference that must point at an existing row, and a column that must always hold a value are declared as constraints (e.g., primary key, unique, foreign key, not null). A check in application code alone is bypassed by concurrent writes, by another application using the schema, and by a defect in the code.
- **A prior check in code is for the message**: the application may also check before writing, so the user gets a clear message instead of a constraint error. The constraint remains the guarantee, and the code handles the constraint's rejection as well.

## MIGRATION: Schema Changes

- **Scripts only**: the owner changes the schema only through migration scripts committed to its repository. A change made by hand in a database tool cannot be reviewed, and cannot be reproduced in another environment.
- **Ordered**: a reviewer can derive the full apply order of the migration scripts from the repository's files alone (e.g., a sequence number or a timestamp in each file name). An order kept only in someone's memory is lost the first time someone else applies them.
- **Data that defines the schema is a migration too**: rows the application cannot run without (code tables, master values the code refers to by key) are inserted and changed by migration scripts, under the same rules as the schema.

## APPLIED: Applied Migrations

- **The database records what it has applied**: each database holds the list of migrations already applied to it, written by the same step that applies them. The next migration to apply is then read from the database being changed, not remembered by the person applying it.

## FORWARD: Forward Only

Both rules below apply to a migration from the moment it is pushed where another developer or a pipeline can apply it to a shared environment — any database other than one developer's own local database. A migration whose history is unknown counts as pushed.

- **Undo is a new migration**: reverting a schema change is done by adding a migration that reverses it. A deleted or edited migration leaves databases that already applied it different from databases built afterwards.
- **An applied migration is never edited**: its content stays as it was applied. A database records only that a migration ran, not what the file said at the time.

## MEANING: Changing What Data Means

- **Stored rows follow the new meaning**: a migration that changes what a column or a code value means (a new unit, a split value, a renamed state) also converts the rows already stored under the old meaning, in the same migration. Rows left in the old meaning are read with the new one, and nothing marks which is which.

## DEPLOY: Deploying Schema Changes

- **Compatible with every running version**: a migration keeps working for every version of the code that runs against the database while the migration is applied — the version still running and the version being deployed. Clients and instances are rarely replaced at the same moment, and a version that meets a schema it does not expect fails on its first query.
- **Destructive changes come last**: removing or renaming a column or a table is split across releases — first a migration adds the new shape and the code moves to it, then a later migration removes the old shape once no running version uses it (expand, then contract).
- **Migrations go before the code that needs them**: code that depends on a migration runs against a database only after that migration has been applied to it.
- **A deployment without its migrations is incomplete**: a deployment counts as done only when every migration it carries has been applied to that environment's database.
- **A database inside each installation is migrated by the application**: where every installation holds its own database (e.g., a file the application opens on the user's machine), the application applies its pending migrations at startup, before its first query. No deployment step reaches those databases.

## QUERY: Query Shape

- **Set work runs in the query**: filtering, joining, sorting, and aggregating rows happen in the query the database executes, not by fetching rows and processing them in code. Fetching rows to discard or count them moves data the decision never needed, and the cost grows with the table. A table the application loads whole once and keeps cached (e.g., a code table) is filtered in memory: no fetch is made for the decision.
- **Never one query per row**: data that an operation needs (a decision, a screen, a report) is fetched by a query that returns all of it, not by issuing a query for each row of an earlier result. A query per row multiplies round trips by the row count.
