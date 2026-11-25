namespace ScreenNap.E2ETests.Infrastructure;

internal sealed record MenuEntry(uint Id, string Text, bool IsSeparator, bool IsChecked, bool IsDisabled);
