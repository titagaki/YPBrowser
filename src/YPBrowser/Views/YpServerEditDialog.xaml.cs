using System.Windows;
using YPBrowser.Settings;

namespace YPBrowser.Views;

/// <summary>
/// YP サーバー 1 件の設定。
/// 設定画面は即時反映だが、ここは「追加をやめる」が要る場面なので OK / キャンセルを持つ。
/// OK を押すまで元の設定には書き戻さない。
/// </summary>
public partial class YpServerEditDialog : Window
{
    private readonly YpServerSettings _server;

    public YpServerEditDialog(YpServerSettings server, string title)
    {
        _server = server;
        InitializeComponent();

        Title = title;

        NameBox.Text = server.Name;
        UrlBox.Text = server.Url;
        HostBox.Text = server.Host;

        Loaded += (_, _) => NameBox.Focus();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        _server.Name = NameBox.Text.Trim();
        _server.Url = UrlBox.Text.Trim();
        _server.Host = HostBox.Text.Trim();

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
