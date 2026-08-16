using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Helpers;

/// <summary>
/// 旧形式からの移行。
/// ・旧「お気に入り」形式（条件 + 色 + 通知 + 除外フラグを1件が全部抱える）→ タグ方式
/// ・旧「動作」ページの設定 → 「全般」ページの設定
/// </summary>
public static class SettingsMigration
{
    /// <summary>
    /// 設定画面が選ばせる自動更新間隔（秒）。<c>0</c> は「更新しない」。
    /// 並び順がそのまま UI の並び順になる。
    /// </summary>
    public static readonly int[] RefreshIntervalPresets = [60, 30, 120, 0];

    /// <summary>
    /// 組み込みタグの存在を保証し、旧 <see cref="AppSettings.Favorites"/> をタグ + ルールへ変換し、
    /// 旧「動作」設定を新しい形へ移す。変換したら旧データは消す（保存時に設定ファイルから消える）。
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

        if (MigrateBehavior(settings)) changed = true;
        if (MigratePlayers(settings)) changed = true;

        return changed;
    }

    /// <summary>
    /// 旧プレイヤー形式（名前 +「既定」フラグ。タイプの区別なし）を、
    /// コンテンツタイプごとの形へ移す。
    ///
    /// 旧形式にはタイプの情報が無く、機械的に振り分けることはできない。
    /// 既定のプレイヤー（無ければ先頭）だけを「その他」として引き継ぎ、残りは捨てる。
    /// 残しても、どのチャンネルからも呼ばれない行が並ぶだけになるため。
    /// </summary>
    private static bool MigratePlayers(AppSettings settings)
    {
        var legacy = settings.Players.Where(IsLegacyPlayer).ToList();
        if (legacy.Count == 0) return false;

        var players = settings.Players.Where(p => !IsLegacyPlayer(p)).ToList();

        // 「その他」がすでに埋まっているなら、旧データを充てる先が無い
        if (!players.Any(p => string.IsNullOrEmpty(p.ContentType)))
        {
            var kept = legacy.FirstOrDefault(p => p.IsDefault == true) ?? legacy[0];
            kept.ContentType = PlayerContentTypes.Fallback;
            kept.ArgumentTemplate = RewriteUrlToken(kept.ArgumentTemplate);
            players.Add(kept);
        }

        foreach (var player in players)
        {
            player.Name = null;
            player.IsDefault = null;
        }

        settings.Players = players;
        return true;
    }

    /// <summary>旧形式の項目が設定ファイルに書かれていた = 移行対象。</summary>
    private static bool IsLegacyPlayer(PlayerSettings player) =>
        player.Name is not null || player.IsDefault is not null;

    /// <summary>置換子 <c>{url}</c> は <c>{stream}</c> へ改名した。</summary>
    private static string RewriteUrlToken(string template) =>
        template.Replace("{url}", "{stream}", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 旧「動作」ページの設定を移す。
    /// 旧項目は非 null なら「設定ファイルに書かれていた」= 移行対象。移した後は null にして、
    /// 次回以降の保存でファイルから消えるようにする。
    /// </summary>
    private static bool MigrateBehavior(AppSettings settings)
    {
        var behavior = settings.Behavior;
        var changed = false;

        if (behavior.StartMinimized is { } startMinimized)
        {
            if (startMinimized) behavior.StartupState = StartupWindowState.Minimized;
            behavior.StartMinimized = null;
            changed = true;
        }

        if (behavior.MinimizeToTray is { } minimizeToTray)
        {
            behavior.MinimizeButtonAction = minimizeToTray
                ? MinimizeButtonAction.MinimizeToTray
                : MinimizeButtonAction.KeepInTaskbar;
            behavior.MinimizeToTray = null;
            changed = true;
        }

        if (behavior.NotifyOnFavorite is { } notifyOnFavorite)
        {
            settings.Notifications.NotifyOnFavorite = notifyOnFavorite;
            behavior.NotifyOnFavorite = null;
            changed = true;
        }

        var rounded = RoundRefreshInterval(behavior.RefreshIntervalSeconds);
        if (rounded != behavior.RefreshIntervalSeconds)
        {
            behavior.RefreshIntervalSeconds = rounded;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 自由入力だった頃の更新間隔をプリセットへ丸める。
    /// 正の値は 0（更新しない）へは丸めない。速く更新したかった設定を
    /// 「更新しない」に化けさせると、ユーザーには故障に見えるため。
    /// </summary>
    public static int RoundRefreshInterval(int seconds)
    {
        if (seconds <= 0) return 0;

        return RefreshIntervalPresets
            .Where(preset => preset > 0)
            .OrderBy(preset => Math.Abs(preset - seconds))
            // 等距離のときは短い方（更新が止まったように見えない側）を選ぶ
            .ThenBy(preset => preset)
            .First();
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
