using ScreenNap.Hotkeys;
using ScreenNap.Tests.TestDoubles;
using Xunit;

namespace ScreenNap.Tests.Hotkeys;

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

    [Theory]
    [Trait("Requirement", "HK-1")]
    [Trait("Requirement", "HK-3")]
    [InlineData(3000, 1, 0x31u)]
    [InlineData(3008, 9, 0x39u)]
    [InlineData(3009, 0, 0x30u)]
    internal void DigitAndVirtualKeyFor_AnyHotkeyId_NameItsDigitKey(int id, int expectedDigit, uint expectedVirtualKey)
    {
        Assert.Equal(expectedDigit, HotkeyInterpreter.DigitFor(id));
        Assert.Equal(expectedVirtualKey, HotkeyInterpreter.VirtualKeyFor(id));
    }

    [Fact]
    [Trait("Requirement", "HK-1")]
    [Trait("Requirement", "HK-3")]
    internal void HotkeyIds_Always_ListsDigitsOneToNineThenZero()
    {
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9, 0], HotkeyInterpreter.HotkeyIds.Select(HotkeyInterpreter.DigitFor));
    }

    [Theory]
    [Trait("Requirement", "HK-5")]
    [InlineData(null, 3001, false, false)]
    [InlineData(3000, 3000, true, false)]
    [InlineData(3000, 3001, false, false)]
    [InlineData(3000, 3001, true, true)]
    [InlineData(3001, 3000, true, true)]
    [InlineData(3000, 3009, true, true)]
    [InlineData(3009, 3000, true, true)]
    internal void IsChordedPress_AnyHeldState_IgnoresOnlyAnotherDigitWhileTheFirstIsHeld(int? lastActed, int pressed, bool lastActedKeyHeld, bool expected)
    {
        Assert.Equal(expected, HotkeyInterpreter.IsChordedPress(lastActed, pressed, lastActedKeyHeld));
    }
}
