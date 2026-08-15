using YPBrowser.Helpers;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Tests;

public class SettingsMigrationTests
{
    private static FavoriteSettings Fav(
        string title, string word,
        bool isNG = false, bool isRegex = false,
        string back = "#FFFF99", string text = "#000000",
        List<string>? fields = null) =>
        new()
        {
            Title = title,
            Word = word,
            IsNG = isNG,
            IsRegex = isRegex,
            BackColor = back,
            TextColor = text,
            TargetFields = fields ?? ["ChannelName"],
        };

    [Fact]
    public void EmptySettings_GetBothBuiltInTags()
    {
        var settings = new AppSettings();
        SettingsMigration.Migrate(settings);

        Assert.Contains(settings.Tags, t => t.Id == TagDefinition.FavoriteId && t.BuiltIn);
        Assert.Contains(settings.Tags, t => t.Id == TagDefinition.NgId && t.BuiltIn);
        Assert.Empty(settings.Rules);
    }

    [Fact]
    public void BuiltInTags_AreNotDuplicatedOnRepeatedMigration()
    {
        var settings = new AppSettings();
        SettingsMigration.Migrate(settings);
        SettingsMigration.Migrate(settings);

        Assert.Single(settings.Tags, t => t.Id == TagDefinition.FavoriteId);
        Assert.Single(settings.Tags, t => t.Id == TagDefinition.NgId);
    }

    [Fact]
    public void Migrate_IsIdempotentAndReportsNoChangeOnSecondRun()
    {
        var settings = new AppSettings { Favorites = [Fav("絵", "お絵かき")] };

        Assert.True(SettingsMigration.Migrate(settings));
        var ruleCount = settings.Rules.Count;
        var tagCount = settings.Tags.Count;

        Assert.False(SettingsMigration.Migrate(settings));
        Assert.Equal(ruleCount, settings.Rules.Count);
        Assert.Equal(tagCount, settings.Tags.Count);
    }

    [Fact]
    public void LegacyFavorite_BecomesRuleThatTagsFavorite()
    {
        var settings = new AppSettings { Favorites = [Fav("絵", "お絵かき")] };
        SettingsMigration.Migrate(settings);

        var rule = Assert.Single(settings.Rules);
        Assert.Equal("絵", rule.Name);
        Assert.Contains(TagDefinition.FavoriteId, rule.TagIds);
        Assert.Equal("お絵かき", rule.Conditions[0].Pattern);
        Assert.Equal(ConditionField.ChannelName, rule.Conditions[0].Field);
    }

    [Fact]
    public void LegacyNgFavorite_BecomesRuleThatTagsNg()
    {
        var settings = new AppSettings { Favorites = [Fav("アニメ除外", "アニメ", isNG: true)] };
        SettingsMigration.Migrate(settings);

        var rule = Assert.Single(settings.Rules);
        Assert.Equal([TagDefinition.NgId], rule.TagIds);
        Assert.DoesNotContain(TagDefinition.FavoriteId, rule.TagIds);
    }

    [Fact]
    public void LegacyRegexFlag_BecomesRegexMatchType()
    {
        var settings = new AppSettings { Favorites = [Fav("正規", @"\d+", isRegex: true)] };
        SettingsMigration.Migrate(settings);

        Assert.Equal(ConditionMatchType.Regex, settings.Rules[0].Conditions[0].MatchType);
    }

    [Fact]
    public void LegacyPlainFlag_BecomesContainsMatchType()
    {
        var settings = new AppSettings { Favorites = [Fav("部分", "ゲーム")] };
        SettingsMigration.Migrate(settings);

        Assert.Equal(ConditionMatchType.Contains, settings.Rules[0].Conditions[0].MatchType);
    }

    /// <summary>旧実装は対象フィールドを連結してから1回照合していた = フィールドの OR。</summary>
    [Fact]
    public void MultipleTargetFields_BecomeOrConditions()
    {
        var settings = new AppSettings
        {
            Favorites = [Fav("複数", "音楽", fields: ["ChannelName", "ContactUrl"])],
        };
        SettingsMigration.Migrate(settings);

        var rule = settings.Rules[0];
        Assert.Equal(RuleCombinator.Or, rule.Combinator);
        Assert.Equal(2, rule.Conditions.Count);
        Assert.All(rule.Conditions, c => Assert.Equal("音楽", c.Pattern));
    }

    /// <summary>ジャンル / 詳細 / コメントは新形式では 1 つの「説明文」に統合されている。</summary>
    [Fact]
    public void GenreDescriptionComment_CollapseIntoASingleDescriptionCondition()
    {
        var settings = new AppSettings
        {
            Favorites = [Fav("説明", "BGM", fields: ["Genre", "Description", "Comment"])],
        };
        SettingsMigration.Migrate(settings);

        var condition = Assert.Single(settings.Rules[0].Conditions);
        Assert.Equal(ConditionField.Description, condition.Field);
    }

    [Fact]
    public void UnknownTargetField_FallsBackToChannelName()
    {
        var settings = new AppSettings { Favorites = [Fav("謎", "x", fields: ["Nonsense"])] };
        SettingsMigration.Migrate(settings);

        var condition = Assert.Single(settings.Rules[0].Conditions);
        Assert.Equal(ConditionField.ChannelName, condition.Field);
    }

    /// <summary>YP名 / コーデック / 曲名 は条件のフィールドから外したので移行先が無い。</summary>
    [Theory]
    [InlineData("YpName")]
    [InlineData("ChannelType")]
    [InlineData("TrackTitle")]
    public void DroppedTargetField_AloneFallsBackToChannelName(string field)
    {
        var settings = new AppSettings { Favorites = [Fav("旧", "x", fields: [field])] };
        SettingsMigration.Migrate(settings);

        var condition = Assert.Single(settings.Rules[0].Conditions);
        Assert.Equal(ConditionField.ChannelName, condition.Field);
    }

    [Fact]
    public void DroppedTargetField_IsRemovedButOtherFieldsSurvive()
    {
        var settings = new AppSettings
        {
            Favorites = [Fav("混在", "音楽", fields: ["ChannelName", "TrackTitle", "ContactUrl"])],
        };
        SettingsMigration.Migrate(settings);

        var fields = settings.Rules[0].Conditions.Select(c => c.Field).ToList();
        Assert.Equal([ConditionField.ChannelName, ConditionField.ContactUrl], fields);
    }

    /// <summary>旧「アーティスト」は新「Playing」へ移行できる。</summary>
    [Fact]
    public void LegacyTrackArtist_MigratesToPlaying()
    {
        var settings = new AppSettings { Favorites = [Fav("経路", "Gateway", fields: ["TrackArtist"])] };
        SettingsMigration.Migrate(settings);

        var condition = Assert.Single(settings.Rules[0].Conditions);
        Assert.Equal(ConditionField.TrackArtist, condition.Field);
        Assert.Equal("Gateway", condition.Pattern);
    }

    [Fact]
    public void FavoritesWithNoKeyword_AreDropped()
    {
        // 旧実装でも Word が空のものは評価されていない
        var settings = new AppSettings { Favorites = [Fav("空", "")] };
        SettingsMigration.Migrate(settings);

        Assert.Empty(settings.Rules);
    }

    [Fact]
    public void LegacyColors_MoveOntoATagOrderedAheadOfTheBuiltIns()
    {
        var settings = new AppSettings { Favorites = [Fav("絵", "お絵かき", back: "#AABBCC", text: "#112233")] };
        SettingsMigration.Migrate(settings);

        var appearance = settings.Tags[0];
        Assert.Equal("#AABBCC", appearance.BackColor);
        Assert.Equal("#112233", appearance.ForeColor);
        // 色を持つタグが先頭 = 行の色はこちらが勝つ
        Assert.True(settings.Tags.IndexOf(appearance)
            < settings.Tags.FindIndex(t => t.Id == TagDefinition.FavoriteId));
        Assert.Contains(appearance.Id, settings.Rules[0].TagIds);
    }

    [Fact]
    public void FavoritesSharingAnAppearance_ShareOneTag()
    {
        var settings = new AppSettings
        {
            Favorites = [Fav("a", "a"), Fav("b", "b"), Fav("c", "c", back: "#001122")],
        };
        SettingsMigration.Migrate(settings);

        // 既定色ぶんで 1 つ + #001122 ぶんで 1 つ
        var appearanceTags = settings.Tags.Where(t => !t.BuiltIn).ToList();
        Assert.Equal(2, appearanceTags.Count);
    }

    /// <summary>通知は組み込みの「お気に入り」タグが担当する。移行タグで有効にすると二重に鳴る。</summary>
    [Fact]
    public void AppearanceTags_DoNotNotify()
    {
        var settings = new AppSettings { Favorites = [Fav("絵", "お絵かき")] };
        SettingsMigration.Migrate(settings);

        Assert.All(settings.Tags.Where(t => !t.BuiltIn), t => Assert.False(t.Notify));
    }

    [Fact]
    public void LegacyFavorites_AreClearedSoTheyDoNotMigrateTwice()
    {
        var settings = new AppSettings { Favorites = [Fav("絵", "お絵かき")] };
        SettingsMigration.Migrate(settings);

        Assert.Empty(settings.Favorites);
    }

    [Fact]
    public void ExistingTagModelRules_AreNotOverwrittenByLeftoverLegacyData()
    {
        var settings = new AppSettings
        {
            Rules = [new Rule { Name = "既存" }],
            Favorites = [Fav("旧", "旧")],
        };
        SettingsMigration.Migrate(settings);

        var rule = Assert.Single(settings.Rules);
        Assert.Equal("既存", rule.Name);
        Assert.Empty(settings.Favorites);
    }

    [Fact]
    public void MigratedRules_KeepTheirOriginalEvaluationOrder()
    {
        var settings = new AppSettings
        {
            Favorites = [Fav("1つ目", "a"), Fav("2つ目", "b"), Fav("3つ目", "c")],
        };
        SettingsMigration.Migrate(settings);

        Assert.Equal(["1つ目", "2つ目", "3つ目"], settings.Rules.OrderBy(r => r.Order).Select(r => r.Name));
    }

    [Fact]
    public void DisabledFavorite_StaysDisabled()
    {
        var fav = Fav("止めてる", "x");
        fav.Enabled = false;
        var settings = new AppSettings { Favorites = [fav] };
        SettingsMigration.Migrate(settings);

        Assert.False(settings.Rules[0].Enabled);
    }
}
