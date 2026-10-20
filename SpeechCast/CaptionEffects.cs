// Decorated main caption, in the spirit of design tools' text effects: drop shadow, glow, echo,
// double outline, hollow, neon, glitch, terminal, gloss (any color), material fills (gold, silver,
// copper, hologram, or one's own image), sparkles, gaming pastel and gaming RGB. Built from one distance field of the
// text mask, so wide outlines and glows cost the same as thin ones, 2026-10-20. GPLv2.
// The materials are generated here (no image files to ship or license).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace SpeechCastMe
{
    internal sealed class CaptionEffect
    {
        public Color[] Fill;                  // top-to-bottom stops per line
        public float[] FillAt;                // where each stop sits (0-1); null = evenly spaced
        public Material Texture;              // or a material sampled per line (gold, silver, an image ...)
        public float Highlight;               // a glassy lighter band across the upper part of each line
        public float Bevel;                   // embossed edges: light top-left, dark bottom-right (0-1)
        public bool Hollow;                   // letters are left transparent
        public bool Scanlines;                // darker every third row (terminal)
        public int Glitter;                   // bright specks inside the letters
        public Color Inner; public float InnerWidth;
        public Color Outer; public float OuterWidth;
        public Color Outer2; public float Outer2Width;  // a third outline, outside the second
        public Color Glow; public float GlowRadius;
        public Color Shadow; public PointF ShadowOffset; public float ShadowBlur;
        public Color Echo; public PointF EchoStep; public int EchoCount;
        public bool Glitch;
        public bool Rainbow;                  // outline and glow run through the hues from left to right (RGB lighting)
        public Color Sparkle; public int SparkleCount;
        public int PixelScale = 1;            // >1 while drawn supersampled: fixed pixel sizes grow with it

        public const string Standard = "standard";

        // id, menu name, default color ("" = no color choice: the effect has its own material).
        internal static readonly object[][] Catalog = {
            new object[] { Standard, "標準（フォント色と縁取り）", "#000000" },
            new object[] { "shadow", "ドロップシャドウ", "#000000" },
            new object[] { "glow", "グロウ", "#A855F7" },
            new object[] { "echo", "エコー", "#A855F7" },
            new object[] { "double", "縁取り二重", "#FFFFFF" },
            new object[] { "hollow", "中抜き", "#FFFFFF" },
            new object[] { "neon", "ネオン", "#22D3EE" },
            new object[] { "glitch", "グリッチ", "#FF2BD6" },
            new object[] { "terminal", "ターミナル", "" },
            new object[] { "gloss", "光沢（好きな色）", "#2F80ED" },
            new object[] { "gold", "金", "" },
            new object[] { "silver", "銀", "" },
            new object[] { "copper", "銅", "" },
            new object[] { "hologram", "レインボー", "" },
            new object[] { "sparkle", "きらきら", "#FFF6B0" },
            new object[] { "fancy", "ゲーミング女子", "" },
            new object[] { "gaming", "ゲーミング男子", "" },

            new object[] { "image", "画像で塗る", "" } };

        internal static string DefaultColor(string id)
        {
            foreach (var entry in Catalog) if ((string)entry[0] == id) return (string)entry[2];
            return "#000000";
        }

        internal static bool HasColor(string id) { return DefaultColor(id) != ""; }

        // One effect from its id, color and strength (1 = normal; it scales widths and offsets),
        // on top of the caption font's own fill and outline. image: the picture for 画像で塗る.
        internal static CaptionEffect Build(string id, Color accent, float strength, CaptionFont style, string image)
        {
            float s = Math.Max(0.3F, Math.Min(3F, strength));
            Color[] font = { style.Colors.HighForeColor, style.Colors.LowForeColor };
            float outline = Math.Max(0, Math.Min(12, style.BorderSize));
            var fx = new CaptionEffect { Fill = font, Inner = style.Colors.BorderColor, InnerWidth = outline };
            switch (id)
            {
                case "shadow":
                    fx.Shadow = Color.FromArgb(190, accent); fx.ShadowOffset = new PointF(4 * s, 5 * s); fx.ShadowBlur = 3 * s; break;
                case "glow":
                    fx.Glow = Color.FromArgb(235, accent); fx.GlowRadius = 12 * s; break;
                case "echo":
                    fx.Echo = accent; fx.EchoStep = new PointF(4 * s, 4 * s); fx.EchoCount = 3; break;
                case "double":
                    fx.Outer = accent; fx.OuterWidth = 3 * s; break;
                case "hollow":
                    fx.Hollow = true; fx.Inner = accent; fx.InnerWidth = 3 * s; break;
                case "neon":
                    fx.Fill = new[] { Mix(accent, Color.White, 0.85F), Color.White };
                    fx.Inner = accent; fx.InnerWidth = 2 * s; fx.Glow = Color.FromArgb(235, accent); fx.GlowRadius = 14 * s; break;
                case "glitch":
                    fx.Glitch = true; fx.Echo = accent; fx.EchoStep = new PointF(3 * s, 0); break;
                case "terminal":
                    fx.Fill = new[] { C("#D8FFD0"), C("#5CFF3A"), C("#1FCB12") }; fx.Scanlines = true;
                    fx.Inner = C("#021A06"); fx.InnerWidth = 2 * s; fx.Glow = Color.FromArgb(150, 57, 255, 20); fx.GlowRadius = 8 * s; break;
                case "gloss":
                    // Chrome like pachinko captions: bright to white at the top, a sharp horizon in the
                    // middle, deep below it and lighter again at the foot; white / dark / black outlines.
                    fx.Fill = new[] { Mix(accent, Color.White, 0.92F), Mix(accent, Color.White, 0.55F), Mix(accent, Color.White, 0.25F),
                        Mix(accent, Color.Black, 0.55F), Mix(accent, Color.Black, 0.2F), accent, Mix(accent, Color.White, 0.55F) };
                    fx.FillAt = new[] { 0F, 0.28F, 0.49F, 0.5F, 0.66F, 0.84F, 1F };
                    fx.Bevel = 0.4F;
                    fx.Inner = Color.White; fx.InnerWidth = 2 * s;
                    fx.Outer = Mix(accent, Color.Black, 0.6F); fx.OuterWidth = 2 * s;
                    fx.Shadow = Color.FromArgb(180, 0, 0, 0); fx.ShadowOffset = new PointF(4 * s, 5 * s); fx.ShadowBlur = 3 * s; break;
                case "gold": case "silver": case "copper":
                    // Smooth metal like pachinko captions: light at the top, a horizon in the middle,
                    // a warm glow at the foot; a thin dark edge with a thin bright rim outside it.
                    string[] metal = id == "gold" ? new[] { "#FFFBD6", "#FFE46E", "#F4B31C", "#A85E00", "#D98E0C", "#FFE27C" }
                        : id == "silver" ? new[] { "#FFFFFF", "#ECEFF3", "#BAC2CC", "#66707D", "#A3ACB8", "#F1F4F7" }
                        : new[] { "#FFF0E2", "#F9BF8E", "#DE7F42", "#8A3814", "#C2622C", "#F6C29C" };
                    fx.Fill = Array.ConvertAll(metal, C);
                    fx.FillAt = new[] { 0F, 0.3F, 0.48F, 0.52F, 0.76F, 1F };
                    fx.Bevel = 0.35F;
                    fx.Inner = id == "gold" ? C("#5A2A04") : id == "silver" ? C("#2B323C") : C("#4A1A08"); fx.InnerWidth = 2 * s;
                    fx.Outer = id == "gold" ? C("#F6CF62") : id == "silver" ? C("#F4F6F8") : C("#F3B488"); fx.OuterWidth = 1.5F * s;
                    fx.Shadow = Color.FromArgb(150, 0, 0, 0); fx.ShadowOffset = new PointF(3 * s, 4 * s); fx.ShadowBlur = 4 * s;
                    break;
                case "hologram":
                    fx.Texture = Material.Named(id); fx.Bevel = 0.35F;
                    fx.Inner = C("#6B4FB0"); fx.InnerWidth = 1.5F * s; fx.Outer = Color.White; fx.OuterWidth = 2.5F * s;
                    fx.Shadow = Color.FromArgb(120, 0, 0, 0); fx.ShadowOffset = new PointF(3 * s, 4 * s); fx.ShadowBlur = 4 * s;
                    fx.Glow = Color.FromArgb(110, 255, 140, 235); fx.GlowRadius = 6 * s;
                    break;
                case "image":
                    fx.Texture = Material.FromFile(image); fx.Bevel = 0.5F;
                    fx.Inner = Color.FromArgb(230, 0, 0, 0); fx.InnerWidth = 3 * s; fx.Outer = Color.White; fx.OuterWidth = 2 * s; break;
                case "sparkle":
                    fx.Sparkle = accent; fx.SparkleCount = (int)(16 * s); fx.Glow = Color.FromArgb(140, accent); fx.GlowRadius = 8 * s;
                    fx.Outer = Mix(accent, Color.White, 0.5F); fx.OuterWidth = 1.5F * s; break;
                case "fancy":
                    // Gaming pastel: pink, lavender and sky blue, kept mid-bright with a deep purple edge.
                    fx.Fill = new[] { C("#FFB8E6"), C("#E39BFF"), C("#B79BFF"), C("#8FC8FF"), C("#C9A2FF") };
                    fx.Highlight = 0.25F;
                    fx.Inner = C("#9B6FE0"); fx.InnerWidth = 1.5F * s; fx.Outer = Color.White; fx.OuterWidth = 3.5F * s;
                    fx.Glow = Color.FromArgb(120, 200, 140, 255); fx.GlowRadius = 8 * s; break;
                case "gaming":
                    // Gaming gear: dark gunmetal letters with RGB edge lighting running across them.
                    fx.Fill = new[] { C("#5B606B"), C("#2B2E35"), C("#15171B"), C("#0B0C0F"), C("#2A2D33") };
                    fx.FillAt = new[] { 0F, 0.3F, 0.55F, 0.8F, 1F };
                    fx.Highlight = 0.18F; fx.Bevel = 0.6F; fx.Rainbow = true;
                    fx.Inner = Color.White; fx.InnerWidth = 2.5F * s; fx.Outer = Color.FromArgb(10, 10, 14); fx.OuterWidth = 1.5F * s;
                    fx.Glow = Color.FromArgb(220, 255, 255, 255); fx.GlowRadius = 11 * s; break;
            }
            // From ターミナル on, the decoration stays inside the outlines: no glow or shadow around it.
            if (Array.FindIndex(Catalog, e => (string)e[0] == id) >= Array.FindIndex(Catalog, e => (string)e[0] == "terminal"))
            {
                fx.Glow = Color.Empty; fx.GlowRadius = 0;
                fx.Shadow = Color.Empty; fx.ShadowOffset = PointF.Empty; fx.ShadowBlur = 0;
            }
            return fx;
        }

        // A copy for drawing at k times the size (supersampling): every length in pixels grows.
        internal CaptionEffect Scaled(int k)
        {
            var copy = (CaptionEffect)MemberwiseClone();
            copy.PixelScale = PixelScale * k;
            copy.InnerWidth *= k; copy.OuterWidth *= k; copy.Outer2Width *= k; copy.GlowRadius *= k;
            copy.ShadowOffset = new PointF(ShadowOffset.X * k, ShadowOffset.Y * k); copy.ShadowBlur *= k;
            copy.EchoStep = new PointF(EchoStep.X * k, EchoStep.Y * k);
            return copy;
        }

        private static Color C(string html) { return ColorTranslator.FromHtml(html); }

        internal static Color Mix(Color a, Color b, float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }
    }

    // A fill texture: generated metal (bands, brushed grain, diagonal sheen, specks) or an image.
    // Sampled with y relative to the text line, so the bands sit the same way on every line.
    internal sealed class Material
    {
        public readonly int Width, Height;
        private readonly int[] argb;
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        private Material(int width, int height, int[] argb) { Width = width; Height = height; this.argb = argb; }

        public Color At(int x, float lineT, float lineHeight)
        {
            // Scale so the material's height spans one line.
            int sx = (int)(x * Height / Math.Max(1F, lineHeight)) % Width;
            int sy = Math.Max(0, Math.Min(Height - 1, (int)(lineT * Height)));
            return Color.FromArgb(argb[sy * Width + (sx < 0 ? sx + Width : sx)]);
        }

        internal static Material Named(string id)
        {
            lock (cache)
            {
                Material m;
                if (cache.TryGetValue(id, out m)) return m;
                switch (id)
                {
                    case "silver": m = Metal(new[] { "#FFFFFF", "#E8ECF1", "#B9C0CA", "#7D8794", "#F2F5F8", "#9AA3AF", "#5C6672", "#DDE3EA" }, 3, false); break;
                    case "copper": m = Metal(new[] { "#FFE8D2", "#F6B47E", "#D2733A", "#8C3A17", "#F0A066", "#B55322", "#6B260C", "#E9A27A" }, 5, false); break;
                    case "hologram": m = Metal(new[] { "#FF8AD8", "#B58CFF", "#6FD0FF", "#6FF5B8", "#FFF07A", "#FFAA70", "#E07CFF", "#70E0FF" }, 7, true); break;
                    default: m = Metal(new[] { "#FFF8D6", "#FBE08A", "#E7B32C", "#A86C05", "#FFE79A", "#D19A16", "#7A4C02", "#F5CF5E" }, 1, false); break;
                }
                cache[id] = m;
                return m;
            }
        }

        // Pictures stay loaded while their file is unchanged.
        private static string imagePath; private static DateTime imageTime; private static Material imageMaterial;

        internal static Material FromFile(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return Named("gold");
                var time = File.GetLastWriteTimeUtc(path);
                if (path == imagePath && time == imageTime && imageMaterial != null) return imageMaterial;
                using (var stream = new MemoryStream(File.ReadAllBytes(path)))
                using (var loaded = new Bitmap(stream))
                {
                    // Height 256 keeps sampling cheap; width keeps the aspect.
                    int h = 256, w = Math.Max(16, loaded.Width * h / Math.Max(1, loaded.Height));
                    using (var scaled = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(scaled)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(loaded, 0, 0, w, h); }
                        imageMaterial = new Material(w, h, Pixels(scaled));
                    }
                }
                imagePath = path; imageTime = time;
                return imageMaterial;
            }
            catch (Exception) { return Named("gold"); }
        }

        // Metal: several light / dark bands down the line, fine horizontal grain, a few diagonal
        // sheens and scattered bright specks. hue: shimmer the colors sideways (hologram).
        private static Material Metal(string[] stops, int seed, bool hue)
        {
            const int W = 512, H = 128;
            var colors = Array.ConvertAll(stops, ColorTranslator.FromHtml);
            var random = new Random(seed);
            var grain = new float[H];
            for (int y = 0; y < H; y++) grain[y] = (float)(random.NextDouble() - 0.5) * 0.10F;
            var px = new int[W * H];
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)(H - 1);
                for (int x = 0; x < W; x++)
                {
                    float shift = hue ? (float)Math.Sin(x / 37.0) * 0.18F : 0F;
                    float tt = Math.Max(0, Math.Min(1, t + shift));
                    float scaled = tt * (colors.Length - 1);
                    int i = Math.Min(colors.Length - 2, (int)scaled);
                    Color c = CaptionEffect.Mix(colors[i], colors[i + 1], scaled - i);
                    float light = grain[y] * 0.4F;
                    // Diagonal sheens.
                    float diag = (x - y * 1.4F) % 170F; if (diag < 0) diag += 170F;
                    float sheen = (float)Math.Exp(-Math.Pow((diag - 60) / 7.0, 2)) * 0.45F + (float)Math.Exp(-Math.Pow((diag - 72) / 2.5, 2)) * 0.35F;
                    light += sheen;
                    c = light >= 0 ? CaptionEffect.Mix(c, Color.White, light) : CaptionEffect.Mix(c, Color.Black, -light);
                    px[y * W + x] = c.ToArgb() | unchecked((int)0xFF000000);
                }
            }
            return new Material(W, H, px);
        }

        private static int[] Pixels(Bitmap bitmap)
        {
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var result = new int[bitmap.Width * bitmap.Height];
                for (int y = 0; y < bitmap.Height; y++) Marshal.Copy(data.Scan0 + y * data.Stride, result, y * bitmap.Width, bitmap.Width);
                return result;
            }
            finally { bitmap.UnlockBits(data); }
        }
    }

    internal static class CaptionEffectRenderer
    {
        // Outlines come from a distance field of the thresholded letters, which steps along curves;
        // drawing at twice the size and scaling down makes their edges as smooth as plain text.
        private const int Supersample = 2;

        internal static void Draw(Graphics target, Rectangle rectangle, string text, CaptionFont style, bool aaMode, CaptionEffect fx)
        {
            if (rectangle.Width < 1 || rectangle.Height < 1 || string.IsNullOrEmpty(text)) return;
            var big = fx.Scaled(Supersample);
            var bigStyle = new CaptionFont(style);
            bigStyle.Size = style.Size * Supersample;
            int pad = Pad(big);
            pad += pad % Supersample;
            using (var layer = new Bitmap(rectangle.Width * Supersample + pad * 2, rectangle.Height * Supersample + pad * 2, PixelFormat.Format32bppPArgb))
            {
                layer.SetResolution(target.DpiX, target.DpiY);
                using (var g = Graphics.FromImage(layer))
                    DrawAt(g, new Rectangle(pad, pad, rectangle.Width * Supersample, rectangle.Height * Supersample), text, bigStyle, aaMode, big);
                var state = target.Save();
                target.InterpolationMode = InterpolationMode.HighQualityBilinear;
                target.PixelOffsetMode = PixelOffsetMode.HighQuality;
                target.CompositingQuality = CompositingQuality.HighQuality;
                target.DrawImage(layer, new Rectangle(rectangle.X - pad / Supersample, rectangle.Y - pad / Supersample, layer.Width / Supersample, layer.Height / Supersample));
                target.Restore(state);
            }
        }

        private static int Pad(CaptionEffect fx)
        {
            float inner = fx.Inner.IsEmpty ? 0 : fx.InnerWidth, outer = (fx.Outer.IsEmpty ? 0 : fx.OuterWidth) + (fx.Outer2.IsEmpty ? 0 : fx.Outer2Width), glow = fx.Glow.IsEmpty ? 0 : fx.GlowRadius;
            float reachOffset = Math.Max(Math.Max(Math.Abs(fx.ShadowOffset.X), Math.Abs(fx.ShadowOffset.Y)) + fx.ShadowBlur * 2,
                Math.Max(Math.Abs(fx.EchoStep.X), Math.Abs(fx.EchoStep.Y)) * Math.Max(1, fx.EchoCount));
            return (int)Math.Ceiling(inner + outer + glow + reachOffset) + (fx.SparkleCount > 0 ? 16 * fx.PixelScale : 0) + (fx.Glitch ? 6 * fx.PixelScale : 0) + 2;
        }

        private static void DrawAt(Graphics target, Rectangle rectangle, string text, CaptionFont style, bool aaMode, CaptionEffect fx)
        {
            float inner = fx.Inner.IsEmpty ? 0 : fx.InnerWidth, outer = (fx.Outer.IsEmpty ? 0 : fx.OuterWidth) + (fx.Outer2.IsEmpty ? 0 : fx.Outer2Width), glow = fx.Glow.IsEmpty ? 0 : fx.GlowRadius;
            float outerOnly = fx.Outer.IsEmpty ? 0 : fx.OuterWidth;
            int pad = Pad(fx);
            int width = rectangle.Width + pad * 2, height = rectangle.Height + pad * 2;
            using (var mask = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var font = style.CreateFont())
            using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
            {
                mask.SetResolution(target.DpiX, target.DpiY);
                if (style.IsDirectionVertical) format.FormatFlags |= StringFormatFlags.DirectionVertical | StringFormatFlags.DirectionRightToLeft;
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                using (var graphics = Graphics.FromImage(mask))
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.TextContrast = 4;
                    if (aaMode) graphics.DrawString(text, font, Brushes.White, new PointF(pad, pad), format);
                    else graphics.DrawString(text, font, Brushes.White, new RectangleF(pad, pad, rectangle.Width, rectangle.Height), format);
                }
                float lineHeight = Math.Max(1, font.GetHeight(target));
                byte[] coverage = Alpha(mask);
                int n = width * height;
                int left = width, right = -1, top = height, bottom = -1;
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    if (coverage[y * width + x] != 0) { left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y); }
                if (right < 0) return;
                float[] distance = DistanceField(coverage, width, height);
                var pixels = new float[n * 4]; // premultiplied B G R A
                // Back to front: glow, outer outline, inner outline, the letters.
                for (int y = 0; y < height; y++)
                {
                    float t = ((y - pad) % lineHeight + lineHeight) % lineHeight / lineHeight;
                    Color rowFace = Gradient(fx.Fill, fx.FillAt, t);
                    // A glassy reflection across the upper part of the line.
                    float shine = fx.Highlight > 0 && t > 0.04F && t < 0.46F ? fx.Highlight * (float)Math.Sin((t - 0.04F) / 0.42F * Math.PI) : 0F;
                    for (int x = 0; x < width; x++)
                    {
                        int i = y * width + x;
                        float d = distance[i];
                        if (d > inner + outer + glow + 1) continue;
                        float a = coverage[i] / 255F;
                        Color face = fx.Texture != null ? fx.Texture.At(x - pad, t, lineHeight) : rowFace;
                        if (a > 0)
                        {
                            if (shine > 0) face = CaptionEffect.Mix(face, Color.White, shine);
                            if (fx.Bevel > 0)
                            {
                                // Embossed edges from the mask itself: lit from the top-left.
                                int o = 2 * fx.PixelScale;
                                int ul = (y >= o && x >= o) ? coverage[i - o * width - o] : 0, dr = (y < height - o && x < width - o) ? coverage[i + o * width + o] : 0;
                                float e = (ul - dr) / 255F;
                                face = e > 0 ? CaptionEffect.Mix(face, Color.White, e * fx.Bevel * 0.6F) : CaptionEffect.Mix(face, Color.Black, -e * fx.Bevel * 0.5F);
                            }
                            if (fx.Scanlines && y / fx.PixelScale % 3 == 0) face = CaptionEffect.Mix(face, Color.Black, 0.35F);
                        }
                        Color innerColor = fx.Inner, glowColor = fx.Glow;
                        if (fx.Rainbow)
                        {
                            Color hue = Hue((x - pad) / (lineHeight * 5F));
                            innerColor = CaptionEffect.Mix(hue, Color.White, 0.15F);
                            glowColor = Color.FromArgb(fx.Glow.A, hue);
                        }
                        if (glow > 0) { float g = d - inner - outer; if (g < glow) Over(pixels, i, glowColor, (float)Math.Pow(1 - Math.Max(0, g) / glow, 2)); }
                        if (!fx.Outer2.IsEmpty) Over(pixels, i, fx.Outer2, Clamp(inner + outer + 0.5F - d));
                        if (outerOnly > 0) Over(pixels, i, fx.Outer, Clamp(inner + outerOnly + 0.5F - d));
                        if (inner > 0) Over(pixels, i, innerColor, Clamp(inner + 0.5F - d));
                        if (fx.Hollow) Erase(pixels, i, a);
                        else Over(pixels, i, face, a);
                    }
                }
                // Copies under the decoration: echoes, the glitch color split, the shadow.
                if (!fx.Echo.IsEmpty)
                {
                    var shape = new float[n];
                    for (int i = 0; i < n; i++) shape[i] = Math.Max(coverage[i] / 255F, Clamp(inner + 0.5F - distance[i]));
                    if (fx.Glitch)
                    {
                        Under(pixels, width, height, shape, new PointF(-fx.EchoStep.X, 0), Color.FromArgb(0, 230, 255), 0.9F);
                        Under(pixels, width, height, shape, fx.EchoStep, fx.Echo, 0.9F);
                    }
                    else
                        for (int k = 1; k <= fx.EchoCount; k++)
                            Under(pixels, width, height, shape, new PointF(fx.EchoStep.X * k, fx.EchoStep.Y * k), fx.Echo, 0.75F / k);
                }
                if (!fx.Shadow.IsEmpty)
                {
                    var alpha = new float[n];
                    for (int i = 0; i < n; i++) alpha[i] = pixels[i * 4 + 3];
                    int blur = (int)Math.Round(fx.ShadowBlur);
                    for (int pass = 0; pass < 3 && blur > 0; pass++) { BoxBlur(alpha, width, height, blur, true); BoxBlur(alpha, width, height, blur, false); }
                    Under(pixels, width, height, alpha, fx.ShadowOffset, fx.Shadow, 1F);
                }
                if (fx.Glitter > 0) Glitter(pixels, coverage, width, height, left, right, top, bottom, fx.Glitter * (right - left) / 400 + 8, text.GetHashCode());
                if (fx.Glitch) SliceShift(pixels, width, height, top, bottom, text.GetHashCode(), fx.PixelScale);
                using (var result = ToBitmap(pixels, width, height, target))
                {
                    if (fx.SparkleCount > 0) Sparkles(result, distance, width, height, inner + outer + 2, fx, text.GetHashCode());
                    target.DrawImageUnscaled(result, rectangle.X - pad, rectangle.Y - pad);
                }
            }
        }

        private static float Clamp(float v) { return v < 0 ? 0 : v > 1 ? 1 : v; }

        // Red, yellow, green, cyan, blue, magenta and back, once per unit of t.
        private static Color Hue(float t)
        {
            float h = (t % 1F + 1F) % 1F * 6F;
            int i = (int)h; float f = h - i;
            float[] r = { 1, 1 - f, 0, 0, f, 1 }, g = { f, 1, 1, 1 - f, 0, 0 }, b = { 0, 0, f, 1, 1, 1 - f };
            return Color.FromArgb((int)(r[i % 6] * 255), (int)(g[i % 6] * 255), (int)(b[i % 6] * 255));
        }

        private static void Over(float[] p, int i, Color color, float coverage)
        {
            if (coverage <= 0) return;
            float a = coverage * color.A / 255F, keep = 1 - a;
            int k = i * 4;
            p[k] = color.B * a + p[k] * keep; p[k + 1] = color.G * a + p[k + 1] * keep; p[k + 2] = color.R * a + p[k + 2] * keep; p[k + 3] = a + p[k + 3] * keep;
        }

        // Hollow letters: cut the glyph's inside out of what is already there.
        private static void Erase(float[] p, int i, float amount)
        {
            if (amount <= 0) return;
            float keep = 1 - amount;
            int k = i * 4;
            p[k] *= keep; p[k + 1] *= keep; p[k + 2] *= keep; p[k + 3] *= keep;
        }

        // Lays a shape (alpha 0-1) offset by `offset` underneath what is drawn.
        private static void Under(float[] p, int width, int height, float[] shape, PointF offset, Color color, float strength)
        {
            int ox = (int)Math.Round(offset.X), oy = (int)Math.Round(offset.Y);
            float ca = color.A / 255F * strength;
            for (int y = 0; y < height; y++)
            {
                int sy = y - oy;
                if (sy < 0 || sy >= height) continue;
                for (int x = 0; x < width; x++)
                {
                    int sx = x - ox;
                    if (sx < 0 || sx >= width) continue;
                    float a = shape[sy * width + sx] * ca;
                    if (a <= 0) continue;
                    int k = (y * width + x) * 4;
                    float under = a * (1 - p[k + 3]);
                    p[k] += color.B * under; p[k + 1] += color.G * under; p[k + 2] += color.R * under; p[k + 3] += under;
                }
            }
        }

        // Glitch: a few horizontal bands nudged sideways, picked from the text so they hold still.
        private static void SliceShift(float[] p, int width, int height, int top, int bottom, int seed, int scale)
        {
            var random = new Random(seed);
            int bands = 2;
            for (int b = 0; b < bands; b++)
            {
                int y0 = random.Next(top, Math.Max(top + 1, bottom)), h = random.Next(2, 4) * scale, shift = random.Next(2, 4) * scale * (random.Next(2) == 0 ? -1 : 1);
                if (shift == 0) continue;
                var row = new float[width * 4];
                for (int y = y0; y < Math.Min(height, y0 + h); y++)
                {
                    Array.Copy(p, y * width * 4, row, 0, width * 4);
                    for (int x = 0; x < width; x++)
                    {
                        int sx = x - shift;
                        int k = (y * width + x) * 4;
                        if (sx < 0 || sx >= width) { p[k] = p[k + 1] = p[k + 2] = p[k + 3] = 0; continue; }
                        Array.Copy(row, sx * 4, p, k, 4);
                    }
                }
            }
        }

        // Bright specks inside the letters (fixed by the text, so they hold still).
        private static void Glitter(float[] p, byte[] coverage, int width, int height, int left, int right, int top, int bottom, int count, int seed)
        {
            var random = new Random(seed ^ 0x5A5A);
            for (int k = 0, tries = 0; k < count && tries < count * 20; tries++)
            {
                int x = random.Next(left, right + 1), y = random.Next(top, bottom + 1);
                if (coverage[y * width + x] < 230) continue;
                float strength = 0.55F + (float)random.NextDouble() * 0.45F;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    if (xx < 0 || yy < 0 || xx >= width || yy >= height) continue;
                    float w = (dx == 0 && dy == 0 ? 1F : 0.35F) * strength;
                    Over(p, yy * width + xx, Color.White, w * coverage[yy * width + xx] / 255F);
                }
                k++;
            }
        }

        // Four-point glints scattered just outside the letters, placed from the text so they hold still.
        private static void Sparkles(Bitmap result, float[] distance, int width, int height, float near, CaptionEffect fx, int seed)
        {
            var random = new Random(seed);
            using (var g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int placed = 0;
                for (int tries = 0; tries < fx.SparkleCount * 60 && placed < fx.SparkleCount; tries++)
                {
                    int x = random.Next(width), y = random.Next(height);
                    float d = distance[y * width + x];
                    if (d < near || d > near + 10 * fx.PixelScale) continue;
                    float r = (4 + (float)Math.Pow(random.NextDouble(), 2) * 12) * fx.PixelScale;
                    Star(g, x, y, r, Color.FromArgb(240, fx.Sparkle));
                    placed++;
                }
            }
        }

        // A glint: long thin rays, a shorter diagonal cross and a soft core.
        private static void Star(Graphics g, float x, float y, float r, Color color)
        {
            using (var core = new GraphicsPath())
            {
                core.AddEllipse(x - r * 0.7F, y - r * 0.7F, r * 1.4F, r * 1.4F);
                using (var halo = new PathGradientBrush(core) { CenterColor = Color.FromArgb(150, color), SurroundColors = new[] { Color.FromArgb(0, color) } })
                    g.FillPath(halo, core);
            }
            Ray(g, x, y, r * 1.6F, r * 0.13F, 0F, color);
            Ray(g, x, y, r * 1.6F, r * 0.13F, 90F, color);
            Ray(g, x, y, r * 0.75F, r * 0.09F, 45F, Color.FromArgb(color.A * 3 / 4, color));
            Ray(g, x, y, r * 0.75F, r * 0.09F, 135F, Color.FromArgb(color.A * 3 / 4, color));
            using (var dot = new SolidBrush(Color.White)) g.FillEllipse(dot, x - r * 0.16F, y - r * 0.16F, r * 0.32F, r * 0.32F);
        }

        private static void Ray(Graphics g, float x, float y, float length, float halfWidth, float angle, Color color)
        {
            var state = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angle);
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(new[] { new PointF(-length, 0), new PointF(0, -halfWidth), new PointF(length, 0), new PointF(0, halfWidth) });
                using (var brush = new SolidBrush(color)) g.FillPath(brush, path);
            }
            g.Restore(state);
        }

        private static Color Gradient(Color[] stops, float[] at, float t)
        {
            if (stops.Length == 1) return stops[0];
            if (at == null)
            {
                float scaled = t * (stops.Length - 1);
                int i = Math.Min(stops.Length - 2, (int)scaled);
                return CaptionEffect.Mix(stops[i], stops[i + 1], scaled - i);
            }
            for (int i = 0; i < stops.Length - 1; i++)
                if (t <= at[i + 1]) return CaptionEffect.Mix(stops[i], stops[i + 1], (t - at[i]) / Math.Max(0.0001F, at[i + 1] - at[i]));
            return stops[stops.Length - 1];
        }

        private static void BoxBlur(float[] v, int width, int height, int r, bool horizontal)
        {
            int length = horizontal ? width : height, lines = horizontal ? height : width;
            var line = new float[length];
            for (int l = 0; l < lines; l++)
            {
                for (int i = 0; i < length; i++) line[i] = v[horizontal ? l * width + i : i * width + l];
                float sum = 0; int count = 0;
                for (int i = 0; i <= r && i < length; i++) { sum += line[i]; count++; }
                for (int i = 0; i < length; i++)
                {
                    v[horizontal ? l * width + i : i * width + l] = sum / count;
                    int add = i + r + 1, drop = i - r;
                    if (add < length) { sum += line[add]; count++; }
                    if (drop >= 0) { sum -= line[drop]; count--; }
                }
            }
        }

        // Euclidean distance (px) to the nearest ink pixel, 0 on ink; exact squared EDT
        // (Felzenszwalb-Huttenlocher), one pass per axis.
        private static float[] DistanceField(byte[] coverage, int width, int height)
        {
            const float Far = 1e10F;
            var grid = new float[width * height];
            for (int i = 0; i < grid.Length; i++) grid[i] = coverage[i] >= 128 ? 0 : Far;
            var f = new float[Math.Max(width, height)];
            var d = new float[f.Length];
            var v = new int[f.Length];
            var z = new float[f.Length + 1];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++) f[y] = grid[y * width + x];
                Transform(f, height, d, v, z);
                for (int y = 0; y < height; y++) grid[y * width + x] = d[y];
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++) f[x] = grid[y * width + x];
                Transform(f, width, d, v, z);
                for (int x = 0; x < width; x++) grid[y * width + x] = d[x];
            }
            for (int i = 0; i < grid.Length; i++)
                grid[i] = coverage[i] >= 128 ? 0 : Math.Max(0, (float)Math.Sqrt(grid[i]) - coverage[i] / 255F); // soften by antialiasing
            return grid;
        }

        private static void Transform(float[] f, int n, float[] d, int[] v, float[] z)
        {
            int k = 0;
            v[0] = 0; z[0] = float.NegativeInfinity; z[1] = float.PositiveInfinity;
            for (int q = 1; q < n; q++)
            {
                float s;
                while (true)
                {
                    s = ((f[q] + q * q) - (f[v[k]] + v[k] * v[k])) / (2F * q - 2F * v[k]);
                    if (s <= z[k] && k > 0) { k--; continue; }
                    break;
                }
                k++; v[k] = q; z[k] = s; z[k + 1] = float.PositiveInfinity;
            }
            k = 0;
            for (int q = 0; q < n; q++)
            {
                while (z[k + 1] < q) k++;
                d[q] = (q - v[k]) * (q - v[k]) + f[v[k]];
            }
        }

        private static byte[] Alpha(Bitmap bitmap)
        {
            var area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var raw = new byte[data.Stride * bitmap.Height];
                Marshal.Copy(data.Scan0, raw, 0, raw.Length);
                var alpha = new byte[bitmap.Width * bitmap.Height];
                for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++) alpha[y * bitmap.Width + x] = raw[y * data.Stride + x * 4 + 3];
                return alpha;
            }
            finally { bitmap.UnlockBits(data); }
        }

        private static Bitmap ToBitmap(float[] p, int width, int height, Graphics like)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            bitmap.SetResolution(like.DpiX, like.DpiY);
            var raw = new byte[width * height * 4];
            for (int i = 0; i < width * height; i++)
            {
                int k = i * 4;
                float a = p[k + 3];
                if (a <= 0.002F) continue;
                raw[k] = (byte)Math.Min(255, p[k] / a); raw[k + 1] = (byte)Math.Min(255, p[k + 1] / a); raw[k + 2] = (byte)Math.Min(255, p[k + 2] / a);
                raw[k + 3] = (byte)Math.Min(255, a * 255);
            }
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                if (data.Stride == width * 4) Marshal.Copy(raw, 0, data.Scan0, raw.Length);
                else for (int y = 0; y < height; y++) Marshal.Copy(raw, y * width * 4, data.Scan0 + y * data.Stride, width * 4);
            }
            finally { bitmap.UnlockBits(data); }
            return bitmap;
        }
    }
}
