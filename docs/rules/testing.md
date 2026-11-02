# Testing

Part of the shared core rules (`docs/rules/`). Governs which tests an application has,
how each kind of test is written, how tests trace to the specification, and when each
kind runs. Stack-agnostic: the test frameworks, the automation libraries, and the way a
test environment is provisioned are the stack's or the application's choice.

## Test Layers

Every test belongs to one layer, decided by what the code under test goes through while
the test runs. A test that fits more than one row belongs to the first one.

| Layer | The code under test goes through | Example |
|---|---|---|
| End-to-end | the built application's external entry point — one of its windows, its command line, or its network API (only these three) — with the application running as its own process | a window driven through UI automation; the executable run with arguments |
| Boundary | a database, a file format another system reads, or a network protocol (only these three), using an instance the test run creates | a query run against a database the test run builds; a file the code writes and the test parses back |
| Unit | only code called directly in the test's own process | a decision function checked against a table of inputs |

- **A command per layer**: `AGENTS.md` names the command that runs the tests of each layer the application has. Unit and boundary tests may share one command; end-to-end tests have a command of their own, and the unit and boundary command does not run them.
- **End-to-end tests once a requirement is observable**: an application has end-to-end tests once its specification states a requirement whose outcome can be observed through its external entry point (e.g., something shown in a window, standard output or an exit code, an API response, a file the application writes for another system). An artifact without an external entry point (e.g., a class library) has no end-to-end layer.
- **Input cases live in the unit layer**: every input case of a decision is tested in the unit layer, even when an end-to-end test also passes through the decision. An end-to-end test checks that the path through the entry point works, not every input. Where the decision runs outside the test's process (e.g., a condition in a database query), its input cases are tested in the boundary layer instead.

## Environment Independence

- Tests never read what is outside the test run's control — anything it does not set up itself, apart from the tools the repository's setup instructions install (e.g., the culture/locale, the current time, the timezone, environment variables, files neither the test nor the repository provides, pre-existing database contents, network availability) — even where the outcome happens not to vary with it today
- A read through a library or platform call counts as a read. A scratch directory the platform provides, and a service the test run starts itself and reaches locally, are under the test run's control. So is a database the test run creates, whether its server is a tool the setup instructions install or a service the test run starts
- **Inputs are set by the test**: where the code under test uses such an input (e.g., culture, time), the test sets it explicitly — a unit or boundary test passes it to the code, and an end-to-end test sets it through the application's configuration or the environment of the process it starts. An outcome that depends on an input the end-to-end test cannot set is checked by a unit or boundary test instead. The application an end-to-end test starts may read inputs the test does not set (e.g., the machine's culture); the end-to-end test then checks only outcomes that do not depend on them
- **No third-party services**: tests never call a service the project does not run, free sandboxes included — its availability, limits, and answers are outside the test's control. A unit or boundary test substitutes it behind the interface the code depends on; an end-to-end test points the application's configuration at a substitute service the test run starts

## Self-Sufficient Test Data

- Each test creates the minimal data it needs (e.g., through test data builders with sensible defaults)
- No test depends on data something other than the test can change (e.g., a shared fixture database, data prepared by hand) — shared mutable test data rots. A read-only file committed beside the test is the test's own data
- **Separate databases where keys do not separate**: in every layer, where another test's data could change a test's outcome even under other keys (e.g., the code processes every row of a table), the test uses a database no other test uses
- **Start-up data takes the production path**: data a real installation writes before first use (e.g., settings rows, version stamps, code tables) is created in a test by the installer, migrator, or setup script that writes it in production. Data copied from a running environment is never test data

## Table-Driven Tests

- A function whose spec is "input → output" is tested as a specification table (a parameterized test), not as near-identical test methods
- Rows run null/none → empty → minimal → main cases → boundaries
- Input values are self-describing, so the table reads as documentation

## Tests Accompany the Change

- **Decisions are tested**: a change that adds a decision, or changes its outcome for some input (its condition, a threshold, a value it uses), includes tests that pin the new outcome. Behavior no test observes breaks in the next change without any gate failing.
- **Defects are reproduced first**: a defect fix includes a test that fails without the fix and passes with it, so the defect cannot return unnoticed.
- **Requirements are tested**: a change that adds or changes a requirement includes the tests that verify it (Requirement Coverage).
- **An ID brings the requirement in full**: the change that gives an existing requirement its ID also adds what this file requires for it — a verifying test, and the input cases of the decisions that produce its outcome.

## Boundary Tests

- **Test the boundary itself**: code that crosses the program's boundary (a database query, a file format another system reads, a network protocol) is tested through that boundary with data the test creates (e.g., a database the test run creates and migrates, a file the test writes and parses back). A test of only the logic behind it leaves the query or the format unverified. A test of any layer that goes through the boundary satisfies this rule.
- **Built from the repository**: a database a test uses is built from the schema definitions the repository holds (its migrations or its schema project), never restored from a backup or copied from a running database.

## End-to-End Tests

- **The built application, driven from outside**: the test starts the application as built from the repository, as its own process, and drives it only through its external entry point. The application has no code path for tests alone — no branch taken only when it detects a test, and no setting that only tests use. Copying the build output to a directory the test run creates, and writing the application's configuration file there, still runs the application as built.
- **An environment the test run creates**: every database, file location, and service the application uses during the test is created by the test run, and the application reaches it through its configuration (`standard.md` Configuration Values) or its command-line arguments.
- **The outcome where it lands**: the test checks the outcome itself — the stored row, the written file, the next window shown — not only the message that reports it. Reading the stored row or the written file directly is allowed.
- **Waits are conditions**: the test waits for an observable condition (a window appears, a row exists) with a time limit, never for a fixed duration.
- **Each test owns its data**: each test uses keys no other test uses, so tests pass in any order and alongside each other's data. Inserting and removing that data directly in the test run's environment is allowed.
- **Nothing is left behind**: every process the test run started and every environment it created is removed when the run ends, after a failure as well.

## Requirement Coverage

Requirement IDs are defined in `documentation.md` (Requirement IDs).

- **A test names the requirement it verifies**: the ID appears in the test's name, in an attribute on the test, or in a comment directly above the test. The stack rule file states which of these its language supports.
- **A verifying test fails without the behavior**: a test verifies a requirement when it names the ID and fails if the code that produces the requirement's outcome is removed or bypassed. For "ORD-6: the order total is shown on the order window", a test that reads the total from the window verifies ORD-6; a test of the total calculation alone does not. For "ORD-3: totals are rounded half up", a test with a total such as 10.125 verifies ORD-3; a test with 10.10, which rounding leaves unchanged, does not. For "ORD-8: the order list shows every order of the customer", a test with two orders of the customer verifies ORD-8; a test with one order still passes when the list shows only the first, so it does not.
- **Partial tests may name the ID too**: a test that checks only part of a requirement (e.g., a unit test of one rounding case behind it) may name the requirement's ID.
- **Every requirement is verified**: each requirement a specification states is verified by at least one test. A requirement whose outcome is observable through the external entry point (Test Layers) is verified by at least one end-to-end test; an outcome that depends on an input the end-to-end test cannot set (Environment Independence) is verified by a unit or boundary test instead.
- **No test names a missing requirement**: a test never names an ID that no specification states. The change that removes a requirement removes every reference to its ID.
- A requirement that has no ID yet (`documentation.md` Requirement IDs, older specifications) is outside these rules until it gets one.

## When Tests Run

- **Unit and boundary tests**: at the AUDIT gate of every implementation task (`standard.md` AUDIT) and in every build that produces a distributable.
- **End-to-end tests**: all of them, before a pull request is opened and again after every later push to it (`git.md` Pull Requests). A repository whose application rules let changes land without a pull request runs them before each such push. The AUDIT gate need not run them.
