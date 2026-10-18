# Speech Cast SP — UI 試作版3

2026-10-18 / 2.0.2.3

## 今回の変更

- 元の段構成を整理して復元。上から「ボイス・音量」「Play・自動更新・ビュー操作」「URL・次スレキーワード」の3段です。
- ボイス選択、読み上げ音量、新着音量、字幕・瞬間表示・読み上げ・着信音・リロード音・位置調整枠・代替字幕・設定をボイス段へ戻しました。古い画像アイコンを文字記号・絵文字へ変更。
- Play段は 📋・✏️・⏮️・⏪️・⏹️・▶️・⏩️・⏭️ の絵文字。ターボだけ「ターボ」の文字表示です。各操作にはツールチップを付けています。
- Play段の右端に 🔽（自動更新）、文字縮小・拡大、最新レスへの移動を配置。
- URL段の左側ボタン・ラベルを除去。URLを入力してEnterで読み込みます。その右に次スレキーワードと自動移動ON/OFFを配置。狭い幅ではキーワードのラベルを省略し、フォームとON/OFFを残します。
- スレッド読み込み成功後は自動更新をONにし、直ちに末尾へ移動。既存レスは末尾から開始します。自動更新を手動でONにしたときも直ちに末尾へ移動します。
- テロップのタイトル帯は文字幅＋左右各4pxへ縮小。高さもフォント高さ＋4pxに縮小。未読み込み時は「SpeechcastSP」、読み込み後はスレ名と現在数・総数のみを表示。
- アプリアイコンは元の左上・右下の文字配置を参考にした、黒背景・白文字の斜体SP。

1行／2行、番号・名前・日付・時間・IDの独立ON/OFF、背景・フォント設定、透過テロップ、最小幅240pxは前版から継続。「表示」メニューから設定します。F11でコメントだけの表示にし、F11またはEscで戻ります。

## 起動

Preview.cmd：サンプルコメントによるオフラインプレビュー。通常の設定の読み書き、掲示板への接続・読み上げ・投稿は行いません。更新・表示・フォント・音量等のUIを試せます。

SpeechCastSP.exe：通常起動。互換性のためAppData/Roaming/SpeechCastMeの設定とブックマークを使用します。旧版と同時利用すると設定保存が競合するため、通常利用はどちらか一方を起動してください。

## 検証

Release / x86 / .NET Framework 4.6 ビルド成功。既存コード由来の警告は残存。

回帰テスト36項目成功。実WebBrowserでのスクロール位置保持、読み込み後の自動更新ON・末尾移動、再ON時の末尾移動、3段の既存コントロールの配置、右端への固定とボタン順、URL段の構成、タイトル帯の初期・読み込み後の表示、文字描画・リンク・設定互換を含みます。

実掲示板への接続・投稿・音声再生の通しテストとOBS各方式での透過取り込みは未検証です。プレビューでは音声・投稿などの操作は無効表示になります。絵文字の見た目はWindowsのフォント描画に従い、モノクロになる場合があります。

## ソースとビルド

対応ソースは SpeechCastSP-source-v3.zip。原作者・派生版作者の著作権表示とGPLv2を維持しています。LICENSE、README.md、readme_me.txtを参照してください。

```
MSBuild SpeechCast\SpeechCastMe.csproj /p:Configuration=Release /p:GenerateManifests=false
csc /platform:x86 /out:SpeechCast\bin\Release\UiRegressionTestsSP.exe /reference:SpeechCast\bin\Release\SpeechCastSP.exe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll UiRegressionTests.cs
SpeechCast\bin\Release\UiRegressionTestsSP.exe
```

Visual StudioのMSBuildと.NET Framework 4.6の参照アセンブリが必要です。GitHubへのpushは行っていません。Fast受信、個別レスモード、コメント一覧の透明化・分離は後段の作業です。
