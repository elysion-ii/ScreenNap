namespace ScreenNap.App;

internal readonly record struct MonitorIdentity(
    ushort EdidManufacturerId,
    ushort EdidProductCodeId,
    uint ConnectorInstance);
