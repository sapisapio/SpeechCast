// Stream comments: the comment feed in a transparent overlay window, 2026-10-19. GPLv2.
// Text is drawn like the caption: white with a black outline, so it stays
// readable on any background.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SpeechCastMe
{
    internal sealed class FormStreamOverlay : OverlayWindow
    {
        private const int Inset = 10, Gap = 6;
        private readonly Func<IList<Response>> source;

        public FormStreamOverlay(Func<IList<Response>> source, Rectangle bounds) : base("Speech Cast SP 配信コメント", bounds)
        {
            this.source = source;
        }

        protected override string RenderKey()
        {
            var responses = source();
            var config = FormMain.UserConfig;
            if (config == null || responses == null) return null;
            var last = responses.Count > 0 ? responses[responses.Count - 1] : null;
            return string.Join("|", responses.Count, last == null ? "" : last.Number + last.RawText, responses.Count > 0 ? responses[0].Number : 0,
                config.StreamFontName, config.StreamFontSize, config.CommentFontSize, config.CommentLayout,
                config.CommentShowNumber, config.CommentShowName, config.CommentShowDate, config.CommentShowTime, config.CommentShowID);
        }

        protected override Bitmap Render(Size size) { return RenderFrame(source(), size); }

        // Newest comment at the bottom, older ones stacked above until the top edge.
        internal static Bitmap RenderFrame(IList<Response> responses, Size size)
        {
            var config = FormMain.UserConfig;
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                bool twoLine = config.CommentLayout == "two" || config.CommentLayout == "named";
                float pixels = Math.Max(10, Math.Min(80, config.StreamFontSize > 0 ? config.StreamFontSize : config.CommentFontSize));
                string fontName = string.IsNullOrEmpty(config.StreamFontName) ? BundledFont.FamilyName : config.StreamFontName;
                var body = Style(fontName, pixels, Color.White);
                var meta = Style(fontName, pixels * 0.78F, Color.FromArgb(214, 222, 233));
                int width = size.Width - Inset * 2;
                int bottom = size.Height - Inset;
                using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
                using (var bodyFont = body.CreateFont())
                using (var metaFont = meta.CreateFont())
                {
                    format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                    for (int i = responses.Count - 1; i >= 0 && bottom > 0 && width > 0; i--)
                    {
                        string header = Header(responses[i], config);
                        string text = (responses[i].Text ?? "").Replace("\r\n", "\n");
                        if (!twoLine && header.Length > 0) { text = header + "  " + text; header = ""; }
                        int bodyHeight = Measure(g, text, bodyFont, width, format);
                        int metaHeight = header.Length > 0 ? Measure(g, header, metaFont, width, format) : 0;
                        int top = bottom - bodyHeight - metaHeight;
                        if (metaHeight > 0) CaptionRasterizer.DrawOutlined(g, new Rectangle(Inset, top, width, metaHeight), header, meta, false);
                        CaptionRasterizer.DrawOutlined(g, new Rectangle(Inset, top + metaHeight, width, bodyHeight), text, body, false);
                        bottom = top - Gap;
                    }
                }
            }
            return bitmap;
        }

        internal static CaptionFont Style(string name, float pixels, Color color)
        {
            var style = new CaptionFont { Name = string.IsNullOrEmpty(name) ? "Meiryo" : name, Size = pixels * 72F / 96F, BorderSize = 3, Bold = false };
            style.Colors.HighForeColor = color;
            style.Colors.LowForeColor = color;
            style.Colors.BorderColor = Color.Black;
            return style;
        }

        internal static int Measure(Graphics g, string text, Font font, int width, StringFormat format)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (int)Math.Ceiling(g.MeasureString(text, font, width, format).Height) + 2;
        }

        private static string Header(Response response, UserConfig config)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(response.SourceLabel)) parts.Add(response.SourceLabel);
            if (config.CommentShowNumber) parts.Add(response.DisplayNumber + ":");
            if (config.CommentShowName && !string.IsNullOrEmpty(response.Name)) parts.Add(response.Name);
            string timestamp = response.DateTime ?? "";
            if (config.CommentShowDate) { var m = Regex.Match(timestamp, @"\d{4}/\d{1,2}/\d{1,2}(?:[（(][^）)]*[）)])?"); if (m.Success) parts.Add(m.Value); }
            if (config.CommentShowTime) { var m = Regex.Match(timestamp, @"\d{1,2}:\d{2}"); if (m.Success) parts.Add(m.Value); }
            if (config.CommentShowID && !string.IsNullOrEmpty(response.ID)) parts.Add("ID:" + response.ID);
            return string.Join(" ", parts);
        }
    }
}
