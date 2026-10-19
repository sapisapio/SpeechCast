// Looks for the stream comments window: original designs in the genres seen on stream overlays
// (chat, talk bubbles, neon, Japanese paper, terminal, RPG window, vertical writing...), 2026-10-19. GPLv2.
// The plain outlined text ("outline") is drawn by FormStreamOverlay itself.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace SpeechCastMe
{
    internal static class StreamTemplates
    {
        public const string Outline = "outline";

        // id, menu name; the order of the menu.
        internal static readonly string[][] Catalog = {
            new[] { "outline", "縁取り" }, new[] { "bubble", "吹き出し" }, new[] { "card", "カード" }, new[] { "neon", "ネオン" },
            new[] { "vertical", "縦書き" }, new[] { "chat", "チャット" }, new[] { "talk", "トーク" }, new[] { "pop", "ポップ" },
            new[] { "washi", "和風" }, new[] { "terminal", "ターミナル" }, new[] { "rpg", "RPG（黒）" }, new[] { "rpgblue", "RPG（青）" }, new[] { "glass", "ガラス" } };

        // A panel look; Person* use the commenter's own color.
        private sealed class Look
        {
            public Color Fill, Fill2, Edge, Glow, Meta, Body, Accent;
            public float Radius = 12F, EdgeWidth = 1F;
            public bool Shadow, AccentBar, PersonFill, PersonMeta, PersonAccent, DoubleEdge, FullWidth, Seal, Tail, NameTab, Quote;
            public string BodyFace, MetaPrefix = "";
            public FontStyle BodyStyle = FontStyle.Regular;
        }

        private static Look For(string template)
        {
            switch (template)
            {
                case "card": return new Look { Fill = Color.FromArgb(246, 248, 250, 252), Edge = Color.FromArgb(40, 15, 23, 42), Meta = Color.FromArgb(100, 116, 139),
                    Body = Color.FromArgb(30, 41, 59), Radius = 10F, Shadow = true, AccentBar = true, PersonAccent = true };
                case "neon": return new Look { Fill = Color.FromArgb(185, 8, 12, 24), Edge = Color.FromArgb(125, 240, 250), Glow = Color.FromArgb(125, 240, 250),
                    Meta = Color.FromArgb(125, 240, 250), Body = Color.White, EdgeWidth = 1.6F };
                case "pop": return new Look { Edge = Color.White, EdgeWidth = 4F, Radius = 18F, Shadow = true, PersonFill = true,
                    Meta = Color.FromArgb(235, 255, 255, 255), Body = Color.White, BodyStyle = FontStyle.Bold };
                case "washi": return new Look { Fill = Color.FromArgb(242, 247, 241, 226), Edge = Color.FromArgb(120, 160, 120, 70), Meta = Color.FromArgb(150, 40, 30),
                    Body = Color.FromArgb(40, 30, 22), Radius = 3F, Shadow = true, Seal = true, BodyFace = "Yu Mincho", BodyStyle = FontStyle.Bold };
                case "terminal": return new Look { Fill = Color.FromArgb(225, 6, 10, 8), Edge = Color.FromArgb(80, 74, 222, 128), Meta = Color.FromArgb(74, 222, 128),
                    Body = Color.FromArgb(187, 247, 208), Radius = 4F, BodyFace = "MS Gothic", MetaPrefix = "> ", FullWidth = true };
                case "rpg": return new Look { Fill = Color.FromArgb(240, 0, 0, 0), Edge = Color.White, EdgeWidth = 3F, Meta = Color.White, Body = Color.White,
                    Radius = 7F, FullWidth = true, NameTab = true, Quote = true, BodyFace = "MS Gothic" };
                // The blue gradient window with a double frame.
                case "rpgblue": return new Look { Fill = Color.FromArgb(235, 20, 30, 110), Fill2 = Color.FromArgb(235, 6, 10, 48), Edge = Color.White, EdgeWidth = 3F,
                    Meta = Color.FromArgb(255, 230, 120), Body = Color.White, Radius = 8F, DoubleEdge = true, FullWidth = true };
                case "glass": return new Look { Fill = Color.FromArgb(150, 255, 255, 255), Edge = Color.FromArgb(200, 255, 255, 255), Meta = Color.FromArgb(71, 85, 105),
                    Body = Color.FromArgb(15, 23, 42), Radius = 16F, EdgeWidth = 1.2F, Shadow = true };
                case "talk": return new Look { Fill = Color.FromArgb(245, 255, 255, 255), Body = Color.FromArgb(25, 28, 34), Meta = Color.White, Radius = 16F, Tail = true, PersonMeta = true };
                default: return new Look { Fill = Color.FromArgb(205, 17, 21, 28), Edge = Color.FromArgb(60, 255, 255, 255), Meta = Color.FromArgb(147, 197, 253),
                    Body = Color.White, Radius = 14F };
            }
        }

        // The same commenter always gets the same color: from the ID, or the name without one.
        internal static Color PersonColor(Response response, double lightness)
        {
            string key = !string.IsNullOrEmpty(response.ID) ? response.ID : response.Name ?? "";
            uint hash = 2166136261;
            foreach (char c in key) hash = (hash ^ c) * 16777619;
            return FromHsl((hash % 360) / 360.0, 0.72, lightness);
        }

        private static Color FromHsl(double h, double s, double l)
        {
            Func<double, double> f = n => { double k = (n + h * 12) % 12, a = s * Math.Min(l, 1 - l); return l - a * Math.Max(-1, Math.Min(Math.Min(k - 3, 9 - k), 1)); };
            return Color.FromArgb((int)(f(0) * 255), (int)(f(8) * 255), (int)(f(4) * 255));
        }

        internal static void Draw(Graphics g, string template, IList<Response> responses, Size size, Func<Response, string> header,
            string fontName, float pixels, int inset, int gap, IntroLayer intro)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
            {
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                switch (template)
                {
                    case "vertical": Vertical(g, responses, size, header, fontName, pixels, inset, gap, format, intro); return;
                    case "chat": Chat(g, responses, size, fontName, pixels, inset, format, intro); return;
                    default: Panels(g, For(template), responses, size, header, fontName, pixels, inset, gap, format, intro); return;
                }
            }
        }

        // Newest comment at the bottom; each comment a panel sized to its text (or the full width).
        private static void Panels(Graphics g, Look look, IList<Response> responses, Size size, Func<Response, string> header,
            string fontName, float pixels, int inset, int gap, StringFormat format, IntroLayer intro)
        {
            float padX = Math.Max(8F, pixels * 0.6F) + (look.DoubleEdge ? 6F : 0F), padY = Math.Max(5F, pixels * 0.36F) + (look.DoubleEdge ? 4F : 0F);
            float bar = look.AccentBar ? 4F : look.Seal ? pixels * 0.9F : 0F, tail = look.Tail ? pixels * 0.5F : 0F;
            int maxWidth = size.Width - inset * 2;
            float bottom = size.Height - inset;
            using (var bodyFont = MakeFont(look.BodyFace ?? fontName, pixels, look.BodyStyle))
            using (var metaFont = MakeFont(look.BodyFace ?? fontName, pixels * 0.72F, FontStyle.Bold))
            {
                float textMax = maxWidth - padX * 2 - bar - tail;
                if (textMax < 20) return;
                for (int i = responses.Count - 1; i >= 0 && bottom > 0; i--)
                {
                    var d = i == responses.Count - 1 && intro != null ? intro.Graphics : g;
                    var response = responses[i];
                    Color person = PersonColor(response, 0.55);
                    string meta = header(response);
                    if (meta.Length > 0) meta = look.MetaPrefix + meta;
                    string text = (response.Text ?? "").Replace("\r\n", "\n").Trim();
                    if (look.Quote) { text = "＊「" + text + "」"; meta = string.IsNullOrEmpty(response.Name) ? "名無し" : response.Name; }
                    var bodySize = g.MeasureString(text.Length > 0 ? text : " ", bodyFont, (int)textMax, format);
                    // Talk bubbles and name tabs carry the name outside the panel.
                    bool metaInside = !look.Tail && !look.NameTab;
                    var metaSize = meta.Length > 0 ? g.MeasureString(meta, metaFont, (int)textMax, format) : SizeF.Empty;
                    float metaGap = meta.Length > 0 && metaInside ? pixels * 0.18F : 0F;
                    float w = look.FullWidth ? maxWidth : Math.Min(maxWidth, Math.Max(bodySize.Width, metaInside ? metaSize.Width : 0) + padX * 2 + bar);
                    float h = bodySize.Height + (metaInside ? metaSize.Height + metaGap : 0) + padY * 2 + (look.NameTab ? metaSize.Height * 0.35F : 0F);
                    float outside = metaInside ? 0F : look.NameTab ? metaSize.Height * 0.6F : metaSize.Height + 2F;
                    // Talk: each commenter keeps a side, by their color.
                    bool right = look.Tail && (PersonColor(response, 0.5).GetHue() % 2F) < 1F;
                    float left = right ? size.Width - inset - w - tail : inset + tail;
                    var box = new RectangleF(left, bottom - h, w, h);
                    var fill = look.PersonFill ? person : look.Fill;
                    Panel(d, look, box, fill, look.PersonAccent ? person : look.Accent, right);
                    float x = box.X + padX + bar, y = box.Y + padY;
                    if (meta.Length > 0)
                    {
                        using (var metaBrush = new SolidBrush(look.PersonMeta ? Color.White : look.Meta))
                        {
                            if (metaInside) { d.DrawString(meta, metaFont, metaBrush, new RectangleF(x, y, textMax, metaSize.Height + 2), format); y += metaSize.Height + metaGap; }
                            else if (look.NameTab)
                            {
                                // The speaker's name in its own small window across the top edge.
                                var tab = new RectangleF(box.X + 14F, box.Y - metaSize.Height * 0.6F - 3F, metaSize.Width + 16F, metaSize.Height + 6F);
                                using (var tabPath = ToolbarIcons.RoundRect(tab.X, tab.Y, tab.Width, tab.Height, 4F))
                                using (var tabFill = new SolidBrush(look.Fill))
                                using (var tabEdge = new Pen(look.Edge, 2F))
                                { d.FillPath(tabFill, tabPath); d.DrawPath(tabEdge, tabPath); }
                                d.DrawString(meta, metaFont, metaBrush, new RectangleF(tab.X + 8F, tab.Y + 3F, metaSize.Width + 2, metaSize.Height + 2), format);
                                y += metaSize.Height * 0.35F;
                            }
                            else
                            {
                                // A name tag in the commenter's color above the bubble.
                                var tag = new RectangleF(right ? box.Right - metaSize.Width - 12 : box.X, box.Y - outside - 2, metaSize.Width + 12, metaSize.Height + 2);
                                using (var tagPath = ToolbarIcons.RoundRect(tag.X, tag.Y, tag.Width, tag.Height, tag.Height / 2F))
                                using (var tagFill = new SolidBrush(person)) d.FillPath(tagFill, tagPath);
                                d.DrawString(meta, metaFont, metaBrush, new RectangleF(tag.X + 6, tag.Y + 1, metaSize.Width + 2, metaSize.Height + 2), format);
                            }
                        }
                    }
                    using (var bodyBrush = new SolidBrush(look.Body)) d.DrawString(text, bodyFont, bodyBrush, new RectangleF(x, y, textMax, bodySize.Height + 2), format);
                    bottom = box.Y - outside - gap - (look.Tail ? 4 : 0);
                }
                if (intro != null) intro.Finish(g);
            }
        }

        private static void Panel(Graphics g, Look look, RectangleF box, Color fill, Color accent, bool tailRight)
        {
            using (var path = ToolbarIcons.RoundRect(box.X, box.Y, box.Width, box.Height, look.Radius))
            {
                if (look.Tail)
                {
                    // A small tail pointing out of the bubble's lower corner.
                    float tx = tailRight ? box.Right : box.X, dir = tailRight ? 1F : -1F, ty = box.Bottom - look.Radius;
                    path.AddPolygon(new[] { new PointF(tx - dir * 2F, ty - 10F), new PointF(tx + dir * 9F, ty + 6F), new PointF(tx - dir * 2F, ty + 2F) });
                    path.FillMode = FillMode.Winding;
                }
                if (look.Shadow)
                    using (var shadowPath = ToolbarIcons.RoundRect(box.X + 1.5F, box.Y + 3F, box.Width, box.Height, look.Radius))
                    using (var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0))) g.FillPath(shadow, shadowPath);
                if (look.Glow != Color.Empty)
                    for (int i = 4; i >= 1; i--)
                        using (var glow = new Pen(Color.FromArgb(22 * (5 - i), look.Glow), look.EdgeWidth + i * 2.4F)) g.DrawPath(glow, path);
                if (look.Fill2 != Color.Empty)
                    using (var gradient = new LinearGradientBrush(box, fill, look.Fill2, 90F)) g.FillPath(gradient, path);
                else using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                if (look.Edge != Color.Empty) using (var edge = new Pen(look.Edge, look.EdgeWidth)) g.DrawPath(edge, path);
                if (look.DoubleEdge)
                    using (var innerPath = ToolbarIcons.RoundRect(box.X + 5F, box.Y + 5F, box.Width - 10F, box.Height - 10F, Math.Max(2F, look.Radius - 4F)))
                    using (var inner = new Pen(Color.FromArgb(170, look.Edge), 1.2F)) g.DrawPath(inner, innerPath);
                if (look.AccentBar)
                {
                    var state = g.Save();
                    g.SetClip(path);
                    using (var brush = new SolidBrush(accent)) g.FillRectangle(brush, box.X, box.Y, 4F, box.Height);
                    g.Restore(state);
                }
                if (look.Seal)
                {
                    // A vermilion seal at the left, like a hanko on paper.
                    float s = Math.Min(box.Height - 10F, 22F);
                    using (var seal = new SolidBrush(Color.FromArgb(200, 190, 40, 30))) g.FillEllipse(seal, box.X + 7F, box.Y + 7F, s * 0.8F, s * 0.8F);
                }
            }
        }

        // Twitch-style lines: the name in the commenter's color, then the message, outlined for any background.
        private static void Chat(Graphics g, IList<Response> responses, Size size, string fontName, float pixels, int inset, StringFormat format, IntroLayer intro)
        {
            int width = size.Width - inset * 2;
            float bottom = size.Height - inset;
            using (var font = MakeFont(fontName, pixels, FontStyle.Regular))
            using (var nameFont = MakeFont(fontName, pixels, FontStyle.Bold))
            using (var outline = new Pen(Color.FromArgb(230, 0, 0, 0), Math.Max(2F, pixels / 7F)) { LineJoin = LineJoin.Round })
            {
                for (int i = responses.Count - 1; i >= 0 && bottom > 0; i--)
                {
                    var d = i == responses.Count - 1 && intro != null ? intro.Graphics : g;
                    var response = responses[i];
                    string name = string.IsNullOrEmpty(response.Name) ? "名無し" : response.Name;
                    string text = (response.Text ?? "").Replace("\r\n", " ").Replace("\n", " ").Trim();
                    float nameWidth = g.MeasureString(name + " ", nameFont, PointF.Empty, format).Width;
                    // The message wraps under itself, indented past the name.
                    var textSize = g.MeasureString(text.Length > 0 ? text : " ", font, (int)Math.Max(20, width - nameWidth), format);
                    float top = bottom - textSize.Height;
                    using (var path = new GraphicsPath())
                    {
                        path.AddString(name, nameFont.FontFamily, (int)nameFont.Style, pixels, new PointF(inset, top), format);
                        d.DrawPath(outline, path);
                        using (var brush = new SolidBrush(PersonColor(response, 0.66))) d.FillPath(brush, path);
                    }
                    using (var path = new GraphicsPath())
                    {
                        path.AddString(text, font.FontFamily, (int)font.Style, pixels, new RectangleF(inset + nameWidth, top, width - nameWidth, textSize.Height + 4), format);
                        d.DrawPath(outline, path);
                        d.FillPath(Brushes.White, path);
                    }
                    bottom = top - pixels * 0.3F;
                }
                if (intro != null) intro.Finish(g);
            }
        }

        // Vertical writing: columns read right to left; the newest comment is the leftmost column.
        private static void Vertical(Graphics g, IList<Response> responses, Size size, Func<Response, string> header, string fontName, float pixels, int inset, int gap, StringFormat format, IntroLayer intro)
        {
            using (var vertical = (StringFormat)format.Clone())
            using (var font = MakeFont(fontName, pixels, FontStyle.Bold))
            using (var metaFont = MakeFont(fontName, pixels * 0.66F, FontStyle.Bold))
            using (var outline = new Pen(Color.FromArgb(235, 0, 0, 0), Math.Max(2F, pixels / 7F)) { LineJoin = LineJoin.Round })
            {
                vertical.FormatFlags |= StringFormatFlags.DirectionVertical | StringFormatFlags.DirectionRightToLeft; // columns run right to left
                int height = size.Height - inset * 2;
                float left = inset;
                for (int i = responses.Count - 1; i >= 0 && left < size.Width - inset; i--)
                {
                    var d = i == responses.Count - 1 && intro != null ? intro.Graphics : g;
                    var response = responses[i];
                    string text = Upright((response.Text ?? "").Replace("\r\n", "\n").Trim()), meta = Upright(header(response));
                    // Measured vertically: Width is the column thickness, Height the column length.
                    var textSize = g.MeasureString(text.Length > 0 ? text : " ", font, new SizeF(size.Width, height), vertical);
                    var metaSize = meta.Length > 0 ? g.MeasureString(meta, metaFont, new SizeF(size.Width, height), vertical) : SizeF.Empty;
                    float columns = textSize.Width + metaSize.Width + 4F;
                    // Right to left inside one comment: the name column first (rightmost), then the text.
                    float x = left + columns;
                    if (meta.Length > 0)
                    {
                        x -= metaSize.Width;
                        using (var metaBrush = new SolidBrush(PersonColor(response, 0.7)))
                        using (var path = new GraphicsPath())
                        {
                            path.AddString(meta, metaFont.FontFamily, (int)metaFont.Style, pixels * 0.66F, new RectangleF(x, inset, metaSize.Width + 2, height), vertical);
                            d.DrawPath(outline, path);
                            d.FillPath(metaBrush, path);
                        }
                        x -= 4F;
                    }
                    x -= textSize.Width;
                    using (var path = new GraphicsPath())
                    {
                        path.AddString(text, font.FontFamily, (int)font.Style, pixels, new RectangleF(x, inset, textSize.Width + 2, height), vertical);
                        d.DrawPath(outline, path);
                        d.FillPath(Brushes.White, path);
                    }
                    left += columns + gap + pixels * 0.4F;
                }
                if (intro != null) intro.Finish(g);
            }
        }

        // Vertical text lays Latin letters, digits and ASCII marks on their side; their
        // full-width forms stand upright like kana ("23:41" -> "２３：４１").
        internal static string Upright(string text)
        {
            var chars = (text ?? "").ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c >= '!' && c <= '~') chars[i] = (char)(c - '!' + '！');
                else if (c == ' ') chars[i] = '　';
            }
            return new string(chars);
        }

        private static Font MakeFont(string name, float pixels, FontStyle style)
        {
            if (name == BundledFont.FamilyName && BundledFont.Family != null) return new Font(BundledFont.Family, pixels, FontStyle.Regular, GraphicsUnit.Pixel);
            try
            {
                var font = new Font(name, pixels, style, GraphicsUnit.Pixel);
                if (font.Name == name || name == null) return font;
                font.Dispose();
            }
            catch (ArgumentException) { }
            return new Font("Yu Gothic UI", pixels, style, GraphicsUnit.Pixel);
        }
    }

    // The newest comment is drawn on its own layer, then laid over the frame sliding in from
    // the left and fading in: the same entrance for every template.
    internal sealed class IntroLayer : IDisposable
    {
        private readonly Bitmap layer;
        private readonly float progress;
        public readonly Graphics Graphics;

        public IntroLayer(Size size, float progress, Graphics like)
        {
            this.progress = Math.Max(0F, Math.Min(1F, progress));
            layer = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics = Graphics.FromImage(layer);
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            Graphics.PixelOffsetMode = like.PixelOffsetMode;
        }

        public void Finish(Graphics target)
        {
            float eased = 1F - (float)Math.Pow(1F - progress, 3);
            var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = eased };
            using (var attributes = new System.Drawing.Imaging.ImageAttributes())
            {
                attributes.SetColorMatrix(matrix);
                int dx = (int)Math.Round(-(1F - eased) * 28F);
                target.DrawImage(layer, new Rectangle(dx, 0, layer.Width, layer.Height), 0, 0, layer.Width, layer.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        public void Dispose() { Graphics.Dispose(); layer.Dispose(); }
    }
}
