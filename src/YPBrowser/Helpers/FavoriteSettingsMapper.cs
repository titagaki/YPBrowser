using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Helpers;

public static class FavoriteSettingsMapper
{
    public static List<FavoriteItem> ToFavoriteItems(IEnumerable<FavoriteSettings> settings) =>
        settings.Select(f => new FavoriteItem
        {
            Title = f.Title,
            Word = f.Word,
            TargetFields = ParseTargetFields(f.TargetFields),
            IsRegex = f.IsRegex,
            IsNG = f.IsNG,
            NotifyEnabled = f.NotifyEnabled,
            Enabled = f.Enabled,
            BackColor = f.BackColor,
            TextColor = f.TextColor,
            SoundFile = f.SoundFile,
        }).ToList();

    public static FavoriteTargetFields ParseTargetFields(List<string> fields)
    {
        var result = FavoriteTargetFields.None;
        foreach (var f in fields)
        {
            if (Enum.TryParse<FavoriteTargetFields>(f, out var flag))
                result |= flag;
        }
        return result == FavoriteTargetFields.None ? FavoriteTargetFields.ChannelName : result;
    }
}
