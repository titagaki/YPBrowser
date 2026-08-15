using YPBrowser.Models;

namespace YPBrowser.Tests;

/// <summary>
/// コンボボックスの中身と並び順。UI から見える順序なので、変えるときは意図的に変える。
/// </summary>
public class RuleOptionsTests
{
    [Fact]
    public void Fields_AreInTheAgreedOrder()
    {
        Assert.Equal(
            [
                ConditionField.ChannelName,
                ConditionField.Description,
                ConditionField.ContactUrl,
                ConditionField.TrackArtist,
            ],
            RuleOptions.Fields.Select(o => o.Value));
    }

    [Fact]
    public void Fields_CoverEveryConditionFieldExactlyOnce()
    {
        // 列挙型に値を足したのにコンボへ出し忘れる、を防ぐ
        Assert.Equal(
            Enum.GetValues<ConditionField>().OrderBy(v => v),
            RuleOptions.Fields.Select(o => o.Value).OrderBy(v => v));
    }

    [Fact]
    public void Fields_UseTheAgreedLabels()
    {
        Assert.Equal(
            ["チャンネル名", "ジャンル/詳細/コメント", "コンタクトURL", "Playing"],
            RuleOptions.Fields.Select(o => o.Label));
    }

    /// <summary>YP名 / コーデック / 曲名 は条件のフィールドから外した。</summary>
    [Theory]
    [InlineData("YpName")]
    [InlineData("ChannelType")]
    [InlineData("TrackTitle")]
    public void Fields_NoLongerOfferTheRemovedFields(string removed)
    {
        Assert.False(Enum.TryParse<ConditionField>(removed, out _));
    }

    [Fact]
    public void MatchTypes_PutRegexLast()
    {
        Assert.Equal(
            [ConditionMatchType.Contains, ConditionMatchType.Exact, ConditionMatchType.Regex],
            RuleOptions.MatchTypes.Select(o => o.Value));
        Assert.Equal("正規表現", RuleOptions.MatchTypes[^1].Label);
    }

    [Fact]
    public void MatchTypes_CoverEveryMatchTypeExactlyOnce()
    {
        Assert.Equal(
            Enum.GetValues<ConditionMatchType>().OrderBy(v => v),
            RuleOptions.MatchTypes.Select(o => o.Value).OrderBy(v => v));
    }

    /// <summary>並び順を変えても既定は正規表現のまま（主な利用者は正規表現を書く層）。</summary>
    [Fact]
    public void NewCondition_StillDefaultsToRegex()
    {
        Assert.Equal(ConditionMatchType.Regex, new RuleCondition().MatchType);
    }

    [Fact]
    public void DefaultActions_CoverEveryTagDefaultAction()
    {
        Assert.Equal(
            Enum.GetValues<TagDefaultAction>().OrderBy(v => v),
            RuleOptions.DefaultActions.Select(o => o.Value).OrderBy(v => v));
    }
}
