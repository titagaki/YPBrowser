using System.Windows;
using System.Windows.Controls;
using YPBrowser.ViewModels;

namespace YPBrowser.Views.SettingsPages;

public partial class YpServersPage : UserControl
{
    public SettingsViewModel ViewModel { get; }
    private bool _loading;

    public YpServersPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        ServerList.SelectionChanged += ServerList_SelectionChanged;
    }

    private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _loading = true;
        var s = ViewModel.SelectedYpServer;
        YpNameBox.Text = s?.Name ?? "";
        YpUrlBox.Text = s?.Url ?? "";
        YpHostBox.Text = s?.Host ?? "";
        _loading = false;
    }

    private void YpNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedYpServer != null)
            ViewModel.SelectedYpServer.Name = YpNameBox.Text;
    }

    private void YpUrlBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedYpServer != null)
            ViewModel.SelectedYpServer.Url = YpUrlBox.Text;
    }

    private void YpHostBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedYpServer != null)
            ViewModel.SelectedYpServer.Host = YpHostBox.Text;
    }
}
