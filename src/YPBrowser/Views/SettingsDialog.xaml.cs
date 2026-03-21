using System.Windows;
using System.Windows.Controls;
using YPBrowser.Abstractions;
using YPBrowser.ViewModels;
using YPBrowser.Views.SettingsPages;

namespace YPBrowser.Views;

public partial class SettingsDialog : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly ISettingsService _settings;

    public SettingsDialog(SettingsViewModel viewModel, ISettingsService settings)
    {
        _viewModel = viewModel;
        _settings = settings;
        InitializeComponent();

        // Navigate to first item
        NavList.SelectedIndex = 0;
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var tag = (NavList.SelectedItem as ListBoxItem)?.Tag?.ToString();
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
