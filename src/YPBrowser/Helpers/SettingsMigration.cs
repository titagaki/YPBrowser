using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Helpers;

/// <summary>
/// 旧「お気に入り」形式（条件 + 色 + 通知 + 除外フラグを1件が全部抱える）から
/// タグ方式（ルールはタグを付けるだけ / 見た目はタグの属性）への移行。
/// </summary>
public static class SettingsMigration
{
    /// <summary>
    /// 組み込みタグの存在を保証し、旧 <see cref="AppSettings.Favorites"/> をタグ + ルールへ変換する。
    /// 変換したら旧リストは空にする（保存時に設定ファイルから消える）。
    /// 何か変更したら true。
    /// </summary>
    public static bool Migrate(AppSettings settings)
    {
        var changed = EnsureBuiltInTags(settings);

        // すでにタグ方式のルールがあるなら、旧データがあっても二重に取り込まない。
        if (settings.Favorites.Count > 0 && settings.Rules.Count == 0)
        {
            MigrateFavorites(settings);
            changed = true;
        }

        if (settings.Favorites.Count > 0)
        {
            settings.Favorites = [];
            changed = true;
        }

        return changed;
    }

    /// <summary>お気に入り / NG は削除できない組み込みタグ。無ければ先頭側に足す。</summary>
    private static bool EnsureBuiltInTags(AppSettings settings)
    {
        var changed = false;

        if (!settings.Tags.Any(t => t.Id == TagDefinition.NgId))
        {
            settings.Tags.Insert(0, TagDefinition.CreateNg());
            changed = true;
        }
        if (!settings.Tags.Any(t => t.Id == TagDefinition.FavoriteId))
        {
            settings.Tags.Insert(0, TagDefinition.CreateFavorite());
            changed = true;
        }

        // 組み込みタグは、UI で消せないことが分かるようにフラグを立て直しておく
        foreach (var tag in settings.Tags)
        {
            var builtIn = tag.Id is TagDefinition.FavoriteId or TagDefinition.NgId;
            if (tag.BuiltIn != builtIn)
            {
                tag.BuiltIn = builtIn;
                changed = true;
            }
        }

        return changed;
    }

    private static void MigrateFavorites(AppSettings settings)
    {
        // 旧形式では色は1件ごとの属性だったので、そのままだとお気に入りの数だけタグができる。
        // 見た目が同じものは1つのタグにまとめる。
        var appearanceTags = new Dictionary<(string?, string?), TagDefinition>();
        var created = new List<TagDefinition>();
        var order = 0;

        foreach (var fav in settings.Favorites)
        {
            if (string.IsNullOrEmpty(fav.Word)) continue;  // 旧実装でも評価されていない

            var rule = new Rule
            {
                Name = string.IsNullOrWhiteSpace(fav.Title) ? fav.Word : fav.Title,
                Enabled = fav.Enabled,
                Order = order++,
                // 旧実装は対象フィールドを連結してから1回照合していた = フィールドの OR
                Combinator = RuleCombinator.Or,
                Conditions = [.. BuildConditions(fav)],
            };

            if (fav.IsNG)
            {
                rule.TagIds.Add(TagDefinition.NgId);
            }
            else
            {
                // 「お気に入りビューに出る」という性質は組み込みタグが引き継ぐ。
                // 色だけを持つタグを別に作り、タグ一覧で組み込みタグより前に置いて色を優先させる。
                var appearance = GetOrCreateAppearanceTag(appearanceTags, created, fav);
                if (appearance is not null) rule.TagIds.Add(appearance.Id);
                rule.TagIds.Add(TagDefinition.FavoriteId);
            }

            settings.Rules.Add(rule);
        }

        // 組み込みタグより前に置くことで、行の色は移行したタグが勝つ
        settings.Tags.InsertRange(0, created);
    }

    private static List<RuleCondition> BuildConditions(FavoriteSettings fav)
    {
        var matchType = fav.IsRegex ? ConditionMatchType.Regex : ConditionMatchType.Contains;
        var fields = new List<ConditionField>();

        foreach (var name in fav.TargetFields)
        {
            var field = name switch
            {
                "ChannelName" => (ConditionField?)ConditionField.ChannelName,
                // ジャンル / 詳細 / コメントは新形式では 1 つにまとまっている
                "Genre" or "Description" or "Comment" => ConditionField.Description,
                "ContactUrl" => ConditionField.ContactUrl,
                // 旧「アーティスト」= 新「Playing」
                "TrackArtist" => ConditionField.TrackArtist,
                // YP名 / コーデック / 曲名 は条件のフィールドから外したので移行先が無い
                _ => null,
            };
            if (field is not null && !fields.Contains(field.Value)) fields.Add(field.Value);
        }

        // 移行先の無いフィールドしか指定されていなかった場合の受け皿
        if (fields.Count == 0) fields.Add(ConditionField.ChannelName);

        return [.. fields.Select(f => new RuleCondition
        {
            Field = f,
            MatchType = matchType,
            Pattern = fav.Word,
        })];
    }

    private static TagDefinition? GetOrCreateAppearanceTag(
        Dictionary<(string?, string?), TagDefinition> cache,
        List<TagDefinition> created,
        FavoriteSettings fav)
    {
        var back = Normalize(fav.BackColor);
        var fore = Normalize(fav.TextColor);
        if (back is null && fore is null) return null;  // 色指定なし = 組み込みタグだけで足りる

        var key = (back, fore);
        if (cache.TryGetValue(key, out var existing)) return existing;

        var tag = new TagDefinition
        {
            Name = string.IsNullOrWhiteSpace(fav.Title) ? fav.Word : fav.Title,
            BackColor = back,
            ForeColor = fore,
            DefaultAction = TagDefaultAction.Normal,
            // 通知は組み込みの「お気に入り」タグが担当する。ここで有効にすると二重に鳴る。
            Notify = false,
        };
        cache[key] = tag;
        created.Add(tag);
        return tag;
    }

    private static string? Normalize(string? hex) =>
        ColorHelper.IsValid(hex) ? hex : null;
}
