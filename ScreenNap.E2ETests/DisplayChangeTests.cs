using System.Diagnostics;
using ScreenNap.E2ETests.Infrastructure;
using Xunit;

namespace ScreenNap.E2ETests;

public sealed class DisplayChangeTests
{
    private const string DebouncedEntry = "Display configuration changed (debounced)";
    private const int BurstLength = 3;
    private static readonly TimeSpan s_burstInterval = TimeSpan.FromMilliseconds(100);

    public DisplayChangeTests() => TestAssets.RequireNoRunningScreenNap();

    [Fact]
    [Trait("Requirement", "DISP-1")]
    internal void DisplayChange_Burst_ReconcilesOnceAfterTheBurstSettles()
    {
        using ScreenNapApp app = ScreenNapApp.Start();

        for (int i = 0; i < BurstLength; i++)
        {
            app.Post(Win32.WM_DISPLAYCHANGE, 0, 0);
            Thread.Sleep(s_burstInterval);
        }
        var sinceLastChange = Stopwatch.StartNew();
        app.WaitForLog(DebouncedEntry);
        TimeSpan settled = sinceLastChange.Elapsed;

        // A later single change is reconciled after anything the burst could still have queued
        app.Post(Win32.WM_DISPLAYCHANGE, 0, 0);
        Wait.Until(() => DebouncedCount(app) >= 2, TimeSpan.FromSeconds(10), "the single change is reconciled");

        Assert.Equal(2, DebouncedCount(app));
        Assert.True(settled >= TimeSpan.FromMilliseconds(300), $"Reconciliation ran {settled.TotalMilliseconds:0}ms after the last change");
    }

    private static int DebouncedCount(ScreenNapApp app)
        => app.LogLines().Count(line => line.EndsWith(DebouncedEntry, StringComparison.Ordinal));

    [Fact]
    [Trait("Requirement", "DISP-6")]
    [Trait("Requirement", "INV-2")]
    internal void DisplayChange_LiveBlackout_KeepsExactlyOneBlackout()
    {
        using ScreenNapApp app = ScreenNapApp.Start();
        app.Select(ScreenNapApp.MonitorCommandBase);
        app.WaitForBlackoutCount(1);

        app.Post(Win32.WM_DISPLAYCHANGE, 0, 0);
        app.WaitForLog(DebouncedEntry);
        app.OpenMenu();

        Assert.Single(app.BlackoutWindows());
    }
}
