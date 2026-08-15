using System.Windows;
using YPBrowser.Helpers;
using YPBrowser.Models;
using YPBrowser.ViewModels;

namespace YPBrowser.Views;

public partial class TagsDialog : Window
{
    public TagsViewModel ViewModel { get; }

    public TagsDialog(TagsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private void PickForeColor_Click(object sender, RoutedEventArgs e)
    {
        if (TagOf(sender) is not { } tag) return;
        if (PickColor(tag.ForeColor) is { } hex) tag.ForeColor = hex;
    }

    private void PickBackColor_Click(object sender, RoutedEventArgs e)
    {
        if (TagOf(sender) is not { } tag) return;
        if (PickColor(tag.BackColor) is { } hex) tag.BackColor = hex;
    }

    /// <summary>16進テキストだけにせず、パレット / RGB スライダーで選ばせる。</summary>
    private string? PickColor(string? current)
    {
        var dialog = new ColorPickerDialog(current) { Owner = this };
        return dialog.ShowDialog() == true ? dialog.SelectedHex : null;
    }

    private void ClearColors_Click(object sender, RoutedEventArgs e)
    {
        if (TagOf(sender) is not { } tag) return;
        tag.ForeColor = null;
        tag.BackColor = null;
    }

    private void PickSound_Click(object sender, RoutedEventArgs e)
    {
        if (TagOf(sender) is not { } tag) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "通知音を選ぶ",
            Filter = "WAV ファイル (*.wav)|*.wav|すべてのファイル (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) == true)
            tag.SoundPath = dialog.FileName;
    }

    private static TagDefinition? TagOf(object sender) =>
        (sender as FrameworkElement)?.Tag as TagDefinition;

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveAsync();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
