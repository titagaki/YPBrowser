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
        OutDirBox.Text   = d.OutputDirectory;
        FilenameBox.Text = d.FileNameTemplate;
        _loading = false;
    }

    private void OutDirBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) ViewModel.Downloader.OutputDirectory = OutDirBox.Text;
    }

    private void FilenameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) ViewModel.Downloader.FileNameTemplate = FilenameBox.Text;
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
