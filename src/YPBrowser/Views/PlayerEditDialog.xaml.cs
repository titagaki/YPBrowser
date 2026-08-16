using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using YPBrowser.Models;
using YPBrowser.Settings;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views;

/// <summary>
/// コンテンツタイプ 1 つ分のプレイヤー設定。
/// 設定画面は即時反映だが、ここは「追加をやめる」が要る場面なので OK / キャンセルを持つ。
/// OK を押すまで元のプレイヤーには書き戻さない。
/// </summary>
public partial class PlayerEditDialog : Window
{
    private static readonly FontFamily MonospaceFont = new("Consolas, Cascadia Mono");

    private readonly PlayerSettings _player;

    /// <param name="selectableContentTypes">
    /// 選ばせるタイプ。他のプレイヤーが担当済みのものは呼び出し側で除いてある。
    /// </param>
    public PlayerEditDialog(PlayerSettings player, string title, IReadOnlyList<string> selectableContentTypes)
    {
        _player = player;
        InitializeComponent();

        Title = title;

        var options = selectableContentTypes
            .Select(type => new SettingOption<string>(type, PlayerContentTypes.Label(type)))
            .ToList();
        TypeBox.ItemsSource = options;
        TypeBox.SelectedItem = options.Find(o => o.Value == player.ContentType) ?? options.FirstOrDefault();

        ExeBox.Text = player.ExecutablePath;
        ArgsBox.Text = player.ArgumentTemplate;
        PlaceholderList.ItemsSource = PlayerPlaceholders.All;

        Loaded += (_, _) => ExeBox.Focus();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
            Title = "プレイヤーの実行ファイルを選択",
        };

        if (dialog.ShowDialog() != true) return;

        ExeBox.Text = dialog.FileName;

        // 書き方を知っているプレイヤーなら、引数がまだ初期値のうちに入れておく
        if (IsArgumentUntouched() && PlayerPresets.ForExecutable(dialog.FileName) is { } preset)
            ArgsBox.Text = preset.ArgumentTemplate;
    }

    /// <summary>引数が初期値のまま = 上書きしても入力を失わない。</summary>
    private bool IsArgumentUntouched() =>
        string.IsNullOrWhiteSpace(ArgsBox.Text) || ArgsBox.Text == PlayerSettings.DefaultArgumentTemplate;

    /// <summary>「設定例 ▾」は押した位置にメニューを開く。</summary>
    private void Presets_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        var menu = new ContextMenu
        {
            PlacementTarget = button,
            Placement = PlacementMode.Bottom,
        };

        foreach (var preset in PlayerPresets.All)
        {
            // 引数だけを出すと、どのプレイヤー向けの書き方か分からない
            var item = new MenuItem { Header = preset.Display, FontFamily = MonospaceFont };
            item.Click += (_, _) => ApplyPreset(preset);
            menu.Items.Add(item);
        }

        menu.IsOpen = true;
    }

    /// <summary>
    /// 引数を例で置き換える。実行ファイルが空なら、どのプレイヤー向けの例かが残るように
    /// ファイル名も入れる（パスまでは分からないので、置き場所が違うなら参照で選び直す）。
    /// </summary>
    private void ApplyPreset(PlayerPreset preset)
    {
        ArgsBox.Text = preset.ArgumentTemplate;
        if (string.IsNullOrWhiteSpace(ExeBox.Text)) ExeBox.Text = preset.ExecutableName;

        ArgsBox.Focus();
        ArgsBox.CaretIndex = ArgsBox.Text.Length;
    }

    /// <summary>置換子の行を押したら、引数の入力位置へ差し込む。</summary>
    private void InsertPlaceholder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not PlayerPlaceholder placeholder) return;

        var start = ArgsBox.SelectionStart;
        ArgsBox.SelectedText = placeholder.Token;  // 選択中の文字があれば置き換わる

        ArgsBox.Focus();
        ArgsBox.CaretIndex = start + placeholder.Token.Length;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        _player.ContentType = (TypeBox.SelectedItem as SettingOption<string>)?.Value
            ?? PlayerContentTypes.Fallback;
        _player.ExecutablePath = ExeBox.Text.Trim();
        _player.ArgumentTemplate = ArgsBox.Text;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
