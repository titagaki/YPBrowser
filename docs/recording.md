# 録音・自動ダウンロード機能

## 仕組みの概要

外部ツール（ffmpeg 等）には依存しない。
`HttpClient` で PeerCast のストリーム URL に接続し、
レスポンスボディをそのままファイルに書き込む方式。

```
RecordService.StartRecording()
    │
    ├─ RecordingEntry を生成・_active に登録
    └─ Task.Run( DownloadAsync )  ← バックグラウンドで実行
           │
           ├─ ResolveStreamUrlAsync()   ← PLS を解決して実ストリーム URL を取得
           │       GET /pls/{id} → PLS テキストをパース → File1= の URL を抽出
           │
           └─ リトライループ (最大 10 回、5 秒間隔)
                   │
                   └─ GET /stream/{id}?tip=...
                           CopyWithProgressAsync → FileStream に書き込み
                           entry.AddBytes(n) で進捗を積算
```

## PLS 解決が必要な理由

`ChannelItem.StreamUrl` は `http://localhost:7144/pls/{id}?tip={host}` だが、
この URL に HTTP GET すると PeerCast は **PLS テキストファイル**を返す。

```
[playlist]
NumberOfEntries=1
File1=http://localhost:7144/stream/{id}?tip={host}
Title1=チャンネル名
Length1=-1
Version=2
```

実際のストリームデータは `File1` に書かれた URL（`/stream/{id}`）で取得できる。
この 2 段階解決を `ResolveStreamUrlAsync()` が担う。

### M3U にも対応

一部の PeerCast 実装は M3U 形式を返すことがある。
`ResolveStreamUrlAsync()` は `[playlist]` 検出で PLS、
`#EXTM3U` 検出で M3U をパースする。
どちらでもなければ初期 URL をそのまま使う（直接ストリーム配信の PeerCast に対応）。

## HttpClient の設定

録音用の `HttpClient` は **タイムアウト無限大** で登録されている。

```csharp
services.AddHttpClient("RecordService", client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan;
});
```

ストリームは数時間続くことがあり、10 秒のようなタイムアウトを設定すると
録音が途中で強制終了される。

## 録音の開始・停止

```csharp
// 開始
_recordService.StartRecording(channel, settings);

// 停止
_recordService.StopRecording(channelId);

// 録音中かどうか
bool recording = _recordService.IsRecording(channelId);

// 現在録音中の一覧
IReadOnlyList<RecordingEntry> active = _recordService.ActiveRecordings;
```

`ConcurrentDictionary<string, (CancellationTokenSource, RecordingEntry)>` で
録音中チャンネルを管理する。`StopRecording()` は CTS をキャンセルし、
`CopyWithProgressAsync` が `OperationCanceledException` で終了する。

### 停止時のファイル

キャンセルされた時点までのバイト列がファイルに残る。
不完全なファイルでも多くのプレイヤーは途中まで再生できる。

## 自動再試行

接続が切れた場合、自動で再接続を試みる。

| 設定 | 値 |
|---|---|
| 最大試行回数 | 10 回 |
| 再試行間隔 | 5 秒 |
| キャンセル時 | リトライしない |

再試行時は同じファイルに追記する（`FileMode.Create` は最初の 1 回のみ）。
`RecordingEntry.RetryCount` が 1 ずつ増加し、UI のリトライ列に反映される。

```
attempt 0: 接続成功、ダウンロード中 → 切断
attempt 1: 5 秒待機 → 再接続・ダウンロード再開  (RetryCount = 1)
...
attempt 10: 失敗なら中断してログにエラー出力
```

## 進捗追跡

バックグラウンドスレッド上でのバイト加算と UI 更新を分離している。

```
[バックグラウンドスレッド]
CopyWithProgressAsync → entry.AddBytes(n)
                            └─ Interlocked.Add(_pendingBytes, n)  ← スレッドセーフ

[UI スレッド・1秒ごと]
DispatcherTimer.Tick → entry.Tick()
    └─ BytesDownloaded = _pendingBytes  → StatusDisplay が更新される
```

`BytesDownloaded` は毎秒まとめて更新されるため、表示の数字は 1 秒ごとに変化する。

## RecordingEntry

録音 1 件分の実行時状態を表す `ObservableObject`。設定値（`DownloaderSettings`）とは別に存在する。

| プロパティ | 型 | 内容 |
|---|---|---|
| `ChannelId` | string | チャンネル識別子 |
| `ChannelName` | string | チャンネル名 |
| `ChannelDetail` | string | `GenreDescription`（ジャンル・説明） |
| `FilePath` | string | 保存先フルパス |
| `StartedAt` | DateTime | 録音開始時刻 |
| `BytesDownloaded` | long | 保存済みバイト数（1秒毎更新） |
| `RetryCount` | int | 再試行回数 |
| `IsActive` | bool | `true` = 録音中、`false` = 終了 |
| `DisplayName` | string (computed) | 終了後は `"チャンネル名（終了）"` |
| `RetryCountDisplay` | string (computed) | 0 のとき空文字 |
| `StatusDisplay` | string (computed) | `"(0:09:39) 5,444KB"` 形式 |

`IsActive` が `false` になると終了時刻が固定され、`StatusDisplay` の経過時間が止まる。

## UI からの操作

チャンネル一覧の右クリックメニュー「録音/録画」は **トグル**動作。

```csharp
if (_recordService.IsRecording(channel.Id))
    StopRecording → StatusText = "録音停止: ..."
else
    StartRecording → StatusText = "録音開始: ..."
```

### 録画中タブ

メインウィンドウの「録画中 (N)」タブで録音状況を一覧できる。

- 新しい録音は**上に追加**される
- 録音中のエントリは**薄紫背景**、終了後は白背景で残る
- 停止ボタンは録音中のみ表示

`MainViewModel.RecordingEntries` が `RecordService.RecordingsChanged` イベントで更新される。
停止したエントリは `IsActive = false` になるだけで削除されない。

## ファイル名の生成

```
FileNameTemplate: "{channelName}_{timestamp}"
→ あくえり_20260321_232258.flv
```

コーデックから拡張子を自動判定（MP3→.mp3, FLV→.flv, OGG→.ogg 等）。
未知のコーデックは `.ts` になる。

ファイル名に使えない文字（`\`, `:`, `*` 等）は `_` に置換される。

保存先フォルダが未設定の場合は `%USERPROFILE%\Downloads` に保存される。

## 自動ダウンロードルール

### マッチングのタイミング

`MainViewModel.OnRefreshCompleted()` の中で、
`ChannelDiff.ApplyDiff()` によって `Diff == New` が設定された直後に評価される。

```csharp
_diffService.ApplyDiff(_allChannels, newList);  // New フラグが立つ

if (!isFirstFetch)  // 起動時はスキップ
{
    var rules = AutoDownloadSettingsMapper.ToRuleItems(...);
    foreach (var ch in _autoDownloadService.GetChannelsToAutoDownload(newList, rules))
        _recordService.StartRecording(ch, settings);
}
```

`Diff == New` のチャンネルだけが対象なので、
既に放送中のチャンネルは更新サイクルが回っても再度録音開始されない。

### 初回フェッチのスキップ

起動直後の最初のフェッチでは全チャンネルが `New` になるため、
自動録音を**スキップする**（`isFirstFetch` ガード）。
これがないと起動時に全マッチチャンネルの録音が一斉に始まる。

### ルールの優先順位

複数のルールにマッチしても録音は **1 回だけ**開始される。
`GetChannelsToAutoDownload()` は `LINQ Any()` でマッチを判定するため、
最初にマッチしたルールで判定が終わる。

## 落とし穴

### 同じチャンネルへの多重録音

`StartRecording()` は `_active.ContainsKey(channel.Id)` で既存録音をチェックし、
録音中なら何もしない。ただし手動と自動が競合する場合
（自動ダウンロードが録音を開始した直後に手動で開始しようとした場合）、
`ConcurrentDictionary.TryAdd()` の競合によって安全に弾かれる。

### 停止後の `_active` クリーンアップ

`DownloadAsync()` が完了（正常・キャンセル・エラー）した場合、
`finally` ブロックで `_active.TryRemove()` を呼んで CTS を解放する。
`StopRecording()` が先に呼ばれた場合は `_active` から既に削除済みのため
`TryRemove()` は何も削除しない（二重解放なし）。

どちらが先に呼ばれても `RecordingsChanged` が発火し、UI が更新される。

### 最大リトライ後のファイル

10 回リトライしても接続できない場合、ダウンロードタスクが終了し
`RecordingEntry.IsActive` が `false` になる。
ファイルはそれまでに書き込まれたバイト列がそのまま残る（自動削除しない）。
