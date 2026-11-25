using System.Globalization;

namespace ScreenNap.Monitors;

// What identifies a physical monitor across display-path changes (specification MON-7,
// docs/adr/0005-monitor-identity-by-serial-number-or-connection.md)
internal readonly record struct MonitorIdentity
{
    private MonitorIdentity(string value) => Value = value;

    internal string Value { get; }

    // The EDID's manufacturer, product code, and serial number when it carries a serial
    // number; otherwise the connection, the monitor's device interface path
    internal static MonitorIdentity For(byte[]? edid, string connection)
    {
        if (edid is not null && Edid.Parse(edid) is { SerialNumber: { } serial } parsed)
            return new MonitorIdentity(string.Create(CultureInfo.InvariantCulture, $"EDID {parsed.ManufacturerId:X4} {parsed.ProductCode:X4} {serial}"));
        return new MonitorIdentity($"Connection {connection}");
    }

    public override string ToString() => Value;
}
