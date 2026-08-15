using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using YPBrowser.Abstractions;
using YPBrowser.ViewModels;

namespace YPBrowser.Views;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly ISettingsService _settings;
    private readonly INotificationService _notificationService;

    public MainWindow(MainViewModel viewModel, ISettingsService settings, INotificationService notificationService)
    {
        ViewModel = viewModel;
        _settings = settings;
        _notificationService = notificationService;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += Window_Loaded;
        Closing += Window_Closing;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _settings.LoadAsync();
        _notificationService.Initialize();
        ViewModel.Initialize(Dispatcher);

        var ws = _settings.Current.Window;
        if (ws.Width > 0)
        {
            Width = ws.Width;
            Height = ws.Height;
        }
        if (ws.SplitterPosition > 0)
            DetailRow.Height = new GridLength(ws.SplitterPosition);
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.Current.Window.Width = ActualWidth;
        _settings.Current.Window.Height = ActualHeight;
        _settings.Current.Window.SplitterPosition = DetailRow.Height.Value;
        await _settings.SaveAsync();
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
        var settingsWin = new SettingsDialog(dialog, _settings);
        settingsWin.Owner = this;
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
