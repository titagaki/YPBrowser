using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using YPBrowser.ViewModels;

namespace YPBrowser.Views.SettingsPages;

public partial class PlayersPage : UserControl
{
    public SettingsViewModel ViewModel { get; }
    private bool _loading;

    public PlayersPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private void PlayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _loading = true;
        var p = ViewModel.SelectedPlayer;
        PlayerNameBox.Text = p?.Name ?? "";
        PlayerExeBox.Text = p?.ExecutablePath ?? "";
        PlayerArgsBox.Text = p?.ArgumentTemplate ?? "\"{url}\"";
        PlayerDefaultCheck.IsChecked = p?.IsDefault ?? false;
        _loading = false;
    }

    private void PlayerNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedPlayer != null)
            ViewModel.SelectedPlayer.Name = PlayerNameBox.Text;
    }

    private void PlayerExeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedPlayer != null)
            ViewModel.SelectedPlayer.ExecutablePath = PlayerExeBox.Text;
    }

    private void PlayerArgsBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedPlayer != null)
            ViewModel.SelectedPlayer.ArgumentTemplate = PlayerArgsBox.Text;
    }

    private void PlayerDefaultCheck_Click(object sender, RoutedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedPlayer != null)
            ViewModel.SelectedPlayer.IsDefault = PlayerDefaultCheck.IsChecked ?? false;
    }

    private void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPlayer == null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
            Title = "プレイヤーの実行ファイルを選択"
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.SelectedPlayer.ExecutablePath = dialog.FileName;
            PlayerExeBox.Text = dialog.FileName;
        }
    }
}
