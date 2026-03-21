# 録音・自動ダウンロード機能

## 仕組みの概要

外部ツール（ffmpeg 等）には依存しない。
`HttpClient` で PeerCast のストリーム URL に接続し、
レスポンスボディをそのままファイルに書き込む方式。

```
RecordService.StartRecording()
    │
    ├─ CancellationTokenSource を生成・保存
    └─ Task.Run( DownloadAsync )  ← バックグラウンドで実行
           │
           ├─ ResolveStreamUrlAsync()   ← PLS を解決して実ストリーム URL を取得
           │       GET /pls/{id} → PLS テキストをパース → File1= の URL を抽出
           │
           └─ GET /stream/{id}?tip=...  ← 実ストリームに接続
                   レスポンスボディ → FileStream に CopyToAsync
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
```

`ConcurrentDictionary<string, CancellationTokenSource>` で
録音中チャンネルを管理する。`StopRecording()` は CTS をキャンセルし、
`CopyToAsync` が `OperationCanceledException` で終了する。

### 停止時のファイル

キャンセルされた時点までのバイト列がファイルに残る。
不完全なファイルでも多くのプレイヤーは途中まで再生できる。

## UI からの操作

右クリックメニュー「録音/録画」は **トグル**動作。

```csharp
if (_recordService.IsRecording(channel.Id))
    StopRecording → StatusText = "録音停止: ..."
else
    StartRecording → StatusText = "録音開始: ..."
```

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

### ネットワーク切断

録音中に PeerCast が停止したり接続が切れた場合、
`CopyToAsync` は例外で終了し、ログに記録されてファイルは閉じられる。
自動再試行はない。
