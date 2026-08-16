using YPBrowser.Models;
using YPBrowser.Services;
using YPBrowser.Settings;

namespace YPBrowser.Tests;

/// <summary>
/// YP ごとの実行時状態が取得をまたいで残ること。
/// 以前は取得のたびに YpServerItem を作り直していたため、ここが全部捨てられていた。
/// </summary>
public class YpServerStateServiceTests
{
    private static YpServerSettings Server(
        string name = "テストYP", string url = "http://example.com/index.txt", string host = "") =>
        new() { Name = name, Url = url, Host = host };

    [Fact]
    public void SameEndpoint_KeepsTheSameState()
    {
        var service = new YpServerStateService();
        var settings = Server();

        var first = service.GetOrAdd(settings);
        first.ChannelCount = 42;
        first.LastUpdateTime = new DateTime(2026, 8, 16, 21, 32, 0);

        var second = service.GetOrAdd(settings);

        Assert.Same(first, second);
        Assert.Equal(42, second.ChannelCount);
    }

    /// <summary>改名で状態を失うと、名前を変えた直後だけ「未取得」に見えてしまう。</summary>
    [Fact]
    public void Renaming_KeepsTheState()
    {
        var service = new YpServerStateService();

        var before = service.GetOrAdd(Server(name: "旧しいYP"));
        before.ChannelCount = 7;

        var after = service.GetOrAdd(Server(name: "新しいYP"));

        Assert.Same(before, after);
        Assert.Equal(7, after.ChannelCount);
        Assert.Equal("新しいYP", after.Name);
    }

    [Theory]
    [InlineData("http://other.example.com/index.txt", "")]
    [InlineData("http://example.com/index.txt", "192.168.0.1:7144")]
    public void DifferentEndpoint_StartsFresh(string url, string host)
    {
        var service = new YpServerStateService();

        var original = service.GetOrAdd(Server());
        original.ChannelCount = 99;

        var moved = service.GetOrAdd(Server(url: url, host: host));

        Assert.NotSame(original, moved);
        Assert.Equal(0, moved.ChannelCount);
        Assert.False(moved.HasEverFetched);
    }

    [Fact]
    public void GetOrAdd_CopiesSettingsWithoutTouchingState()
    {
        var service = new YpServerStateService();

        var state = service.GetOrAdd(Server());
        state.LastError = "接続できません";

        var updated = service.GetOrAdd(new YpServerSettings
        {
            Name = "名前を変えたYP",
            Url = "http://example.com/index.txt",
            Enabled = false,
        });

        Assert.Equal("名前を変えたYP", updated.Name);
        Assert.False(updated.Enabled);
        Assert.Equal("接続できません", updated.LastError);
    }

    [Fact]
    public void Find_ReturnsNullBeforeTheFirstFetch()
    {
        var service = new YpServerStateService();

        Assert.Null(service.Find(Server()));

        service.GetOrAdd(Server());

        Assert.NotNull(service.Find(Server()));
    }
}

/// <summary>設定画面の行に出す 1 行の書式。</summary>
public class YpServerItemStatusTests
{
    [Fact]
    public void BeforeTheFirstFetch_SaysNotFetched()
    {
        Assert.Equal("未取得", new YpServerItem().StatusDisplay);
    }

    [Fact]
    public void AfterASuccessfulFetch_ShowsTimeAndCount()
    {
        var item = new YpServerItem
        {
            LastUpdateTime = new DateTime(2026, 8, 16, 21, 32, 5),
            ChannelCount = 1234,
        };

        Assert.Equal("21:32:05 更新 ・ 1,234 件", item.StatusDisplay);
        Assert.False(item.HasError);
    }

    /// <summary>失敗は必ず理由まで出す。理由が出ないと「人が居ない」と区別できない。</summary>
    [Fact]
    public void AfterAFailure_ShowsTheReason()
    {
        var item = new YpServerItem { LastError = "応答がありません（10 秒でタイムアウト）" };

        Assert.Equal("取得できません: 応答がありません（10 秒でタイムアウト）", item.StatusDisplay);
        Assert.True(item.HasError);
    }

    [Fact]
    public void AfterAFailureThatFollowedASuccess_KeepsTheLastSuccessTime()
    {
        var item = new YpServerItem
        {
            LastUpdateTime = new DateTime(2026, 8, 16, 21, 32, 5),
            ChannelCount = 1234,
            LastError = "接続できません",
        };

        Assert.Equal("取得できません: 接続できません（最終取得 21:32）", item.StatusDisplay);
    }

    [Fact]
    public void StatusDisplay_IsRaisedWhenTheStateChanges()
    {
        var item = new YpServerItem();
        var changed = new List<string?>();
        item.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        item.LastError = "接続できません";

        Assert.Contains(nameof(YpServerItem.StatusDisplay), changed);
        Assert.Contains(nameof(YpServerItem.HasError), changed);
    }
}
