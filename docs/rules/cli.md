# CLI Standards

**Applies when** the application exposes a command-line interface: it is a console
application, or a GUI application that accepts command-line options.

A conditional core file of the shared core rules (`docs/rules/`). Governs the
command-line interface of an application: syntax, option and subcommand naming, help
output, stream separation, exit codes, configuration precedence, and version display.
Baseline is GNU-style options with subcommands.

This file is **not a list of features to implement**. It defines the naming, syntax, and
behavioral standard to follow when adding CLI capabilities. Except for the mandatory
`--version`, implement an option only when the application provides the corresponding
feature.

## Enforcement matrix

No rule in this file has a mechanical guard: every rule is an **AUDIT** item. A project
scaffold may seed compliant defaults (e.g., a console entry point that already
implements `--help` and `--version`), but nothing prevents their removal — AUDIT still
checks them.

| ID | Rule | Mechanism | Check |
|----|------|-----------|-------|
| SYNTAX | `app [global-options] <command> [command-options] [arguments]` | — | AUDIT |
| OPTIONS | Long form for every option; short forms only for frequent options | — | AUDIT |
| RESERVED | `--version`/`-V` is mandatory; `--help`/`-h`, `-v`, `-q` are reserved names | — | AUDIT — scaffolds may seed the implementation; nothing guards its removal |
| VERSIONOUT | `--version` prints `<AppName> X.Y.Z` — bare version, no build metadata | The stack's version-management rules keep the source value clean (e.g., `dotnet.md` VERSION) | AUDIT |
| SUBCOMMANDS | Subcommand structure, naming, and cross-command consistency | — | AUDIT |
| HELP | Help content and per-level `--help` | — | AUDIT |
| STREAMS | Results to stdout; logs, progress, warnings, and errors to stderr | — | AUDIT |
| EXITCODES | 0 success / 1 runtime error / 2 usage error | — | AUDIT |
| ERRORMSG | `app: <message>` on stderr, with a `--help` hint on usage errors | — | AUDIT |
| CONFIG | Precedence: options > environment variables > config file > built-in defaults | — | AUDIT |
| COMPAT | Avoid incompatible changes; a rename that is made replaces the old name outright | — | AUDIT |

## SYNTAX: Command Structure

Commands follow this structure:

```
app [global-options] <command> [command-options] [arguments]
```

- Global options go immediately after the command name
- Subcommand-specific options go after the subcommand
- Arguments naming the processing targets (file names, ...) go last

Example:

```
app --verbose build --output dist/ src/
```

## OPTIONS: Option Form

### Long form is the canonical name

- Every option MUST have a long form (`--name`)
- Long-form values are accepted both as `--output file.txt` and `--output=file.txt`
- Option names use lowercase letters and hyphens only (e.g., `--dry-run`) — no underscores, no uppercase
- Options take only the `--name` and `-x` forms; no other prefix (e.g., Windows-style `/silent`) is accepted

### Short forms only for frequent options

- Provide a short form (`-o`, ...) only for options used routinely
- A short form is one `-` plus one letter
- Valueless short forms can be bundled (`-abc`)
- Short-form values are accepted as `-o file.txt`

### Argument separator

- Everything after `--` is treated as arguments; no option parsing beyond it

```
app build -- --strange-filename
```

## RESERVED: Reserved Options

The table below is a name reservation, not an implementation demand: **when the feature
exists, it uses this name and short form**. Only the version option is mandatory, in both forms: `--version` and `-V`.

| Long form | Short | Behavior | Implementation |
|---|---|---|---|
| `--help` | `-h` | Print help and exit | Only with a help feature (recommended) |
| `--version` | `-V` | Print the version and exit | Mandatory |
| `--verbose` | `-v` | Enable detailed output | Only with a verbose-output feature |
| `--quiet` | `-q` | Suppress all output except warnings | Only with an output-suppression feature |

- `-v` is reserved for verbose; version uses uppercase `-V`
- When both `--verbose` and `--quiet` are implemented and given together, the later one wins

### Conventional names for common features

When implementing one of these features, use the conventional name — never invent a new
one:

| Option | Purpose |
|---|---|
| `--output <path>` / `-o` | Output destination |
| `--config <path>` / `-c` | Configuration file |
| `--force` / `-f` | Skip confirmation |
| `--dry-run` | Show what would happen without changing anything |
| `--format <name>` | Output format (`json`, `text`, ...) |
| `--no-color` | Disable colored output |

## VERSIONOUT: Version Output

- `--version` prints a single line: the application's name, a space, and the product version — `MyApp 1.22.3`. When the command name differs from the application name, the application name is printed — the same name a GUI would display (Version Display in `standard.md`)
- The version is the bare product version only: no build hash, build date, commit id, or other metadata
- The value comes from the project's single version definition (see the stack rule's version-management rules, e.g., `dotnet.md` VERSION), never from a hardcoded string
- This is the CLI shape of the Version Display rule in `standard.md`; GUI display requirements are stated there

## SUBCOMMANDS: Subcommands

### When to use them

- Split into subcommands when the application has several independent operations
- A small CLI with a single operation gets no subcommands
- Subcommand names are verbs (`build`, `show`, `set`) or target nouns (`config`), nested at most two levels (e.g., `app config set`)

### Naming and consistency

- Subcommand names are lowercase only
- An option meaning the same thing has the same name and role across all subcommands (if one command uses `--output`, another never uses `--out` for it)
- CRUD-like operations are uniformly `list` / `show` / `set` (or `add`) / `remove`

## HELP: Help Output

This section applies when `--help` is implemented (RESERVED: recommended, not
mandatory), except Behavior without arguments, which applies to every CLI.

### Per-level `--help`

`--help` works at every level:

```
app --help              # overall summary and subcommand list
app config --help       # config description and its subcommand list
app config set --help   # usage of config set
```

### Help content

The `--help` output contains:

1. A one-line summary
2. Usage (`Usage: app <command> [options]`)
3. The subcommand list (when subcommands exist)
4. The option list (short form, long form, description)
5. Representative usage examples (when an example shows more than the usage line already does — a leaf command with no arguments or options needs none)

### Behavior without arguments

- When running with no arguments has no meaningful behavior, print a brief usage and exit with code 2. A GUI application that opens its window when started without arguments has meaningful behavior
- Not the full help: only the `Usage:` line plus, when `--help` is implemented, a "see `--help`" pointer

## STREAMS: Standard Output and Standard Error

- Results (the actual output) go to standard output
- Logs, progress, warnings, and errors go to standard error
- When `--format json` is implemented, standard output carries nothing but the JSON

## EXITCODES: Exit Codes

Callers (shell scripts, schedulers, automated orchestrators) branch strictly on exit codes. Standardize exit code semantics across all commands:

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Runtime error (the operation failed) |
| 2 | Usage error (invalid option or argument) |

- 0 means the operation did everything asked. An outcome that did less (e.g., completed with skipped items) never exits 0: it exits 1, or a code 3 or above
- Codes 3 and above, when used, are documented in the help or the documentation

## ERRORMSG: Error Messages

- Errors go to standard error in the form `app: <message>`
- On an invalid option, print the error and, when `--help` is implemented, a `--help` pointer:

```
app: unknown option '--outpt'
Try 'app --help' for more information.
```

- Suggesting the existing name an input most likely meant (`did you mean '--output'?`) is recommended; an error without a suggestion is not a violation

## CONFIG: Configuration and Environment Variables

Precedence, strongest first:

1. Command-line options
2. Environment variables
3. Configuration file
4. Built-in defaults

- Environment variable names are the `APP_` prefix (the uppercased application name) plus upper snake case (e.g., `APP_OUTPUT`)
- When environment variables are implemented, the help documents which option each one corresponds to

## COMPAT: Compatibility

- Avoid breaking changes to published option names, subcommand names, and exit-code semantics.
- A rename that is made replaces the old name outright, without backward-compatibility aliases. Compatibility aliases expand the test surface and clutter `--help` documentation.
- To guarantee machine-readable stability, provide `--format json`; plain-text terminal output provides no stability guarantees.

## Checklist

Before publishing a new CLI, confirm:

- [ ] `--version` / `-V` works (and `--help` / `-h`, when implemented)
- [ ] `--version` prints `<AppName> X.Y.Z` with no build metadata
- [ ] Every option has a long form
- [ ] Everything after `--` is treated as arguments
- [ ] `--help`, when implemented, works under each subcommand
- [ ] Same-named options mean the same thing across all subcommands
- [ ] Results go to stdout; logs and errors go to stderr
- [ ] Exit codes follow the 0 / 1 / 2 contract
- [ ] An invalid option produces a usage error (exit code 2)
