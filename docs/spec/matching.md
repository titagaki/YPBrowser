# 仕様: お気に入り・NG・自動ダウンロードのマッチング

対象: `FavoriteMatchService`, `AutoDownloadMatchService`, `FavoriteTargetFields`,
`FavoriteSettingsMapper`, `AutoDownloadSettingsMapper`

お気に入りルールと自動ダウンロードルールは、同じマッチエンジン（対象フィールド結合 → 部分一致 or 正規表現）を使う。

## 1. 対象フィールド `FavoriteTargetFields`

```csharp
[Flags]
None = 0, ChannelName = 1, Genre = 2, Description = 4, Comment = 8,
ContactUrl = 16, YpName = 32, ChannelType = 64, TrackTitle = 128, TrackArtist = 256,
All = 511
```

`ChannelItem` の対応プロパティは同名。9 種すべてがお気に入り・自動ダウンロードの両方で使える。

### JSON との変換

設定ファイルでは `List<string>`（例: `["ChannelName", "Genre"]`）。読み込み時に `Enum.TryParse` で
フラグへ変換し、解釈できない文字列は無視する。結果が `None`（空リスト・全て不正）の場合は
`ChannelName` にフォールバックする。

## 2. 判定

### 対象テキストの構築

選択されたフィールドを**列挙順**（上記の宣言順）に取り出し、空でないものを半角スペースで連結する。
結果が空文字ならマッチしない。

### 一致判定

| `IsRegex` | 判定方法 |
|---|---|
| `false` | `text.Contains(Word, OrdinalIgnoreCase)`（部分一致・大文字小文字を無視） |
| `true` | `Regex(Word, IgnoreCase \| Compiled).IsMatch(text)` |

正規表現はパターン文字列をキーにサービス内でキャッシュされる。
不正なパターンは `null` としてキャッシュされ、そのルールは**常に不一致**になる（例外は出ない）。

### 評価対象外のルール

`Enabled == false` または `Word` が空のルールはスキップする。

## 3. お気に入り／NG の適用（`MatchAll`）

各チャンネルについて、まず状態をリセットする。

```
IsFavorite = false, IsNG = false, FavBackColor = null, FavTextColor = null, FavPriority = -1
```

続いてお気に入りルールを**リストの先頭から順に**評価する。

| 状況 | 動作 |
|---|---|
| `IsNG` のルールに一致 | `IsNG = true` にして**そのチャンネルの評価を打ち切る**（NG が最優先） |
| NG でないルールに最初に一致 | `IsFavorite = true`、`FavPriority = ルールのインデックス`、色を設定 |
| 2 件目以降の非 NG 一致 | 何もしない（色・優先度は最初の一致が勝つ） |

NG ルールがお気に入りルールより後ろにあっても、一致すれば `IsNG = true` になる
（`IsFavorite` は先に true になったままだが、表示フィルタは NG を優先して扱う）。

### 色のパース

`#RRGGBB` 形式（`#` は任意、6 桁固定）のみ受け付ける。それ以外・パース失敗時は `null`（色指定なし）。

### 新着お気に入りの抽出

```csharp
GetNewFavoriteChannels() = channels.Where(IsFavorite && !IsNG && Diff == New)
```

## 4. 自動ダウンロードの適用

```csharp
GetChannelsToAutoDownload() = channels.Where(Diff == New && rules.Any(r => Match(ch, r)))
```

| 項目 | 仕様 |
|---|---|
| 対象 | `Diff == New` のチャンネルのみ |
| 複数ルール一致 | 1 回だけ録音を開始する（`Any()` で判定） |
| 実行タイミング | `OnRefreshCompleted` 内、`MatchAll` の後 |
| 初回フェッチ | スキップ |
| ルール 0 件 | 評価自体を行わない |
| 録音中のチャンネル | `RecordService` 側で二重開始を防止（[recording.md](recording.md#2-開始と停止)） |

自動ダウンロードルールには NG・通知・色の概念がない（`Title` / `Word` / `TargetFields` / `IsRegex` / `Enabled` のみ）。

## 5. 未適用の設定

| 設定 | 状況 |
|---|---|
| `FavoriteSettings.NotifyEnabled` | `FavoriteItem` まで運ばれるが、通知の判定に使われない。通知の有無は `Notifications.Enabled` と `Behavior.NotifyOnFavorite` のみで決まる |
| `FavoriteSettings.SoundFile` | 運ばれるが再生されない |
| `FavoriteItem.FavPriority` | 設定されるが、表示順は「お気に入りか否か」と視聴者数のみで決まる |
