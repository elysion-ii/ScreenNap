using ScreenNap.Core;
using ScreenNap.Native;

namespace ScreenNap.Tests.TestDoubles;

internal sealed class FakeIdentifyOverlays : IIdentifyOverlays
{
    internal HashSet<int> FailingNumbers { get; } = [];
    internal List<(int DisplayNumber, RECT Bounds, bool ClosesAllAfterDelay)> Shown { get; } = [];
    internal int DismissCalls { get; private set; }

    public bool AnyShown => Shown.Count > 0;

    public bool Show(int displayNumber, RECT bounds, bool closesAllAfterDelay)
    {
        if (FailingNumbers.Contains(displayNumber))
            return false;

        Shown.Add((displayNumber, bounds, closesAllAfterDelay));
        return true;
    }

    public void DismissAll()
    {
        DismissCalls++;
        Shown.Clear();
    }
}
