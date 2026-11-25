namespace ScreenNap.Native;

internal sealed class WindowClass
{
    private readonly uint _style;
    // Windows calls this delegate for as long as the class is registered; this field keeps it
    // from being collected (docs/rules/ScreenNap.md, Window Procedures)
    private readonly WNDPROC _wndProc;
    private readonly IWindowApi _api;

    internal WindowClass(string name, uint style, WNDPROC wndProc, IWindowApi api)
    {
        Name = name;
        _style = style;
        _wndProc = wndProc;
        _api = api;
    }

    internal string Name { get; }

    internal bool IsRegistered { get; private set; }

    // On failure the caller reads the cause from IWindowApi.LastError
    internal bool Register(IntPtr hInstance, IntPtr background, IntPtr cursor)
    {
        if (IsRegistered)
            return true;

        IsRegistered = _api.RegisterClass(Name, _style, _wndProc, hInstance, background, cursor);
        return IsRegistered;
    }

    // On failure the caller reads the cause from IWindowApi.LastError
    internal bool Unregister(IntPtr hInstance)
    {
        if (!IsRegistered)
            return true;

        if (!_api.UnregisterClass(Name, hInstance))
            return false;

        IsRegistered = false;
        return true;
    }
}
