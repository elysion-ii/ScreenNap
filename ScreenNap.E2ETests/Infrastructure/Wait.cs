using System.Diagnostics;

namespace ScreenNap.E2ETests.Infrastructure;

internal static class Wait
{
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(50);

    internal static void Until(Func<bool> condition, TimeSpan timeout, string description)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > timeout)
                throw new TimeoutException($"Timed out after {timeout.TotalSeconds:0.#}s waiting until {description}");
            Thread.Sleep(s_pollInterval);
        }
    }

    internal static IntPtr ForWindow(Func<IntPtr> probe, TimeSpan timeout, string description)
    {
        IntPtr window = IntPtr.Zero;
        Until(() => (window = probe()) != IntPtr.Zero, timeout, description);
        return window;
    }
}
