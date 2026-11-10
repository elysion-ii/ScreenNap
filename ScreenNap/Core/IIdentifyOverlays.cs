using ScreenNap.Native;

namespace ScreenNap.Core;

internal interface IIdentifyOverlays
{
    bool AnyShown { get; }
    bool Show(int displayNumber, RECT bounds, bool closesAllAfterDelay);
    void DismissAll();
}
