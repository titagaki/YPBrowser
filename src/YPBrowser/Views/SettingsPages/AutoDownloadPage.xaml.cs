using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using YPBrowser.ViewModels;

namespace YPBrowser.Views.SettingsPages;

public partial class AutoDownloadPage : UserControl
{
    public AutoDownloadViewModel ViewModel { get; }
    private bool _loading;

    public AutoDownloadPage(AutoDownloadViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        LoadDownloaderFields();
    }

    private void LoadDownloaderFields()
    {
        _loading = true;
        var d = ViewModel.Downloader;
        ExeBox.Text      = d.ExecutablePath;
        OutDirBox.Text   = d.OutputDirectory;
        ArgsBox.Text     = d.ArgumentTemplate;
        FilenameBox.Text = d.FileNameTemplate;
        _loading = false;
    }

    private void ExeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) ViewModel.Downloader.ExecutablePath = ExeBox.Text;
    }

    private void OutDirBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) ViewModel.Downloader.OutputDirectory = OutDirBox.Text;
    }

    private void ArgsBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) ViewModel.Downloader.ArgumentTemplate = ArgsBox.Text;
    }

    private void FilenameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) ViewModel.Downloader.FileNameTemplate = FilenameBox.Text;
    }

    private void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
            Title  = "録音/録画ツールの実行ファイルを選択"
        };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.Downloader.ExecutablePath = dialog.FileName;
            ExeBox.Text = dialog.FileName;
        }
    }

    private void BrowseDir_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "保存先フォルダを選択"
        };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.Downloader.OutputDirectory = dialog.FolderName;
            OutDirBox.Text = dialog.FolderName;
        }
    }

    private void RuleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _loading = true;
        var r = ViewModel.SelectedRule;
        RuleTitleBox.Text        = r?.Title   ?? "";
        RuleWordBox.Text         = r?.Word    ?? "";
        RuleRegexCheck.IsChecked = r?.IsRegex ?? false;
        _loading = false;
    }

    private void RuleTitleBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedRule != null)
            ViewModel.SelectedRule.Title = RuleTitleBox.Text;
    }

    private void RuleWordBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedRule != null)
            ViewModel.SelectedRule.Word = RuleWordBox.Text;
    }

    private void RuleRegexCheck_Click(object sender, RoutedEventArgs e)
    {
        if (!_loading && ViewModel.SelectedRule != null)
            ViewModel.SelectedRule.IsRegex = RuleRegexCheck.IsChecked ?? false;
    }
}
