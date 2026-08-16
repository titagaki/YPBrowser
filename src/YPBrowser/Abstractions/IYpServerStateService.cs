using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Abstractions;

/// <summary>
/// YP ごとの実行時状態 (<see cref="YpServerItem"/>) の置き場。
/// 設定は永続、こちらはアプリの寿命だけ生きる。
/// </summary>
public interface IYpServerStateService
{
    /// <summary>設定に対応する状態を返す。無ければ作る。設定側の値は毎回写し直す。</summary>
    YpServerItem GetOrAdd(YpServerSettings settings);

    /// <summary>対応する状態を探す。まだ一度も取得していない YP では <c>null</c>。</summary>
    YpServerItem? Find(YpServerSettings settings);
}
