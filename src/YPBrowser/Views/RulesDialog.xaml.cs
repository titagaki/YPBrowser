using System.Windows;
using System.Windows.Input;
using YPBrowser.Models;
using YPBrowser.ViewModels;

namespace YPBrowser.Views;

public partial class RulesDialog : Window
{
    public RulesViewModel ViewModel { get; }

    public RulesDialog(RulesViewModel viewModel, IReadOnlyList<ChannelItem> channels)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.SetChannels(channels);
    }

    private void AddTag_Click(object sender, RoutedEventArgs e) => CommitTagInput();

    private void TagInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        CommitTagInput();
        e.Handled = true;
    }

    private void CommitTagInput()
    {
        // 既存タグを選んだ場合も、入力欄に打った新しい名前の場合も Text から取れる
        var name = TagInput.Text;
        if (string.IsNullOrWhiteSpace(name)) return;

        ViewModel.AddTagByNameCommand.Execute(name);
        TagInput.Text = "";
        TagInput.SelectedItem = null;
    }

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        // 不正な正規表現が残っている間はダイアログを閉じさせない
        var error = ViewModel.FindBlockingError();
        if (error is not null)
        {
            MessageBox.Show(this, error, "ルール", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ViewModel.SaveAsync();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
