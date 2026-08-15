# YPBrowser 仕様書

実装が「何をするか」だけを記述する。**なぜそうしたか（設計理由・背景・経緯）は書かない**。
理由は [`docs/design/`](../design/) を参照。

記述はすべて現在の実装 (`src/YPBrowser/`) に基づく事実。
実装されていない設定・機能は「未適用」「未実装」と明記する。

## 一覧

| ファイル | 範囲 |
|---|---|
| [yp-fetch.md](yp-fetch.md) | YP サーバーからの取得、index.txt パース、フィルタ、更新サイクル |
| [channel-diff.md](channel-diff.md) | チャンネルの差分判定、ログ（消滅チャンネル）管理 |
| [matching.md](matching.md) | お気に入り・NG・自動ダウンロードルールのマッチング |
| [recording.md](recording.md) | 録音（HTTP ストリーム保存）、URL 解決、再試行、進捗 |
| [settings.md](settings.md) | 設定ファイルの全項目・既定値・適用状況 |
| [ui.md](ui.md) | 画面構成、表示規則、操作、起動・終了時の動作 |

## 用語

| 用語 | 意味 |
|---|---|
| YP | Yellow Pages。チャンネル一覧 (`index.txt`) を配信するサーバー |
| チャンネル | YP に掲載された配信 1 件。`ChannelItem` で表す |
| ログ | YP の一覧から消えた後、一定時間保持されるチャンネル |
| ローカル PeerCast | 視聴・録音の接続先となる自ホストの PeerCast（既定 `localhost:7144`） |
