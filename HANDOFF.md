# 申し送り（Speech Cast SP / ui-refresh ブランチ）

## RocketDock固有の原因を特定（2026-10-19、v15対策）

RocketDock 1.3.5は最初の数値アイコングループIDを0x0fffでマスクする処理があり、C#標準の32512が存在しない3840になって32pxのShellアイコンへフォールバックします。登録中のEXE21件を比較し、256pxを持つのにこの条件で検索失敗するのはSpeechCast SPの2版だけでした。おつまみキャストはID1、OBSは文字列IDなので該当しません。
NormalizeAppIcon.csとMSBuildターゲットで32512→1へ正規化。画像の画素やアイコン画像リソースIDは変更しません。実APIによる旧版の失敗と修正版の256px選択を再現、既存アイコンテスト50項目も合格。詳細はROCKETDOCK-ICON.md、テストはRocketDockCompatibilityTests.cs。実際のドック画面での最終確認は未実施です。登録設定は変更していません。
ExplorerではOBSやおつまみキャストのコピーもEXE側がぼける別の表示差を確認しています。このID対策でExplorerも直ったとは主張しません。以下の旧調査と区別してください。

## Codexによるアイコン修正（2026-10-19）

下記の旧調査を引き継ぎ、青い「スピ」案06・余白削減版を組み込みました。
AppIconの暗黙の32px選択を再現し、256pxフレームをWindowsで直接デコードしてWM_GETICON/ICON_BIGに渡すよう変更。タイトルバーは専用の小フレームを保持し、ハンドル再作成にも対応。ICOは55/137pxを含む13サイズ。
新規パスのEXEとICOは、ネイティブ抽出とShell取得の両経路で同じ画素になることを確認。埋め込み自体の劣化は再現していないため、リソースID・DPI宣言の変更やキャッシュ削除は行っていません。実際のRocketDockの見え方は未確認です。
詳細・再生成手順はICON.md、専用テストはIconRegressionTests.cs。ファイルバージョン2.0.2.14（AssemblyVersionは維持）。
専用テスト50項目合格。既存UI回帰テストは大量コメント処理まで通過しましたが、HTTP復旧テストの模擬サーバー待機で未完走です。

2026-10-19 時点。Claude Code から Codex への引き継ぎ。

## 最優先の未解決事項：exe のアイコンがにじむ

### 症状
- 同じアイコンでも、`.ico` ファイルはエクスプローラーや RocketDock でくっきり表示される。
- `SpeechCastSP.exe` にすると、エクスプローラー（表示サイズを大きくしたとき）でも RocketDock（通常55px、ズームで最大137px）でも**にじむ**。
- ほかのアプリの exe（OBS、Firefox、Steam、おつまみキャスト〈Electron製〉など）はきれいに出る。
- **ユーザーの見立て：実装（exe への埋め込み方）が原因。** ドックのせいではない。

### アイコンの組み込み箇所
- `SpeechCast/app3.ico`：アプリのアイコン。今はユーザーが手作りした「青.ico」をそのままコピーしたもの（256px PNG → 48 → 32 → 16px の4サイズ。おつまみキャストの exe と同じ構成）。
  **ユーザーが新しいアイコンファイルを用意する予定。**
- 以前の別の試み（黒地に白い「SC」）として、`CreateScIcon.cs`（ico 書き出し用ツール）、`ICON.md`、`SpeechCast/Assets/sc-icon-master.png` がある（コミット ac93eaf）。今の app3.ico はこれではなく「青.ico」。
- `SpeechCast/SpeechCastMe.csproj`
  - `<ApplicationIcon>app3.ico</ApplicationIcon>`：exe の Win32 アイコン（csc の /win32icon）
  - `<EmbeddedResource Include="app3.ico"><LogicalName>SpeechCastMe.app.ico</LogicalName>`：ウィンドウ用
- `SpeechCast/AppIcon.cs`：埋め込んだ ico を `new Icon(stream)` で読み、各フォームの `Icon` に設定する。`Form` の非公開フィールド `defaultIcon` にも入れて、ダイアログにも同じアイコンを使う（`Program.cs` で `AppIcon.UseAsDefault()`）。
  これはタイトルバーとタスクバー用。**エクスプローラーやドックが見るのは exe の Win32 アイコン（ApplicationIcon）の方。**

### 確認済みのこと（ここまでは問題なし）
- ビルドした exe から `PrivateExtractIcons` で 256/128/64/48px を取り出すと、それぞれのサイズで取れる。256px は元の ico と**ピクセル単位で完全に一致**する。
- exe 内の RT_GROUP_ICON（#32512）の並び順は、ico と同じになる（今は 256png, 48, 32, 16）。
- 試したが改善しなかったこと：
  1. ico のサイズを小さい順（16→256）から大きい順（256→16）に並べ替えた
  2. 256px の PNG 圧縮をやめて、非圧縮の DIB にした
- 画面の拡大率は 100%（2560x1440 と 1920x1080 の2画面）。

### 未確認の疑い（次に調べること）
1. **Windows のアイコンキャッシュ**：同じパスの exe を何度も上書きしてきた。新しいファイル名・新しいフォルダーにコピーした exe でもにじむか確認する。キャッシュの更新は `ie4uinit.exe -show`。
2. **埋め込み方の違い**：csc の /win32icon（MSBuild の ApplicationIcon）ではなく、rcedit や Resource Hacker で exe のアイコンを差し替えて比べる。おつまみキャストは electron-builder（rcedit）で埋め込まれている。
3. **RT_GROUP_ICON の ID と言語**：おつまみキャストのグループ ID は `#1`、こちらは `#32512`（csc の既定）。言語 ID や、ほかに古いアイコンリソースが残っていないかも確認する。
4. **exe のマニフェスト**：DPI 対応の宣言がない（app.manifest なし）。エクスプローラーのアイコン表示に影響するかどうか。

## ビルドとテスト

```powershell
# ビルド（VS 2022 と Windows 10/11 SDK が必要。WinRT の音声機能で Windows.winmd を参照）
msbuild SpeechCast/SpeechCastMe.csproj /restore /p:Configuration=Release /p:Platform=x86

# UI・データ処理の回帰テスト（150項目、すべて通る状態）
$bin = "SpeechCast/bin/x86/Release"
$ref = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6"
csc /platform:x86 /target:exe /out:$bin/UiRegressionTests.exe /r:$bin/SpeechCastSP.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xml.dll /r:System.Web.Extensions.dll /r:$ref\System.Speech.dll UiRegressionTests.cs
# $bin の中で UiRegressionTests.exe を実行（プレビュー設定を使うので、ユーザーの設定は書き換えない）

# MQTT（jpnkn Fast）の通信テスト：FastIntegrationTests.cs を MQTTnet.dll と一緒にコンパイルして実行
```

配布 ZIP の中身：`SpeechCastSP.exe`、`SpeechCastSP.exe.config`、`MQTTnet.dll`、`LICENSE`、`README.md`、`readme_me.txt`、`FAST-MODE.md`、`Readme-SP.md`（= `UI-preview-notes.md`）、`Preview.cmd`、`ThirdParty/`。

## このブランチで入れた機能（概要）
- jpnkn Fast：スレッドモードと板全体モード（`FormMain.Fast.cs`、`JpnknFast*.cs`）
- 読み上げ：Windows 標準の SAPI 音声に加えて Windows 内蔵の音声（Ayumi/Ichiro）と VOICEVOX・AivisSpeech を自動検出（`SpeechEngine.cs`、`OneCoreVoices.cs`、`FormMain.Voices.cs`）
- 自動更新（レス取得）と自動送り（次のレスへ進む）を分離。読み上げ中のレスをハイライト、クリックでそのレスを読む、未読数の表示（`FormMain.Reading.cs`）
- コメント欄：PeerCast プレイヤー付属ビューア風の色分け、1行/2行表示、区切り線（`Response.cs`、`Resources/resView.html`）
- 透過ウィンドウ（OBS のウィンドウキャプチャ用）：配信コメント（`FormStreamOverlay.cs`）、自由テロップ（`FormFreeTelop.cs`、`FormMain.Telop.cs`）。共通部分は `OverlayWindow.cs`
- 内蔵フォント：源暎エムゴ v2 Black（SIL OFL、`BundledFont.cs`、ライセンスは `ThirdParty/`）
- 掲示板を開くダイアログ、前回の板・スレを起動時に開く（`FormMain.OpenBoard.cs`）
- ツールバーのアイコンはコードで描画（`ToolbarIcons.cs`）

詳しい使い方は `UI-preview-notes.md` を参照。

## 今後の予定（ユーザーとの合意）
- 時計・タイマー・ポモドーロ・ToDo などのガジェットは、**きちんとデザインしてから**作る（テロップの文字を流用しただけのものは要らない）。スレ情報のガジェットは不要。
- おつまみキャスト（Electron）への統合は、この SP が完成した後。
