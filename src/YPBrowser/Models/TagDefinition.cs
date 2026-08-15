using System.Text.Json.Serialization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Helpers;

namespace YPBrowser.Models;

/// <summary>タグを持つチャンネルを一覧でどう扱うかの既定。</summary>
public enum TagDefaultAction
{
    /// <summary>通常表示。</summary>
    Normal,
    /// <summary>強調して上位にソートする。</summary>
    Highlight,
    /// <summary>一覧から隠す（そのタグのビューでは見える）。</summary>
    Hidden,
}

/// <summary>
/// ルールがチャンネルに付与するタグ。色・通知・既定の扱いはすべてタグ側の属性で、
/// ルールは「どのタグを付けるか」しか持たない。
/// </summary>
public partial class TagDefinition : ObservableObject
{
    /// <summary>組み込みタグ「お気に入り」の固定 ID。星ボタンが付与する。</summary>
    public const string FavoriteId = "builtin-favorite";
    /// <summary>組み込みタグ「NG」の固定 ID。</summary>
    public const string NgId = "builtin-ng";

    /// <summary>不変の内部 ID。リネームしてもルール側の参照が壊れないようにする。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [ObservableProperty] private string _name = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ForeColorValue))]
    [NotifyPropertyChangedFor(nameof(HasColor))]
    private string? _foreColor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackColorValue))]
    [NotifyPropertyChangedFor(nameof(HasColor))]
    private string? _backColor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHidden))]
    [NotifyPropertyChangedFor(nameof(IsHighlight))]
    private TagDefaultAction _defaultAction = TagDefaultAction.Normal;

    [ObservableProperty] private bool _notify;

    /// <summary>通知音のファイルパス。null なら既定音。</summary>
    [ObservableProperty] private string? _soundPath;

    /// <summary>お気に入り / NG。削除できないが色と通知は変更できる。</summary>
    public bool BuiltIn { get; set; }

    [JsonIgnore] public Color? ForeColorValue => ColorHelper.Parse(ForeColor);
    [JsonIgnore] public Color? BackColorValue => ColorHelper.Parse(BackColor);

    /// <summary>文字色・背景色のどちらかが設定されている。行の色を決めるときの候補になる。</summary>
    [JsonIgnore] public bool HasColor => ForeColorValue is not null || BackColorValue is not null;

    [JsonIgnore] public bool IsHidden => DefaultAction == TagDefaultAction.Hidden;
    [JsonIgnore] public bool IsHighlight => DefaultAction == TagDefaultAction.Highlight;

    /// <summary>編集用の複製。ID を引き継ぐのでルール側の参照は保たれる。</summary>
    public TagDefinition Clone() => new()
    {
        Id = Id,
        Name = Name,
        ForeColor = ForeColor,
        BackColor = BackColor,
        DefaultAction = DefaultAction,
        Notify = Notify,
        SoundPath = SoundPath,
        BuiltIn = BuiltIn,
    };

    public static TagDefinition CreateFavorite() => new()
    {
        Id = FavoriteId,
        Name = "お気に入り",
        BackColor = "#FFF4CE",
        ForeColor = "#4A3A00",
        DefaultAction = TagDefaultAction.Highlight,
        Notify = true,
        BuiltIn = true,
    };

    public static TagDefinition CreateNg() => new()
    {
        Id = NgId,
        Name = "NG",
        DefaultAction = TagDefaultAction.Hidden,
        Notify = false,
        BuiltIn = true,
    };
}
