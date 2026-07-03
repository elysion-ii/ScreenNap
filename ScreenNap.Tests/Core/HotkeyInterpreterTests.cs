using ScreenNap.Core;
using Xunit;

namespace ScreenNap.Tests.Core;

public sealed class HotkeyInterpreterTests
{
    [Theory]
    [InlineData(3009, 1, -1)]
    [InlineData(3000, 2, 0)]
    [InlineData(3008, 2, 8)]
    [InlineData(2999, 0, -1)]
    [InlineData(3010, 0, -1)]
    [InlineData(5000, 0, -1)]
    public void Interpret_MapsHotkey(int id, int expectedKind, int expectedIndex)
    {
        HotkeyAction action = HotkeyInterpreter.Interpret(id);

        Assert.Equal((HotkeyActionKind)expectedKind, action.Kind);
        Assert.Equal(expectedIndex, action.MonitorIndex);
    }
}
