using System.Runtime.InteropServices;
using ScreenNap.Native;
using ScreenNap.Resources;

namespace ScreenNap.App;

internal sealed class TrayIcon
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

    internal void Create()
    {
        var nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_MESSAGE | WindowStyles.NIF_ICON | WindowStyles.NIF_TIP;
        nid.uCallbackMessage = WindowStyles.WM_TRAYICON;
        nid.hIcon = _iconNormal;
        SetTipText(ref nid, Strings.TooltipNormal);

        Shell32.Shell_NotifyIconW(WindowStyles.NIM_ADD, ref nid);
        _created = true;

        // Set version for modern notification behavior
        var versionData = CreateBaseData();
        versionData.uVersion = WindowStyles.NOTIFYICON_VERSION_4;
        Shell32.Shell_NotifyIconW(WindowStyles.NIM_SETVERSION, ref versionData);
    }

    internal void Remove()
    {
        if (!_created)
            return;

        var nid = CreateBaseData();
        Shell32.Shell_NotifyIconW(WindowStyles.NIM_DELETE, ref nid);
        _created = false;

        if (_iconNormal != IntPtr.Zero)
            User32.DestroyIcon(_iconNormal);
        if (_iconActive != IntPtr.Zero)
            User32.DestroyIcon(_iconActive);
    }

    internal void UpdateState(int activeCount)
    {
        if (!_created)
            return;

        var nid = CreateBaseData();
        nid.uFlags = WindowStyles.NIF_ICON | WindowStyles.NIF_TIP;

        if (activeCount > 0)
        {
            nid.hIcon = _iconActive;
            SetTipText(ref nid, string.Format(Strings.TooltipActive, activeCount));
        }
        else
        {
            nid.hIcon = _iconNormal;
            SetTipText(ref nid, Strings.TooltipNormal);
        }

        Shell32.Shell_NotifyIconW(WindowStyles.NIM_MODIFY, ref nid);
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
        // szTip is 128 chars max (including null terminator)
        int length = Math.Min(text.Length, 127);
        for (int i = 0; i < length; i++)
            nid.szTip[i] = text[i];
        nid.szTip[length] = '\0';
    }
}
