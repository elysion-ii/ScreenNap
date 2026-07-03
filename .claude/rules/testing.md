# Testing Rules

## Scope

Applies to `ScreenNap.Tests/`.

## Test Row Ordering

When writing parameterized tests (`[Theory]` + `[InlineData]` / `[MemberData]`), order rows consistently:

1. `null` / none
2. Empty (empty string, empty collection)
3. Minimal (single element, shortest valid input)
4. Main cases (typical business scenarios)
5. Boundaries (edge values, maximum lengths, off-by-one)

## Culture Independence

Tests must not depend on the OS culture. When a test exercises culture-sensitive behavior, pin the culture explicitly inside the test.
