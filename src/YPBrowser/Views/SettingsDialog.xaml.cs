using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;
using YPBrowser.Services;
using YPBrowser.ViewModels;
using YPBrowser.Views.SettingsPages;

namespace YPBrowser.Views;

public sealed partial class SettingsDialog : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly SettingsService _settings;

    public SettingsDialog(SettingsViewModel viewModel, SettingsService settings)
    {
        _viewModel = viewModel;
        _settings = settings;
        InitializeComponent();
        this.SetWindowSize(700, 520);
        this.CenterOnScreen();

        // Navigate to first item
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
        ContentFrame.Content = tag switch
        {
            "YP" => new YpServersPage(_viewModel),
            "Players" => new PlayersPage(_viewModel),
            "Display" => new DisplayPage(_settings),
            "Network" => new NetworkPage(_settings),
            "Notifications" => new NotificationsPage(_settings),
            "Behavior" => new BehaviorPage(_settings),
            _ => null,
        };
    }

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveAsync();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
