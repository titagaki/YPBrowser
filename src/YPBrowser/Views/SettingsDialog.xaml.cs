using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YPBrowser.ViewModels;
using YPBrowser.Views.SettingsPages;

namespace YPBrowser.Views;

public partial class SettingsDialog : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsDialog(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();

        NavList.SelectedIndex = 0;
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var tag = (NavList.SelectedItem as ListBoxItem)?.Tag?.ToString();
        ContentFrame.Content = tag switch
        {
            "General" => new GeneralPage(_viewModel),
            "YP" => new YpServersPage(_viewModel),
            "Display" => new DisplayPage(_viewModel),
            "Notifications" => new NotificationsPage(_viewModel),
            "Download" => new AutoDownloadPage(_viewModel.AutoDownload),
            "Network" => new NetworkPage(_viewModel),
            _ => null,
        };
    }

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        // 入力中のテキストボックスは、フォーカスが外れないと束縛先へ届かない。
        // OK を押しただけで閉じると、最後に打った値が落ちる
        CommitFocusedEdit();

        await _viewModel.ApplyAsync();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    /// <summary>今フォーカスがある入力欄の値を確定させる。</summary>
    private void CommitFocusedEdit()
    {
        var focused = Keyboard.FocusedElement as FrameworkElement;
        focused?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }
}
