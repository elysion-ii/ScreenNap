using System.Text;
using ScreenNap.Monitors;
using Xunit;

namespace ScreenNap.Tests.Monitors;

public sealed class EdidTests
{
    [Theory]
    [Trait("Requirement", "MON-7")]
    [InlineData(0u, null, null)]
    [InlineData(0u, "", null)]
    [InlineData(0u, "   ", null)]
    [InlineData(1234u, null, "1234")]
    [InlineData(0u, "7MT0167B3H9L", "7MT0167B3H9L")]
    [InlineData(1234u, "7MT0167B3H9L", "7MT0167B3H9L")]
    [InlineData(uint.MaxValue, null, "4294967295")]
    internal void Parse_AnySerialNumbers_PrefersTheTextThenTheNonzeroNumber(uint numericSerial, string? serialText, string? expected)
    {
        Assert.Equal(new Edid(0xAC10, 0xA0DB, expected), Edid.Parse(Build(0xAC10, 0xA0DB, numericSerial, serialText)));
    }

    [Fact]
    [Trait("Requirement", "MON-7")]
    internal void Parse_SerialTextInALaterDescriptor_FindsIt()
    {
        byte[] edid = Build(0xAC10, 0xA0DB, 0, null);
        WriteDescriptor(edid, 3, 0xFF, "LATER");

        Assert.Equal("LATER", Edid.Parse(edid)?.SerialNumber);
    }

    [Theory]
    [Trait("Requirement", "MON-7")]
    [InlineData(0)]
    [InlineData(127)]
    internal void Parse_ShorterThanABaseBlock_ReturnsNull(int length)
    {
        Assert.Null(Edid.Parse(Build(0xAC10, 0xA0DB, 1, null).AsSpan(0, length)));
    }

    [Fact]
    [Trait("Requirement", "MON-7")]
    internal void Parse_WrongHeader_ReturnsNull()
    {
        byte[] edid = Build(0xAC10, 0xA0DB, 1, null);
        edid[0] = 0x01;

        Assert.Null(Edid.Parse(edid));
    }

    // A 128-byte EDID base block with the given identifying fields; the serial text, when
    // given, goes into the first display descriptor, and a monitor-name descriptor follows it
    internal static byte[] Build(ushort manufacturer, ushort productCode, uint numericSerial, string? serialText)
    {
        var edid = new byte[128];
        byte[] header = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];
        header.CopyTo(edid, 0);
        edid[8] = (byte)(manufacturer >> 8);
        edid[9] = (byte)manufacturer;
        edid[10] = (byte)productCode;
        edid[11] = (byte)(productCode >> 8);
        BitConverter.GetBytes(numericSerial).CopyTo(edid, 12);
        if (serialText is not null)
            WriteDescriptor(edid, 1, 0xFF, serialText);
        WriteDescriptor(edid, 2, 0xFC, "DELL P2417H");
        return edid;
    }

    private static void WriteDescriptor(byte[] edid, int index, byte tag, string text)
    {
        int offset = 54 + (index * 18);
        edid[offset + 3] = tag;
        byte[] padded = [.. Enumerable.Repeat((byte)0x20, 13)];
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        bytes.CopyTo(padded, 0);
        if (bytes.Length < 13)
            padded[bytes.Length] = 0x0A;
        padded.CopyTo(edid, offset + 5);
    }
}
