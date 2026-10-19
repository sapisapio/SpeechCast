# RocketDockでSpeechCastだけが滲む原因と対策

2026-10-19。ユーザー環境のRocketDock 1.3.5と実際の登録ファイルを比較した調査。

## 結論

ランチャーについては、**C#が標準で割り当てるアイコンの数値ID 32512と、RocketDockの数値ID処理の相性**を特定しました。EXEに許される解像度が低いわけでも、画像を埋め込むと画素が劣化するわけでもありません。

RocketDockの最初のアイコングループを取得する処理には `id & 0x0FFF` があり、32512（0x7F00）が3840（0x0F00）に変わります。存在しない3840を探して失敗すると、SHGetFileInfoによる32pxの画像へフォールバックします。収録済みの256pxが使われず、55px/137pxへ拡大される経路です。

SpeechCastのアイコングループを**1に変更**すれば、同じ処理を通しても1のままで、RocketDockの512px要求は最大の256px画像を選べます。変更するのはグループの番号だけで、画像の画素・PNG/DIB形式・アイコン画像リソースのIDはそのままです。

## 「他のアプリは正常」との比較

33登録のうち空の区切り2件を除く31件を調査しました。EXEアイコン21件を解析、外部画像等7件、参照先が見つからないもの2件、外部リソースDLLの別管理で未解析1件。21件には比較用のOBSコピーやSpeechCastの別版も含みます。

**256pxを収録しているのに、このID切り詰めで検索できなくなるものは、登録中のSpeechCast SPの2版だけでした。** 全一覧は `SpeechCast-RocketDock-icon-audit.csv`。

| アプリ | アイコングループID | 切り詰め後 | 最大画像 | この処理での検索 |
|---|---|---|---|---|
| SpeechCast SP v14 | 32512 | 3840 | 256px | 失敗 |
| 手元のSpeechCast SP v27 | 32512 | 3840 | 256px | 失敗 |
| おつまみキャスト（インストール済みEXE） | 1 | 1 | 256px | 成功 |
| OBS | IDI_ICON1（文字列） | 変更なし | 256px | 成功 |
| Firefox / Parsec | 1 | 1 | 256px | 成功 |
| Winamp | 102 | 102 | 256px | 成功 |
| PeCaRecorder | 128 | 128 | 256px | 成功 |
| nisejs | 134 | 134 | 32px | 成功 |
| 元のSpeechCast ME | 32512 | 3840 | 32px | 失敗・32pxにフォールバック |
| JikagakiDesktop | 32512 | 3840 | 32px | 失敗・32pxにフォールバック |

元のSpeechCastにも同じID問題はありますが、元々32pxしかないため、32pxフォールバックによって「収録していた高解像度が失われる」追加の低下はありません。見た目の印象まで完全に同じになるという意味ではありません。

## 再現した処理と検証範囲

インストール済みRocketDock.exeを読み取り専用で解析しました。バイナリは変更していません。

- SHA256: `9FF98D6FD2539CEFC9F42103A7F72388BED6EE590400559B92BC7430228DA36A`
- 最初の名前を列挙するコールバック: VA 0x427D20
- 数値名の処理: VA 0x427D52、命令 `and eax, 0xfff`
- グループ検索失敗後のShell経路: VA 0x429014、SHGetFileInfoW、flags 0x100
- 通常の画像選択: VA 0x428F59、LookupIconIdFromDirectoryExに512×512を要求

同じマスク処理とWindows APIを使う回帰テストで、旧32512→3840の検索失敗、Shellフォールバックが32pxになること、修正後の1→1の検索成功、256px選択を再現しました。

```powershell
csc /platform:x86 /r:System.Drawing.dll RocketDockCompatibilityTests.cs
./RocketDockCompatibilityTests.exe old/SpeechCastSP.exe fixed/SpeechCastSP.exe
```

既存アイコン専用テスト50項目も合格。全13サイズでICOとEXEが画素単位で一致し、実行中ウィンドウ・小アイコン・ハンドル再作成も確認しました。大規模UIテストは前回のHTTP模擬サーバー待機のため完走していません。今回はアプリ機能を変更していないため、それを解決済みとは扱っていません。

**実際のRocketDock画面上での修正版の最終比較は未実施です。** 登録設定は読み取っただけで書き換えていません。帰宅後、新しい修正版EXEを登録して拡大時を比較してください。OS全体のキャッシュ削除やExplorer再起動は実施していません。

## ExplorerとICO直接指定は別の比較

Explorerでは、サイズを数字で描き分けた検査用ICOを使い、EXEが48px、ICOが137pxを選ぶ表示状態を実画面で確認しました。コピーしたOBSのEXEと、そのEXEから抽出・再構成したICOにも同じ見た目の差が出ました。おつまみキャストもEXEとICOで差があり、図案をC#とElectronのEXEで入れ替えてもExplorer側の差は残りました。

したがって、**今回のID対策でExplorerまで直ったとは主張しません。** これは、他の登録アプリとのID差を実証できたRocketDock固有の問題と分けて扱います。

また、GDI+でICOファイルを直接読む検査では、こちらの13サイズ・小さい順ICOは256px、ユーザー保存の4サイズ・大きい順ICOとおつまみキャストのICOは16pxとして開かれました。ICO直接指定は、EXEからのリソース取得とは別の読み方になり、常にICOの方が高画質になるとは限りません。これがユーザー報告の逆転と整合する経路です。実際のRocketDockの全経路を動的追跡した結果ではないため、個々の比較画像の左右の原因まで断定はしていません。

## 実装

`NormalizeAppIcon.cs` をビルド中に実行し、cscが生成したRT_GROUP_ICONを32512から1へ変更します。画像リソースやその他のWin32リソースは保持。再ビルド・増分ビルドの両方で動作し、既に1なら変更しない設計です。外部のResource Hackerやrceditは不要です。

参考: [EnumResourceNames](https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-enumresourcenamesw)、[FindResource](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-findresourcew)、[UpdateResource](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-updateresourcew)。
