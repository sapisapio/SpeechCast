# 申し送り（Speech Cast SP）

2026-10-20 時点。Claude Code と Codex の間の引き継ぎメモです。

## 現在の状態

- 作業ブランチは `window-theme`。機能はほぼ完成。ユーザーが GitHub への公開を準備しているところ。
- 変更点の一覧は `CHANGES.md`、使い方は `UI-preview-notes.md`（配布物では `Readme-SP.md`）とアプリ内の ヘルプ →「使い方」。
- アイコンの問題（RocketDock でにじむ）は解決済み。原因は RocketDock 1.3.5 がアイコングループ ID を 0x0fff でマスクすること。ID を 32512→1 に正規化して対処した。詳細は `ROCKETDOCK-ICON.md` と `ICON.md`。

## 残っていること

- アプリ内の使い方の OBS のページ：ユーザーから OBS のスクショをもらって `SpeechCast/Tutorial/10.png` と `10.txt` を足す。
- 最後に全体の仕組みのチェック（ユーザーの指示で保留中）。調査中に一度、スレを開いたときに古いレスが再生されたように見えたことがある。
- 実配信での長時間動作、OBS への取り込み、耳での音の確認はユーザーが行う。音や見た目の確認は、こちらで繰り返さずユーザーに頼む。

## 主なファイル

- 読み上げ：`SpeechEngine.cs`（SAPI・Windows 内蔵の声・VOICEVOX / AivisSpeech の自動検出）、`FormMain.Voices.cs`、`FormMain.Reading.cs`
- jpnkn Fast：`FormMain.Fast.cs`、`JpnknFast*.cs`（`FAST-MODE.md`）
- 画面とテーマ：`FormMain.ReaderUi.cs`、`FormMain.Theme.cs`、`WindowTheme.cs`、`ToolbarIcons.cs`、`Controls/`（`ThemedTrackBar`、`ComboChrome`、`DescribedMenuItem`）
- テロップ：`CaptionRasterizer.cs`（標準の縁取り）、`CaptionEffects.cs`（装飾。2倍で描いて縮小する）、`CaptionBackgrounds.cs`（背景）、`FormCaption*.cs`
- 配信向けの窓：`OverlayWindow.cs`（共通）、`FormStreamOverlay.cs` と `StreamTemplates.cs`（配信コメント、スキン13種）、`FormFreeTelop.cs` と `FormMain.Telop.cs`（自由テロップ）、`Gadgets.cs`・`GadgetTimer.cs`・`GadgetSounds.cs`・`FormMain.Gadgets.cs`（時計・タイマー）
- 設定画面：`FormSettings.cs`、`FormSettings.Display.cs`（表示・配信・ガジェット・ショートカットのタブと、字幕タブの追加分）
- ショートカット：`FormMain.Hotkeys.cs`（RegisterHotKey。F12 単独は Windows の予約なので既定は Ctrl+F12）
- 使い方：`FormTutorial.cs`。exe の横の `Tutorial` フォルダーの `01.png` + `01.txt` … を1ページずつ読む（txt の1行目が見出し）。ページ数はファイルの数で決まる。
- 設定：`UseConfig.cs`（XmlSerializer の public フィールド。保存先は `%AppData%\SpeechCastMe\Application.cfg`）

## ビルドとテスト

```powershell
# ビルド（VS 2022 と Windows 10/11 SDK が必要）
msbuild SpeechCast/SpeechCastMe.csproj /restore /p:Configuration=Release /p:Platform=x86

# UI・データ処理の回帰テスト（すべて通る状態）
$bin = "SpeechCast/bin/x86/Release"
$ref = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6"
csc /platform:x86 /target:exe /out:$bin/UiRegressionTests.exe /r:$bin/SpeechCastSP.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xml.dll /r:System.Web.Extensions.dll /r:$ref\System.Speech.dll UiRegressionTests.cs
# $bin の中で UiRegressionTests.exe を実行（プレビュー用の設定を使うので、ユーザーの設定は書き換えない）
```

- ユーザーがアプリを起動していると bin の exe がロックされる。そのときの新しいビルドは `obj/x86/Release` にある。
- 回帰テストはロジックを変えたときとコミット前だけ回す（小さな見た目の調整では回さない）。
- 環境変数 `SPEECHCAST_UI_PREVIEW=1` で、ネット接続も読み上げもしないプレビューモードで起動する（スクショ用）。

## 配布 ZIP の中身

`SpeechCastSP.exe`、`SpeechCastSP.exe.config`、`MQTTnet.dll`、`Sounds/`、`Tutorial/`、`LICENSE`、`README.md`、`readme_me.txt`、`CHANGES.md`、`FAST-MODE.md`、`Readme-SP.md`（= `UI-preview-notes.md`）、`ThirdParty/`。

## 今後の予定（ユーザーとの合意）

- おつまみキャスト（Electron）への統合は、この SP が完成した後。
