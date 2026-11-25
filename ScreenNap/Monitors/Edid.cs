using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace ScreenNap.Monitors;

// The fields of a monitor's EDID base block that identify the monitor (VESA E-EDID 1.4)
internal readonly record struct Edid(ushort ManufacturerId, ushort ProductCode, string? SerialNumber)
{
    private const int BaseBlockLength = 128;
    private const int ManufacturerOffset = 8;
    private const int ProductCodeOffset = 10;
    private const int SerialNumberOffset = 12;
    private const int FirstDescriptorOffset = 54;
    private const int DescriptorLength = 18;
    private const int DescriptorCount = 4;
    private const byte SerialNumberDescriptorTag = 0xFF;
    private const int DescriptorTextOffset = 5;
    private const byte DescriptorTextEnd = 0x0A;

    private static readonly byte[] s_header = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];

    // Null when the bytes are not an EDID base block. The serial number is the serial-number
    // text descriptor when present, else the numeric serial number when not zero (specification MON-7)
    internal static Edid? Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < BaseBlockLength || !bytes[..s_header.Length].SequenceEqual(s_header))
            return null;

        ushort manufacturer = BinaryPrimitives.ReadUInt16BigEndian(bytes[ManufacturerOffset..]);
        ushort productCode = BinaryPrimitives.ReadUInt16LittleEndian(bytes[ProductCodeOffset..]);
        uint numericSerial = BinaryPrimitives.ReadUInt32LittleEndian(bytes[SerialNumberOffset..]);

        string? serial = SerialNumberText(bytes)
            ?? (numericSerial == 0 ? null : numericSerial.ToString(CultureInfo.InvariantCulture));
        return new Edid(manufacturer, productCode, serial);
    }

    private static string? SerialNumberText(ReadOnlySpan<byte> bytes)
    {
        for (int i = 0; i < DescriptorCount; i++)
        {
            ReadOnlySpan<byte> descriptor = bytes.Slice(FirstDescriptorOffset + (i * DescriptorLength), DescriptorLength);
            // A display descriptor starts with three zero bytes, then its tag
            if (descriptor[0] != 0 || descriptor[1] != 0 || descriptor[2] != 0 || descriptor[3] != SerialNumberDescriptorTag)
                continue;

            ReadOnlySpan<byte> text = descriptor[DescriptorTextOffset..];
            int end = text.IndexOf(DescriptorTextEnd);
            string value = Encoding.ASCII.GetString(end >= 0 ? text[..end] : text).Trim();
            return value.Length == 0 ? null : value;
        }

        return null;
    }
}
