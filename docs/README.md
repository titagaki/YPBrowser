# YPBrowser ドキュメント

「何をするか（仕様）」と「なぜそうしたか（設計）」を分けて置く。

| ディレクトリ | 内容 | 書くもの / 書かないもの |
|---|---|---|
| [spec/](spec/) | 仕様書 | 実装の事実（値・書式・条件・手順）のみ。理由は書かない |
| [design/](design/) | 設計ノート | 判断の理由・背景・トレードオフ。仕様の再掲はしない |
| [investigations/](investigations/) | 未検証の調査 | 事実と仮説を明記して分ける。検証できたら spec / design へ移す |

タスクの進捗は [roadmap.md](roadmap.md) で管理する。
既知の実装のズレ（未着手）は [investigations/implementation-gaps.md](investigations/implementation-gaps.md) にまとまっている。

## 仕様（spec/）

| ファイル | 範囲 |
|---|---|
| [spec/yp-fetch.md](spec/yp-fetch.md) | YP からの取得、index.txt の 19 フィールド、フィルタ、更新サイクル |
| [spec/channel-diff.md](spec/channel-diff.md) | 差分判定（New/Up/Down/Changed/Log）、ログの保持 |
| [spec/matching.md](spec/matching.md) | タグ判定・ルール・旧形式からの移行・自動ダウンロードのマッチング |
| [spec/recording.md](spec/recording.md) | 録音、PLS/M3U 解決、再試行、進捗、ファイル名 |
| [spec/settings.md](spec/settings.md) | 設定の全項目・既定値・適用状況 |
| [spec/ui.md](spec/ui.md) | 画面構成、表示規則、操作、起動・終了 |

## 設計（design/）

| ファイル | 内容 |
|---|---|
| [design/architecture.md](design/architecture.md) | MVVM + DI の理由、レイヤー、ライフタイム |
| [design/decisions.md](design/decisions.md) | 個別の判断（ログ 1 時間、初回スキップ、録音の自前実装ほか） |
| [design/concepts.md](design/concepts.md) | 似た型の使い分け（`Rule` と `TagDefinition` など） |

## よくある疑問への答え

**Q. ルールとタグは何が違う？**
→ [design/concepts.md](design/concepts.md#rule-vs-tagdefinition)。ルールはタグを付けるだけで、色・通知・非表示はタグ側の属性。

**Q. 前のバージョンのお気に入りはどうなる？**
→ 初回起動時に自動でタグ + ルールへ移行される。[spec/matching.md](spec/matching.md#8-旧お気に入り形式からの移行-settingsmigration)

**Q. NG にしたチャンネルの中身を見たい**
→ 左のビュー欄で `NG` タグを選ぶ。または一覧下端のバーの「表示する」。[spec/ui.md](spec/ui.md#32-隠す判定)

**Q. `StreamUrl` に GET したのに短いファイルしか保存されない**
→ [spec/recording.md](spec/recording.md#3-ストリーム-url-の解決)。`/pls/` は PLS テキストを返す。

**Q. 起動直後に全チャンネルが「新着」になるのはなぜ？**
→ [spec/channel-diff.md](spec/channel-diff.md#初回フェッチ) と
[design/decisions.md](design/decisions.md#なぜ初回フェッチで自動ダウンロードだけスキップするか)

**Q. 設定ダイアログでキャンセルしても変更が戻らない設定がある**
→ [spec/settings.md](spec/settings.md#4-設定ダイアログのキャンセル挙動)

**Q. 設定を変えても表示が変わらない**
→ 未適用の設定がある。[spec/settings.md](spec/settings.md#3-全設定項目) の「適用」列と
[investigations/implementation-gaps.md](investigations/implementation-gaps.md) を参照。
