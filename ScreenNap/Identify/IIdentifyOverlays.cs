using ScreenNap.Native;

namespace ScreenNap.Identify;

internal interface IIdentifyOverlays
{
    bool AnyShown { get; }
    bool Show(int displayNumber, RECT bounds, bool closesAllAfterDelay);
    void DismissAll();
}
