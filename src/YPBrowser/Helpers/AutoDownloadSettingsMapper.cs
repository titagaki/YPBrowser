using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Helpers;

public static class AutoDownloadSettingsMapper
{
    public static List<AutoDownloadRuleItem> ToRuleItems(IEnumerable<AutoDownloadRuleSettings> settings) =>
        settings.Select(s => new AutoDownloadRuleItem
        {
            Title        = s.Title,
            Word         = s.Word,
            TargetFields = ParseTargetFields(s.TargetFields),
            IsRegex      = s.IsRegex,
            Enabled      = s.Enabled,
        }).ToList();

    /// <summary>
    /// 設定ファイル上の <c>["ChannelName", "Genre"]</c> 形式をフラグへ変換する。
    /// 解釈できない文字列は無視し、結果が空なら <c>ChannelName</c> にフォールバックする。
    /// </summary>
    public static MatchTargetFields ParseTargetFields(List<string> fields)
    {
        var result = MatchTargetFields.None;
        foreach (var f in fields)
        {
            if (Enum.TryParse<MatchTargetFields>(f, out var flag))
                result |= flag;
        }
        return result == MatchTargetFields.None ? MatchTargetFields.ChannelName : result;
    }
}
