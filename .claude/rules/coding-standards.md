# Shared Coding Standards

Common technical standards for ScreenNap (C# / .NET 10 / Raw Win32).

For project-specific rules, see `screennap.md`.

---

## COMMENTS: Code Comments

- **Language:** English
- **Style:** Simple inline comments (`//`). No XML documentation.
- **Simple Code:** Brief one-line "what" description
- **Complex Logic:** Explain "why", especially for non-obvious Win32 behavior

---

## NAMESPACE: Namespaces

- **File-scoped namespaces only** (`namespace X;`)

---

## VAR: Type Inference

- **Use `var`** for `new` expressions and other obvious types
- **Use explicit types** for P/Invoke return values (`IntPtr`, `int`, `bool`) and when the type is not clear from the right-hand side

---

## STRING: String Comparison

- **Always specify `StringComparison`** for string methods (`StartsWith`, `EndsWith`, `IndexOf`, `Contains`, `Equals`)
- **Use `StringComparison.Ordinal`** for technical comparisons (device paths, identifiers)
- **Use `StringComparison.OrdinalIgnoreCase`** when case-insensitivity is needed

---

## ERROR: Error Handling

- **No empty catch blocks:** Every `catch` must handle or log the exception
- **Check Win32 return values:** After P/Invoke calls, check the return value and use `Marshal.GetLastWin32Error()` when the function documents `SetLastError`
- **User-facing errors:** Display via tray notification balloon or Win32 `MessageBox`

---

## FILEPATH: File Paths

- **Use `AppContext.BaseDirectory`** for paths relative to the application executable
- **Do NOT use `Directory.GetCurrentDirectory()`** — it changes based on how the app is launched

---

## CONSTANTS: Constants

- **No magic numbers/strings:** Extract hardcoded values to named constants
- **Win32 constants** go in `Native/WindowStyles.cs`
- **Application constants** as `const` in the owning class

---

## DISPOSE: Resource Cleanup

Win32 resources must be explicitly cleaned up. Required cleanup:

| Resource | Cleanup |
|----------|---------|
| Windows (HWND) | `DestroyWindow` |
| Tray icon | `Shell_NotifyIcon(NIM_DELETE, ...)` |
| Menus (HMENU) | `DestroyMenu` |
| Timers | `KillTimer` |
| Window classes | `UnregisterClass` (at shutdown) |

Perform cleanup in `WM_DESTROY` handler or the application shutdown path.
