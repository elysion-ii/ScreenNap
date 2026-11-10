# Specifications

Part of the shared core rules (`docs/rules/`). Governs what a specification states about
each decision of the application, where each answer comes from, and how an existing
implementation's behavior that no answer confirms is classified and recorded. A decision
the specification leaves open is settled by whoever writes the code, and the traps that
formal checks find in implementations — conditions that match nothing or everything, a
check racing its own write, a failure read as an answer, a value trusted across a
boundary — are decisions nobody stated. Stack-agnostic: how a specification is checked
against this file (a solver, a model checker, enumerating the cases, review) is the
application's choice.

What a specification is, its sections, requirement text, and requirement IDs are in
`documentation.md`; spec-first is in `standard.md` (Specifications); the tests that verify
requirements are in `testing.md` (Requirement Coverage).

## Scope

- **Decisions a change adds or changes**: a change that adds a decision, or changes a decision's inputs, classes, or outcomes, meets this file for that decision.
- **Every decision when written from the implementation**: a specification written from an existing implementation meets this file for every decision of the application.
- **Other decisions wait**: a decision that existed before this file applied to its application, and that no change has added or changed since, is outside this file until its specification is written from the implementation or a change adds or changes it.
- **One criterion before and after the code**: the questions below are the same whether the specification is written before the implementation or from it. Only the sources an answer can come from differ (Where Answers Come From).

## Decisions

- **A decision**: a point where an outcome observable from outside the application differs according to a value the application reads. Observable from outside means one of these only: what the application shows, what it returns or exits with, what it writes outside its own process (a file, a database, a message, another system), and whether it accepts, rejects, or carries out an operation.
- **Not a decision**: a choice whose alternatives produce the same observable outcome (e.g., which of two algorithms computes the same total), and a value that never changes an observable outcome.
- **One decision per outcome**: the conditions that together choose among the alternatives of one outcome (e.g., whether an order is accepted) are one decision, however many places in the code carry them.
- **The inputs of a decision**: every value whose change can change the decision's outcome — e.g., input from a user or another system, configuration values, stored data, the time, and the outcome of another operation.

## Questions

For each decision in scope, the specification answers every question below whose
condition holds for that decision.

- **Answered by requirements**: a question is answered by requirements of the specification (`documentation.md`, Requirement IDs) that state what the question asks. The section of the specification they sit in does not matter.
- **Answered or recorded**: when the specification is written from an existing implementation, a question is also settled by an entry in one of the application's ledgers (Ledgers). A specification meets this file for a decision when every question that applies to it is answered or recorded.
- **Classes are conditions**: an answer states the outcome for a class of values by a condition over the values (e.g., "a quantity from 1 to 999"), never by example values alone. Concrete input → output pairs belong in test code (`documentation.md`, Document responsibilities); the class statement stays in the specification.
- **Each heading ID names a question**: the ID before each question heading (OUTCOMES, SETTINGS, ...) is how a ledger entry or a review names the question.

### Conditions and Settings

#### OUTCOMES: Every value has one outcome

An input no condition covers falls into whichever branch the code happens to reach, and a
branch no input reaches goes unnoticed.

- **Applies to**: every decision.
- **Classes cover every value**: the specification names the decision's inputs and divides the values of each into classes that together cover every value the input can carry where it enters the application — invalid values as well as valid ones, and missing, empty, zero, and negative values wherever the input can carry them. A source that was read but holds no value for the input (an empty field, an absent key) is a class here, not under UNAVAILABLE.
- **One outcome per class**: each class, and each combination of classes of different inputs, has exactly one outcome. A statement covering several combinations at once (e.g., "whatever the quantity") counts for all of them. Where two classes can hold the same value, the specification states which one decides.
- **Boundaries are exact**: a class bounded by an ordered value (a number, a length, a date, a time) states the boundary value, whether the boundary belongs to the class, and its unit — for a date or a time, also the time zone it is read in.
- **Order is an outcome**: an outcome that is a sequence (rows listed, records written, messages sent) states its order, or states that the order carries no meaning.
- **Rules that limit the answer**, e.g.: `standard.md` External Input (invalid input is rejected, never repaired), Configuration Values.

#### SETTINGS: Settings that empty or override each other

A combination of settings can leave an option that matches nothing, or a setting that a
stronger one has already decided, and neither shows until someone asks why the setting
does nothing.

- **Applies when**: the decision reads two or more settings — values set in advance by an operator or a user to select what the decision does (e.g., configuration values, filters, rules, schedules, limits).
- **Combinations that match nothing**: for each combination of settings under which no value of the other inputs can reach one of the decision's outcomes (e.g., an item both included and excluded, a range whose start is after its end), the specification states whether the combination is rejected when the settings are set, or accepted with the outcome the specification states for it.
- **Settings that override**: for each pair of settings where a value of one leaves the other without effect (e.g., "new customers only" set together with "first purchase only"), the specification states which setting decides.
- **Rules that limit the answer**, e.g.: `standard.md` Configuration Values (Validate on read).

#### FORMATS: Values in an earlier representation

Settings, stored values, and messages outlive the version that wrote them; read with
today's rules, an old value can mean something else without anything failing.

- **Applies when**: an input of the decision can arrive in a representation other than the one the application writes now (e.g., a settings file from an earlier version, rows stored before a format change, a file an older version of another system produces).
- **Every earlier value has a reading**: the specification states, for each earlier representation, the value in the current representation each earlier value is read as, so that both give the same outcome — or that the earlier value is rejected, and what the application does then.
- **Rules that limit the answer**, e.g.: `db.md` MEANING.

### Concurrency and Time

#### SHARED: State that other operations change at the same time

A check and the write it guards are separate steps; operations that run between them all
pass the check against a value that is no longer true, and a limit enforced that way is
exceeded without any operation failing.

- **Applies when**: the decision reads state that it or its operation also changes, or that it is meant to keep within a limit (e.g., a stock count, a balance, a quota, a uniqueness requirement) — whether the application keeps that state or another system does — and another operation can change that state, or be decided against it, before the first operation's effect is counted in it: another request, another user, another instance of the application, or another process using the same store.
- **Excluded only by statement**: the question does not apply when the specification states that no other operation changes the state at the same time; that statement is itself an answer with a source (Where Answers Come From).
- **The invariant is named**: the specification states the condition the state always satisfies, under a name (e.g., "STOCK: the stock of an item is never below zero").
- **Simultaneous operations**: the specification states the outcome when operations act on the same state at the same time — which one proceeds and what each other one gets (e.g., rejected with a message) — and the invariant holds in that outcome.
- **Admitted means counted**: every operation the decision admits counts toward the state, where the state is kept, from the moment it is admitted, so the next decision on the same state sees it — in the other system, when another system keeps the limit. An admission recorded later (e.g., queued and written in the background), or never recorded where the limit is kept, leaves a window in which the limit is exceeded.
- **Rules that limit the answer**, e.g.: `standard.md` Writes That Hold Together (Concurrent updates), `db.md` CONSTRAINTS.

#### STALE: Data older than the state it stands for

A copy lags its source; a limit checked against the copy is broken by the lag alone, with
no concurrency involved.

- **Applies when**: the decision reads a copy of state the application or another system keeps elsewhere (e.g., a replica, a cache, an exported file, a value another system sent earlier, a value the application read once and keeps, such as settings read at start).
- **The age is stated**: the specification states how old the copy may be when the decision uses it — a maximum age, or that the decision reads the current state — and the outcome when the copy is older than that.
- **Invariants hold at that age**: every invariant the specification names over the state (SHARED) holds when the decision uses a copy of the maximum age.

#### PARTIAL: Reading while another writes

A reader that meets a write in progress, or one that stopped partway, sees a state that
never existed as a whole.

- **Applies when**: the decision writes state that a reader can read while the write is in progress or after the write stopped partway — another process, another system, another thread, or the application itself after a restart.
- **Units are named**: the specification states which values form one unit — values valid only together.
- **Whole or nothing**: a reader sees each unit either entirely as before the write or entirely as after it, never a part of each, including after the writer stops partway.
- **Rules that limit the answer**, e.g.: `standard.md` Writes That Hold Together (Atomic units).

#### REPEAT: The same intent arriving more than once

A retry after a timeout, a duplicate delivery, or a second press of a button applies the
same intent twice unless something identifies it as the same.

- **Applies when**: an operation that changes state can arrive or run more than once for one intent — a retry after a timeout or a failure, a duplicate delivery, a user submitting twice, a re-run after a stop partway.
- **What identifies the intent**: the specification states what identifies the arrivals of one intent (e.g., an identifier the sender fixes before its first attempt and reuses on every retry). An identifier made anew for each attempt identifies nothing.
- **Later arrivals change nothing**: every arrival after the first leaves the state as the first arrival left it and gets the outcome the first arrival got, so a sender that retries learns the result of its intent.
- **A uniqueness rule is another decision**: a rule that refuses a second intent (e.g., one redemption per customer) does not refuse a later arrival of an intent it already admitted.
- **Rules that limit the answer**, e.g.: `standard.md` Writes That Hold Together (Safe to re-run).

#### LOOPS: Repetition that must end

A retry or polling loop that ends only when another system answers, or when an outside
deadline passes, runs forever when that system never answers and the deadline is not set.

- **Applies when**: the application repeats an action until a condition holds (e.g., a retry, polling, waiting for a resource, raising a value step by step).
- **A bound the application counts**: the specification states a maximum number of repetitions or a maximum duration that the application itself measures, and the outcome when it is reached. A condition outside the application's control (another system's answer, a deadline a caller may set) is never the only bound.

#### PROPAGATION: Changes that must reach another place

A change sent once, as an event, is lost when the application stops between committing
it and sending it; nothing later notices that the target never received it.

- **Applies when**: a change the application commits must afterwards appear somewhere else (e.g., another system, a copy, a derived value, a notification, a search index).
- **A state, not an event**: the specification states the condition that relates the target to the source once the change has reached it (e.g., "every order recorded as shipped is in the carrier's list"), and by when that condition holds after the commit.
- **Survives a stop**: the condition holds by that time even when the application stops at any point after the commit and starts again.
- **Rules that limit the answer**, e.g.: `standard.md` Writes That Hold Together (Atomic units, Safe to re-run).

### Boundaries and Trust

#### UNAVAILABLE: Inputs that cannot be obtained

When a failure to obtain a value comes back as "false" or "no match", every negation and
exclusion built on top turns the failure into a match; and two checks guarding the same
operation, one blocking on failure and one allowing, make the operation's fate depend on
which check fails.

- **Applies when**: obtaining an input can fail — reading its source fails (the source cannot be reached, does not exist, reports an error, or does not answer in time) or evaluating a condition on it raises an error — so that no value exists to classify. A value obtained but empty, malformed, or out of range is a class under OUTCOMES.
- **A class of its own**: the specification states the outcome when the input cannot be obtained, as an outcome stated apart from the outcomes for obtained values, even where the two are the same.
- **Stated after every combination**: the outcome is stated as seen from outside, after every negation, exclusion, and combination (any-of, all-of) the decision applies to the failed input.
- **Checks guarding one operation agree**: where two or more decisions can each stop the same operation, the specification states the operation's outcome when any of their inputs cannot be obtained.
- **Rules that limit the answer**, e.g.: `standard.md` Failure Reporting, Configuration Values (Required values fail fast).

#### EMPTY: Empty sets

An all-of over no conditions is true and an any-of over none is false, so a rule with no
conditions matches everything in one place and nothing in another.

- **Applies when**: the decision ranges over a set that can be empty — a set of conditions (e.g., a rule with no conditions, an empty filter, an empty allow or deny list) or a set of items (e.g., an input with no rows, a search with no results).
- **Stated, not inferred**: the specification states the outcome for the empty set explicitly, as seen from outside, never leaving it to follow from the operator used to combine the members.

#### ORIGIN: Who can set each input

An identity, a permission, or a price taken from a value the requester controls (a query
parameter, a header, a field a client fills, a file the user can edit) is whatever the
requester chooses, and a check that runs only in a program the requester controls is
skipped by calling past it.

- **Applies to**: every input of every decision.
- **The origin is stated**: the specification states where each input comes from and who can set it.
- **Protected effects name their check**: an input that someone outside the application can set has an effect the specification does not let that party choose (e.g., who the requester is, what they may do, a price, a limit) only through a check the specification states — what verifies the input, and that the check runs where the input enters the application. A check in a program that party controls (e.g., a client, a browser page) does not count.
- **Rules that limit the answer**, e.g.: `standard.md` External Input (Validate at the boundary), Values Never Become Code.

#### REPRESENTATION: Values crossing a boundary

Two systems that disagree on a unit, a precision, a time zone, or the meaning of a code
exchange values that convert without error and mean something else.

- **Applies when**: the decision reads or writes a value exchanged with another system, or stored where another system or another version reads it (e.g., a file, a database, an API, a message).
- **Both sides are stated**: the specification states the value's representation as each side holds it — unit, precision, time zone or offset, character encoding, the meaning of each code value, and range, each where it applies.
- **Values one side cannot hold**: the specification states the outcome for a value one side can hold and the other cannot represent (e.g., too large, more precise, an unknown code).
- **Rules that limit the answer**, e.g.: `standard.md` Stored Time, External Input (Canonicalize, then validate).

## Where Answers Come From

Each answer has one of three sources. An answer the writer of the specification picked
because nothing ruled it out is a decision nobody made.

- **A rule**: the rule files under `docs/rules/`, this file included. Where they admit exactly one answer to a question, that answer is the specification's. Where they admit several, the user chooses among them.
- **The user**: the person who decides what the application must do — through the request that started the change, an answer to a question, or a statement already in the specification.
- **The code**: when the specification is written from an existing implementation, what the code does — including what its comments, tests, and guards declare — is a candidate answer, never an answer on its own (Classifying an Implementation's Behavior).
- **Unanswered questions go to the user**: a question that applies and that no rule and no user answers is asked of the user. Before the implementation exists, the decision is not implemented until the user answers.

## Classifying an Implementation's Behavior

Applies when the specification is written from an existing implementation, to every
question that applies to every decision. The code's answer — what the code does in the
case the question asks about — belongs to the first of these classes that holds (only
these three):

1. **Violation**: a rule prohibits the code's answer, or the code's answer contradicts a statement already in the specification or an answer the user gave.
2. **Specified**: a rule admits only the code's answer, or the user confirms it.
3. **Unconfirmed**: every other code answer, including one that reading the code cannot settle.

A comment, a test, or a guard that declares the code's answer does not move it out of
Unconfirmed: it shows what a developer wrote, not what the user decided.

## Handling Each Class

- **Specified answers are requirements**: a specified answer is stated in the specification as a requirement, and brings what `testing.md` requires for a requirement.
- **A violation is fixed or deferred by the user**: for each violation, the user decides to fix it now or to defer it. Never fix a violation without the user's decision.
- **A fixed violation**: the specification states, as a requirement, the answer the rule requires — or the user's answer, where the rules admit several — and the code changes to that answer in the same change.
- **A deferred violation**: recorded in the violation ledger, with the answer the fix will implement — or, where the rules admit several and the user has not chosen, the answers they admit. The code keeps its behavior, and the specification never states that behavior as a requirement.
- **A contradicted statement**: where a violation contradicts a statement already in the specification, the user may also replace the statement with the code's answer, which makes the code's answer specified. Deferred instead, the statement moves from the specification into the violation ledger as the answer the fix will implement, because a specification stating behavior the code does not have fails every later AUDIT of the application.
- **An unconfirmed answer is asked**: the user is asked to confirm it. Confirmed, it is specified. Answered otherwise, the code's answer contradicts the user's answer and is a violation.
- **An unanswered question is recorded**: until the user answers, the unconfirmed answer is recorded in the unconfirmed ledger. The code keeps its behavior, and the specification does not state it.
- **Recorded behavior changes only with its entry resolved**: a change that alters a behavior recorded in either ledger resolves the entry in the same change — an unconfirmed entry through the user's answer, a violation through its fix.

## Ledgers

The ledgers hold behavior the implementation has and the specification deliberately does
not state. They are facts about the implementation, so they sit beside the specification,
not inside it: a specification states behavior the application must exhibit
(`documentation.md`, Document responsibilities).

- **Paths**: an application's unconfirmed ledger is `docs/references/<App>/unconfirmed-behavior.md` and its violation ledger is `docs/references/<App>/violating-behavior.md`, `<App>` being the identifier in the `AGENTS.md` Applications table. A repository that holds a single-file `docs/references/<App>.md` moves it into `docs/references/<App>/` (`documentation.md`, Application scope).
- **Read with the specification**: whenever an application's specification is read — before a change to the application and in AUDIT step 1 (`standard.md`) — its ledgers are read as well.
- **One entry per decision and question**: each entry names the decision, the question by its heading ID, the code's answer, the code that produces it (file and symbol), and whether a comment, a test, or a guard declares it.
- **An unconfirmed entry also states**: the question put to the user, and the answers the rules allow.
- **A violation entry also states**: the rule or the statement the code's answer breaks, and the answer the fix will implement (or the answers the rules admit, where the user has not chosen).
- **Resolved entries leave**: the change that resolves an entry removes it, and a ledger left without entries is deleted.
