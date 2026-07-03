namespace ScreenNap.Core;

internal readonly record struct MonitorIdentity(
    ushort EdidManufacturerId,
    ushort EdidProductCodeId,
    uint ConnectorInstance);
