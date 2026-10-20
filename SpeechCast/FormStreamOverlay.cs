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
            // A new newest comment starts its entrance; the first frame just shows what is there.
            string newest = last == null ? "" : last.Number + last.RawText;
            if (introKey != null && newest != introKey && last != null) introStart = DateTime.Now;
            introKey = newest;
            float progress = IntroProgress();
            SetRefreshInterval(progress < 1F ? 30 : 250);
            return string.Join("|", progress < 1F ? ((int)(progress * 24)).ToString() : "-", responses.Count, last == null ? "" : last.Number + last.RawText, responses.Count > 0 ? responses[0].Number : 0,
                config.StreamFontName, config.StreamFontSize, config.CommentFontSize, config.CommentLayout,
                config.StreamShowNumber, config.StreamShowName, config.StreamShowDate, config.StreamShowTime, config.StreamShowID, config.StreamTemplate);
        }

        // The newest comment slides in for IntroMilliseconds after it arrives.
        private const double IntroMilliseconds = 400;
        private string introKey;
        private DateTime introStart = DateTime.MinValue;

        private float IntroProgress()
        {
            var config = FormMain.UserConfig;
            if (config == null || !config.StreamIntro) return 1F;
            return (float)Math.Min(1.0, (DateTime.Now - introStart).TotalMilliseconds / IntroMilliseconds);
        }

        protected override Bitmap Render(Size size) { return RenderFrame(source(), size, IntroProgress()); }

        internal static Bitmap RenderFrame(IList<Response> responses, Size size) { return RenderFrame(responses, size, 1F); }

        // Newest comment at the bottom, older ones stacked above until the top edge.
        internal static Bitmap RenderFrame(IList<Response> responses, Size size, float introProgress)
        {
            var config = FormMain.UserConfig;
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                int inset = Inset;
                bool twoLine = config.CommentLayout == "two" || config.CommentLayout == "named";
                float pixels = Math.Max(10, Math.Min(80, config.StreamFontSize > 0 ? config.StreamFontSize : config.CommentFontSize));
                string fontName = string.IsNullOrEmpty(config.StreamFontName) ? BundledFont.FamilyName : config.StreamFontName;
                using (var intro = introProgress < 1F && responses.Count > 0 ? new IntroLayer(size, introProgress, g) : null)
                {
                if (!string.IsNullOrEmpty(config.StreamTemplate) && config.StreamTemplate != StreamTemplates.Outline)
                {
                    StreamTemplates.Draw(g, config.StreamTemplate, responses, size, r => Header(r, config), fontName, pixels, inset, Gap, intro);
                    return bitmap;
                }
                var body = Style(fontName, pixels, Color.White);
                var meta = Style(fontName, pixels * 0.78F, Color.FromArgb(214, 222, 233));
                int width = size.Width - inset * 2;
                int bottom = size.Height - inset;
                using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
                using (var bodyFont = body.CreateFont())
                using (var metaFont = meta.CreateFont())
                {
                    format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                    for (int i = responses.Count - 1; i >= 0 && bottom > 0 && width > 0; i--)
                    {
                    var d = i == responses.Count - 1 && intro != null ? intro.Graphics : g;
                        string header = Header(responses[i], config);
                        string text = (responses[i].Text ?? "").Replace("\r\n", "\n");
                        if (!twoLine && header.Length > 0) { text = header + "  " + text; header = ""; }
                        int bodyHeight = Measure(g, text, bodyFont, width, format);
                        int metaHeight = header.Length > 0 ? Measure(g, header, metaFont, width, format) : 0;
                        int top = bottom - bodyHeight - metaHeight;
                        if (metaHeight > 0) CaptionRasterizer.DrawOutlined(d, new Rectangle(inset, top, width, metaHeight), header, meta, false);
                        CaptionRasterizer.DrawOutlined(d, new Rectangle(inset, top + metaHeight, width, bodyHeight), text, body, false);
                        bottom = top - Gap;
                    }
                    if (intro != null) intro.Finish(g);
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
            if (config.StreamShowNumber) parts.Add(response.DisplayNumber + ":");
            if (config.StreamShowName && !string.IsNullOrEmpty(response.Name)) parts.Add(response.Name);
            string timestamp = response.DateTime ?? "";
            if (config.StreamShowDate) { var m = Regex.Match(timestamp, @"\d{4}/\d{1,2}/\d{1,2}(?:[（(][^）)]*[）)])?"); if (m.Success) parts.Add(m.Value); }
            if (config.StreamShowTime) { var m = Regex.Match(timestamp, @"\d{1,2}:\d{2}"); if (m.Success) parts.Add(m.Value); }
            if (config.StreamShowID && !string.IsNullOrEmpty(response.ID)) parts.Add("ID:" + response.ID);
            return string.Join(" ", parts);
        }
    }
}
