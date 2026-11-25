using ScreenNap.Native;

namespace ScreenNap.Identify;

internal interface IIdentifyOverlays
{
    bool AnyShown { get; }
    OverlayShowResult Show(int displayNumber, RECT bounds, bool startsClosingTimer);
    void DismissAll();
}
