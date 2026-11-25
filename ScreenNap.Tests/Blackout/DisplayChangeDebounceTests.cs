using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Monitors;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Blackout;

public sealed class DisplayChangeDebounceTests
{
    private static readonly CultureInfo s_english = CultureInfo.GetCultureInfo("en-US");
    private static readonly IntPtr s_messageWindow = new(0x77);

    [Fact]
    [Trait("Requirement", "DISP-1")]
    internal void OnDisplayChange_TimerCannotStart_LogsItAndDoesNotReconcileUntilTheNextChange()
    {
        var api = new FakeWindowApi();
        api.TimerStartFails = (_, _) => true;
        int enumerations = 0;
        var debounce = new DisplayChangeDebounce(api, Manager(), () => { enumerations++; return []; });
        using var log = new LogCapture();

        debounce.OnDisplayChange(s_messageWindow);

        Assert.Empty(api.Timers);
        Assert.Equal(0, enumerations);
        LogAssert.Logged(log, "WARN", $"SetTimer failed for display-change debounce; this display change is not reconciled (Win32 error: {FakeWindowApi.FailureError})");

        api.TimerStartFails = (_, _) => false;
        debounce.OnDisplayChange(s_messageWindow);
        Assert.True(api.Timers.ContainsKey((s_messageWindow, DisplayChangeDebounce.TimerId)));

        debounce.OnTimer(s_messageWindow);

        Assert.Empty(api.Timers);
        Assert.Equal(1, enumerations);
    }

    [Fact]
    [Trait("Requirement", "ERR-10")]
    internal void OnTimer_TimerCannotBeStopped_LogsItAndStillReconciles()
    {
        var api = new FakeWindowApi();
        api.TimerStopFails = (_, _) => true;
        int enumerations = 0;
        var debounce = new DisplayChangeDebounce(api, Manager(), () => { enumerations++; return []; });
        debounce.OnDisplayChange(s_messageWindow);
        using var log = new LogCapture();

        debounce.OnTimer(s_messageWindow);

        Assert.Equal(1, enumerations);
        LogAssert.Logged(log, "WARN", "KillTimer failed for display-change debounce");
    }

    private static BlackoutManager Manager() => new(new FakeBlackoutWindowFactory(), new FakeUserNotifier(), s_english);
}
