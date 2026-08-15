using YPBrowser.Models;
using YPBrowser.Services;

namespace YPBrowser.Tests;

public class TagMatchServiceTests
{
    private static readonly TagDefinition Favorite = TagDefinition.CreateFavorite();
    private static readonly TagDefinition Ng = TagDefinition.CreateNg();

    private static ChannelItem Channel(
        string name = "テストch",
        string genre = "ゲーム",
        string description = "",
        string comment = "",
        string contactUrl = "",
        string ypName = "SP",
        string trackArtist = "") =>
        new()
        {
            ChannelName = name,
            Genre = genre,
            Description = description,
            Comment = comment,
            ContactUrl = contactUrl,
            YpName = ypName,
            TrackArtist = trackArtist,
        };

    private static Rule Rule(
        ConditionField field,
        string pattern,
        ConditionMatchType matchType = ConditionMatchType.Contains,
        bool negate = false,
        params string[] tagIds) =>
        new()
        {
            Name = "r",
            Conditions = [new RuleCondition
            {
                Field = field, Pattern = pattern, MatchType = matchType, Negate = negate,
            }],
            TagIds = [.. tagIds],
        };

    // --- 一致方式 ---

    [Fact]
    public void Contains_IsCaseInsensitive()
    {
        var svc = new TagMatchService();
        var rule = Rule(ConditionField.ChannelName, "abc");
        Assert.True(svc.Evaluate(rule, Channel(name: "xxABCxx")));
    }

    [Fact]
    public void Exact_RequiresWholeField()
    {
        var svc = new TagMatchService();
        var rule = Rule(ConditionField.ChannelName, "いまいch", ConditionMatchType.Exact);

        Assert.True(svc.Evaluate(rule, Channel(name: "いまいch")));
        Assert.False(svc.Evaluate(rule, Channel(name: "いまいch 2")));
    }

    /// <summary>星ルールが Exact である理由: 名前を正規表現として評価すると誤爆する。</summary>
    [Fact]
    public void Exact_TreatsRegexMetacharactersLiterally()
    {
        var svc = new TagMatchService();
        var rule = Rule(ConditionField.ChannelName, "a.c", ConditionMatchType.Exact);

        Assert.True(svc.Evaluate(rule, Channel(name: "a.c")));
        Assert.False(svc.Evaluate(rule, Channel(name: "abc")));
    }

    [Fact]
    public void Regex_Matches()
    {
        var svc = new TagMatchService();
        var rule = Rule(ConditionField.ChannelName, @"Stream\d+", ConditionMatchType.Regex);
        Assert.True(svc.Evaluate(rule, Channel(name: "Stream123")));
    }

    [Fact]
    public void Regex_InvalidPattern_NeverMatchesAndDoesNotThrow()
    {
        var svc = new TagMatchService();
        var rule = Rule(ConditionField.ChannelName, "[invalid", ConditionMatchType.Regex);
        Assert.False(svc.Evaluate(rule, Channel(name: "[invalid")));
    }

    [Fact]
    public void Regex_InvalidPattern_IsReportedByValidate()
    {
        var svc = new TagMatchService();
        var bad = new RuleCondition { MatchType = ConditionMatchType.Regex, Pattern = "[invalid" };
        var good = new RuleCondition { MatchType = ConditionMatchType.Regex, Pattern = @"\d+" };

        Assert.NotNull(svc.ValidatePattern(bad));
        Assert.Null(svc.ValidatePattern(good));
    }

    [Fact]
    public void Validate_IgnoresNonRegexMatchTypes()
    {
        var svc = new TagMatchService();
        var condition = new RuleCondition { MatchType = ConditionMatchType.Contains, Pattern = "[invalid" };
        Assert.Null(svc.ValidatePattern(condition));
    }

    [Fact]
    public void EmptyPattern_NeverMatches()
    {
        var svc = new TagMatchService();
        Assert.False(svc.Evaluate(Rule(ConditionField.ChannelName, ""), Channel()));
    }

    // --- フィールド ---

    [Fact]
    public void Description_ConcatenatesGenreDetailAndComment()
    {
        var svc = new TagMatchService();
        var channel = Channel(genre: "ぷ絵かき", description: "詳細", comment: "コメント");

        Assert.True(svc.Evaluate(Rule(ConditionField.Description, "ぷ絵かき"), channel));
        Assert.True(svc.Evaluate(Rule(ConditionField.Description, "詳細"), channel));
        Assert.True(svc.Evaluate(Rule(ConditionField.Description, "コメント"), channel));
        // チャンネル名は説明文には含まれない
        Assert.False(svc.Evaluate(Rule(ConditionField.Description, "テストch"), channel));
    }

    /// <summary>
    /// 「Playing」は index.txt の 11 番目。配信経路（`… via Peercast Gateway`）が入ってくる。
    /// </summary>
    [Fact]
    public void Playing_MatchesTheTrackArtistSlot()
    {
        var svc = new TagMatchService();
        var channel = Channel(trackArtist: "210.157.193.184 via Peercast Gateway");

        Assert.True(svc.Evaluate(Rule(ConditionField.TrackArtist, "Peercast Gateway"), channel));
        Assert.True(svc.Evaluate(Rule(ConditionField.TrackArtist, "210.157.193.184"), channel));
        // 他のフィールドには混ざらない
        Assert.False(svc.Evaluate(Rule(ConditionField.Description, "Gateway"), channel));
        Assert.False(svc.Evaluate(Rule(ConditionField.ChannelName, "Gateway"), channel));
    }

    [Fact]
    public void Playing_WorksWithRegex()
    {
        var svc = new TagMatchService();
        var channel = Channel(trackArtist: "210.157.193.184 via Peercast Gateway");
        var rule = Rule(ConditionField.TrackArtist, @"^\d+\.\d+\.\d+\.\d+ via ", ConditionMatchType.Regex);

        Assert.True(svc.Evaluate(rule, channel));
    }

    /// <summary>実際の index.txt 行から Playing の値が取れることを、パース経由で確かめる。</summary>
    [Fact]
    public void Playing_ReadsFieldElevenOfARealIndexLine()
    {
        var line = "も＠ｃｈ<>4285FAA2A846562F5F50BE10F321A087<>49.212.151.50:7152"
            + "<>https://bbs.jpnkn.com/test/read.cgi/monyatto/1786713468/<> 自由<>&lt;Free&gt;"
            + "<>-1<>-1<>1296<>FLV<>210.157.193.184 via Peercast Gateway<><><>"
            + "<>%E3%82%82%EF%BC%A0%EF%BD%83%EF%BD%88<>1:59<>click<><>1";
        var parsed = Helpers.YpParser.ParseLines(line, "SP", "http://yp.test/");

        var ch = Assert.Single(parsed);
        Assert.Equal("210.157.193.184 via Peercast Gateway", ch.TrackArtist);

        var svc = new TagMatchService();
        var channel = Channel(name: ch.ChannelName, genre: ch.Genre, description: ch.Description,
            trackArtist: ch.TrackArtist);
        Assert.True(svc.Evaluate(Rule(ConditionField.TrackArtist, "via Peercast Gateway"), channel));
    }

    // --- combinator / negate ---

    [Fact]
    public void And_RequiresAllConditions()
    {
        var svc = new TagMatchService();
        var rule = new Rule
        {
            Combinator = RuleCombinator.And,
            Conditions =
            [
                new RuleCondition { Field = ConditionField.Description, MatchType = ConditionMatchType.Contains, Pattern = "ぷ絵かき" },
                new RuleCondition { Field = ConditionField.Description, MatchType = ConditionMatchType.Contains, Pattern = "Blender" },
            ],
        };

        Assert.False(svc.Evaluate(rule, Channel(genre: "ぷ絵かき")));
        Assert.True(svc.Evaluate(rule, Channel(genre: "ぷ絵かき", description: "Blender練習")));
    }

    [Fact]
    public void Or_RequiresAnyCondition()
    {
        var svc = new TagMatchService();
        var rule = new Rule
        {
            Combinator = RuleCombinator.Or,
            Conditions =
            [
                new RuleCondition { Field = ConditionField.Description, MatchType = ConditionMatchType.Contains, Pattern = "ぷ絵かき" },
                new RuleCondition { Field = ConditionField.Description, MatchType = ConditionMatchType.Contains, Pattern = "Blender" },
            ],
        };

        Assert.True(svc.Evaluate(rule, Channel(genre: "ぷ絵かき")));
        Assert.False(svc.Evaluate(rule, Channel(genre: "雑談")));
    }

    [Fact]
    public void Negate_InvertsTheCondition()
    {
        var svc = new TagMatchService();
        var rule = Rule(ConditionField.Description, "3D", ConditionMatchType.Contains, negate: true);

        Assert.True(svc.Evaluate(rule, Channel(genre: "ぷ絵かき")));
        Assert.False(svc.Evaluate(rule, Channel(genre: "3Dモデリング")));
    }

    [Fact]
    public void NoConditions_NeverMatches()
    {
        var svc = new TagMatchService();
        Assert.False(svc.Evaluate(new Rule { Name = "空" }, Channel()));
    }

    // --- ApplyTags ---

    [Fact]
    public void ApplyTags_AppliesEveryTagOfEveryMatchingRule()
    {
        var svc = new TagMatchService();
        var music = new TagDefinition { Name = "音楽" };
        var channel = Channel(genre: "音楽", description: "作業用BGM");

        svc.ApplyTags(
            [channel],
            [
                Rule(ConditionField.Description, "音楽", tagIds: [music.Id]),
                Rule(ConditionField.Description, "BGM", tagIds: [Favorite.Id]),
            ],
            [Favorite, Ng, music]);

        Assert.Equal(2, channel.Tags.Count);
        Assert.True(channel.IsFavorite);
        Assert.Contains(channel.Tags, t => t.Id == music.Id);
    }

    [Fact]
    public void ApplyTags_SkipsDisabledRules()
    {
        var svc = new TagMatchService();
        var rule = Rule(ConditionField.ChannelName, "テスト", tagIds: [Favorite.Id]);
        rule.Enabled = false;

        var channel = Channel();
        svc.ApplyTags([channel], [rule], [Favorite, Ng]);

        Assert.Empty(channel.Tags);
    }

    [Fact]
    public void ApplyTags_RespectsOrderAndStopProcessing()
    {
        var svc = new TagMatchService();
        var later = new TagDefinition { Name = "後" };

        var first = Rule(ConditionField.ChannelName, "テスト", tagIds: [Ng.Id]);
        first.Order = 0;
        first.StopProcessing = true;

        var second = Rule(ConditionField.ChannelName, "テスト", tagIds: [later.Id]);
        second.Order = 1;

        var channel = Channel();
        // 順番に依存しないよう、わざと逆順で渡す
        svc.ApplyTags([channel], [second, first], [Favorite, Ng, later]);

        Assert.Single(channel.Tags);
        Assert.Equal(Ng.Id, channel.Tags[0].Id);
    }

    [Fact]
    public void ApplyTags_DoesNotDuplicateTheSameTag()
    {
        var svc = new TagMatchService();
        var channel = Channel();

        svc.ApplyTags(
            [channel],
            [
                Rule(ConditionField.ChannelName, "テスト", tagIds: [Favorite.Id]),
                Rule(ConditionField.ChannelName, "ch", tagIds: [Favorite.Id]),
            ],
            [Favorite, Ng]);

        Assert.Single(channel.Tags);
    }

    [Fact]
    public void ApplyTags_ClearsTagsThatNoLongerMatch()
    {
        var svc = new TagMatchService();
        var channel = Channel();
        var rule = Rule(ConditionField.ChannelName, "テスト", tagIds: [Favorite.Id]);

        svc.ApplyTags([channel], [rule], [Favorite, Ng]);
        Assert.NotEmpty(channel.Tags);

        rule.Conditions[0].Pattern = "別のもの";
        svc.ApplyTags([channel], [rule], [Favorite, Ng]);
        Assert.Empty(channel.Tags);
    }

    [Fact]
    public void ApplyTags_IgnoresTagIdsThatNoLongerExist()
    {
        var svc = new TagMatchService();
        var channel = Channel();
        var rule = Rule(ConditionField.ChannelName, "テスト", tagIds: ["deleted-tag", Favorite.Id]);

        svc.ApplyTags([channel], [rule], [Favorite, Ng]);

        Assert.Single(channel.Tags);
        Assert.Equal(TagDefinition.FavoriteId, channel.Tags[0].Id);
    }

    /// <summary>色は「色が設定されている最初のタグ」。順序はタグ定義の並び順で決まる。</summary>
    [Fact]
    public void ApplyTags_OrdersTagsByTagListOrderSoColorIsDeterministic()
    {
        var svc = new TagMatchService();
        var green = new TagDefinition { Name = "音楽", BackColor = "#E6F4EA" };
        var yellow = new TagDefinition { Name = "ぷ絵かき", BackColor = "#FFF4CE" };
        var channel = Channel();

        // ルールの並びは 緑 → 黄 だが、タグ一覧の並びは 黄 → 緑
        svc.ApplyTags(
            [channel],
            [
                Rule(ConditionField.ChannelName, "テスト", tagIds: [green.Id]),
                Rule(ConditionField.ChannelName, "ch", tagIds: [yellow.Id]),
            ],
            [yellow, green, Favorite, Ng]);

        Assert.Equal(yellow.Id, channel.Tags[0].Id);
        Assert.Equal(ColorHelperHex(yellow), ColorHelperHex(channel.Tags.First(t => t.HasColor)));
    }

    private static string? ColorHelperHex(TagDefinition tag) => tag.BackColor;

    // --- 表示側の派生プロパティ ---

    [Fact]
    public void HiddenTag_MakesChannelHidden()
    {
        var svc = new TagMatchService();
        var channel = Channel();
        svc.ApplyTags([channel], [Rule(ConditionField.ChannelName, "テスト", tagIds: [Ng.Id])], [Favorite, Ng]);

        Assert.True(channel.IsHidden);
        Assert.False(channel.IsHighlighted);
    }

    [Fact]
    public void HighlightTag_MakesChannelHighlighted()
    {
        var svc = new TagMatchService();
        var channel = Channel();
        svc.ApplyTags([channel], [Rule(ConditionField.ChannelName, "テスト", tagIds: [Favorite.Id])], [Favorite, Ng]);

        Assert.True(channel.IsHighlighted);
        Assert.False(channel.IsHidden);
    }

    // --- 通知 ---

    [Fact]
    public void GetChannelsToNotify_OnlyNewChannelsWithANotifyTag()
    {
        var svc = new TagMatchService();
        var quiet = new TagDefinition { Name = "静か", Notify = false };

        var newFav = Channel(name: "新着お気に入り");
        newFav.Diff = ChannelDiff.New;
        var oldFav = Channel(name: "既出お気に入り");
        oldFav.Diff = ChannelDiff.None;
        var newQuiet = Channel(name: "新着だが通知なし");
        newQuiet.Diff = ChannelDiff.New;

        var rules = new List<Rule>
        {
            Rule(ConditionField.ChannelName, "お気に入り", tagIds: [Favorite.Id]),
            Rule(ConditionField.ChannelName, "通知なし", tagIds: [quiet.Id]),
        };
        svc.ApplyTags([newFav, oldFav, newQuiet], rules, [Favorite, Ng, quiet]);

        var notify = svc.GetChannelsToNotify([newFav, oldFav, newQuiet]);

        Assert.Single(notify);
        Assert.Equal("新着お気に入り", notify[0].ChannelName);
    }

    [Fact]
    public void GetChannelsToNotify_SkipsHiddenChannels()
    {
        var svc = new TagMatchService();
        var channel = Channel();
        channel.Diff = ChannelDiff.New;

        svc.ApplyTags(
            [channel],
            [
                Rule(ConditionField.ChannelName, "テスト", tagIds: [Favorite.Id]),
                Rule(ConditionField.ChannelName, "テスト", tagIds: [Ng.Id]),
            ],
            [Favorite, Ng]);

        Assert.Empty(svc.GetChannelsToNotify([channel]));
    }

    // --- 星ルール ---

    [Fact]
    public void StarRule_UsesExactMatchOnChannelName()
    {
        var rule = Models.Rule.CreateStarRule("いまいch");

        Assert.True(rule.IsAuto);
        Assert.Equal(ConditionMatchType.Exact, rule.Conditions[0].MatchType);
        Assert.Equal(ConditionField.ChannelName, rule.Conditions[0].Field);
        Assert.Contains(TagDefinition.FavoriteId, rule.TagIds);
    }

    [Fact]
    public void IsStarRuleFor_MatchesOnlyItsOwnChannel()
    {
        var rule = Models.Rule.CreateStarRule("いまいch");

        Assert.True(rule.IsStarRuleFor("いまいch"));
        Assert.False(rule.IsStarRuleFor("べつのch"));
    }

    [Fact]
    public void IsStarRuleFor_RejectsHandWrittenRules()
    {
        var rule = Rule(ConditionField.ChannelName, "いまいch", ConditionMatchType.Exact, tagIds: [TagDefinition.FavoriteId]);
        Assert.False(rule.IsStarRuleFor("いまいch"));
    }
}
