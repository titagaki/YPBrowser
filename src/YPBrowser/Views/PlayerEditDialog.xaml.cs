using Microsoft.Win32;
using System.Windows;
using YPBrowser.Settings;

namespace YPBrowser.Views;

/// <summary>
/// 外部プレイヤー 1 件の編集。
/// 設定画面は即時反映だが、ここは「追加をやめる」が要る場面なので OK / キャンセルを持つ。
/// OK を押すまで元のプレイヤーには書き戻さない。
/// </summary>
public partial class PlayerEditDialog : Window
{
    private readonly PlayerSettings _player;

    public PlayerEditDialog(PlayerSettings player, string title)
    {
        _player = player;
        InitializeComponent();

        Title = title;
        NameBox.Text = player.Name;
        ExeBox.Text = player.ExecutablePath;
        ArgsBox.Text = player.ArgumentTemplate;

        Loaded += (_, _) => NameBox.Focus();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
            Title = "プレイヤーの実行ファイルを選択",
        };

        if (dialog.ShowDialog() == true)
        {
            ExeBox.Text = dialog.FileName;
            // 名前が空のままなら、ファイル名を初期値として入れておく
            if (string.IsNullOrWhiteSpace(NameBox.Text))
                NameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        _player.Name = NameBox.Text.Trim();
        _player.ExecutablePath = ExeBox.Text.Trim();
        _player.ArgumentTemplate = ArgsBox.Text;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
