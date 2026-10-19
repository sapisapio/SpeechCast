# SC icon

2026-10-19。提供された旧32pxアイコンを参考に、黒・白の円形バッジとして再制作。Sを左上、Cを右下に置く構成を維持し、文字を太めの幾何学的な斜体へ変更しました。アプリ名はSpeech Cast SPのままです。

マスター画像：`SpeechCast/Assets/sc-icon-master.png`（透過PNG）。アプリアイコン：`SpeechCast/app3.ico`。

ICOには16・20・24・32・40・48・64・96・128・256pxを収録。全フレーム32bitアルファで、256pxはPNG圧縮、それ以外は32bit DIBとANDマスクです。WindowsのLoadImageで全10サイズの読み込み・寸法・透明な角を検証しています。

再書き出し：

```powershell
csc /r:System.Drawing.dll CreateScIcon.cs
./CreateScIcon.exe SpeechCast/Assets/sc-icon-master.png SpeechCast/app3.ico preview.png
```

画像の制作には組み込みのimage_genを使用。ICOへの変換・縮小とサイズ確認はCreateScIcon.csで実施しました。

## Generation prompt

Redesign this tiny legacy Windows application icon into ONE finished, crisp, modern app icon, square canvas, high resolution, transparent outside a circular badge. Preserve its identity: exactly two uppercase letters S and C in a diagonal composition, S in the upper-left area and C in the lower-right area. The old red S / yellow C / blue background are ONLY a composition reference; replace ALL colors. New palette: near-black circular disk and pure white lettering, with a very fine neutral light-gray outer rim so the icon remains visible on a dark Windows taskbar. An OBS-like restrained monochrome aesthetic, but an original SC typographic monogram, absolutely no OBS swirl symbol. Use a sophisticated, sturdy, slightly oblique modern geometric sans-serif font with smooth rounded curves, optically balanced, bold enough for 16px use, not generic Times serif or calligraphy. Make both letters large and clearly recognizable as S and C; some diagonal overlap is fine but keep their silhouettes visually separate and avoid ambiguous merged shapes. Circle almost fills the square with consistent 4 percent transparent padding. Flat vector-like graphic, razor-clean contours, carefully balanced optical margins, no texture, no lighting, no gradients, no 3D, no shadow outside the circle, no extraneous symbols, no extra text. Deliver just the standalone centered circular black-and-white SC icon on actual transparent background, not a mockup or presentation sheet.
