using ScreenNap.Native;
using ScreenNap.Tray;

namespace ScreenNap.Tests.TestDoubles;

// Stands in for the notification area: it keeps what the tray shows, which only a call that
// succeeds changes, and fails the calls a test names. Like Windows, it suppresses the standard
// tooltip of a version 4 icon unless the tooltip is set with NIF_SHOWTIP
internal sealed class FakeTrayIconApi : ITrayIconApi
{
    internal const int FailureError = 1460;
    internal static readonly IntPtr NormalIcon = new(0x501);
    internal static readonly IntPtr ActiveIcon = new(0x502);

    public int LastError { get; private set; }

    internal List<(uint Command, uint Flags, string Tip, string Info)> Calls { get; } = [];
    internal HashSet<uint> FailingCommands { get; } = [];
    internal bool DestroyIconFails { get; set; }
    internal HashSet<string> IconsFailingToLoad { get; } = new(StringComparer.Ordinal);
    internal List<IntPtr> DestroyedIcons { get; } = [];

    internal bool IconShown { get; private set; }
    internal IntPtr ShownIcon { get; private set; }
    internal string ShownTip => _version == WindowStyles.NOTIFYICON_VERSION_4 && !_showTip ? string.Empty : _tip;
    internal List<string> ShownNotifications { get; } = [];

    private string _tip = string.Empty;
    private bool _showTip;
    private uint _version;

    internal int Count(uint command) => Calls.Count(call => call.Command == command);

    public unsafe bool NotifyIcon(uint command, ref NOTIFYICONDATAW data)
    {
        string tip;
        string info;
        fixed (char* tipBuffer = data.szTip)
            tip = new string(tipBuffer);
        fixed (char* infoBuffer = data.szInfo)
            info = new string(infoBuffer);
        Calls.Add((command, data.uFlags, tip, info));

        if (FailingCommands.Contains(command) || (command != WindowStyles.NIM_ADD && !IconShown))
        {
            LastError = FailureError;
            return false;
        }

        switch (command)
        {
            case WindowStyles.NIM_ADD:
                if (IconShown)
                {
                    LastError = FailureError;
                    return false;
                }
                IconShown = true;
                break;
            case WindowStyles.NIM_DELETE:
                IconShown = false;
                _version = 0;
                return true;
            case WindowStyles.NIM_SETVERSION:
                _version = data.uVersion;
                return true;
        }

        if ((data.uFlags & WindowStyles.NIF_ICON) != 0)
            ShownIcon = data.hIcon;
        if ((data.uFlags & WindowStyles.NIF_TIP) != 0)
        {
            _tip = tip;
            _showTip = (data.uFlags & WindowStyles.NIF_SHOWTIP) != 0;
        }
        if ((data.uFlags & WindowStyles.NIF_INFO) != 0)
            ShownNotifications.Add(info);
        return true;
    }

    // Explorer drops every tray icon when the taskbar is recreated
    internal void RecreateTaskbar()
    {
        IconShown = false;
        _version = 0;
    }

    // The small-icon size Windows reports; zero stands for a scaling that cannot be read
    internal int SmallIconPixels { get; set; } = 16;
    // The size each icon was loaded at
    internal List<int> LoadedSizes { get; } = [];

    public int SmallIconSize() => SmallIconPixels;

    public IntPtr LoadIcon(string resourceFileName, int pixelSize)
    {
        LoadedSizes.Add(pixelSize);
        return IconsFailingToLoad.Contains(resourceFileName) ? IntPtr.Zero
            : resourceFileName.Contains("active", StringComparison.Ordinal) ? ActiveIcon : NormalIcon;
    }

    public bool DestroyIcon(IntPtr icon)
    {
        if (DestroyIconFails)
        {
            LastError = FailureError;
            return false;
        }
        DestroyedIcons.Add(icon);
        return true;
    }
}
