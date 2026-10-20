// Clock and timer gadgets: their own overlay windows for OBS window capture, drawn
// as one rounded panel each in a dark or light style chosen apart from the app theme, 2026-10-19. GPLv2.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;

namespace SpeechCastMe
{
    internal sealed class GadgetPalette
    {
        public Color Panel, Border, Label, Digits, Date, Accent;

        public static readonly GadgetPalette Dark = new GadgetPalette {
            Panel = Color.FromArgb(240, 40, 43, 47), Border = Color.FromArgb(255, 72, 76, 82),
            Label = Color.White, Digits = Color.White, Date = Color.FromArgb(226, 229, 233), Accent = Color.FromArgb(125, 240, 250) };

        public static readonly GadgetPalette Light = new GadgetPalette {
            Panel = Color.FromArgb(244, 237, 238, 240), Border = Color.FromArgb(255, 214, 217, 221),
            Label = Color.FromArgb(24, 26, 29), Digits = Color.FromArgb(24, 26, 29), Date = Color.FromArgb(40, 43, 47), Accent = Color.FromArgb(12, 116, 140) };

        public static GadgetPalette For(string name) { return name == "light" ? Light : Dark; }
    }

    internal static class GadgetArt
    {
        private const string Face = "Segoe UI";
        // Layouts measured from the reference design, in its pixels; drawings scale to the window.
        internal static readonly Size ClockDesign = new Size(620, 350);
        internal static readonly Size TimerDesign = new Size(765, 295);

        internal static string Theme { get { return FormMain.UserConfig != null ? FormMain.UserConfig.GadgetTheme : "dark"; } }

        internal static Bitmap RenderClock(DateTime now, Size size)
        {
            var palette = GadgetPalette.For(Theme);
            return Draw(size, ClockDesign, palette, g => {
                Spaced(g, "CLOCK", 45F, FontStyle.Bold, palette.Label, 66F, 76F, 0.2F);
                Spaced(g, now.ToString("HH:mm"), 190F, FontStyle.Bold, palette.Digits, 60F, 241F, 0F);
                string date = now.ToString("yyyy.MM.dd ", CultureInfo.InvariantCulture) + now.ToString("ddd", CultureInfo.InvariantCulture).ToUpperInvariant();
                Spaced(g, date, 40F, FontStyle.Regular, palette.Date, 68F, 304F, 0.14F);
            });
        }

        internal static Bitmap RenderTimer(GadgetTimer.Reading reading, bool running, DateTime now, Size size)
        {
            var palette = GadgetPalette.For(Theme);
            // Just finished: the digits blink for a minute, then stay lit.
            bool dim = reading.Finished && reading.FinishedAt != null && (now - reading.FinishedAt.Value).TotalSeconds < 60 && now.Millisecond >= 500;
            var digits = dim ? Color.FromArgb(90, palette.Accent) : palette.Accent;
            return Draw(size, TimerDesign, palette, g => {
                // A filled dot while running, a ring while stopped.
                var dot = new RectangleF(58F, 43F, 36F, 36F);
                if (running) using (var brush = new SolidBrush(palette.Accent)) g.FillEllipse(brush, dot);
                else using (var pen = new Pen(palette.Accent, 5F)) g.DrawEllipse(pen, RectangleF.Inflate(dot, -2.5F, -2.5F));
                Spaced(g, reading.Label, 47F, FontStyle.Bold, palette.Accent, 122F, 78F, 0.16F);
                var value = reading.Value < TimeSpan.Zero ? TimeSpan.Zero : reading.Value;
                int hours = Math.Min(99, (int)value.TotalHours);
                string main = hours.ToString("00") + ":" + value.Minutes.ToString("00"), tail = ":" + value.Seconds.ToString("00");
                if (reading.Idle) digits = Color.FromArgb(80, palette.Accent); // an alarm not set: dim zeros
                // Shrink to the panel when the face is wider than the design's (keeps a right margin like the left one).
                float fit = Math.Min(1F, (TimerDesign.Width - 62F - 58F) / (Width(g, main, 195F, FontStyle.Bold) + 4F + Width(g, tail, 150F, FontStyle.Regular)));
                float x = Spaced(g, main, 195F * fit, FontStyle.Bold, digits, 62F, 247F, 0F);
                Spaced(g, tail, 150F * fit, FontStyle.Regular, digits, x + 4F * fit, 247F, 0F);
            });
        }

        internal static string TimerText(GadgetTimer.Reading reading)
        {
            if (reading.Idle) return "--";
            var value = reading.Value < TimeSpan.Zero ? TimeSpan.Zero : reading.Value;
            return Math.Min(99, (int)value.TotalHours).ToString("00") + ":" + value.Minutes.ToString("00") + ":" + value.Seconds.ToString("00");
        }

        // The panel fills the window; the content is laid out in design pixels from its top-left.
        private static Bitmap Draw(Size size, Size design, GadgetPalette palette, Action<Graphics> content)
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float scale = Math.Min(size.Width / (float)design.Width, size.Height / (float)design.Height);
                float radius = Math.Max(4F, 26F * scale);
                using (var panel = ToolbarIcons.RoundRect(0.5F, 0.5F, size.Width - 1.5F, size.Height - 1.5F, radius))
                {
                    using (var fill = new SolidBrush(palette.Panel)) g.FillPath(fill, panel);
                    using (var edge = new Pen(palette.Border, Math.Max(1F, 2F * scale))) g.DrawPath(edge, panel);
                }
                g.ScaleTransform(scale, scale);
                content(g);
            }
            return bitmap;
        }

        private static float Width(Graphics g, string text, float size, FontStyle style)
        {
            using (var font = new Font(Face, size, style, GraphicsUnit.Pixel))
            using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
            {
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                return g.MeasureString(text, font, PointF.Empty, format).Width;
            }
        }

        // Draws text with its baseline at y and extra spacing between letters (in em);
        // returns the x where the text ends.
        private static float Spaced(Graphics g, string text, float size, FontStyle style, Color color, float x, float baseline, float spacing)
        {
            using (var family = new FontFamily(Face))
            using (var font = new Font(family, size, style, GraphicsUnit.Pixel))
            using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
            using (var brush = new SolidBrush(color))
            using (var path = new GraphicsPath())
            {
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                float ascent = size * family.GetCellAscent(style) / family.GetEmHeight(style);
                float top = baseline - ascent;
                if (spacing <= 0F)
                {
                    path.AddString(text, family, (int)style, size, new PointF(x, top), format);
                    g.FillPath(brush, path);
                    return x + g.MeasureString(text, font, PointF.Empty, format).Width;
                }
                foreach (char c in text)
                {
                    string s = c.ToString();
                    path.AddString(s, family, (int)style, size, new PointF(x, top), format);
                    x += g.MeasureString(s, font, PointF.Empty, format).Width + size * spacing;
                }
                g.FillPath(brush, path);
                return x - size * spacing;
            }
        }
    }

    internal sealed class FormClockGadget : OverlayWindow
    {
        public FormClockGadget(Rectangle bounds) : base("Speech Cast SP 時計", bounds) { }

        protected override string RenderKey() { return GadgetArt.Theme + "|" + DateTime.Now.ToString("yyyyMMddHHmm"); }

        protected override Bitmap Render(Size size) { return GadgetArt.RenderClock(DateTime.Now, size); }
    }

    internal sealed class FormTimerGadget : OverlayWindow
    {
        private readonly GadgetTimer timer;

        // The window title names the kind, so OBS can tell several timers apart.
        public FormTimerGadget(GadgetTimer timer, Rectangle bounds, string kind) : base("Speech Cast SP " + kind, bounds) { this.timer = timer; }

        protected override string RenderKey()
        {
            var reading = timer.Read();
            var now = DateTime.Now;
            bool blinking = reading.Finished && reading.FinishedAt != null && (now - reading.FinishedAt.Value).TotalSeconds < 60;
            return string.Join("|", GadgetArt.Theme, reading.Label, GadgetArt.TimerText(reading), timer.Running, blinking && now.Millisecond >= 500);
        }

        protected override Bitmap Render(Size size) { return GadgetArt.RenderTimer(timer.Read(), timer.Running, DateTime.Now, size); }
    }
}
