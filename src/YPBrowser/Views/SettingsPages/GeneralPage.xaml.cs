using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        var player = new PlayerSettings();
        if (ShowEditDialog(player, "プレイヤーを追加"))
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
            Name = player.Name,
            ExecutablePath = player.ExecutablePath,
            ArgumentTemplate = player.ArgumentTemplate,
        };

        if (!ShowEditDialog(draft, "プレイヤーを編集")) return;

        player.Name = draft.Name;
        player.ExecutablePath = draft.ExecutablePath;
        player.ArgumentTemplate = draft.ArgumentTemplate;
    }

    private void SetDefaultPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (GetPlayer(sender) is { } player) _owner.SetDefaultPlayer(player);
    }

    private void RemovePlayer_Click(object sender, RoutedEventArgs e)
    {
        if (GetPlayer(sender) is { } player) _owner.RemovePlayer(player);
    }

    private bool ShowEditDialog(PlayerSettings player, string title)
    {
        var dialog = new PlayerEditDialog(player, title) { Owner = Window.GetWindow(this) };
        return dialog.ShowDialog() == true;
    }

    /// <summary>メニュー項目は、開いた行の DataContext をそのまま引き継いでいる。</summary>
    private static PlayerSettings? GetPlayer(object sender) =>
        (sender as FrameworkElement)?.DataContext as PlayerSettings;
}
