# 仕様: タグ判定・ルール・自動ダウンロードのマッチング

対象: `TagMatchService`, `TagDefinition`, `Rule`, `RuleCondition`, `ChannelViewItem`,
`AutoDownloadMatchService`, `MatchTargetFields`, `AutoDownloadSettingsMapper`, `SettingsMigration`

## 1. 全体の流れ

```
index.txt 取得（前回との差分を検出）
  → 判定（ルールを上から順に評価）
  → タグ付与（チャンネルにタグを付けるだけ。表示は決めない）
  → ビュー（タグを見て、色・通知・非表示を決める）
```

ルールは**タグを付けるところまで**しか行わない。色・通知・非表示はすべてタグ側の属性。

## 2. タグ `TagDefinition`

| 項目 | 型 | 内容 |
|---|---|---|
| `Id` | string | 不変の内部 ID。ルールは必ずこれで参照する |
| `Name` | string | 表示名。変更してもルール側の参照は壊れない |
| `ForeColor` / `BackColor` | string? | `#RRGGBB`。null で色指定なし |
| `DefaultAction` | enum | `Normal` / `Highlight` / `Hidden` |
| `Notify` | bool | 新着時に通知する |
| `SoundPath` | string? | 通知音。null / 空 / 存在しないパスなら既定音 |
| `BuiltIn` | bool | 削除できない |

### 組み込みタグ

| `Id` | `Name` | `DefaultAction` | `Notify` | 備考 |
|---|---|---|---|---|
| `builtin-favorite` | お気に入り | `Highlight` | `true` | 星ボタンが付与する |
| `builtin-ng` | NG | `Hidden` | `false` | |

設定読み込み時に存在しなければ自動で追加される。`BuiltIn` は ID から毎回付け直される。

## 3. ルール `Rule`

| 項目 | 型 | 内容 |
|---|---|---|
| `Id` | string | |
| `Name` | string | |
| `Enabled` | bool | false のルールは評価しない |
| `Order` | int | 小さいほど先に評価 |
| `Combinator` | enum | `And`（すべて満たす）/ `Or`（いずれか満たす） |
| `Conditions` | `RuleCondition[]` | 0 件のルールは**常に不一致** |
| `TagIds` | string[] | 付与するタグの ID |
| `StopProcessing` | bool | 一致したらそのチャンネルの評価を打ち切る |
| `IsAuto` | bool | 星ボタンが自動生成したルール |

### 条件 `RuleCondition`

| 項目 | 型 | 内容 |
|---|---|---|
| `Field` | enum | 下表 |
| `MatchType` | enum | `Regex`（既定）/ `Contains` / `Exact` |
| `Negate` | bool | 判定結果を反転（「不一致」） |
| `Pattern` | string | 空文字は**常に不一致** |

| `Field` | UI の表示名 | 参照するもの |
|---|---|---|
| `ChannelName` | チャンネル名 | `ChannelName` |
| `Description` | ジャンル/詳細/コメント | `Genre` / `Description` / `Comment` を空要素を除き半角スペースで連結 |
| `ContactUrl` | コンタクトURL | `ContactUrl` |
| `TrackArtist` | Playing | `TrackArtist`（index.txt の 11 番目） |

コンボボックスもこの順に並ぶ。

`TrackArtist` を「Playing」と呼ぶのは、この欄にアーティスト名だけでなく
配信中の曲名や配信経路が入ってくるため。実例:

```
も＠ｃｈ<>4285FAA2…<>49.212.151.50:7152<>https://…<> 自由<>&lt;Free&gt;<>-1<>-1<>1296<>FLV
  <>210.157.193.184 via Peercast Gateway<><><><>…
                                    ↑ 11 番目 = Playing
```

### 一致判定

| `MatchType` | UI の表示名 | 判定方法 |
|---|---|---|
| `Contains` | 部分一致 | `text.Contains(Pattern, OrdinalIgnoreCase)` |
| `Exact` | 完全一致 | `string.Equals(text, Pattern, OrdinalIgnoreCase)` |
| `Regex` | 正規表現 | `Regex(Pattern, IgnoreCase \| Compiled).IsMatch(text)` |

コンボボックスもこの順に並ぶ（正規表現が最下段）。既定値は `Regex`。

正規表現はパターン文字列をキーにサービス内でキャッシュされる。
不正なパターンは `null` としてキャッシュされ、その条件は**常に不一致**になる（例外は出ない）。

`ValidatePattern()` は `MatchType == Regex` のときだけ検証し、不正なら例外メッセージを返す。

## 4. 判定アルゴリズム（`ApplyTags`）

```
foreach channel in channels:
    matched = []
    foreach rule in rules.where(Enabled).orderBy(Order):
        if evaluate(rule, channel):
            matched.addRange(rule.TagIds)   // 重複は足さない
            if rule.StopProcessing: break
    channel.Tags = matched
        .where(タグ一覧に実在するもの)       // 消されたタグの ID は捨てる
        .orderBy(タグ一覧での並び順)
```

`evaluate` は `Combinator` に従って条件を集約し、`Negate` は各条件の結果を反転する。

`channel.Tags` は毎回まるごと差し替わるので、一致しなくなったタグは自動的に消える。
**タグ一覧の並び順にそろえてある**ので、色を決めるときに「最初のもの」を使える。

## 5. 表示への反映

`ChannelItem` の派生プロパティ。ルールもタグ判定もここには関与しない。

| プロパティ | 定義 |
|---|---|
| `IsNew` | `Diff == New`（前回ポーリングに存在しなかった） |
| `IsHidden` | `Hidden` のタグを 1 つでも持つ |
| `IsHighlighted` | `Highlight` のタグを 1 つでも持つ |
| `IsFavorite` | `builtin-favorite` を持つ |
| `TagBackColor` / `TagForeColor` | 色が設定されている**最初の**タグの色（タグ一覧の並び順で決まる） |

## 6. 通知（`GetChannelsToNotify`）

```csharp
channels.Where(IsNew && !IsHidden && Tags.Any(t => t.Notify))
```

`Hidden` のタグが付いたチャンネルは、通知タグも持っていても通知しない。
トーストの見出しと通知音は「通知が有効なタグのうち先頭のもの」から取る。

通知音が指定されていれば `SoundPlayer` で再生し、トースト側は無音にする
（パッケージ化されていない Win32 アプリでは、トーストの `audio src` に任意のパスを渡せないため）。

全体のオン・オフは `Notifications.Enabled` と `Behavior.NotifyOnFavorite` が握っている。

## 7. 星ボタン（自動生成ルール）

一覧の星をオンにすると、以下のルールを生成して保存し、その場で再判定する。

| 項目 | 値 |
|---|---|
| `Name` | チャンネル名 |
| `Conditions` | 1 件（`ChannelName` / `Exact` / チャンネル名） |
| `TagIds` | `[builtin-favorite]` |
| `IsAuto` | `true` |
| `Order` | 既存ルールの最大 + 1 |

**必ず `Exact`。`Regex` にしない**（`いまいch` を正規表現として評価すると誤爆する）。

オフにすると `IsStarRuleFor(チャンネル名)` を満たすルールをすべて削除する。判定は
「`IsAuto` かつ お気に入りタグを持ち、条件が `ChannelName` / `Exact` / 非 `Negate` の 1 件だけで、
パターンがそのチャンネル名と一致」。手で書いたルールは `IsAuto` が false なので消えない。

そのため、手書きルールがお気に入りタグを付けているチャンネルは、星をオフにしても星が消えない。

## 8. 旧「お気に入り」形式からの移行（`SettingsMigration`）

設定読み込み時に 1 回だけ走り、変換したらすぐ保存し直す。
`Rules` がすでに 1 件以上あるときは、旧データが残っていても取り込まない（二重取り込みの防止）。

| 旧 `FavoriteSettings` | 新しい形 |
|---|---|
| `Title`（空なら `Word`） | `Rule.Name` |
| `Word` | 各条件の `Pattern`。**空のものは移行しない**（旧実装でも評価されていない） |
| `Enabled` | `Rule.Enabled` |
| 並び順 | `Rule.Order`（0 から連番） |
| `TargetFields` | フィールドごとに 1 条件 + `Combinator = Or` |
| `IsRegex` | `true` → `Regex` / `false` → `Contains` |
| `IsNG == true` | `TagIds = [builtin-ng]` |
| `IsNG == false` | `TagIds = [色タグ, builtin-favorite]` |
| `BackColor` / `TextColor` | 色だけを持つタグへ移す |
| `NotifyEnabled` / `SoundFile` | 移行しない（通知は組み込みタグが担当する） |

`Genre` / `Description` / `Comment` はいずれも `Description` 条件になり、重複は 1 つにまとめる。
`TrackArtist` は `TrackArtist`（Playing）へそのまま移行する。

`YpName` / `ChannelType` / `TrackTitle` は条件のフィールドから外したので移行先が無い。
指定されていても**その条件だけを捨てる**。他のフィールドも指定されていればそちらは残る。
移行先の無いフィールドしか指定されていなかった場合と、解釈できないフィールド名しか無い場合は
`ChannelName` にフォールバックする（条件 0 件のルールは何にも一致しないため）。

### 色タグ

旧形式では色が 1 件ごとの属性だったので、そのままでは お気に入りの数だけタグができる。
**`(BackColor, TextColor)` が同じものは 1 つのタグにまとめ**、名前は最初に使った `Title` を使う。
`DefaultAction = Normal`・`Notify = false` とし、タグ一覧の**先頭**（組み込みタグより前）に置く。

- 「お気に入りビューに出る」性質と通知は組み込みタグが引き継ぐ
- 行の色は色タグが勝つ（並び順で先にあるため）
- 通知を色タグ側でも有効にすると二重に鳴るので、必ず `false`

両方の色が不正・空なら色タグは作らず、組み込みタグだけを付ける。

移行後は `Favorites` を空にする（保存時に設定ファイルから消える）。

## 9. 自動ダウンロードの適用

自動ダウンロードはタグ方式に含めず、独立したルール（`AutoDownloadRuleSettings`）のまま。

```csharp
GetChannelsToAutoDownload() = channels.Where(Diff == New && rules.Any(r => Match(ch, r)))
```

| 項目 | 仕様 |
|---|---|
| 対象 | `Diff == New` のチャンネルのみ |
| 対象フィールド | `MatchTargetFields`（Flags）。選択されたフィールドを列挙順に半角スペースで連結して 1 回照合 |
| 判定 | `IsRegex` で `Regex` / `Contains` を切り替え（大文字小文字は無視） |
| 複数ルール一致 | 1 回だけ録音を開始する（`Any()` で判定） |
| 実行タイミング | `OnRefreshCompleted` 内、`ApplyTags` の後 |
| 初回フェッチ | スキップ |
| ルール 0 件 | 評価自体を行わない |
| 録音中のチャンネル | `RecordService` 側で二重開始を防止（[recording.md](recording.md#2-開始と停止)） |

`Enabled == false` または `Word` が空のルールはスキップする。

## 10. 未適用の設定

| 設定 | 状況 |
|---|---|
| `Notifications.SoundFile` | タグごとの `SoundPath` が使われるため参照されない |
| `Display.FavoriteNameColor` | 行の色はタグ側の設定で決まるため参照されない |
