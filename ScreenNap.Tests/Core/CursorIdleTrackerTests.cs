using ScreenNap.Core;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class CursorIdleTrackerTests
{
    [Fact]
    public void OnMouseMove_FirstPositionChangeDoesNotShowVisibleCursor()
    {
        var tracker = new CursorIdleTracker(0);

        CursorAction action = tracker.OnMouseMove(1, 1, 100);

        Assert.Equal(CursorAction.None, action);
    }

    [Fact]
    public void OnMouseMove_FirstPositionAtOriginResetsIdleTimer()
    {
        var tracker = new CursorIdleTracker(0);

        CursorAction action = tracker.OnMouseMove(0, 0, 500);

        Assert.Equal(CursorAction.None, action);
        Assert.Equal(CursorAction.None, tracker.OnTimerTick(10499));
        Assert.Equal(CursorAction.Hide, tracker.OnTimerTick(10500));
    }

    [Fact]
    public void OnMouseMove_SameCoordinatesAreIgnored()
    {
        var tracker = new CursorIdleTracker(0);
        tracker.OnMouseMove(1, 1, 100);

        CursorAction action = tracker.OnMouseMove(1, 1, 500);

        Assert.Equal(CursorAction.None, action);
        Assert.Equal(CursorAction.Hide, tracker.OnTimerTick(10100));
    }

    [Theory]
    [InlineData(9999, 0)]
    [InlineData(10000, 2)]
    [InlineData(10001, 2)]
    public void OnTimerTick_UsesTimeoutBoundary(long tick, int expected)
    {
        var tracker = new CursorIdleTracker(0);

        Assert.Equal((CursorAction)expected, tracker.OnTimerTick(tick));
    }

    [Fact]
    public void OnTimerTick_HidesOnlyOnce()
    {
        var tracker = new CursorIdleTracker(0);
        tracker.OnTimerTick(10000);

        Assert.Equal(CursorAction.None, tracker.OnTimerTick(20000));
    }

    [Fact]
    public void HiddenCursor_MoveShowsAndCanHideAgain()
    {
        var tracker = new CursorIdleTracker(0);
        tracker.OnTimerTick(10000);

        Assert.Equal(CursorAction.Show, tracker.OnMouseMove(1, 1, 11000));
        Assert.Equal(CursorAction.Hide, tracker.OnTimerTick(21000));
    }
}
