using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using YPBrowser.Models;
using YPBrowser.Settings;
using YPBrowser.ViewModels;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class GeneralPage : UserControl
{
    private readonly SettingsViewModel _owner;

    public GeneralPageViewModel ViewModel { get; }

    public GeneralPage(SettingsViewModel owner)
    {
        _owner = owner;
        ViewModel = new GeneralPageViewModel(owner);
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void AddPlayer_Click(object sender, RoutedEventArgs e)
    {
        var selectable = SelectableContentTypes(null);
        if (selectable.Count == 0)
        {
            // 1 タイプにつき 1 件なので、全部埋まったら追加できるものが無い
            MessageBox.Show(Window.GetWindow(this),
                "すべてのタイプにプレイヤーを設定済みです。", "プレイヤーを追加",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var player = new PlayerSettings { ContentType = selectable[0] };
        if (ShowEditDialog(player, "プレイヤーを追加", selectable))
            _owner.AddPlayer(player);
    }

    /// <summary>「⋯」は押した位置にメニューを開く。行そのものは押しても何も起きない。</summary>
    private void PlayerMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is null) return;

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    private void EditPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (GetPlayer(sender) is not { } player) return;

        // 編集を中断できるように、書き戻す前の値を控えておく
        var draft = new PlayerSettings
        {
            ContentType = player.ContentType,
            ExecutablePath = player.ExecutablePath,
            ArgumentTemplate = player.ArgumentTemplate,
        };

        if (!ShowEditDialog(draft, "プレイヤーを編集", SelectableContentTypes(player))) return;

        var typeChanged = draft.ContentType != player.ContentType;
        player.ContentType = draft.ContentType;
        player.ExecutablePath = draft.ExecutablePath;
        player.ArgumentTemplate = draft.ArgumentTemplate;

        if (typeChanged) _owner.ReorderPlayer(player);
    }

    private void RemovePlayer_Click(object sender, RoutedEventArgs e)
    {
        if (GetPlayer(sender) is { } player) _owner.RemovePlayer(player);
    }

    /// <summary>まだ他のプレイヤーが担当していないタイプ。編集中の 1 件は自分の分を残す。</summary>
    private List<string> SelectableContentTypes(PlayerSettings? editing)
    {
        var used = _owner.UsedContentTypes(editing);
        return [.. PlayerContentTypes.Selectable
            .Where(type => !used.Contains(type, StringComparer.OrdinalIgnoreCase))];
    }

    private bool ShowEditDialog(PlayerSettings player, string title, IReadOnlyList<string> selectableContentTypes)
    {
        var dialog = new PlayerEditDialog(player, title, selectableContentTypes)
        {
            Owner = Window.GetWindow(this),
        };
        return dialog.ShowDialog() == true;
    }

    /// <summary>メニュー項目は、開いた行の DataContext をそのまま引き継いでいる。</summary>
    private static PlayerSettings? GetPlayer(object sender) =>
        (sender as FrameworkElement)?.DataContext as PlayerSettings;
}
