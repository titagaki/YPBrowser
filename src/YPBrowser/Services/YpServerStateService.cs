using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Services;

/// <summary>
/// YP ごとの実行時状態を保持する。
///
/// 以前は取得のたびに <see cref="YpServerItem"/> を new していたため、
/// <c>YpFetchService</c> が書き込んだ最終取得時刻・件数・エラーが毎回捨てられていた。
/// 「実行時状態の置き場が無い」ことがその原因だったので、置き場そのものを用意した。
///
/// 取得は自動更新の裏スレッド、表示は UI スレッドから触るので、辞書はロックで守る
/// （<see cref="YpServerItem"/> 自身のプロパティ変更通知は WPF が UI スレッドへ渡す）。
/// </summary>
public class YpServerStateService : IYpServerStateService
{
    private readonly Dictionary<string, YpServerItem> _byKey = [];
    private readonly Lock _gate = new();

    public YpServerItem GetOrAdd(YpServerSettings settings)
    {
        var key = KeyOf(settings);

        lock (_gate)
        {
            if (!_byKey.TryGetValue(key, out var item))
            {
                item = new YpServerItem();
                _byKey[key] = item;
            }

            CopySettings(settings, item);
            return item;
        }
    }

    public YpServerItem? Find(YpServerSettings settings)
    {
        lock (_gate)
            return _byKey.GetValueOrDefault(KeyOf(settings));
    }

    /// <summary>
    /// 名前ではなく接続先で引く。改名しても状態を引き継ぎ、
    /// URL やホストを変えたら「別の YP」として未取得から始まるようにするため。
    /// </summary>
    private static string KeyOf(YpServerSettings settings) =>
        $"{settings.Url.Trim()}\n{settings.Host.Trim()}".ToLowerInvariant();

    /// <summary>設定側で変わりうる値だけを写す。実行時状態には触らない。</summary>
    private static void CopySettings(YpServerSettings from, YpServerItem to)
    {
        to.Name = from.Name;
        to.Url = from.Url;
        to.Host = from.Host;
        to.Enabled = from.Enabled;
    }
}
