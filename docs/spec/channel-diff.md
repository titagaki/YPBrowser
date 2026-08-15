# 仕様: 差分判定とログ管理

対象: `ChannelDiffService`, `ChannelDiff`, `MainViewModel.OnRefreshCompleted`

## 1. `ChannelDiff` の値

| 値 | 意味 |
|---|---|
| `None` | 変化なし、またはログから復活した直後 |
| `New` | 前回リストにもログにも存在しなかった |
| `Up` | `Listeners` が前回より増加 |
| `Down` | `Listeners` が前回より減少（前回が `>= 0` のときのみ） |
| `Changed` | リスナー数以外の監視対象フィールドが変化 |
| `Log` | 前回は存在したが今回の一覧から消えた |

## 2. 判定手順（`ApplyDiff(oldList, newList)`）

チャンネルの同定キーは `Id`。`oldList` に同じ `Id` が複数ある場合は最初の 1 件を使う。

### 2.1 `newList` の各チャンネル

```
oldList に同じ Id がある？
├─ ある
│   ├─ Listeners > 前回          → Up
│   ├─ Listeners < 前回 かつ 前回 >= 0 → Down
│   ├─ 監視対象フィールドが変化   → Changed
│   └─ それ以外                  → None
└─ ない
    ├─ 同じ YP のログに同じ Id がある → ログから除去して None
    └─ ない                          → New
```

判定は上から順に評価し、最初に成立した 1 つだけが設定される。
リスナー数の増減と内容変更が同時に起きた場合は `Up` / `Down` が優先される。

`Listeners == -1`（不明）から実数値への変化は `Up`、実数値から `-1` への変化は
`prev.Listeners >= 0` の条件により `Down` にならず `Changed` または `None` になる。

### 2.2 `Changed` の監視対象フィールド

`Description`, `Genre`, `ChannelType`, `TrackTitle`, `TrackArtist` の 5 つ。
`Comment`, `Relays`, `BitrateKbps`, `BroadcastTimeStr` などの変化は `Changed` にならない。

### 2.3 消滅したチャンネル

`oldList` にあって `newList` にない、かつ `Diff != Log` のチャンネルを:

1. `Diff = Log`、`LoggedAt = 現在時刻` に設定
2. その YP のログリストに追加

すでに `Diff == Log` のもの（前回の消滅で登録済み）は再登録しない。

### 2.4 期限切れログの削除

`ApplyDiff` の最後に、全 YP のログから `現在時刻 - LoggedAt > 1 時間` のエントリを削除する。
`GetLogChannels()` / `GetAllLogChannels()` の呼び出し時にも同じ削除を行う。

## 3. ログの保持

```csharp
Dictionary<string, List<ChannelItem>> _logByYp;  // キー: YpName
```

| 項目 | 仕様 |
|---|---|
| 保持期間 | 1 時間（`LoggedAt` から） |
| 分離単位 | YP 名ごと |
| 再登場時 | ログから削除し、`Diff = None`（`New` にはならない） |
| 取得 API | `GetLogChannels(ypName)` / `GetAllLogChannels()`（全 YP をマージ） |

`ChannelDiffService` はシングルトンで、アプリ実行中このキャッシュを保持する。アプリ終了で失われる。

## 4. 更新完了時の処理順（`MainViewModel.OnRefreshCompleted`）

UI スレッド（`Dispatcher.BeginInvoke`）で実行される。

```
1. isFirstFetch = (_allChannels.Count == 0)      ← リスト更新前に判定
2. ChannelDiffService.ApplyDiff(_allChannels, newList)
3. TagMatchService.ApplyTags(newList, Rules, Tags)
4. 通知タグの新着を通知        （Behavior.NotifyOnFavorite が true かつ 1 件以上のとき）
5. 自動ダウンロード開始        （isFirstFetch が false かつ ルールが 1 件以上のとき）
6. _allChannels = newList + GetAllLogChannels()
7. 表示フィルタ適用 → トレイのツールチップ更新
```

### 初回フェッチ

起動後 1 回目の取得では `_allChannels` が空のため、全チャンネルが `New` になる。

| 処理 | 初回フェッチでの動作 |
|---|---|
| 自動ダウンロード | スキップする |
| タグの新着通知 | スキップしない（通知タグが付いた全チャンネルを通知する） |

## 5. `_allChannels` の内容

通常チャンネル（今回取得分）＋ 全 YP のログチャンネルを連結したリスト。
表示対象の絞り込みは `ChannelFilterService` が行う（[ui.md](ui.md#3-フィルタ)）。

トレイのツールチップ用の集計は `Diff != Log` のチャンネルのみを対象とする。

```
チャンネル数 = Diff != Log の件数
視聴者数     = Diff != Log かつ Listeners > 0 の Listeners 合計
```
