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
            TargetFields = FavoriteSettingsMapper.ParseTargetFields(s.TargetFields),
            IsRegex      = s.IsRegex,
            Enabled      = s.Enabled,
        }).ToList();
}
