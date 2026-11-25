using System.Globalization;
using System.Runtime.InteropServices;
using ScreenNap.Logging;
using ScreenNap.Native;
using ScreenNap.Notifications;
using ScreenNap.Resources;

namespace ScreenNap.Tray;

internal sealed class TrayIcon : IUserNotifier
{
    private const uint IconId = 1;
    private const string NormalIconResource = "icon-normal.ico";
    private const string ActiveIconResource = "icon-active.ico";
    // The small-icon size at 100% scaling
    private const int DefaultIconSize = 16;
    // Version 4 icons show the standard tooltip only with NIF_SHOWTIP (specification TRAY-1, TRAY-2)
    private const uint TipFlags = WindowStyles.NIF_TIP | WindowStyles.NIF_SHOWTIP;

    private readonly IntPtr _hwnd;
    private readonly CultureInfo _culture;
    private readonly ITrayIconApi _api;
    private IntPtr _iconNormal;
    private IntPtr _iconActive;
    private bool _created;

    internal TrayIcon(IntPtr messageWindowHandle, CultureInfo culture, ITrayIconApi api)
    {
        _hwnd = messageWindowHandle;
        _culture = culture;
        _api = api;
        int iconSize = api.SmallIconSize();
        if (iconSize == 0)
        {
            Logger.Warn($"The display scaling cannot be read; drawing the tray icon at {DefaultIconSize} pixels");
            iconSize = DefaultIconSize;
        }
        Logger.Info($"Tray icon size: {iconSize} pixels");
        _iconNormal = api.LoadIcon(NormalIconResource, iconSize);
        _iconActive = api.LoadIcon(ActiveIconResource, iconSize);
    }

    // An icon without its image is invisible in the tray, so it counts as no icon; an icon
    // Windows did not take keeps its images for another try (specification ERR-2)
    internal TrayIconCreation Create()
    {
        if (_iconNormal == IntPtr.Zero || _iconActive == IntPtr.Zero)
        {
            Logger.Error($"Tray icon image could not be loaded ({(_iconNormal == IntPtr.Zero ? NormalIconResource : ActiveIconResource)})");
            DestroyIcons();
            return TrayIconCreation.ImageMissing;
        }

        if (!Add(0))
            return TrayIconCreation.NotAdded;
        Logger.Info("Tray icon created");
        return TrayIconCreation.Created;
    }

    // Adds the icon after the taskbar was recreated, or after it was not ready at startup
    // (specification TRAY-5, ERR-2); false while Windows does not take it
    internal bool Restore(int activeCount)
    {
        if (!Add(activeCount))
            return false;
        Logger.Info("Tray icon added after the taskbar was created");
        return true;
    }

    private bool Add(int activeCount)
    {
        TrayState state = TrayState.For(activeCount, _culture);
        NOTIFYICONDATAW nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_MESSAGE | WindowStyles.NIF_ICON | TipFlags;
        nid.uCallbackMessage = WindowStyles.WM_TRAYICON;
        nid.hIcon = state.UseActiveIcon ? _iconActive : _iconNormal;
        SetTipText(ref nid, state.TipText);

        _created = _api.NotifyIcon(WindowStyles.NIM_ADD, ref nid);
        if (!_created)
        {
            Logger.Warn("Shell_NotifyIconW(NIM_ADD) failed", _api.LastError);
            return false;
        }

        NOTIFYICONDATAW versionData = CreateBaseData();
        versionData.uVersion = WindowStyles.NOTIFYICON_VERSION_4;
        if (!_api.NotifyIcon(WindowStyles.NIM_SETVERSION, ref versionData))
            Logger.Warn("Shell_NotifyIconW(NIM_SETVERSION) failed", _api.LastError);
        return true;
    }

    public void Notify(string message)
    {
        if (!_created)
        {
            Logger.Warn("Tray notification skipped because the tray icon does not exist");
            return;
        }

        NOTIFYICONDATAW nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_INFO;
        nid.dwInfoFlags = WindowStyles.NIIF_WARNING;
        SetInfoText(ref nid, TrayState.TruncateInfoTitle(Strings.NotifyTitle(_culture)), TrayState.TruncateInfo(message));

        if (!_api.NotifyIcon(WindowStyles.NIM_MODIFY, ref nid))
            Logger.Warn("Shell_NotifyIconW(NIM_MODIFY) failed for a tray notification", _api.LastError);
    }

    internal void Remove()
    {
        if (_created)
        {
            NOTIFYICONDATAW nid = CreateBaseData();
            if (!_api.NotifyIcon(WindowStyles.NIM_DELETE, ref nid))
                Logger.Warn("Shell_NotifyIconW(NIM_DELETE) failed", _api.LastError);
            _created = false;
            Logger.Info("Tray icon removed");
        }

        DestroyIcons();
    }

    private void DestroyIcons()
    {
        if (_iconNormal != IntPtr.Zero)
        {
            if (_api.DestroyIcon(_iconNormal))
                _iconNormal = IntPtr.Zero;
            else
                Logger.Warn("DestroyIcon failed for normal tray icon", _api.LastError);
        }
        if (_iconActive != IntPtr.Zero)
        {
            if (_api.DestroyIcon(_iconActive))
                _iconActive = IntPtr.Zero;
            else
                Logger.Warn("DestroyIcon failed for active tray icon", _api.LastError);
        }
    }

    internal void UpdateState(int activeCount)
    {
        if (!_created)
            return;

        NOTIFYICONDATAW nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_ICON | TipFlags;

        TrayState state = TrayState.For(activeCount, _culture);
        nid.hIcon = state.UseActiveIcon ? _iconActive : _iconNormal;
        SetTipText(ref nid, state.TipText);

        if (!_api.NotifyIcon(WindowStyles.NIM_MODIFY, ref nid))
            Logger.Warn("Shell_NotifyIconW(NIM_MODIFY) failed for the tray state; the next change of the blackout count tries again", _api.LastError);
    }

    private NOTIFYICONDATAW CreateBaseData()
    {
        var nid = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = IconId
        };
        return nid;
    }

    private static unsafe void SetTipText(ref NOTIFYICONDATAW nid, string text)
    {
        string truncated = TrayState.TruncateTip(text);
        int length = truncated.Length;
        for (int i = 0; i < length; i++)
            nid.szTip[i] = truncated[i];
        nid.szTip[length] = '\0';
    }

    private static unsafe void SetInfoText(ref NOTIFYICONDATAW nid, string truncatedTitle, string truncatedText)
    {
        int titleLength = truncatedTitle.Length;
        for (int i = 0; i < titleLength; i++)
            nid.szInfoTitle[i] = truncatedTitle[i];
        nid.szInfoTitle[titleLength] = '\0';

        int length = truncatedText.Length;
        for (int i = 0; i < length; i++)
            nid.szInfo[i] = truncatedText[i];
        nid.szInfo[length] = '\0';
    }
}
