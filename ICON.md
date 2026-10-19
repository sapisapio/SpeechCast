# スピキャスのアイコン

青い円に白い「ス」「ピ」を段違いに置いた案06。円はキャンバス横幅の約99.8%。マスター: `SpeechCast/Assets/supi-icon-master.png`。ICO: `SpeechCast/app3.ico`。旧sc-icon-master.pngは過去のSC案です。

## 再生成

16/20/24/32/40/48/55/64/96/128/137/192/256pxの13サイズ。55/137pxはRocketDockの通常/拡大サイズ。各フレームは高解像度マスターから直接縮小。全て32bit alpha、256pxはPNG、他はDIB＋ANDマスク。

```powershell
csc /r:System.Drawing.dll CreateScIcon.cs
./CreateScIcon.exe SpeechCast/Assets/supi-icon-master.png SpeechCast/app3.ico preview.png
```

## にじみの原因と対策

元のAppIconのnew Icon(stream)は32pxを選択します。さらに.NET Frameworkでは、256pxを指定しても、ICOディレクトリ上の幅0の画像を選ばず、元の4サイズICOでは48px、旧10サイズICOでは128pxが選択されました。WinFormsは選択されたハンドルをWM_SETICON/ICON_BIGに渡すため、実行中のウィンドウから取得するドック等では小画像が拡大される経路がありました。

AppIconは埋め込みICOの最大フレームをCreateIconFromResourceExで直接デコードし、所有する256pxハンドルを保持。タイトルバーにはシステムの小アイコンサイズの専用フレームを設定し、WinFormsの更新とハンドル再作成でも保持します。

新規パスのEXEとICOは、PrivateExtractIcons、およびShellのIShellItemImageFactoryの両経路で32/48/55/64/96/128/137/256pxが画素差0でした。cscの埋め込み自体の劣化はこの調査では再現していません。リソースID変更、DPI宣言変更、OS全体のキャッシュ削除は行っていません。

## 検証

```powershell
csc /platform:x86 /r:System.Drawing.dll /r:System.Windows.Forms.dll IconRegressionTests.cs
./IconRegressionTests.exe SpeechCast/app3.ico path/to/SpeechCastSP.exe
```

13サイズのICO/EXE画素一致、埋め込みリソース一致、旧32px選択、修正版256px、タイトルバー専用フレーム、ハンドル再作成、標準ダイアログを検証します。通常の非表示Formを使い、SpeechCast本体やネットワークは起動しません。

実際のRocketDockの見え方は未確認です。新しい配布フォルダーのEXEを登録して、未起動時/起動中で比較してください。必要ならICOの明示指定と比較できます。

参考: [WinForms Form実装](https://referencesource.microsoft.com/System.Windows.Forms/winforms/Managed/System/WinForms/Form.cs.html)、[CreateIconFromResourceEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createiconfromresourceex)。

## 図案の制作

組み込みimage_genで元画像を制作。透明余白のクロップ、サイズ別の縮小とICO形式変換はC#。

### 制作プロンプト

Finalize EXACTLY concept 06, the BOTTOM RIGHT BLUE CIRCLE in the reference catalog as ONE standalone high-resolution Windows desktop app icon. Reference role: exact design source, use bottom-right BLUE version only, NOT black. Circle must be solid rich true blue #2454AE. Preserve concept06's bold softly squared white Japanese characters and staggered composition: ス upper-left and ピ lower-right. Text exactly スピ, two characters. Both upright, no italic or rotation. Preserve glyph proportions, spacing, scale and silhouette. ピ must have clearly open circular handakuten on upper right not touching perimeter. One entire fully opaque BLUE DISK including all space between characters and hole in handakuten. White lettering fully opaque pure white. ONLY area OUTSIDE the BLUE circle is transparent; do not erase any part of blue disk. Square canvas, centered circle occupying 92 percent width with 4 percent clear margin each edge. Crisp antialiased edges and flat uniform solid colors, suitable for large dock launchers and tiny taskbar. No black circle, gold, rim, border, sound wave, shadows, texture, grain, gradient, gloss, 3D, labels, other text, numbers, thumbnails or other designs. Just the selected blue circular icon on genuine transparent background.

### 透過ムラ修正プロンプト

Correct this icon's alpha defects ONLY. Preserve the exact white スピ lettering, staggered layout, rich blue color, circle size, spacing and transparent outer background. Fill the entire blue disk with perfectly UNIFORM OPAQUE solid #2454AE blue, including upper-right patchy area. No semitransparent spots, no holes, no mottling, no gradient or texture anywhere within the circle. White glyphs also fully opaque. Only outside the perfectly smooth circular perimeter is transparent. Clean flat professional icon, all original shapes unchanged.
