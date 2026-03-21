# YP データ形式と取得処理

## index.txt のフォーマット

PeerCast Yellow Pages サーバーは `index.txt` を `<>` 区切り 19 フィールドで配信する。

```
チャンネル名<>ID<>Host:Port<>ContactURL<>ジャンル<>説明<>視聴者数<>リレー数
<>Kbps<>コーデック<>アーティスト<>アルバム<>曲名<>曲ジャンル<>URLParam
<>放送時間<>きゃすこステータス<>コメント<>isDirect(0/1)
```

### フィールド番号と対応するプロパティ

| # | フィールド | `ChannelItem` プロパティ | 備考 |
|---|---|---|---|
| 0 | チャンネル名 | `ChannelName` | HTML エンティティあり |
| 1 | チャンネル ID | `Id` | 16 進数 32 桁 |
| 2 | Host:Port | `Host` | リレーホスト（空のこともある） |
| 3 | ContactURL | `ContactUrl` | |
| 4 | ジャンル | `Genre` | |
| 5 | 説明 | `Description` | HTML エンティティあり |
| 6 | 視聴者数 | `Listeners` | -1 = 不明 |
| 7 | リレー数 | `Relays` | |
| 8 | Kbps | `BitrateKbps` | |
| 9 | コーデック | `ChannelType` | MP3, FLV, OGG 等 |
| 10 | アーティスト | `TrackArtist` | |
| 11 | アルバム | `TrackAlbum` | |
| 12 | 曲名 | `TrackTitle` | |
| 13 | 曲ジャンル | `TrackGenre` | |
| 14 | URLParam | `UrlParam` | 使用頻度低い |
| 15 | 放送時間 | `BroadcastTimeStr` | 文字列のまま保持 |
| 16 | きゃすこステータス | `KyasukoStatus` | |
| 17 | コメント | `Comment` | HTML エンティティあり |
| 18 | isDirect | `IsDirect` | "1" = 直接配信 |

### 落とし穴: 19 フィールド未満の行

フィールド数が 19 未満の行は**全件スキップ**される。
一部のフィールドだけ取得する「部分パース」はない。
YP サーバーの実装差異で稀に発生する。

### HTML エンティティの処理

チャンネル名・説明・コメントには HTML エンティティが含まれることがある。
`HtmlSpecialCharsHelper.Decode()` で変換してから `ChannelItem` に格納する。

```
&amp;  → &
&lt;   → <
&gt;   → >
&quot; → "
```

### BOM 処理

一部の YP サーバーは UTF-8 BOM を付与して `index.txt` を配信する。
取得直後に `TrimStart('\uFEFF')` で除去する。
処理しないと最初の行のパースが失敗する。

---

## YpFetchService のフィルタリング

index.txt の全チャンネルを取得してから、`YpServerSettings` の条件でフィルタする。

### ビットレートフィルタ

```csharp
if (server.BitrateMin > 0 && ch.BitrateKbps < server.BitrateMin) continue;
if (server.BitrateMax > 0 && ch.BitrateKbps > server.BitrateMax) continue;
```

- `BitrateMin = 0` はフィルタなし（デフォルト）
- `BitrateMax = -1` はフィルタなし（デフォルト）

### コーデックフィルタ（正規表現）

```csharp
// TypeFilter デフォルト値: ".*"（全許可）
// 例: "^(MP3|FLV|OGG)$" で特定コーデックのみ
```

無効な正規表現が設定された場合は **例外を catch して黙殺**し、
フィルタなし（全許可）として動作する。設定画面でのバリデーションは行っていない。

---

## YP サーバーの YpHost 設定

各 `YpServerSettings` には `Host` フィールド（ローカル PeerCast のアドレス）がある。

```
Host = "localhost:7144"  （デフォルト）
```

複数の PeerCast インスタンスを起動している環境（例: ポート違い）や、
リモートの PeerCast を参照したい場合に変更する。

`ChannelItem.StreamUrl` 生成時に `YpHost` が使われる:
```csharp
var localHost = string.IsNullOrEmpty(YpHost) ? "localhost:7144" : YpHost;
return $"http://{localHost}/pls/{Id}{tip}";
```

---

## チャンネル ID の性質

PeerCast のチャンネル ID は 32 桁の 16 進数文字列（例: `F5D7BABEBC6B4E10254BE4AACCAF7846`）。

- 同一チャンネルなら YP が変わっても **同じ ID** を持つことが多い
- `ChannelDiffService` は **ID** でチャンネルを同定する
- ただし異なる YP で同じ ID が登録される可能性はほぼないため衝突は実用上発生しない
