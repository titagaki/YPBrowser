using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
using YPBrowser.ViewModels;

namespace YPBrowser.Views.SettingsPages;

public sealed partial class PlayersPage : Page
{
    public SettingsViewModel ViewModel { get; }
    private bool _loading;

    public PlayersPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
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

    private async void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPlayer == null) return;

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow!);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ViewModel.SelectedPlayer.ExecutablePath = file.Path;
            PlayerExeBox.Text = file.Path;
        }
    }
}
