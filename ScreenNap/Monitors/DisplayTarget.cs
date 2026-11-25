namespace ScreenNap.Monitors;

// What Windows reports about the monitor of one active display path: its friendly name
// (possibly blank), its device interface path, and its EDID when the EDID can be read
internal sealed record DisplayTarget(string FriendlyName, string Connection, byte[]? Edid);
