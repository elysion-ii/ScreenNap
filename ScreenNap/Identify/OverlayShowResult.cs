namespace ScreenNap.Identify;

internal readonly record struct OverlayShowResult(bool Shown, bool StartedClosingTimer, bool NumberDrawn)
{
    internal static OverlayShowResult Failed => new(false, false, false);
}
