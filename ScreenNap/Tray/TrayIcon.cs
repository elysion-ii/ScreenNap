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

    private readonly IntPtr _hwnd;
    private readonly CultureInfo _culture;
    private IntPtr _iconNormal;
    private IntPtr _iconActive;
    private bool _created;

    internal TrayIcon(IntPtr messageWindowHandle, CultureInfo culture)
    {
        _hwnd = messageWindowHandle;
        _culture = culture;
        _iconNormal = IconHelper.LoadIconFromResource("icon-normal.ico");
        _iconActive = IconHelper.LoadIconFromResource("icon-active.ico");
    }

    internal bool Create()
    {
        NOTIFYICONDATAW nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_MESSAGE | WindowStyles.NIF_ICON | WindowStyles.NIF_TIP;
        nid.uCallbackMessage = WindowStyles.WM_TRAYICON;
        nid.hIcon = _iconNormal;
        SetTipText(ref nid, Strings.TooltipNormal(_culture));

        _created = Shell32.Shell_NotifyIconW(WindowStyles.NIM_ADD, ref nid);
        if (!_created)
        {
            Logger.Error("Shell_NotifyIconW(NIM_ADD) failed", Marshal.GetLastWin32Error());
            DestroyIcons();
            return false;
        }
        Logger.Info("Tray icon created");

        NOTIFYICONDATAW versionData = CreateBaseData();
        versionData.uVersion = WindowStyles.NOTIFYICON_VERSION_4;
        if (!Shell32.Shell_NotifyIconW(WindowStyles.NIM_SETVERSION, ref versionData))
            Logger.Warn("Shell_NotifyIconW(NIM_SETVERSION) failed", Marshal.GetLastWin32Error());
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

        if (!Shell32.Shell_NotifyIconW(WindowStyles.NIM_MODIFY, ref nid))
            Logger.Warn("Shell_NotifyIconW(NIM_MODIFY) failed for a tray notification", Marshal.GetLastWin32Error());
    }

    internal void Remove()
    {
        if (_created)
        {
            NOTIFYICONDATAW nid = CreateBaseData();
            if (!Shell32.Shell_NotifyIconW(WindowStyles.NIM_DELETE, ref nid))
                Logger.Warn("Shell_NotifyIconW(NIM_DELETE) failed", Marshal.GetLastWin32Error());
            _created = false;
            Logger.Info("Tray icon removed");
        }

        DestroyIcons();
    }

    private void DestroyIcons()
    {
        if (_iconNormal != IntPtr.Zero)
        {
            if (User32.DestroyIcon(_iconNormal))
                _iconNormal = IntPtr.Zero;
            else
                Logger.Warn("DestroyIcon failed for normal tray icon", Marshal.GetLastWin32Error());
        }
        if (_iconActive != IntPtr.Zero)
        {
            if (User32.DestroyIcon(_iconActive))
                _iconActive = IntPtr.Zero;
            else
                Logger.Warn("DestroyIcon failed for active tray icon", Marshal.GetLastWin32Error());
        }
    }

    internal void UpdateState(int activeCount)
    {
        if (!_created)
            return;

        NOTIFYICONDATAW nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_ICON | WindowStyles.NIF_TIP;

        TrayState state = TrayState.For(activeCount, _culture);
        nid.hIcon = state.UseActiveIcon ? _iconActive : _iconNormal;
        SetTipText(ref nid, state.TipText);

        if (!Shell32.Shell_NotifyIconW(WindowStyles.NIM_MODIFY, ref nid))
            Logger.Warn("Shell_NotifyIconW(NIM_MODIFY) failed", Marshal.GetLastWin32Error());
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
