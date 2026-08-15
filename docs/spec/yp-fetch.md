# 仕様: YP 取得とパース

対象: `YpFetchService`, `AutoRefreshService`, `ChannelItem`

## 1. 取得

### リクエスト URL

```
{YpServerSettings.Url}                            Host が空のとき
{YpServerSettings.Url}?host={URLエンコードした Host}  Host が非空のとき
```

### HTTP

| 項目 | 値 |
|---|---|
| メソッド | GET |
| User-Agent | `YPBrowser/1.0`（`App.xaml.cs` でハードコード。`NetworkSettings.UserAgent` は未適用） |
| HttpClient のタイムアウト | 10 秒（固定） |
| 追加のキャンセル | `max(5, NetworkSettings.TimeoutSeconds)` 秒でリンク CTS をキャンセル |
| プロキシ | 未適用（`NetworkSettings.ProxyUrl` は読まれない） |

実効タイムアウトは上記 2 つの短い方、すなわち `min(10, max(5, TimeoutSeconds))` 秒。

### 結果と失敗時

| ケース | 戻り値 | 副作用 |
|---|---|---|
| 成功 | パース済みチャンネル一覧 | `LastUpdateTime = 現在時刻`、`LastError = null`、`ChannelCount` 更新 |
| キャンセル | 空リスト | なし |
| その他の例外（HTTP エラー含む） | 空リスト | `LastError = 例外メッセージ` |

例外は呼び出し元に伝播しない。

## 2. index.txt のパース

### 前処理

1. レスポンスを UTF-8 として文字列化
2. 先頭の BOM (`﻿`) を除去
3. `\n` で行分割し、各行の前後から `\r` `\n` 半角スペースを除去
4. 空行はスキップ

### 行フォーマット

`<>` 区切りの 19 フィールド。

| # | フィールド | `ChannelItem` プロパティ | 型 | HTML デコード |
|---|---|---|---|---|
| 0 | チャンネル名 | `ChannelName` | string | あり |
| 1 | チャンネル ID | `Id` | string（16 進 32 桁） | なし |
| 2 | Host:Port | `Host` | string | なし |
| 3 | ContactURL | `ContactUrl` | string | あり |
| 4 | ジャンル | `Genre` | string | あり |
| 5 | 説明 | `Description` | string | あり |
| 6 | 視聴者数 | `Listeners` | int | — |
| 7 | リレー数 | `Relays` | int | — |
| 8 | ビットレート | `BitrateKbps` | int | — |
| 9 | コーデック | `ChannelType` | string | なし |
| 10 | アーティスト | `TrackArtist` | string | あり |
| 11 | アルバム | `TrackAlbum` | string | あり |
| 12 | 曲名 | `TrackTitle` | string | あり |
| 13 | 曲ジャンル | `TrackGenre` | string | あり |
| 14 | URLParam | `UrlParam` | string | なし |
| 15 | 放送時間 | `BroadcastTimeStr` | string（文字列のまま） | なし |
| 16 | きゃすこステータス | `KyasukoStatus` | string | なし |
| 17 | コメント | `Comment` | string | あり |
| 18 | isDirect | `IsDirect` | bool（`"1"` のとき true） | なし |

各フィールドは取得時に前後空白を除去する。

### 行のスキップ条件

| 条件 | 動作 |
|---|---|
| フィールド数が 19 未満 | その行を丸ごとスキップ（部分パースはしない） |
| `Id` が空 | スキップ |

### 数値パース

`int.TryParse` に失敗した場合は `-1`。`Listeners = -1` は「不明」を意味する。

### HTML デコード

`HtmlSpecialCharsHelper.Decode()` を上表の対象フィールドに適用する。

```
&amp;  → &     &lt;   → <
&gt;   → >     &quot; → "
```

## 3. サーバー単位のフィルタ

パース直後、`ChannelItem` を一覧に加える前に評価する。

| フィルタ | 条件 | 無効化する値 |
|---|---|---|
| 下限ビットレート | `BitrateMin > 0 && BitrateKbps < BitrateMin` で除外 | `0`（既定） |
| 上限ビットレート | `BitrateMax > 0 && BitrateKbps > BitrateMax` で除外 | `-1` または `0`（既定は `-1`） |
| コーデック | `TypeFilter` を正規表現（IgnoreCase）として `ChannelType` に照合し、不一致を除外 | `".*"`（既定）または空文字 |

`TypeFilter` が不正な正規表現の場合、例外を握りつぶしてフィルタなしとして動作する。設定 UI にバリデーションはない。

## 4. チャンネルに付与される YP メタ情報

| プロパティ | 値 |
|---|---|
| `YpName` | `YpServerSettings.Name` |
| `YpUrl` | `Url` の末尾 `/` を正規化した文字列（必ず `/` 終わり） |
| `YpHost` | `YpServerSettings.Host` |
| `FetchedAt` | パース時刻 |
| `YpPriority` | 常に `0`（未使用） |

## 5. 派生 URL

| プロパティ | 生成規則 | 備考 |
|---|---|---|
| `StreamUrl` | `http://{YpHost または localhost:7144}/pls/{Id}` + `?tip={Host}`（`Host` が非空のとき） | 再生・録音・URL コピーで使用 |
| `DirectStreamUrl` | `http://{Host}/pls/{Id}`、`Host` が空なら空文字 | 現状どこからも使用されない |
| `StatsUrl` | `{YpUrl}getgmt.php?cn={URLエンコードしたChannelName}`、`YpUrl` が空なら空文字 | コンテキストメニュー「統計URLを開く」 |
| `ChatUrl` | `{YpUrl}chat.php?cn={URLエンコードしたChannelName}`、`YpUrl` が空なら空文字 | 現状どこからも使用されない |

## 6. 更新サイクル

`AutoRefreshService` が全 YP の取得をまとめて実行する。

```
Start()
  └─ 即座に 1 回取得
       └─ ループ: max(30, BehaviorSettings.RefreshIntervalSeconds) 秒待機 → 取得
```

1 回の取得（`DoRefreshAsync`）の手順:

1. `IsRefreshing` が true なら何もせず戻る（多重実行なし）
2. `RefreshStarted` イベント発火
3. `Enabled = true` の YP サーバーを設定順に**逐次**取得
4. 全 YP のチャンネルを 1 つのリストに連結
5. `IsRefreshing = false`、`RefreshCompleted` イベント発火（取得できた分だけを渡す）

`RefreshNowAsync()`（手動更新）は最小フェッチ間隔ガードを無視して即座に取得する。

### 最小フェッチ間隔ガード

自動更新時のみ、前回取得から 4 分未満の YP をスキップする判定を持つ。

```csharp
if (!force && DateTime.Now - server.LastUpdateTime < 4分 && server.LastUpdateTime != DateTime.MinValue)
    continue;
```

**現状の実装では、このガードは発動しない。** 判定に使う `YpServerItem` は `DoRefreshAsync` 内で
毎回 `YpServerSettings` から新規生成されるため、`LastUpdateTime` は常に `DateTime.MinValue` になる。
結果として、実際のフェッチ間隔は `RefreshIntervalSeconds`（既定 60 秒、下限 30 秒）と等しい。

### 通知される内容

```csharp
RefreshCompletedEventArgs.Channels  // 今回取得できた全 YP のチャンネル（ログは含まない）
```
