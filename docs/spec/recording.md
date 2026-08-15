# 仕様: 録音

対象: `RecordService`, `RecordingEntry`, `DownloaderSettings`

外部ツール（ffmpeg 等）は使わない。`HttpClient` でストリームに接続し、レスポンスボディをそのままファイルへ書き込む。

## 1. 全体の流れ

```
StartRecording(channel, settings)
  ├─ 二重チェック → RecordingEntry 生成 → _active に登録 → RecordingsChanged 発火
  └─ Task.Run(DownloadAsync)
        ├─ 出力ディレクトリ作成、FileStream を FileMode.Create で開く
        └─ attempt = 0..10 のループ
              ├─ attempt > 0 なら RetryCount 更新 → 5 秒待機
              ├─ ResolveStreamUrlAsync()  ← PLS/M3U を解決
              ├─ GET (ResponseHeadersRead)
              └─ 読み取り 81,920 バイトごとに FileStream へ書き込み + AddBytes()
```

## 2. 開始と停止

| API | 動作 |
|---|---|
| `StartRecording(channel, settings)` | 同じ `ChannelId` が録音中なら警告ログを出して何もしない。それ以外は録音を開始 |
| `StopRecording(channelId)` | `_active` から除去し `CancellationTokenSource` をキャンセル・破棄、`RecordingsChanged` 発火 |
| `IsRecording(channelId)` | `_active` にキーがあるか |
| `ActiveRecordings` | 現在 `_active` にある `RecordingEntry` の一覧 |
| `RecordingsChanged` イベント | 開始時・停止時・ダウンロードタスク終了時に発火 |

管理は `ConcurrentDictionary<ChannelId, (Cts, Entry)>`。`TryAdd` に失敗した場合は開始せず CTS を破棄する。

ダウンロードタスクは終了理由（完了・キャンセル・リトライ上限）にかかわらず `finally` で
`_active` から自身を除去する。すでに `StopRecording` が除去済みなら何もしない。

## 3. ストリーム URL の解決

`ChannelItem.StreamUrl`（`/pls/{Id}`）に GET し、応答内容で分岐する。

| 応答内容 | 使用する URL |
|---|---|
| `[playlist]` を含む | 最初の `File1=` 行の値 |
| `#EXTM3U` または `#EXT-X-` で始まる | `#` で始まらない最初の非空行 |
| 上記以外 | 元の URL をそのまま使う |
| HTTP ステータスが成功でない | 元の URL をそのまま使う |
| 解決結果が空・元 URL と同一 | 元の URL をそのまま使う |

`[playlist]` の判定は大文字小文字を無視した部分一致。解決はリトライのたびに実行される。

PeerCast が返す PLS の例:

```
[playlist]
NumberOfEntries=1
File1=http://localhost:7144/stream/{id}?tip={host}
Title1=チャンネル名
Length1=-1
Version=2
```

## 4. HTTP

| 項目 | 値 |
|---|---|
| HttpClient 名 | `"RecordService"`（`IHttpClientFactory`） |
| タイムアウト | `Timeout.InfiniteTimeSpan` |
| User-Agent | `YPBrowser/1.0` |
| 本体取得 | `HttpCompletionOption.ResponseHeadersRead` |
| 成功判定 | `EnsureSuccessStatusCode()` |

## 5. 再試行

| 項目 | 値 |
|---|---|
| 最大リトライ回数 | 10（初回接続を含めて最大 11 回の試行） |
| 間隔 | 5 秒 |
| キャンセル時 | リトライせず即終了 |
| リトライ時のファイル | 同じ `FileStream` に**追記**（`FileMode.Create` はタスク開始時の 1 回のみ） |
| `RetryCount` | 試行回数（`attempt`）をそのまま代入し、UI のリトライ列に反映 |
| 上限到達時 | エラーログを出してタスク終了。ファイルは削除せず残す |

## 6. 出力先とファイル名

| 項目 | 仕様 |
|---|---|
| 出力ディレクトリ | `DownloaderSettings.OutputDirectory`。空白のみ・未設定なら `%USERPROFILE%\Downloads` |
| 環境変数 | `Environment.ExpandEnvironmentVariables` で展開する |
| ディレクトリ作成 | ダウンロード開始時に自動作成 |
| ファイル名 | `FileNameTemplate` のプレースホルダを置換したもの＋拡張子 |

### プレースホルダ

| 記法 | 置換値 |
|---|---|
| `{channelName}` | チャンネル名（ファイル名に使えない文字を `_` に置換） |
| `{timestamp}` | 録音開始時刻 `yyyyMMdd_HHmmss` |

既定テンプレート: `{channelName}_{timestamp}` → `あくえり_20260321_232258.flv`

### 拡張子（`ChannelType` から判定・大文字小文字無視）

| コーデック | 拡張子 |
|---|---|
| MP3 | `.mp3` |
| OGG / OGV | `.ogg` |
| AAC | `.aac` |
| WMA | `.wma` |
| FLV | `.flv` |
| MKV | `.mkv` |
| WMV | `.wmv` |
| NSV | `.nsv` |
| 上記以外・空 | `.ts` |

同名ファイルが既に存在する場合は上書きされる（`FileMode.Create`）。

## 7. 進捗の追跡

| スレッド | 処理 |
|---|---|
| バックグラウンド | 読み取りごとに `RecordingEntry.AddBytes(n)` → `Interlocked.Add` で内部カウンタに加算 |
| UI（1 秒周期の `DispatcherTimer`） | `RecordingEntry.Tick()` → カウンタを `BytesDownloaded` に反映し、経過時間表示を更新 |

`Tick()` は `IsActive == false` のエントリでは何もしない（表示が停止時刻で固定される）。

## 8. `RecordingEntry`

録音 1 件分の実行時状態（`ObservableObject`）。設定値とは独立。

| プロパティ | 型 | 内容 |
|---|---|---|
| `ChannelId` | string | チャンネル識別子（読み取り専用） |
| `ChannelName` | string | チャンネル名（読み取り専用） |
| `ChannelDetail` | string | 開始時点の `GenreDescription`（読み取り専用） |
| `FilePath` | string | 保存先フルパス（読み取り専用） |
| `StartedAt` | DateTime | 録音開始時刻（読み取り専用） |
| `BytesDownloaded` | long | 保存済みバイト数（1 秒ごとに更新） |
| `RetryCount` | int | 再試行回数 |
| `IsActive` | bool | `true` = 録音中。`false` になった時点の時刻を内部に記録 |
| `DisplayName` | string | `IsActive` なら channel 名、終了後は `チャンネル名（終了）` |
| `RetryCountDisplay` | string | `RetryCount == 0` なら空文字、それ以外は数値 |
| `StatusDisplay` | string | `(H:MM:SS) 5,444KB` 形式。KB は 1024 で除算、時は桁数無制限 |

`IsActive` を `false` にすると経過時間の基準が停止時刻で固定される。

## 9. 手動操作

チャンネル一覧の右クリックメニュー「録音/録画」は**トグル**。

| 状態 | 動作 | ステータスバー |
|---|---|---|
| 録音中でない | 録音開始 | `録音開始: {チャンネル名}` |
| 録音中 | 録音停止 | `録音停止: {チャンネル名}` |

録画中タブの「停止」ボタンは `ChannelId` を指定して `StopRecording` を呼ぶ。

停止した時点までのバイト列はファイルに残る（削除しない）。
