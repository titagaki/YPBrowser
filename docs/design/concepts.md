# 設計: 類似概念の使い分け

似た名前・似た役割の型がなぜ分かれているかを説明する。
各型の項目や値そのものは [`docs/spec/`](../spec/) を参照。

## Rule vs TagDefinition

判定と表示を分けるための 2 つの型。

| | `Rule` | `TagDefinition` |
|---|---|---|
| 責務 | 条件に一致したチャンネルにタグを付ける | 付いたチャンネルをどう見せるか |
| 持つもの | 条件・結合方法・タグ ID・評価順 | 色・既定の扱い・通知・通知音 |
| 参照方向 | タグを **ID で** 参照する | ルールを知らない |

### なぜ分けるか

旧実装では 1 件のお気に入りが `条件 + 色 + 通知 + 除外フラグ` を全部抱えていた。
そのため「通知だけ欲しい」「一時的に NG を見たい」といった要求にルールを増やすしかなく、
「お気に入りリスト」と「無視リスト」を別実装で二重に持つことになっていた。

分けたことで:

- 「無視リスト」という独立した概念が消える（`NG` タグ + 「既定の扱い = 一覧から隠す」で表現される）
- 1 つの条件に複数タグを付けられる
- 「NG を一時的に見る」がビューの切り替えだけで済む
- タグごとの絞り込みビューが自然に作れるので、色を何色も使い分ける必要がなくなる

### なぜ名前ではなく ID で参照するか

タグ名はリネームされうる。名前で参照していると、改名のたびに全ルールを書き換えるか、
書き換え漏れでルールが黙って効かなくなる。ID は不変なので、改名しても参照が壊れない。

### 変換フロー

```
[JSON ファイル]
    ↓ デシリアライズ（Rule / TagDefinition をそのまま永続化）
    ↓ SettingsMigration.Migrate()   ← 旧 Favorites があればここで変換
Rules + Tags
    ↓ TagMatchService.ApplyTags()
ChannelItem.Tags（タグ一覧の並び順）
    ↓ ChannelItem の派生プロパティ
IsHidden / IsHighlighted / IsFavorite / TagBackColor / TagForeColor
```

旧実装にあった「保存用 POCO ↔ 実行時モデル」の 2 段構え（`FavoriteSettings` ↔ `FavoriteItem`）は
やめて 1 種類にした。マッパーの往復で項目を落とすバグが出やすいわりに、
得られるのは Flags 列挙型によるマッチの高速化だけで、チャンネル数が数百では意味がないため。

---

## AutoDownloadRuleSettings vs Rule

どちらも「チャンネルを条件でマッチする」設定だが、目的が違う。

| | `Rule`（タグ方式） | `AutoDownloadRuleSettings` |
|---|---|---|
| マッチ時の動作 | タグを付ける（表示は決めない） | 録音開始 |
| 対象フィールド | 条件ごとに 1 つ（`ConditionField`） | Flags で複数選び、連結して 1 回照合 |
| 条件の数 | 可変・AND/OR・否定あり | 1 つだけ |
| 管理 UI | RulesDialog（独立ウィンドウ） | SettingsDialog 内「ダウンロード」ページ |

### なぜ統合しないか

- タグは「表示をどう変えるか」の概念で、副作用を持たない
- 自動ダウンロードは「特定条件で副作用（録音）を起こす」動作寄りの概念
- 自動ダウンロードをタグ方式に載せるなら「タグが付いたら録音」という別の仕組みが要る。
  今回の作り替えの対象外なので、旧来のルール形式のまま残してある

---

## StreamUrl vs DirectStreamUrl

| | `StreamUrl` | `DirectStreamUrl` |
|---|---|---|
| 経由 | ローカル PeerCast (`localhost:7144`) | 配信元ホストに直接 |
| パス | `/pls/{Id}?tip={Host}` | `/pls/{Id}` |
| `Host` が空の場合 | `?tip=` なしで動く | **空文字列**（使用不可） |
| 使用箇所 | 録音・プレイヤー起動・URL コピー | 現状未使用（予約的） |

### `?tip=` パラメータの意味

PeerCast に対して「このホスト:ポートにあるリレーから取得を試みてほしい」というヒントを渡すパラメータ。
接続先をローカル PeerCast に固定しつつ、P2P 経路のヒントを提供する。

### 落とし穴: `/pls/` は PLS テキストを返す

`StreamUrl` に HTTP GET すると、PeerCast は **PLS テキストファイル**（数百バイト）を返す。
実際のストリームデータを受信するには PLS をパースして `File1=` の URL に再接続が必要。

```
GET http://localhost:7144/pls/{id}?tip=...
→ [playlist]
   File1=http://localhost:7144/stream/{id}?tip=...
   ...

GET http://localhost:7144/stream/{id}?tip=...
→ (実際の音声/映像ストリームデータ)
```

`RecordService` はこの 2 段階解決を `ResolveStreamUrlAsync()` で処理している。

---

## プレイヤーまわりの型

| 型 | 場所 | 役割 |
|---|---|---|
| `PlayerSettings` | `Settings/` | 保存も UI バインディングも実行時もこれ 1 つ |
| `PlayerContentTypes` | `Models/` | 選べるタイプ・「その他」の扱い・並び順 |
| `PlayerPlaceholders` | `Models/` | 引数テンプレートで使える置換子と、その展開 |
| `PlayerPresets` | `Models/` | 編集ダイアログの「設定例」 |
| `PlayerSelection` | `Helpers/` | チャンネルのタイプから使うプレイヤーを選ぶ |

以前は保存用の `PlayerSettings` と実行時の `PlayerItem` に分かれていて、
`MainViewModel` が呼ぶたびに手で詰め替えていた。項目が増えるたびに詰め替えを直す必要があり、
分けている利点も無かったので `PlayerSettings` 1 つにまとめた。

置換子の一覧を `PlayerPlaceholders` に集約したのは、編集ダイアログの説明と実際の展開を
1 か所から作るため。別々に持つと、片方だけ増えて説明と挙動がずれる。

---

## DownloaderSettings と PlayerSettings の違い

| | `PlayerSettings` | `DownloaderSettings` |
|---|---|---|
| 複数登録 | はい（コンテンツタイプごとに 1 件） | いいえ（1つだけ） |
| 外部プロセス起動 | はい | **いいえ**（HttpClient で自前処理） |
| 実行ファイルパス | あり（`ExecutablePath`） | なし |
| 引数テンプレート | あり（`PlayerPlaceholders`） | なし |
| 出力先 | なし | あり（`OutputDirectory`） |
| ファイル名テンプレート | なし | あり（`FileNameTemplate`） |

録音は外部ツール（ffmpeg 等）に依存せず、`HttpClient` でストリームを直接取得して
ファイルに書き込む方式を採用している。

---

## MatchTargetFields（Flags 列挙型）

自動ダウンロードルールのマッチ対象フィールドを表す。`AutoDownloadRuleItem` だけが使う。

タグ方式のルールは条件ごとに 1 フィールドを選ぶ `ConditionField`（チャンネル名 /
ジャンル・詳細・コメント / コンタクトURL / Playing の 4 つ）を使う。こちらの 9 種とは別物で、
YP名・コーデック・曲名はタグ方式の条件では選べない。

```csharp
[Flags]
public enum MatchTargetFields
{
    None        = 0,
    ChannelName = 1 << 0,   // チャンネル名
    Genre       = 1 << 1,   // ジャンル
    Description = 1 << 2,   // 説明
    Comment     = 1 << 3,   // コメント
    ContactUrl  = 1 << 4,   // 連絡先 URL
    YpName      = 1 << 5,   // YP サーバー名
    ChannelType = 1 << 6,   // コーデック（FLV, MP3 等）
    TrackTitle  = 1 << 7,   // 曲名
    TrackArtist = 1 << 8,   // アーティスト
    All         = 511,
}
```

### JSON との往復変換

設定ファイルでは `List<string>` として保存（例: `["ChannelName", "Genre"]`）。
読み込み時に `AutoDownloadSettingsMapper.ParseTargetFields()` で Flags 列挙型に変換する。

```csharp
// None（空リスト）の場合は ChannelName に強制フォールバック
return result == MatchTargetFields.None ? MatchTargetFields.ChannelName : result;
```

フィールドを1つも選ばない状態は UI 上で許可しておらず、
JSON が壊れていた場合のサーフェイスとして ChannelName を使う。

---

## SettingsViewModel vs RulesViewModel vs TagsViewModel

どれも設定を管理する ViewModel だが、別の UI に対応する。

| | `SettingsViewModel` | `RulesViewModel` | `TagsViewModel` |
|---|---|---|---|
| 開く UI | `SettingsDialog` | `RulesDialog` | `TagsDialog` |
| 管理対象 | YP サーバー・プレイヤー・自動ダウンロード | `Rules`（+ 新規タグ） | `Tags` |
| DI ライフタイム | Transient | Transient | Transient |
| 編集対象 | 設定オブジェクトを直接 / 一部は複製 | **複製**（OK でのみ書き戻す） | **複製**（OK でのみ書き戻す） |

### なぜルールとタグで複製を編集するか

設定ダイアログはキャンセルしても一部の変更が残る（[spec/settings.md](../spec/settings.md#4-設定ダイアログのキャンセル挙動)）。
ルールとタグは誤編集の影響が一覧全体に及ぶため、キャンセルで確実に元へ戻せるほうを選んだ。

### なぜルールとタグを別ダイアログにするか

編集の単位が違う。ルールは「条件を書いて試す」作業で、タグは「見た目と扱いを決める」作業。
1 画面に混ぜると、ルール編集中に色をいじれてしまい、どちらを直しているのか分からなくなる。
`RulesDialog` からタグを新規作成できるのは、ルールを書いている途中で
タグ設定へ寄り道させないための例外（名前だけ作り、見た目は後から `TagsDialog` で決める）。

---

## Singleton サービスの状態保持のまとめ

以下のサービスは**状態（記憶）**を持つため Singleton でなければならない。

| サービス | 保持する状態 |
|---|---|
| `ChannelDiffService` | Log チャンネルのキャッシュ（YP 別、1時間有効） |
| `TagMatchService` | コンパイル済み正規表現キャッシュ |
| `AutoDownloadMatchService` | コンパイル済み正規表現キャッシュ |
| `RecordService` | 録音中チャンネルの `CancellationTokenSource` 辞書 |
| `AutoRefreshService` | 定期タイマー |

これらを Transient にすると、状態が毎回リセットされてログが消えたり
正規表現が毎回コンパイルされたりする問題が起きる。
