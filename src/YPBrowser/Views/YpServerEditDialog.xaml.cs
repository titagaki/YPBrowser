using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
        BitrateMinBox.Text = server.BitrateMin.ToString();
        BitrateMaxBox.Text = server.BitrateMax.ToString();
        TypeFilterBox.Text = server.TypeFilter;

        Loaded += (_, _) => NameBox.Focus();
    }

    /// <summary>
    /// 打っている最中に知らせる。取得側は不正な正規表現を黙って無視する（フィルタが
    /// 効かない方が復旧しやすいため）ので、気付ける場所がここしか無い。
    /// </summary>
    private void TypeFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var error = ValidateTypeFilter(TypeFilterBox.Text);

        TypeFilterError.Text = error ?? "";
        TypeFilterError.Visibility = error == null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>正規表現として解釈できなければ理由を返す。問題なければ <c>null</c>。</summary>
    private static string? ValidateTypeFilter(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return null;

        try
        {
            _ = new Regex(pattern, RegexOptions.IgnoreCase);
            return null;
        }
        catch (ArgumentException ex)
        {
            return $"正規表現として読めません: {ex.Message}";
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (ValidateTypeFilter(TypeFilterBox.Text) is { } error)
        {
            // 保存させると「絞り込みが効かない YP」が黙って出来上がる
            MessageBox.Show(this, error, "タイプの指定を直してください",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TypeFilterBox.Focus();
            return;
        }

        _server.Name = NameBox.Text.Trim();
        _server.Url = UrlBox.Text.Trim();
        _server.Host = HostBox.Text.Trim();
        _server.BitrateMin = ParseBitrate(BitrateMinBox.Text, 0);
        _server.BitrateMax = ParseBitrate(BitrateMaxBox.Text, -1);

        // 空欄は「すべて通す」の意味に寄せる。空文字のまま保存すると
        // 取得側で「フィルタ無し」と同じ扱いになり、設定を読んでも意図が分からない
        var typeFilter = TypeFilterBox.Text.Trim();
        _server.TypeFilter = string.IsNullOrEmpty(typeFilter) ? ".*" : typeFilter;

        DialogResult = true;
    }

    /// <summary>数字として読めない入力は「制限なし」に倒す。</summary>
    private static int ParseBitrate(string text, int fallback) =>
        int.TryParse(text.Trim(), out var value) ? value : fallback;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
