using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using YPBrowser.Settings;
using YPBrowser.ViewModels;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class YpServersPage : UserControl
{
    private readonly SettingsViewModel _owner;

    public YpServersPage(SettingsViewModel viewModel)
    {
        _owner = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private void AddServer_Click(object sender, RoutedEventArgs e)
    {
        var server = new YpServerSettings { Name = "新しいYP", Url = "http://" };

        if (ShowEditDialog(server, "YP サーバーを追加"))
            _owner.AddYpServer(server);
    }

    /// <summary>「⋯」は押した位置にメニューを開く。行そのものは押しても何も起きない。</summary>
    private void ServerMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is null) return;

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    private void EditServer_Click(object sender, RoutedEventArgs e)
    {
        if (GetRow(sender) is not { } row) return;

        // 編集を中断できるように、書き戻す前の値を控えておく
        var draft = new YpServerSettings
        {
            Name = row.Settings.Name,
            Url = row.Settings.Url,
            Host = row.Settings.Host,
            Enabled = row.Settings.Enabled,
        };

        if (!ShowEditDialog(draft, "YP サーバーを編集")) return;

        row.Settings.Name = draft.Name;
        row.Settings.Url = draft.Url;
        row.Settings.Host = draft.Host;

        // URL やホストが変わっていれば、対応する取得状況も引き直される
        row.Refresh();
    }

    private void RemoveServer_Click(object sender, RoutedEventArgs e)
    {
        if (GetRow(sender) is not { } row) return;

        var answer = MessageBox.Show(Window.GetWindow(this),
            $"「{row.Name}」を削除しますか？", "YP サーバーを削除",
            MessageBoxButton.OKCancel, MessageBoxImage.Warning);

        if (answer == MessageBoxResult.OK) _owner.RemoveYpServer(row);
    }

    private void MoveServerUp_Click(object sender, RoutedEventArgs e)
    {
        if (GetRow(sender) is { } row) _owner.MoveYpServer(row, -1);
    }

    private void MoveServerDown_Click(object sender, RoutedEventArgs e)
    {
        if (GetRow(sender) is { } row) _owner.MoveYpServer(row, +1);
    }

    private bool ShowEditDialog(YpServerSettings server, string title)
    {
        var dialog = new YpServerEditDialog(server, title)
        {
            Owner = Window.GetWindow(this),
        };
        return dialog.ShowDialog() == true;
    }

    /// <summary>メニュー項目は、開いた行の DataContext をそのまま引き継いでいる。</summary>
    private static YpServerRow? GetRow(object sender) =>
        (sender as FrameworkElement)?.DataContext as YpServerRow;
}
