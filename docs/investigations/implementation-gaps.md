# 調査: ドキュメント・設定と実装のずれ

作成日: 2026-08-15 / 状態: **未検証（コード読解のみ。実機で再現確認していない）**

仕様書作成時（[`docs/spec/`](../spec/)）にコードを読んで見つかった、
「設定・ドキュメント上は存在するが実装が伴っていない」箇所。
実行して確かめたわけではないため、確定情報として扱わないこと。

---

## 1. 最小フェッチ間隔 4 分が発動しない（解消済み・2026-08-16）

### 当時の事実

`AutoRefreshService.DoRefreshAsync` は、判定に使う `YpServerItem` を毎回
`YpServerSettings` から新規生成していた。

```csharp
foreach (var serverSettings in servers.Where(s => s.Enabled))
{
    var server = new YpServerItem { ... };   // LastUpdateTime は既定値 DateTime.MinValue

    if (!force &&
        DateTime.Now - server.LastUpdateTime < MinFetchInterval &&
        server.LastUpdateTime != DateTime.MinValue)   // ← 常に false
        continue;
    ...
}
```

`YpFetchService.FetchAsync` は取得成功時に `server.LastUpdateTime` を更新するが、
その `server` はこのループを抜けた時点で破棄される。

### 現状

ガードを撤去して解消。実アクセス間隔 = ユーザーが設定した更新間隔になり、
YP への負荷は選択肢の下限（60 秒）だけで抑える形にした。

ガードを「直して生かす」のではなく撤去したのは、生かすと別の不具合を生むため。
スキップした YP はチャンネルを 1 件も返さないので、`ChannelDiffService` が
「その YP のチャンネルが全部消えた」と判断してログ送りにしてしまう。
経緯は [design/decisions.md](../design/decisions.md#なぜ更新間隔を設定値どおりにしたか)。

### 積み残し

`YpServerItem` を毎回作り直す構造そのものは変えていない。
`LastError` / `ChannelCount` が残らない問題（本ページ 6）はこれが原因のまま。

---

## 2. UI で編集できるのに反映されない設定

`SettingsDialog` で編集・保存できるが、実行時に読まれていない設定。
一覧は [spec/settings.md](../spec/settings.md#3-全設定項目) の「適用」列を参照。

| 設定 | 状況（コード上の事実） |
|---|---|
| `Display.*` 全 7 項目 | 一覧の描画は XAML と `ChannelDiffToColorConverter` の固定値。設定を読む箇所が無い |
| `Network.ProxyUrl` | 参照箇所が無い |
| `Network.UserAgent` | `App.xaml.cs` に同じ文字列がハードコードされている |
| `Notifications.BalloonTimeoutSeconds` | 参照箇所が無い（トーストの表示時間は OS 任せ） |
| `Behavior.ActiveFilterIndex` | ビュー選択と接続されておらず、保存も復元もされない |
| `Notifications.SoundEnabled` / `SoundFile` | 参照箇所が無い。通知音はタグごとの `SoundPath` が使われる |
| `Window.X` / `Y` | 保存も復元もされない |

### 仮説（未確認）

設定項目だけが先に増え、適用側の実装が追いついていない。
UI から消すか、実装するかの判断が必要。

---

## 3. お気に入り編集が即座に反映されない（解消済み・2026-08-15）

### 当時の事実

`FavoritesDialog` の OK は設定を保存するだけで、`FavoriteMatchService.MatchAll()` を呼ばなかった。
一覧の色分けが変わるのは次の更新サイクル以降だった。

### 現状

タグ方式への作り替えで解消。`RulesDialog` / `TagsDialog` を OK で閉じると
`MainViewModel.ReapplyTags()` が走り、その場で再判定・ビュー欄の再構築・再フィルタを行う。

---

## 4. 設定ダイアログのキャンセルが一部しか効かない（解消済み・2026-08-16）

### 当時の事実

キャンセルで破棄されるのは、YP サーバー・プレイヤー・自動ダウンロードルールの
**追加 / 削除 / 並べ替え**だけ。各項目のフィールド編集と、表示・ネットワーク・通知・動作・
ダウンロードの各ページは `AppSettings` 配下のオブジェクトを直接書き換えるため、
キャンセルしても値がメモリ上に残った。

さらに、残った値はアプリ終了時の `SaveAsync()`（ウィンドウサイズ保存）で JSON に書き込まれた。
つまり「キャンセルしたはずの変更が次回起動時にも残る」。

### 現状

設定ダイアログを「複製を編集して OK で書き戻す」方式に変えたため解消。
キャンセルはどのページの変更も残さない。
理由は [design/decisions.md](../design/decisions.md#なぜ設定ダイアログを複製の編集にしたか)。

---

## 5. ドキュメント側にあった誤り（対応済み）

仕様書作成時に修正したもの。再発防止のため記録だけ残す。

| 誤っていた記述 | 実際 |
|---|---|
| `PlayerSettings.UsePlaylistFile` がある | コミット `e2208e9` で削除済み。プレイリストファイルは生成しない |
| プレイヤー引数テンプレートに `{file}` が使える | `{url}` のみ |
| `AutoRefreshService` が YP ごとの `LastUpdateTime` を保持する | 保持していない（本ページ 1） |
| `RefreshNowAsync(force: true)` という呼び出し | 引数なし `RefreshNowAsync()`。常に force 相当 |
| `ActiveFilterIndex` で前回のフィルタが復元される | 復元されない（本ページ 2） |

---

## 6. YP ごとの `LastError` / `ChannelCount` がどこにも残らない（解消済み・2026-08-16）

もとは本ページ 1 の一部だった。1 のガードを撤去した時点では、原因である
「実行時状態の置き場が無い」構造が残っていたので、いったんこちらへ切り出した。

### 当時の事実

`YpServerItem` は 3 つの実行時状態を持ち、`YpFetchService.FetchAsync` はそこへ書き込んでいた。

```csharp
server.LastUpdateTime = DateTime.Now;
server.LastError = null;
server.ChannelCount = channels.Count;
// catch 節では server.LastError = ex.Message;
```

しかし書き込み先の `YpServerItem` は `DoRefreshAsync` のループ内で毎回 `new` されるため、
ループを抜けた時点で破棄されていた。UI から参照している箇所も無かった。

さらにタイムアウトは `OperationCanceledException` として飛ぶため、
`LastError` を設定しない側の `catch` に落ちていた（`LogDebug` が出るだけ）。
一番起きやすい失敗が、一番記録に残らない状態だった。

### 現状

`IYpServerStateService` を置き場として追加し、`AutoRefreshService` はそこから
`YpServerItem` を引くようにした（URL + ホストで引くので改名では失われない）。
タイムアウトは呼び出し側のキャンセルと別の `catch` で拾い、理由を `LastError` に残す。

表示は設定の Yellow Pages ページ。カード行の 3 段目に
「21:32:05 更新 ・ 63 件」「取得できません: ...」を出す。

実機で確認済み。3 つの既定 YP について、実際の取得時刻と件数が出ることを画面で確かめた。
失敗時の赤字表示は、同じブラシを使う `YpServerEditDialog` の検証エラーで確認している
（YP を落として出す確認まではしていない）。
