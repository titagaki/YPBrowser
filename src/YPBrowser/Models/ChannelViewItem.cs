using CommunityToolkit.Mvvm.ComponentModel;

namespace YPBrowser.Models;

public enum ChannelViewKind
{
    /// <summary>ログを除く全チャンネル。</summary>
    All,
    /// <summary>前回ポーリングになかったチャンネル。</summary>
    New,
    /// <summary>組み込みタグ「お気に入り」を持つチャンネル。</summary>
    Favorite,
    /// <summary>特定のタグを持つチャンネル。</summary>
    Tag,
    /// <summary>一覧から消えたチャンネル。</summary>
    Log,
}

/// <summary>左のビュー欄の1項目。</summary>
public partial class ChannelViewItem : ObservableObject
{
    public ChannelViewKind Kind { get; init; }

    /// <summary><see cref="ChannelViewKind.Tag"/> のときだけ非 null。</summary>
    public TagDefinition? Tag { get; init; }

    public string Name => Tag?.Name ?? Kind switch
    {
        ChannelViewKind.All => "すべて",
        ChannelViewKind.New => "新着",
        ChannelViewKind.Favorite => "お気に入り",
        ChannelViewKind.Log => "ログ",
        _ => "",
    };

    [ObservableProperty] private int _count;

    public bool IsTagView => Kind == ChannelViewKind.Tag;

    /// <summary>ビュー欄の見出し。組み込みビューとタグを分けて並べる。</summary>
    public string GroupName => IsTagView ? "タグ" : "ビュー";

    /// <summary>そのタグ自体が「一覧から隠す」なら、このビューでは隠さず中身を見せる。</summary>
    public bool IncludesHidden => Tag?.IsHidden == true;

    public static ChannelViewItem ForTag(TagDefinition tag) =>
        new() { Kind = ChannelViewKind.Tag, Tag = tag };

    public static ChannelViewItem ForKind(ChannelViewKind kind) => new() { Kind = kind };
}
