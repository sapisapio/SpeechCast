// Free telop: text typed in the main window, shown in its own overlay window, 2026-10-19. GPLv2.
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace SpeechCastMe
{
    internal sealed class FormFreeTelop : OverlayWindow
    {
        private const int Inset = 10;
        private readonly Func<string> source;

        public FormFreeTelop(Func<string> source, Rectangle bounds) : base("Speech Cast SP 自由テロップ", bounds)
        {
            this.source = source;
        }

        protected override string RenderKey()
        {
            var config = FormMain.UserConfig;
            if (config == null) return null;
            return string.Join("|", source() ?? "", config.TelopFontName, config.TelopFontSize);
        }

        protected override Bitmap Render(Size size) { return RenderFrame(source(), size); }

        // Text from the top-left, wrapped to the window width, white with a black outline.
        internal static Bitmap RenderFrame(string text, Size size)
        {
            var config = FormMain.UserConfig;
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            text = (text ?? "").Replace("\r\n", "\n").TrimEnd();
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                int width = size.Width - Inset * 2;
                if (text.Length == 0 || width < 1) return bitmap;
                string name = string.IsNullOrEmpty(config.TelopFontName) ? BundledFont.FamilyName : config.TelopFontName;
                var style = FormStreamOverlay.Style(name, Math.Max(10, Math.Min(160, config.TelopFontSize)), Color.White);
                using (var format = (System.Drawing.StringFormat)System.Drawing.StringFormat.GenericTypographic.Clone())
                using (var font = style.CreateFont())
                {
                    format.FormatFlags |= System.Drawing.StringFormatFlags.MeasureTrailingSpaces;
                    int height = Math.Min(size.Height - Inset, FormStreamOverlay.Measure(g, text, font, width, format));
                    CaptionRasterizer.DrawOutlined(g, new Rectangle(Inset, Inset, width, height), text, style, false);
                }
            }
            return bitmap;
        }
    }
}
