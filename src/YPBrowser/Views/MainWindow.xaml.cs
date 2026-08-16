using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using YPBrowser.Abstractions;
using YPBrowser.Helpers;
using YPBrowser.Settings;
using YPBrowser.ViewModels;

namespace YPBrowser.Views;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly ISettingsService _settings;
    private readonly INotificationService _notificationService;
    private readonly ITrayIconService _tray;

    /// <summary>
    /// 起動時の「最小化」は、最小化ボタンの設定に関係なくタスクバーへ置く。
    /// 「起動時の状態」で最小化を選んだ人が、狙っていないトレイ格納に化けるのを防ぐ。
    /// </summary>
    private bool _startupComplete;

    /// <summary>終了時の保存が済んだか。閉じるのを一度止めて保存するので、二重に走らせない。</summary>
    private bool _saved;

    public MainWindow(
        MainViewModel viewModel,
        ISettingsService settings,
        INotificationService notificationService,
        ITrayIconService tray)
    {
        ViewModel = viewModel;
        _settings = settings;
        _notificationService = notificationService;
        _tray = tray;
        DataContext = viewModel;
        InitializeComponent();

        Closing += Window_Closing;
        Closed += (_, _) => (_tray as IDisposable)?.Dispose();
    }

    /// <summary>
    /// 設定を読み込んだ後、ウィンドウを出す前に呼ぶ。
    /// 「トレイに格納した状態で起動」ではウィンドウを一度も出さないので、
    /// 初期化を <c>Loaded</c> に置くと動き出さない。
    /// </summary>
    public void InitializeApp()
    {
        _notificationService.Initialize();

        _tray.Attach(this);

        // トレイのメニューは、閉じるまでメッセージを自前のループで回している。
        // その中でウィンドウを出し入れすると閉じ切らないので、ループを抜けてから実行する
        _tray.ShowWindowRequested += (_, _) => Dispatcher.BeginInvoke(RestoreFromTray);
        _tray.ExitRequested += (_, _) => Dispatcher.BeginInvoke(ExitFromTray);

        ViewModel.Initialize(Dispatcher);

        var ws = _settings.Current.Window;
        if (ws.Width > 0)
        {
            Width = ws.Width;
            Height = ws.Height;
        }
        if (ws.SplitterPosition > 0)
            DetailRow.Height = new GridLength(ws.SplitterPosition);

        RestorePosition(ws);
    }

    /// <summary>
    /// 前回の位置に戻す。画面の外に出てしまう位置なら戻さず、OS の既定の位置に任せる。
    /// </summary>
    private void RestorePosition(WindowSettings ws)
    {
        if (ws.X is not { } x || ws.Y is not { } y) return;

        if (!WindowPlacement.IsOnScreen(
                new Rect(x, y, Width, Height), WindowPlacement.CurrentVirtualScreen()))
            return;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = x;
        Top = y;
    }

    /// <summary>起動処理が終わったことを知らせる。以降の最小化は設定どおりに扱う。</summary>
    public void MarkStartupComplete() => _startupComplete = true;

    /// <summary>ウィンドウを引っ込めてトレイのアイコンだけにする。</summary>
    public void HideToTray()
    {
        _tray.Show();
        ShowInTaskbar = false;
        Hide();
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();

        // ウィンドウが見えている間はアイコンを出さない（入口を 2 つにしない）
        _tray.Hide();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (!_startupComplete) return;
        if (WindowState != WindowState.Minimized) return;
        if (_settings.Current.Behavior.MinimizeButtonAction != MinimizeButtonAction.MinimizeToTray) return;

        HideToTray();
    }

    /// <summary>
    /// 閉じるボタンは常に終了。トレイに逃がす設定は持たない
    /// （最小化と閉じるの両方に格納があると、どちらで消えたのか分からなくなる）。
    ///
    /// 保存が終わるまで閉じるのを一度止める。`Closing` は待ってくれないので、
    /// そのまま await するとプロセスが先に落ちて書き込みが間に合わない。
    /// </summary>
    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_saved) return;

        e.Cancel = true;

        SaveWindowMetrics();
        await _settings.SaveAsync();

        _saved = true;
        Close();
    }

    /// <summary>
    /// トレイのメニューの「終了」。
    /// 「トレイに格納した状態で起動」だとウィンドウを一度も出していないことがあり、
    /// その状態の <c>Close()</c> では終了まで至らない。ここは明示的に落とす。
    /// </summary>
    private async void ExitFromTray()
    {
        SaveWindowMetrics();
        await _settings.SaveAsync();
        _saved = true;

        // プロセスが消えるまでアイコンが残らないように、先に自分で消す
        (_tray as IDisposable)?.Dispose();

        Application.Current.Shutdown();
    }

    private void SaveWindowMetrics()
    {
        // RestoreBounds は「元に戻した状態」の位置と大きさ。最大化・最小化中でも
        // 通常時の値が取れるので、次に開いたとき元の場所へ戻せる。
        // 一度も表示していない（トレイに格納したまま終了した）ときは空になるので前回の値を残す
        var bounds = RestoreBounds;
        if (bounds.IsEmpty || bounds.Width <= 0) return;

        var ws = _settings.Current.Window;
        ws.Width = bounds.Width;
        ws.Height = bounds.Height;
        ws.X = bounds.X;
        ws.Y = bounds.Y;
        ws.SplitterPosition = DetailRow.Height.Value;
    }

    private void ChannelList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        DetailPanel.SetChannel(ViewModel.SelectedChannel);
    }

    private void ChannelList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenChannelCommand.Execute(ViewModel.SelectedChannel);
    }

    private void ChannelList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.OpenChannelCommand.Execute(ViewModel.SelectedChannel);
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            _ = ViewModel.RefreshCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }

    private void PlayChannel_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenChannelCommand.Execute(ViewModel.SelectedChannel);
    }

    private void CopyUrl_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyUrlCommand.Execute(ViewModel.SelectedChannel);
    }

    private void OpenContact_Click(object sender, RoutedEventArgs e)
    {
        var ch = ViewModel.SelectedChannel;
        if (ch == null || string.IsNullOrEmpty(ch.ContactUrl)) return;
        try { Process.Start(new ProcessStartInfo(ch.ContactUrl) { UseShellExecute = true }); }
        catch { }
    }

    private void OpenStats_Click(object sender, RoutedEventArgs e)
    {
        var ch = ViewModel.SelectedChannel;
        if (ch == null || string.IsNullOrEmpty(ch.StatsUrl)) return;
        try { Process.Start(new ProcessStartInfo(ch.StatsUrl) { UseShellExecute = true }); }
        catch { }
    }

    private void ToggleStar_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.ToggleStarCommand.ExecuteAsync(ViewModel.SelectedChannel);
    }

    /// <summary>選択行のジャンル等を初期値に入れた状態でルール編集を開く。</summary>
    private void CreateRuleFromChannel_Click(object sender, RoutedEventArgs e)
    {
        var channel = ViewModel.SelectedChannel;
        if (channel == null) return;
        OpenRulesDialog(ViewModel.CreateRuleFromChannel(channel));
    }

    private void CopyChannelDetail_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyChannelDetailCommand.Execute(ViewModel.SelectedChannel);
    }

    private void StartRecord_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleRecordCommand.Execute(ViewModel.SelectedChannel);
    }

    private void StopRecord_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string channelId)
            ViewModel.StopRecordCommand.Execute(channelId);
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = App.Services.GetRequiredService<SettingsViewModel>();
        var settingsWin = new SettingsDialog(dialog) { Owner = this };
        settingsWin.ShowDialog();
    }

    private void OpenRules_Click(object sender, RoutedEventArgs e) => OpenRulesDialog(null);

    private void OpenRulesDialog(Models.Rule? initialRule)
    {
        var vm = App.Services.GetRequiredService<RulesViewModel>();
        if (initialRule is not null) vm.StartWith(initialRule);

        var dialog = new RulesDialog(vm, ViewModel.LiveChannels) { Owner = this };
        // ルールを変えたら、次の更新を待たずに一覧へ反映する
        if (dialog.ShowDialog() == true) ViewModel.ReapplyTags();
    }

    private void OpenTags_Click(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<TagsViewModel>();
        var dialog = new TagsDialog(vm) { Owner = this };
        if (dialog.ShowDialog() == true) ViewModel.ReapplyTags();
    }
}
