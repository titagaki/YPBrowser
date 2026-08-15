# 調査: ドキュメント・設定と実装のずれ

作成日: 2026-08-15 / 状態: **未検証（コード読解のみ。実機で再現確認していない）**

仕様書作成時（[`docs/spec/`](../spec/)）にコードを読んで見つかった、
「設定・ドキュメント上は存在するが実装が伴っていない」箇所。
実行して確かめたわけではないため、確定情報として扱わないこと。

---

## 1. 最小フェッチ間隔 4 分が発動しない

### 事実（コードから確認できること）

`AutoRefreshService.DoRefreshAsync` は、判定に使う `YpServerItem` を毎回
`YpServerSettings` から新規生成している。

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

結果として、実際のフェッチ間隔は `BehaviorSettings.RefreshIntervalSeconds`（既定 60 秒・下限 30 秒）と等しい。

### 仮説（未確認）

- YP サーバーごとの状態（`LastUpdateTime` / `LastError` / `ChannelCount`）を保持する意図だったが、
  設定 (`YpServerSettings`) と実行時状態 (`YpServerItem`) の橋渡しが実装されていない
- `LastError` / `ChannelCount` も同じ理由でどこにも表示されていない

### 影響（未検証）

既定設定で各 YP に 60 秒ごとにアクセスする。4 分に 1 回という当初の意図
（[design/decisions.md](../design/decisions.md#なぜ最小フェッチ間隔を-4-分にしたか)）より高頻度。

### 確認すべきこと

- 実際に 60 秒間隔で HTTP リクエストが飛んでいるか（ログまたはパケットで確認）
- `YpServerItem` を Singleton なコレクションとして保持する修正で解決するか

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
| `Behavior.StartMinimized` / `MinimizeToTray` | 参照箇所が無い。トレイアイコン自体が未実装 |
| `Behavior.OpenOnDoubleClick` | 参照箇所が無い。ダブルクリック再生は常に有効 |
| `Behavior.ActiveFilterIndex` | `MainViewModel.ActiveFilterIndex` と接続されておらず、保存も復元もされない |
| `Favorites[].NotifyEnabled` / `SoundFile` / `TextColor` | `FavoriteItem` まで運ばれるが、通知・再生・描画のどこでも使われない |
| `Window.X` / `Y` | 保存も復元もされない |

### 仮説（未確認）

`pcypLite` の設定項目を先に移植し、適用側の実装が追いついていない。
UI から消すか、実装するかの判断が必要。

---

## 3. お気に入り編集が即座に反映されない

### 事実

`FavoritesDialog` の OK は設定を保存するだけで、`FavoriteMatchService.MatchAll()` を呼ばない。
一覧の色分けが変わるのは次の更新サイクル以降。

一方、一覧の右クリック「お気に入りに追加 / 削除」は保存後に `MatchAll()` と再フィルタを行う。

### 確認すべきこと

ダイアログを閉じた直後に再マッチさせるべきか（意図的な差なのか単純な漏れなのか）。

---

## 4. 設定ダイアログのキャンセルが一部しか効かない

### 事実

キャンセルで破棄されるのは、YP サーバー・プレイヤー・自動ダウンロードルールの
**追加 / 削除 / 並べ替え**だけ。各項目のフィールド編集と、表示・ネットワーク・通知・動作・
ダウンロードの各ページは `AppSettings` 配下のオブジェクトを直接書き換えるため、
キャンセルしても値がメモリ上に残る。

さらに、残った値はアプリ終了時の `SaveAsync()`（ウィンドウサイズ保存）で JSON に書き込まれる。
つまり「キャンセルしたはずの変更が次回起動時にも残る」。

詳細は [spec/settings.md](../spec/settings.md#4-設定ダイアログのキャンセル挙動)、
経緯は [design/decisions.md](../design/decisions.md#設定のキャンセルが一部しか効かない)。

### 確認すべきこと

- 実際に「キャンセル → アプリ終了 → 再起動」で値が残るか
- 直すならダイアログを開いた時点でディープコピーを取る方式でよいか

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
