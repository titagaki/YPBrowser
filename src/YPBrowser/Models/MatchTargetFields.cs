namespace YPBrowser.Models;

/// <summary>
/// 自動ダウンロードルールの対象フィールド。選択されたフィールドを連結してから1回照合する。
/// （お気に入り／NG はタグ方式の <see cref="ConditionField"/> に置き換わっている）
/// </summary>
[Flags]
public enum MatchTargetFields
{
    None = 0,
    ChannelName = 1 << 0,
    Genre = 1 << 1,
    Description = 1 << 2,
    Comment = 1 << 3,
    ContactUrl = 1 << 4,
    YpName = 1 << 5,
    ChannelType = 1 << 6,
    TrackTitle = 1 << 7,
    TrackArtist = 1 << 8,
    All = ChannelName | Genre | Description | Comment | ContactUrl | YpName | ChannelType | TrackTitle | TrackArtist,
}
