# チャンネルのライフサイクルと差分管理

## ChannelDiff 列挙値の意味

| 値 | 意味 | いつ設定されるか |
|---|---|---|
| `None` | 変化なし / 通常状態 | 前回から変化がない、またはログから復活した直後 |
| `New` | 新着 | 前回リストになく、ログにもなかった初登場チャンネル |
| `Up` | リスナー増加 | `Listeners` が前回より増えた |
| `Down` | リスナー減少 | `Listeners` が前回より減った（`>= 0` のときのみ） |
| `Changed` | その他変更 | ジャンル・説明・コーデック・トラック情報のいずれかが変わった |
| `Log` | 消滅（履歴） | 前回は存在したが今回のリストからなくなった |

`Up` と `Changed` は排他ではなく、リスナー増加と内容変更が同時に起きた場合は
`Listeners` の増減チェックが先に行われるため `Up` か `Down` になる。

## Log チャンネルとは

YP サーバーから消えたチャンネルを即座に画面から消すのではなく、
**1時間保持してフィルタ「ログ」で参照できる**仕組み。

Delphi 版 pcypLite の「ログ」タブに相当する機能。

### なぜ1時間か

- 一時的なネットワーク障害で YP から消えただけのチャンネルを即削除しない
- 「さっきまであったチャンネルに後から接続したい」という操作を可能にする
- 1時間以上前のログは古すぎるため自動削除

### Log チャンネルの状態遷移

```
[通常リスト] → チャンネル消滅 → Diff = Log、LoggedAt = now → [ログキャッシュ]
                                                                    │
                                         1時間経過 → 自動削除 ←──┤
                                                                    │
                    再度 YP に登場 ←────────────────────────────────┘
                         │
                    ログから削除、Diff = None（「復活」であって「新着」ではない）
```

### 落とし穴: 「再登場」は New にならない

チャンネルが消えて（Diff=Log）から1時間以内に再登場した場合、
`ChannelDiffService` はログキャッシュを確認して `New` にしない。
1時間が経過してログから削除された後に再登場した場合のみ `New` になる。

これは「同じチャンネルの再開を新着として何度も通知しない」という意図的な設計。

## ApplyDiff の呼び出しタイミングと「初回フェッチ問題」

`ApplyDiff(oldList, newList)` は `_allChannels` が空（初回起動）の場合、
`newList` 内の全チャンネルが「前回リストに存在しない」ため全件 `New` になる。

### なぜ問題か

自動ダウンロードルールが設定されている状態で起動すると、
全マッチチャンネルの録音が一斉に始まってしまう。

### 対処（MainViewModel.OnRefreshCompleted）

```csharp
bool isFirstFetch = _allChannels.Count == 0;  // Clear() の前に判定

// ... ApplyDiff、MatchAll ...

if (!isFirstFetch)  // 初回フェッチ時は自動ダウンロードしない
{
    var rules = AutoDownloadSettingsMapper.ToRuleItems(...);
    foreach (var ch in _autoDownloadService.GetChannelsToAutoDownload(newList, rules))
        _recordService.StartRecording(ch, ...);
}
```

通知（`NotifyNewFavorites`）にはこのガードがない。
起動時に全新着お気に入りを通知するのは意図的な挙動（「今放送中のお気に入り」を知らせる）。

## YP 別ログキャッシュの設計

```csharp
private readonly Dictionary<string, List<ChannelItem>> _logByYp = [];
```

複数の YP サーバーからチャンネルを取得するため、ログを YP 名で分離している。
異なる YP で同じチャンネル ID が重複した場合に混ざらないようにするため。

`GetAllLogChannels()` は全 YP のログをマージして返す（表示用）。

## Listeners == -1 の扱い

PeerCast の一部チャンネルはリスナー数が `-1`（不明）になることがある。
`Down` 判定は `prev.Listeners >= 0` の条件付きで行われており、
「不明」から「有数値」への変化は `Up` にも `Down` にもならず `Changed` になる。

```csharp
if (ch.Listeners > prev.Listeners)
    ch.Diff = ChannelDiff.Up;
else if (ch.Listeners < prev.Listeners && prev.Listeners >= 0)
    ch.Diff = ChannelDiff.Down;
```

## 更新サイクルと 4 分の最小フェッチ間隔

デフォルトの自動更新間隔は 60 秒だが、各 YP サーバーへのリクエストは
**最短 4 分に 1 回**に制限されている。

### なぜ 4 分か

YP サーバーへの負荷を下げるためのエチケット。
60 秒ごとに `RefreshCompleted` イベントは発火するが、
各サーバーへの実際の HTTP リクエストは 4 分に 1 回だけ送られる。

### `force: true` フラグ

手動更新ボタン（F5 キーを含む）は `RefreshNowAsync(force: true)` を呼ぶ。
`force: true` の場合は 4 分ガードをスキップして即座にフェッチする。

### 複数 YP 環境での動作

サーバーごとに `LastUpdateTime` が独立しているため、
「YP-A は 3 分前に取得済み → スキップ、YP-B は 5 分前 → 取得」
という部分更新が発生しうる。
`RefreshCompleted` イベントは「取得できた分だけ」のチャンネルリストを渡す。
