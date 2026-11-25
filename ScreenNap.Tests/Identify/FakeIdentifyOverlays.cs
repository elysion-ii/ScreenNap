using ScreenNap.Identify;
using ScreenNap.Native;

namespace ScreenNap.Tests.Identify;

internal sealed class FakeIdentifyOverlays : IIdentifyOverlays
{
    internal HashSet<int> FailingNumbers { get; } = [];
    internal HashSet<int> TimerFailingNumbers { get; } = [];
    internal HashSet<int> NumberFailingNumbers { get; } = [];
    internal List<(int DisplayNumber, RECT Bounds, bool StartsClosingTimer)> Shown { get; } = [];
    internal int DismissCalls { get; private set; }

    public bool AnyShown => Shown.Count > 0;

    public OverlayShowResult Show(int displayNumber, RECT bounds, bool startsClosingTimer)
    {
        if (FailingNumbers.Contains(displayNumber))
            return OverlayShowResult.Failed;

        Shown.Add((displayNumber, bounds, startsClosingTimer));
        return new OverlayShowResult(
            true,
            startsClosingTimer && !TimerFailingNumbers.Contains(displayNumber),
            !NumberFailingNumbers.Contains(displayNumber));
    }

    public void DismissAll()
    {
        DismissCalls++;
        Shown.Clear();
    }
}
