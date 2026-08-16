using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using YPBrowser.Abstractions;

namespace YPBrowser.Services;

/// <summary>
/// <c>Shell_NotifyIcon</c> による常駐アイコン。
///
/// WinForms の <c>NotifyIcon</c> を使わない理由:
/// このプロジェクトは暗黙の using を有効にしているため、<c>UseWindowsForms</c> を足すと
/// <c>System.Windows.Forms</c> が全ファイルに入り、WPF 側の <c>Application</c> /
/// <c>MessageBox</c> / <c>Binding</c> と名前が衝突する。アイコン 1 個のために
/// コードベース全体へ影響を出したくない。
///
/// メニューを WPF の <c>ContextMenu</c> にしない理由:
/// メニューを出したい場面ではウィンドウを隠しているので、WPF はポップアップを
/// ぶら下げる先が無く開けない。トレイのメニューはシェル側の部品でもあるので、
/// 他の常駐アプリと同じ Win32 のメニューを使う。
/// </summary>
public class TrayIconService : ITrayIconService, IDisposable
{
    private const int WM_APP = 0x8000;
    private const int CallbackMessage = WM_APP + 1;

    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    private const int NIM_ADD = 0x0;
    private const int NIM_MODIFY = 0x1;
    private const int NIM_DELETE = 0x2;

    private const int NIF_MESSAGE = 0x1;
    private const int NIF_ICON = 0x2;
    private const int NIF_TIP = 0x4;

    private const int IMAGE_ICON = 1;
    private const int LR_LOADFROMFILE = 0x10;
    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;

    private const int MF_STRING = 0x0;
    private const int MF_SEPARATOR = 0x800;
    private const int TPM_RIGHTBUTTON = 0x2;
    private const int TPM_RETURNCMD = 0x100;

    private const int MenuIdShow = 1;
    private const int MenuIdExit = 2;

    private readonly ILogger<TrayIconService> _logger;

    private nint _hwnd;
    private nint _hIcon;
    private HwndSource? _source;
    private string _tip = "YPBrowser";

    /// <summary>エクスプローラーが再起動すると常駐アイコンは消えるので、登録し直す合図。</summary>
    private readonly uint _taskbarCreatedMessage;

    public bool IsVisible { get; private set; }

    public event EventHandler? ShowWindowRequested;
    public event EventHandler? ExitRequested;

    public TrayIconService(ILogger<TrayIconService> logger)
    {
        _logger = logger;
        _taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
    }

    public void Attach(Window window)
    {
        if (_hwnd != 0) return;

        // 表示していないウィンドウにもハンドルを作らせる。
        // 「トレイに格納した状態で起動」でも受け口が要るため
        _hwnd = new WindowInteropHelper(window).EnsureHandle();
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);

        _hIcon = LoadTrayIcon();
    }

    /// <summary>
    /// 出力フォルダの app.ico を、画面の小アイコンの大きさで読む。
    /// 見つからなければ exe に埋め込まれたアイコンで代用する。
    /// </summary>
    private nint LoadTrayIcon()
    {
        var cx = GetSystemMetrics(SM_CXSMICON);
        var cy = GetSystemMetrics(SM_CYSMICON);

        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(path))
        {
            var handle = LoadImage(0, path, IMAGE_ICON, cx, cy, LR_LOADFROMFILE);
            if (handle != 0) return handle;
            _logger.LogWarning("Failed to load tray icon from {Path}", path);
        }

        var exe = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exe))
        {
            ExtractIconEx(exe, 0, out _, out var small, 1);
            if (small != 0) return small;
        }

        _logger.LogWarning("No tray icon available, falling back to the system icon");
        return LoadIcon(0, 32512);  // IDI_APPLICATION
    }

    public void Show()
    {
        if (IsVisible || _hwnd == 0) return;

        var data = CreateData(NIF_MESSAGE | NIF_ICON | NIF_TIP);
        if (!Shell_NotifyIcon(NIM_ADD, ref data))
        {
            _logger.LogError("Shell_NotifyIcon(NIM_ADD) failed");
            return;
        }

        IsVisible = true;
        _logger.LogInformation("Tray icon shown");
    }

    public void Hide()
    {
        if (!IsVisible) return;

        var data = CreateData(0);
        Shell_NotifyIcon(NIM_DELETE, ref data);
        IsVisible = false;
        _logger.LogInformation("Tray icon hidden");
    }

    public void UpdateTooltip(int channelCount, int totalListeners)
    {
        _tip = $"YPBrowser\n{channelCount} 配信 / {totalListeners} 視聴者";

        if (!IsVisible) return;
        var data = CreateData(NIF_TIP);
        Shell_NotifyIcon(NIM_MODIFY, ref data);
    }

    private NOTIFYICONDATA CreateData(int flags) => new()
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
        hWnd = _hwnd,
        uID = 1,
        uFlags = flags,
        uCallbackMessage = CallbackMessage,
        hIcon = _hIcon,
        // szTip は 128 文字ぶんしか無い。長い名前の YP が並んでも溢れないように切る
        szTip = _tip.Length > 127 ? _tip[..127] : _tip,
    };

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == _taskbarCreatedMessage && IsVisible)
        {
            // 消えた後なので、こちらの状態を戻してから登録し直す
            IsVisible = false;
            Show();
            return 0;
        }

        if (msg != CallbackMessage) return 0;

        switch ((int)lParam)
        {
            case WM_LBUTTONUP:
            case WM_LBUTTONDBLCLK:
                ShowWindowRequested?.Invoke(this, EventArgs.Empty);
                handled = true;
                break;

            case WM_RBUTTONUP:
                ShowMenu();
                handled = true;
                break;
        }

        return 0;
    }

    private void ShowMenu()
    {
        var menu = CreatePopupMenu();
        if (menu == 0) return;

        try
        {
            AppendMenu(menu, MF_STRING, MenuIdShow, "YPBrowser を表示");
            AppendMenu(menu, MF_SEPARATOR, 0, null);
            AppendMenu(menu, MF_STRING, MenuIdExit, "終了");

            // 前面に出しておかないと、メニューの外を押しても閉じずに残る
            SetForegroundWindow(_hwnd);
            GetCursorPos(out var pt);

            // TPM_RETURNCMD にすると選ばれた項目がそのまま返るので、WM_COMMAND を待たなくていい
            var command = TrackPopupMenuEx(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD,
                                           pt.X, pt.Y, _hwnd, 0);

            // メニューを閉じた後の後始末。これが無いと次に開くまで反応が鈍る
            PostMessage(_hwnd, 0x0000 /* WM_NULL */, 0, 0);

            switch (command)
            {
                case MenuIdShow:
                    ShowWindowRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case MenuIdExit:
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    public void Dispose()
    {
        Hide();

        _source?.RemoveHook(WndProc);
        _source = null;

        if (_hIcon != 0)
        {
            DestroyIcon(_hIcon);
            _hIcon = 0;
        }

        GC.SuppressFinalize(this);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public nint hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public int uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int message, ref NOTIFYICONDATA data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegisterWindowMessageW")]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadImageW")]
    private static extern nint LoadImage(nint hInst, string name, int type, int cx, int cy, int load);

    [DllImport("user32.dll")]
    private static extern nint LoadIcon(nint hInstance, int iconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint hIcon);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "ExtractIconExW")]
    private static extern int ExtractIconEx(string file, int index, out nint large, out nint small, int icons);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "AppendMenuW")]
    private static extern bool AppendMenu(nint menu, int flags, nint id, string? item);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(nint menu, int flags, int x, int y, nint hwnd, nint parameters);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(nint menu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(nint hwnd, int msg, nint wParam, nint lParam);
}
