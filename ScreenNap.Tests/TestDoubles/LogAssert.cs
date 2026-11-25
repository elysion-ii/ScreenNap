using ScreenNap.Logging;
using Xunit;

namespace ScreenNap.Tests.TestDoubles;

// How a unit test checks that the code it called logged an entry
internal static class LogAssert
{
    internal static void Logged(LogCapture log, string level, string fragment)
        => Assert.True(
            log.Entries.Any(entry => entry.Level == level && entry.Message.Contains(fragment, StringComparison.Ordinal)),
            $"No {level} entry contains \"{fragment}\". Logged: {string.Join(" | ", log.Entries.Select(entry => $"[{entry.Level}] {entry.Message}"))}");
}
