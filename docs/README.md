# YPBrowser ドキュメント

コードを読んでもすぐにはわからない設計背景・落とし穴・概念の使い分けを記録したドキュメント群。

## ドキュメント一覧

| ファイル | 内容 |
|---|---|
| [architecture.md](architecture.md) | アーキテクチャ全体像、サービス一覧、DI 設計の理由 |
| [channel-lifecycle.md](channel-lifecycle.md) | ChannelDiff の各値の意味、Log チャンネル、初回フェッチ問題、4分インターバル |
| [concepts.md](concepts.md) | 類似概念の使い分け（FavoriteSettings vs FavoriteItem など） |
| [yp-data-format.md](yp-data-format.md) | index.txt の 19 フィールド仕様、パース処理、フィルタリング |
| [recording.md](recording.md) | HTTP ストリーム録音の仕組み、PLS 解決、自動ダウンロードルール |
| [settings.md](settings.md) | 設定システム、2 段階コミット、_loading フラグパターン |

## よくある疑問への答え

**Q. WinUI 3 のはずなのに WPF のコードがある？**
→ [architecture.md](architecture.md) の冒頭参照。実装は WPF (`<UseWPF>true`)。

**Q. `FavoriteSettings` と `FavoriteItem` は何が違う？**
→ [concepts.md](concepts.md#favoritesettings-vs-favoriteitem) 参照。

**Q. `StreamUrl` に GET したのに短いファイルしか保存されない**
→ [recording.md](recording.md#pls-解決が必要な理由) 参照。`/pls/` は PLS テキストを返す。

**Q. 起動直後に全チャンネルが「新着」になるのはなぜ？**
→ [channel-lifecycle.md](channel-lifecycle.md#applyDiff-の呼び出しタイミングと初回フェッチ問題) 参照。

**Q. 設定ダイアログでキャンセルすると変更が戻らない設定がある**
→ [settings.md](settings.md#注意-dowloadersettings-は-in-place-変更) 参照。

**Q. 手動更新ボタンを押しても YP から取得されない**
→ [channel-lifecycle.md](channel-lifecycle.md#force-true-フラグ) 参照。`force: true` なら 4 分ガードをスキップ。
