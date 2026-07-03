using ScreenNap.Native;

namespace ScreenNap.Core;

internal enum CursorAction
{
    None,
    Show,
    Hide
}

internal sealed class CursorIdleTracker
{
    private long _lastMouseMoveTick;
    private int _lastMouseX;
    private int _lastMouseY;
    private bool _hasMousePosition;

    internal CursorIdleTracker(long initialTick)
    {
        _lastMouseMoveTick = initialTick;
    }

    internal bool IsHidden { get; private set; }

    internal CursorAction OnMouseMove(int x, int y, long tick)
    {
        if (_hasMousePosition && x == _lastMouseX && y == _lastMouseY)
            return CursorAction.None;

        _hasMousePosition = true;
        _lastMouseX = x;
        _lastMouseY = y;
        _lastMouseMoveTick = tick;
        if (!IsHidden)
            return CursorAction.None;

        IsHidden = false;
        return CursorAction.Show;
    }

    internal CursorAction OnTimerTick(long tick)
    {
        if (IsHidden || tick - _lastMouseMoveTick < WindowStyles.CURSOR_HIDE_TIMEOUT_MS)
            return CursorAction.None;

        IsHidden = true;
        return CursorAction.Hide;
    }
}
