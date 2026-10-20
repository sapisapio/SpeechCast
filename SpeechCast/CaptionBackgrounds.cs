// Backgrounds for the main caption window, filling exactly its area (set with the position frame):
// when the caption sits beside the game, the desktop does not show through, and viewers see where
// replies appear. Drawn templates, or the user's own image (stretched, cropped to fill, or tiled), 2026-10-20. GPLv2.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace SpeechCastMe
{
    internal static class CaptionBackgrounds
    {
        public const string None = "none", Image = "image";

        internal static readonly string[][] Catalog = {
            new[] { None, "なし（透明）" }, new[] { "smoke", "半透明の黒" }, new[] { "frost", "すりガラス" },
            new[] { "gradient", "グラデーション" }, new[] { "neon", "ネオン枠" }, new[] { "washi", "和紙" },
            new[] { "chalk", "黒板" }, new[] { "game", "ゲーム窓" }, new[] { Image, "自分の画像…" } };

        internal static readonly string[][] ImageFits = { new[] { "stretch", "引き伸ばす" }, new[] { "fill", "トリミングして埋める" }, new[] { "tile", "並べる" } };

        private static string cachedPath;
        private static DateTime cachedTime;
        private static Bitmap cachedImage;

        // The templates are opaque; transparency (0-100 %) is the user's choice, for the whole background.
        internal static void Draw(Graphics g, Size size, string id, string imagePath, string fit, int transparency)
        {
            if (string.IsNullOrEmpty(id) || id == None || size.Width < 4 || size.Height < 4) return;
            float alpha = 1F - Math.Max(0, Math.Min(100, transparency)) / 100F;
            if (alpha <= 0) return;
            using (var layer = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var lg = Graphics.FromImage(layer))
                {
                    lg.SmoothingMode = SmoothingMode.AntiAlias;
                    lg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    lg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    var box = new RectangleF(1.5F, 1.5F, size.Width - 3F, size.Height - 3F);
                    switch (id)
                    {
                        case "smoke": Panel(lg, box, 14F, Color.FromArgb(255, 18, 21, 27), Color.FromArgb(255, 58, 64, 74), 1F); break;
                        case "frost": Frost(lg, box); break;
                        case "gradient":
                            using (var path = ToolbarIcons.RoundRect(box.X, box.Y, box.Width, box.Height, 14F))
                            using (var brush = new LinearGradientBrush(box, Color.FromArgb(255, 49, 27, 94), Color.FromArgb(255, 14, 60, 120), 60F)) lg.FillPath(brush, path);
                            break;
                        case "neon":
                            Panel(lg, box, 14F, Color.FromArgb(255, 8, 10, 22), Color.Empty, 0F);
                            using (var path = ToolbarIcons.RoundRect(box.X + 4, box.Y + 4, box.Width - 8, box.Height - 8, 12F))
                            {
                                for (int i = 4; i >= 1; i--) using (var glow = new Pen(Color.FromArgb(28 * (5 - i), 236, 72, 153), 1.5F + i * 2.5F)) lg.DrawPath(glow, path);
                                using (var edge = new Pen(Color.FromArgb(255, 249, 168, 212), 1.6F)) lg.DrawPath(edge, path);
                            }
                            break;
                        case "washi": Washi(lg, box); break;
                        case "chalk": Chalk(lg, box); break;
                        case "game": GameWindow(lg, box); break;
                        case Image: Picture(lg, size, imagePath, fit); break;
                    }
                }
                var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = alpha };
                using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    attributes.SetColorMatrix(matrix);
                    g.DrawImage(layer, new Rectangle(0, 0, size.Width, size.Height), 0, 0, size.Width, size.Height, GraphicsUnit.Pixel, attributes);
                }
            }
        }

        private static void Panel(Graphics g, RectangleF box, float radius, Color fill, Color edge, float edgeWidth)
        {
            using (var path = ToolbarIcons.RoundRect(box.X, box.Y, box.Width, box.Height, radius))
            {
                using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                if (!edge.IsEmpty) using (var pen = new Pen(edge, edgeWidth)) g.DrawPath(pen, path);
            }
        }

        // Frosted glass, drawn opaque: pale blue-white with a soft sheen and fine grain.
        private static void Frost(Graphics g, RectangleF box)
        {
            using (var path = ToolbarIcons.RoundRect(box.X, box.Y, box.Width, box.Height, 16F))
            {
                using (var brush = new LinearGradientBrush(box, Color.FromArgb(255, 238, 243, 250), Color.FromArgb(255, 205, 218, 235), 75F)) g.FillPath(brush, path);
                var state = g.Save();
                g.SetClip(path);
                using (var sheen = new LinearGradientBrush(box, Color.FromArgb(110, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 100F))
                    g.FillRectangle(sheen, box.X, box.Y, box.Width, box.Height * 0.45F);
                var random = new Random(3);
                int specks = (int)(box.Width * box.Height / 60);
                for (int i = 0; i < specks; i++)
                    using (var dot = new SolidBrush(Color.FromArgb(random.Next(10, 28), random.Next(2) == 0 ? Color.White : Color.SlateGray)))
                        g.FillRectangle(dot, box.X + (float)random.NextDouble() * box.Width, box.Y + (float)random.NextDouble() * box.Height, 1.5F, 1.5F);
                g.Restore(state);
                using (var edge = new Pen(Color.FromArgb(255, 255, 255, 255), 1.4F)) g.DrawPath(edge, path);
            }
        }

        // Paper: warm cream with faint fibres (fixed seed, so it never flickers).
        private static void Washi(Graphics g, RectangleF box)
        {
            using (var path = ToolbarIcons.RoundRect(box.X, box.Y, box.Width, box.Height, 6F))
            {
                using (var brush = new SolidBrush(Color.FromArgb(255, 246, 239, 222))) g.FillPath(brush, path);
                var state = g.Save();
                g.SetClip(path);
                var random = new Random(7);
                int fibres = (int)(box.Width * box.Height / 900);
                for (int i = 0; i < fibres; i++)
                {
                    float x = box.X + (float)random.NextDouble() * box.Width, y = box.Y + (float)random.NextDouble() * box.Height;
                    float len = 6 + (float)random.NextDouble() * 22, angle = (float)(random.NextDouble() * Math.PI);
                    using (var pen = new Pen(Color.FromArgb(30 + random.Next(30), 150, 120, 80), 0.8F))
                        g.DrawLine(pen, x, y, x + (float)Math.Cos(angle) * len, y + (float)Math.Sin(angle) * len);
                }
                g.Restore(state);
                using (var edge = new Pen(Color.FromArgb(140, 160, 120, 70), 1.2F)) g.DrawPath(edge, path);
            }
        }

        // Chalkboard: green-black board in a wooden frame.
        private static void Chalk(Graphics g, RectangleF box)
        {
            using (var frame = ToolbarIcons.RoundRect(box.X, box.Y, box.Width, box.Height, 6F))
            using (var wood = new LinearGradientBrush(box, Color.FromArgb(255, 150, 100, 55), Color.FromArgb(255, 100, 62, 30), 90F)) g.FillPath(wood, frame);
            var board = RectangleF.Inflate(box, -8, -8);
            using (var path = ToolbarIcons.RoundRect(board.X, board.Y, board.Width, board.Height, 3F))
            using (var brush = new LinearGradientBrush(board, Color.FromArgb(255, 38, 66, 52), Color.FromArgb(255, 22, 44, 34), 70F)) g.FillPath(brush, path);
            var random = new Random(11);
            for (int i = 0; i < 6; i++)
            {
                float y = board.Y + (float)random.NextDouble() * board.Height;
                using (var smear = new Pen(Color.FromArgb(18, 255, 255, 255), 10F)) g.DrawLine(smear, board.X + 10, y, board.Right - 10, y + random.Next(-8, 8));
            }
        }

        // A classic console RPG window: navy panel, white double frame.
        private static void GameWindow(Graphics g, RectangleF box)
        {
            using (var path = ToolbarIcons.RoundRect(box.X, box.Y, box.Width, box.Height, 8F))
            using (var brush = new LinearGradientBrush(box, Color.FromArgb(255, 26, 36, 120), Color.FromArgb(255, 8, 12, 52), 90F))
            {
                g.FillPath(brush, path);
                using (var edge = new Pen(Color.White, 3F)) g.DrawPath(edge, path);
            }
            using (var inner = ToolbarIcons.RoundRect(box.X + 6, box.Y + 6, box.Width - 12, box.Height - 12, 5F))
            using (var pen = new Pen(Color.FromArgb(170, 255, 255, 255), 1.2F)) g.DrawPath(pen, inner);
        }

        private static void Picture(Graphics g, Size size, string path, string fit)
        {
            var image = Load(path);
            if (image == null) return;
            if (fit == "tile")
            {
                using (var brush = new TextureBrush(image, WrapMode.Tile)) g.FillRectangle(brush, 0, 0, size.Width, size.Height);
                return;
            }
            if (fit == "fill")
            {
                float scale = Math.Max(size.Width / (float)image.Width, size.Height / (float)image.Height);
                float w = image.Width * scale, h = image.Height * scale;
                g.DrawImage(image, (size.Width - w) / 2F, (size.Height - h) / 2F, w, h);
                return;
            }
            g.DrawImage(image, 0, 0, size.Width, size.Height);
        }

        // The image stays loaded while its file is unchanged (frames are redrawn often).
        private static Bitmap Load(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                var time = File.GetLastWriteTimeUtc(path);
                if (path == cachedPath && time == cachedTime && cachedImage != null) return cachedImage;
                using (var stream = new MemoryStream(File.ReadAllBytes(path)))
                using (var loaded = new Bitmap(stream))
                {
                    if (cachedImage != null) cachedImage.Dispose();
                    cachedImage = new Bitmap(loaded);
                }
                cachedPath = path; cachedTime = time;
                return cachedImage;
            }
            catch (Exception) { return null; }
        }
    }
}
