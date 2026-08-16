using System.Windows;
using System.Windows.Controls;

namespace YPBrowser.Views.Controls;

/// <summary>カード行の使われ方。枠と余白の付き方だけが変わる。</summary>
public enum SettingCardVariant
{
    /// <summary>単独の設定行。枠と背景を自分で持つ。</summary>
    Card,

    /// <summary>子行を束ねる枠の見出し。枠は外側の Border が描くので自分では持たない。</summary>
    GroupHeader,

    /// <summary>見出しの下に積む子行。アイコン欄の幅だけ字下げして、上に区切り線を引く。</summary>
    SubRow,
}

/// <summary>
/// 設定画面の「1 設定 = 1 カード行」。
/// 左からアイコン / 見出し + 説明 / 右のコントロールの順に並ぶ。右のコントロールは
/// <see cref="ContentControl.Content"/> に入れる。
///
/// ページごとに手書きすると間隔と配置がページ間でずれるので、全ページこれを使う。
/// 見た目は Themes/Settings.xaml の暗黙スタイルが決める。
/// </summary>
public class SettingCard : ContentControl
{
    /// <summary>左端のアイコン。Segoe Fluent Icons のグリフを 1 文字入れる。</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(SettingCard),
            new PropertyMetadata(""));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingCard),
            new PropertyMetadata(""));

    /// <summary>補足。書くことが無いカードには付けない（空なら行ごと消える）。</summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingCard),
            new PropertyMetadata(""));

    /// <summary>
    /// 見出しの手前に付く固定幅のラベル（コンテンツタイプなど）。空なら出ない。
    /// 幅が揃うので、子行を縦に並べたときに種別だけを拾い読みできる。
    /// </summary>
    public static readonly DependencyProperty LeadingLabelProperty =
        DependencyProperty.Register(nameof(LeadingLabel), typeof(string), typeof(SettingCard),
            new PropertyMetadata(""));

    /// <summary>見出しの右に付く小さなラベル（「既定」など）。空なら出ない。</summary>
    public static readonly DependencyProperty BadgeProperty =
        DependencyProperty.Register(nameof(Badge), typeof(string), typeof(SettingCard),
            new PropertyMetadata(""));

    /// <summary>説明を等幅で出す。パスのように文字幅が揃っていた方が読める内容用。</summary>
    public static readonly DependencyProperty IsDescriptionMonospaceProperty =
        DependencyProperty.Register(nameof(IsDescriptionMonospace), typeof(bool), typeof(SettingCard),
            new PropertyMetadata(false));

    /// <summary>
    /// 説明の下に出す、実行時に変わる 1 行（最終取得時刻・件数・エラーなど）。空なら出ない。
    /// 設定値である <see cref="Description"/> と分けているのは、
    /// 「自分が設定した内容」と「いま起きていること」が同じ見た目だと読み分けられないため。
    /// </summary>
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(string), typeof(SettingCard),
            new PropertyMetadata(""));

    /// <summary>状態が異常であることを示す。文字色が変わる。</summary>
    public static readonly DependencyProperty IsStatusErrorProperty =
        DependencyProperty.Register(nameof(IsStatusError), typeof(bool), typeof(SettingCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(SettingCardVariant), typeof(SettingCard),
            new PropertyMetadata(SettingCardVariant.Card));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string LeadingLabel
    {
        get => (string)GetValue(LeadingLabelProperty);
        set => SetValue(LeadingLabelProperty, value);
    }

    public string Badge
    {
        get => (string)GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    public bool IsDescriptionMonospace
    {
        get => (bool)GetValue(IsDescriptionMonospaceProperty);
        set => SetValue(IsDescriptionMonospaceProperty, value);
    }

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public bool IsStatusError
    {
        get => (bool)GetValue(IsStatusErrorProperty);
        set => SetValue(IsStatusErrorProperty, value);
    }

    public SettingCardVariant Variant
    {
        get => (SettingCardVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
}
