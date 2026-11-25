namespace ScreenNap.Tray;

// What creating the tray icon at startup found (specification ERR-2)
internal enum TrayIconCreation
{
    Created,
    // An icon image cannot be loaded, so no icon can ever be shown
    ImageMissing,
    // Windows did not take the icon, as before the taskbar is ready; adding it can be tried again
    NotAdded,
}
