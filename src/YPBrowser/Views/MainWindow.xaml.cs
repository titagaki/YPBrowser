using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using WinUIEx;
using YPBrowser.Models;
using YPBrowser.Services;
using YPBrowser.ViewModels;

namespace YPBrowser.Views;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly SettingsService _settings;
    private readonly NotificationService _notificationService;
    private double _detailHeight = 150;

    public MainWindow(MainViewModel viewModel, SettingsService settings, NotificationService notificationService)
    {
        ViewModel = viewModel;
        _settings = settings;
        _notificationService = notificationService;
        InitializeComponent();

        Title = "YPBrowser";
        this.SetWindowSize(900, 600);

        Activated += Window_Activated;
        Closed += Window_Closed;
    }

    private async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= Window_Activated;
        await _settings.LoadAsync();
        _notificationService.Initialize(DispatcherQueue);
        ViewModel.Initialize(DispatcherQueue);

        // Restore window size
        var ws = _settings.Current.Window;
        if (ws.Width > 0) this.SetWindowSize(ws.Width, ws.Height);
        _detailHeight = ws.SplitterPosition > 0 ? ws.SplitterPosition : 150;

        // Restore detail panel height
        if (Content is Grid root)
        {
            var contentGrid = root.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 2);
            if (contentGrid?.RowDefinitions.Count >= 3)
                contentGrid.RowDefinitions[2].Height = new GridLength(_detailHeight);
        }
    }

    private async void Window_Closed(object sender, WindowEventArgs args)
    {
        // Save window size via AppWindow
        var appWindow = AppWindow;
        if (appWindow != null)
        {
            _settings.Current.Window.Width = appWindow.Size.Width;
            _settings.Current.Window.Height = appWindow.Size.Height;
        }
        _settings.Current.Window.SplitterPosition = _detailHeight;
        await _settings.SaveAsync();
    }

    private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DetailPanel.SetChannel(ViewModel.SelectedChannel);
    }

    private void ChannelList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ViewModel.OpenChannelCommand.Execute(ViewModel.SelectedChannel);
    }

    private void ChannelList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.OpenChannelCommand.Execute(ViewModel.SelectedChannel);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.F5)
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

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.RefreshCommand.ExecuteAsync(null);
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = App.Services.GetRequiredService<SettingsViewModel>();
        var settingsWin = new SettingsDialog(dialog, _settings);
        settingsWin.Activate();
    }

    private void OpenFavorites_Click(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<FavoritesViewModel>();
        var favWin = new FavoritesDialog(vm, _settings);
        favWin.Activate();
    }

    private void Splitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (Content is not Grid root) return;
        var contentGrid = root.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 2);
        if (contentGrid?.RowDefinitions.Count < 3) return;

        var detailRow = contentGrid!.RowDefinitions[2];
        var newHeight = Math.Max(80, Math.Min(300, _detailHeight - e.Delta.Translation.Y));
        _detailHeight = newHeight;
        detailRow.Height = new GridLength(newHeight);
    }
}
