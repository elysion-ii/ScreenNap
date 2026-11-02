using ScreenNap.Core;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class CursorIdleTrackerTests
{
    [Fact]
    [Trait("Requirement", "WIN-5")]
    internal void OnMouseMove_FirstPositionWhileCursorVisible_ReturnsNone()
    {
        var tracker = new CursorIdleTracker(0);

        CursorAction action = tracker.OnMouseMove(1, 1, 100);

        Assert.Equal(CursorAction.None, action);
    }

    [Fact]
    [Trait("Requirement", "WIN-5")]
    internal void OnMouseMove_FirstPositionAtOrigin_RestartsIdlePeriod()
    {
        var tracker = new CursorIdleTracker(0);

        CursorAction action = tracker.OnMouseMove(0, 0, 500);

        Assert.Equal(CursorAction.None, action);
        Assert.Equal(CursorAction.None, tracker.OnTimerTick(10499));
        Assert.Equal(CursorAction.Hide, tracker.OnTimerTick(10500));
    }

    [Fact]
    [Trait("Requirement", "WIN-5")]
    internal void OnMouseMove_SameCoordinates_DoesNotRestartIdlePeriod()
    {
        var tracker = new CursorIdleTracker(0);
        tracker.OnMouseMove(1, 1, 100);

        CursorAction action = tracker.OnMouseMove(1, 1, 500);

        Assert.Equal(CursorAction.None, action);
        Assert.Equal(CursorAction.Hide, tracker.OnTimerTick(10100));
    }

    [Theory]
    [InlineData(9999, CursorAction.None)]
    [InlineData(10000, CursorAction.Hide)]
    [InlineData(10001, CursorAction.Hide)]
    [Trait("Requirement", "WIN-5")]
    internal void OnTimerTick_AnyElapsedTime_HidesAtTimeoutBoundary(long tick, CursorAction expected)
    {
        var tracker = new CursorIdleTracker(0);

        Assert.Equal(expected, tracker.OnTimerTick(tick));
    }

    [Fact]
    [Trait("Requirement", "WIN-5")]
    internal void OnTimerTick_AlreadyHidden_ReturnsNone()
    {
        var tracker = new CursorIdleTracker(0);
        tracker.OnTimerTick(10000);

        Assert.Equal(CursorAction.None, tracker.OnTimerTick(20000));
    }

    [Fact]
    [Trait("Requirement", "WIN-5")]
    internal void OnMouseMove_HiddenCursor_ShowsAndAllowsHidingAgain()
    {
        var tracker = new CursorIdleTracker(0);
        tracker.OnTimerTick(10000);

        Assert.Equal(CursorAction.Show, tracker.OnMouseMove(1, 1, 11000));
        Assert.Equal(CursorAction.Hide, tracker.OnTimerTick(21000));
    }
}
