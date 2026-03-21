# 類似概念の使い分けガイド

## FavoriteSettings vs FavoriteItem

同じ「お気に入りルール」を表すが、用途が違う。

| | `FavoriteSettings` | `FavoriteItem` |
|---|---|---|
| 場所 | `Settings/` | `Models/` |
| 基底クラス | 普通の POCO | `ObservableObject` |
| 用途 | JSON 保存・読み込み | マッチング処理・UI バインディング |
| `TargetFields` の型 | `List<string>` | `FavoriteTargetFields`（Flags 列挙型） |
| 生成方法 | 直接 new / JSON デシリアライズ | `FavoriteSettingsMapper.ToFavoriteItems()` で変換 |

### なぜ分けるか

- `FavoriteSettings` は `System.Text.Json` でそのままシリアライズできる単純な型
- `FavoriteItem` は `FavoriteTargetFields` Flags 列挙型で高速なビット演算マッチングができる
- UI バインディング（`ObservableObject`）が必要なのはメモリ上のモデルだけ

### 変換フロー

```
[JSON ファイル]
    ↓ デシリアライズ
FavoriteSettings（POCO）
    ↓ FavoriteSettingsMapper.ToFavoriteItems()
FavoriteItem（ObservableObject）
    ↓ FavoriteMatchService.MatchAll()
ChannelItem に IsFavorite / FavBackColor / FavTextColor を設定
```

---

## AutoDownloadRuleSettings vs FavoriteSettings

どちらも「チャンネルを条件でマッチする」設定だが、目的が違う。

| | `FavoriteSettings` | `AutoDownloadRuleSettings` |
|---|---|---|
| マッチ時の動作 | 色付け + 通知 | 録音開始 |
| NG フラグ | あり（`IsNG`） | なし |
| 通知フラグ | あり（`NotifyEnabled`） | なし |
| 表示色 | あり（`BackColor`, `TextColor`） | なし |
| 管理 UI | FavoritesDialog（独立ウィンドウ） | SettingsDialog 内「ダウンロード」ページ |

ルールのマッチエンジン（テキスト検索・正規表現）は同一ロジック。
`FavoriteTargetFields` 列挙型を両方で共用している。

### なぜ統合しないか

- お気に入りは「色付けして目立たせる」表示寄りの概念
- 自動ダウンロードは「特定条件で副作用を起こす」動作寄りの概念
- 将来的に自動ダウンロードを「停止/上書き」するNG的ルールが必要になる可能性があるため、設計上分離しておく

---

## StreamUrl vs DirectStreamUrl

| | `StreamUrl` | `DirectStreamUrl` |
|---|---|---|
| 経由 | ローカル PeerCast (`localhost:7144`) | 配信元ホストに直接 |
| パス | `/pls/{Id}?tip={Host}` | `/pls/{Id}` |
| `Host` が空の場合 | `?tip=` なしで動く | **空文字列**（使用不可） |
| 使用箇所 | プレイリスト生成・録音・プレイヤー起動 | 現状未使用（予約的） |

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

## PlayerSettings vs PlayerItem

| | `PlayerSettings` | `PlayerItem` |
|---|---|---|
| 場所 | `Settings/` | `Models/` |
| 基底クラス | POCO | `ObservableObject` |
| 用途 | JSON 保存 | 設定 UI バインディング・プレイヤー起動時の引数 |

`PlayerLaunchService.Launch()` は `PlayerItem` を受け取る。
`MainViewModel` が `PlayerSettings` から都度 `PlayerItem` を手動で構築している。

```csharp
// MainViewModel.OpenChannel() より
var playerModel = new PlayerItem
{
    Name             = defaultPlayer.Name,
    ExecutablePath   = defaultPlayer.ExecutablePath,
    ArgumentTemplate = defaultPlayer.ArgumentTemplate,
    UsePlaylistFile  = defaultPlayer.UsePlaylistFile,
};
_playerService.Launch(channel, playerModel);
```

FavoriteSettings → FavoriteItem のような専用 Mapper は存在しない（手動変換）。

---

## DownloaderSettings と PlayerSettings の違い

| | `PlayerSettings` | `DownloaderSettings` |
|---|---|---|
| 複数登録 | はい（リスト） | いいえ（1つだけ） |
| 外部プロセス起動 | はい | **いいえ**（HttpClient で自前処理） |
| 実行ファイルパス | あり（`ExecutablePath`） | なし |
| 引数テンプレート | あり（`{url}`, `{file}`） | なし |
| 出力先 | なし | あり（`OutputDirectory`） |
| ファイル名テンプレート | なし | あり（`FileNameTemplate`） |

録音は外部ツール（ffmpeg 等）に依存せず、`HttpClient` でストリームを直接取得して
ファイルに書き込む方式を採用している。

---

## FavoriteTargetFields（Flags 列挙型）

マッチ対象フィールドを表す。`FavoriteItem` と `AutoDownloadRuleItem` の両方で共用。

```csharp
[Flags]
public enum FavoriteTargetFields
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
読み込み時に `FavoriteSettingsMapper.ParseTargetFields()` で Flags 列挙型に変換する。

```csharp
// None（空リスト）の場合は ChannelName に強制フォールバック
return result == FavoriteTargetFields.None ? FavoriteTargetFields.ChannelName : result;
```

フィールドを1つも選ばない状態は UI 上で許可しておらず、
JSON が壊れていた場合のサーフェイスとして ChannelName を使う。

---

## SettingsViewModel vs FavoritesViewModel

どちらも設定を管理する ViewModel だが、別の UI に対応する。

| | `SettingsViewModel` | `FavoritesViewModel` |
|---|---|---|
| 開く UI | `SettingsDialog` | `FavoritesDialog` |
| 管理対象 | YP サーバー・プレイヤー・自動ダウンロード | お気に入りルール |
| DI ライフタイム | Transient | Transient |
| 保存方式 | `SaveAsync()` で全項目まとめて保存 | `SaveAsync()` で全項目まとめて保存 |

### なぜ FavoritesDialog を分けるか

お気に入りルールは設定項目が多く（対象フィールド 9 種・色設定等）、
`SettingsDialog` に詰め込むと 1 ページが大きくなりすぎるため、独立ダイアログにした。
将来的に `SettingsDialog` の「ダウンロード」ページと統合する余地はある。

---

## Singleton サービスの状態保持のまとめ

以下のサービスは**状態（記憶）**を持つため Singleton でなければならない。

| サービス | 保持する状態 |
|---|---|
| `ChannelDiffService` | Log チャンネルのキャッシュ（YP 別、1時間有効） |
| `FavoriteMatchService` | コンパイル済み正規表現キャッシュ |
| `AutoDownloadMatchService` | コンパイル済み正規表現キャッシュ |
| `RecordService` | 録音中チャンネルの `CancellationTokenSource` 辞書 |
| `AutoRefreshService` | 定期タイマー・各 YP の `LastUpdateTime` |

これらを Transient にすると、状態が毎回リセットされてログが消えたり
正規表現が毎回コンパイルされたりする問題が起きる。
