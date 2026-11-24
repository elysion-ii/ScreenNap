namespace ScreenNap.Core;

internal sealed record MenuItem(bool Checked, bool IsSeparator, int CommandId, string? Text, bool Disabled = false);
