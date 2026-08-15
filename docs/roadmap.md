# ロードマップ / タスク管理

更新日: 2026-08-15

状態: 未着手 / 進行中 / 完了。完了したタスクは 1 行に畳んで「完了」節へ移す。

## 未着手

仕様書作成時に見つかった実装のズレ。**今は手を付けない**方針。
詳細と根拠は [investigations/implementation-gaps.md](investigations/implementation-gaps.md)。

| # | 内容 | 影響 | 参照 |
|---|---|---|---|
| 1 | 最小フェッチ間隔 4 分が発動しない（`YpServerItem` を毎回生成するため `LastUpdateTime` が常に既定値） | 各 YP へ既定 60 秒間隔でアクセスする | [gaps 1](investigations/implementation-gaps.md#1-最小フェッチ間隔-4-分が発動しない) |
| 2 | YP ごとの `LastError` / `ChannelCount` がどこにも残らない・表示されない | 取得失敗をユーザーが気付けない | [gaps 1](investigations/implementation-gaps.md#1-最小フェッチ間隔-4-分が発動しない) |
| 3 | UI で編集できるが未適用の設定が多数（`Display.*` 全項目、`Network.ProxyUrl` / `UserAgent`、`Notifications.BalloonTimeoutSeconds`、`Behavior.StartMinimized` / `MinimizeToTray` / `OpenOnDoubleClick` / `ActiveFilterIndex`、`Favorites[].NotifyEnabled` / `SoundFile` / `TextColor`、`Window.X` / `Y`） | 設定しても何も変わらない | [gaps 2](investigations/implementation-gaps.md#2-ui-で編集できるのに反映されない設定) |
| 4 | お気に入りダイアログの OK 後に再マッチしない | 色分けの反映が次の更新まで遅れる | [gaps 3](investigations/implementation-gaps.md#3-お気に入り編集が即座に反映されない) |
| 5 | 設定ダイアログのキャンセルが一部しか効かない（残った変更は終了時に永続化される） | キャンセルしたはずの変更が残る | [gaps 4](investigations/implementation-gaps.md#4-設定ダイアログのキャンセルが一部しか効かない) |
| 6 | `TypeFilter` に不正な正規表現を入れてもエラー表示がない | 設定ミスに気付けない | [spec/yp-fetch.md](spec/yp-fetch.md#3-サーバー単位のフィルタ) |

3 は「実装する」か「UI と設定項目から消す」かの判断が先に必要。

## 進行中

なし。

## 完了

- 2026-08-15 ドキュメントを仕様 (`docs/spec/`) と設計理由 (`docs/design/`) に分離
