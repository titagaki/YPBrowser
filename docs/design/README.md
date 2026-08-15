# YPBrowser 設計ノート

**なぜそうしたか**だけを記録する。実装が何をするか（値・書式・条件）は
[`docs/spec/`](../spec/) にあり、ここでは繰り返さない。

| ファイル | 内容 |
|---|---|
| [architecture.md](architecture.md) | MVVM + DI を選んだ理由、レイヤー分割、ライフタイム、HttpClient の登録方式 |
| [decisions.md](decisions.md) | 個々の判断の理由（ログ 1 時間、4 分間隔、初回スキップ、録音の自前実装ほか） |
| [concepts.md](concepts.md) | 似た名前・似た役割の型がなぜ分かれているか |

理由が不明・未検証のものはここに書かず、[`docs/investigations/`](../investigations/) に置く。
