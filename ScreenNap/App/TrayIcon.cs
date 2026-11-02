using System.Runtime.InteropServices;
using ScreenNap.Core;
using ScreenNap.Logging;
using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.App;

internal sealed class TrayIcon : IUserNotifier
{
    private const uint IconId = 1;

    private readonly IntPtr _hwnd;
    private IntPtr _iconNormal;
    private IntPtr _iconActive;
    private bool _created;

    internal TrayIcon(IntPtr messageWindowHandle)
    {
        _hwnd = messageWindowHandle;
        _iconNormal = IconHelper.LoadIconFromResource("icon-normal.ico");
        _iconActive = IconHelper.LoadIconFromResource("icon-active.ico");
    }

    internal bool Create()
    {
        NOTIFYICONDATAW nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_MESSAGE | WindowStyles.NIF_ICON | WindowStyles.NIF_TIP;
        nid.uCallbackMessage = WindowStyles.WM_TRAYICON;
        nid.hIcon = _iconNormal;
        SetTipText(ref nid, Strings.TooltipNormal);

        _created = Shell32.Shell_NotifyIconW(WindowStyles.NIM_ADD, ref nid);
        if (!_created)
        {
            Logger.Error($"Shell_NotifyIconW(NIM_ADD) failed (Win32 error: {Marshal.GetLastWin32Error()})");
            DestroyIcons();
            return false;
        }
        Logger.Info("Tray icon created");

        NOTIFYICONDATAW versionData = CreateBaseData();
        versionData.uVersion = WindowStyles.NOTIFYICON_VERSION_4;
        if (!Shell32.Shell_NotifyIconW(WindowStyles.NIM_SETVERSION, ref versionData))
            Logger.Warn($"Shell_NotifyIconW(NIM_SETVERSION) failed (Win32 error: {Marshal.GetLastWin32Error()})");
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
        SetInfoText(ref nid, Strings.NotifyTitle, TrayState.TruncateInfo(message));

        if (!Shell32.Shell_NotifyIconW(WindowStyles.NIM_MODIFY, ref nid))
            Logger.Warn($"Shell_NotifyIconW(NIM_MODIFY) failed for a tray notification (Win32 error: {Marshal.GetLastWin32Error()})");
    }

    internal void Remove()
    {
        if (_created)
        {
            NOTIFYICONDATAW nid = CreateBaseData();
            if (!Shell32.Shell_NotifyIconW(WindowStyles.NIM_DELETE, ref nid))
                Logger.Warn($"Shell_NotifyIconW(NIM_DELETE) failed (Win32 error: {Marshal.GetLastWin32Error()})");
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
                Logger.Warn($"DestroyIcon failed for normal tray icon (Win32 error: {Marshal.GetLastWin32Error()})");
        }
        if (_iconActive != IntPtr.Zero)
        {
            if (User32.DestroyIcon(_iconActive))
                _iconActive = IntPtr.Zero;
            else
                Logger.Warn($"DestroyIcon failed for active tray icon (Win32 error: {Marshal.GetLastWin32Error()})");
        }
    }

    internal void UpdateState(int activeCount)
    {
        if (!_created)
            return;

        NOTIFYICONDATAW nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_ICON | WindowStyles.NIF_TIP;

        TrayState state = TrayState.For(activeCount);
        nid.hIcon = state.UseActiveIcon ? _iconActive : _iconNormal;
        SetTipText(ref nid, state.TipText);

        if (!Shell32.Shell_NotifyIconW(WindowStyles.NIM_MODIFY, ref nid))
            Logger.Warn($"Shell_NotifyIconW(NIM_MODIFY) failed (Win32 error: {Marshal.GetLastWin32Error()})");
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

    private static unsafe void SetInfoText(ref NOTIFYICONDATAW nid, string title, string truncatedText)
    {
        const int TitleCapacity = 64;
        int titleLength = Math.Min(title.Length, TitleCapacity - 1);
        for (int i = 0; i < titleLength; i++)
            nid.szInfoTitle[i] = title[i];
        nid.szInfoTitle[titleLength] = '\0';

        int length = truncatedText.Length;
        for (int i = 0; i < length; i++)
            nid.szInfo[i] = truncatedText[i];
        nid.szInfo[length] = '\0';
    }
}
