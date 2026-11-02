using ScreenNap.Core;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class HotkeyInterpreterTests
{
    [Theory]
    [InlineData(3009, HotkeyActionKind.Identify, -1)]
    [InlineData(3000, HotkeyActionKind.ToggleMonitor, 0)]
    [InlineData(3008, HotkeyActionKind.ToggleMonitor, 8)]
    [InlineData(2999, HotkeyActionKind.None, -1)]
    [InlineData(3010, HotkeyActionKind.None, -1)]
    [InlineData(5000, HotkeyActionKind.None, -1)]
    [Trait("Requirement", "HK-2")]
    [Trait("Requirement", "HK-3")]
    [Trait("Requirement", "MON-2")]
    internal void Interpret_AnyHotkeyId_MapsToAction(int id, HotkeyActionKind expectedKind, int expectedIndex)
    {
        HotkeyAction action = HotkeyInterpreter.Interpret(id);

        Assert.Equal(expectedKind, action.Kind);
        Assert.Equal(expectedIndex, action.MonitorIndex);
    }

    [Theory]
    [Trait("Requirement", "ERR-7")]
    [Trait("Requirement", "L10N-5")]
    [InlineData("en-US", 0, "Ctrl+Shift+Alt+0")]
    [InlineData("en-US", 1, "Ctrl+Shift+Alt+1")]
    [InlineData("en-US", 9, "Ctrl+Shift+Alt+9")]
    [InlineData("ja-JP", 1, "Ctrl+Shift+Alt+1")]
    internal void CombinationFor_AnyDigitAndCulture_NamesTheHotkey(string culture, int digit, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, HotkeyInterpreter.CombinationFor(digit));
    }
}
