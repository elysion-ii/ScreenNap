# Code Review Rules

## Output Location

Save review results to `Archive/review/`.

## File Naming

```
{model-id}_{yyyyMMdd}-{seq}.md
```

- **model-id**: The model that performed the review (e.g., `claude-opus-4-6`)
- **yyyyMMdd**: Review date
- **seq**: Sequence number for the day, zero-padded to 3 digits (e.g., `001`, `002`)

Example: `claude-opus-4-6_20260401-001.md`

## Required Content

Each review file MUST include:

- **Header**: Date, reviewer model, review scope, commit hash
- **Verified OK**: Items confirmed correct (table format)
- **Issues**: Each issue as a separate section with location (file:line), severity, description, and fix suggestion
- **Fix Priority**: Summary table ordered by priority
