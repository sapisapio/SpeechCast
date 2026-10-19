// Stream mode: the comment feed in a click-through window with per-pixel alpha, 2026-10-19. GPLv2.
// OBS "Window Capture" (Windows 10 1903+ method) keeps the transparency, so the
// window can be laid over the broadcast as is. Text is drawn like the caption:
// white with a black outline, so it stays readable on any background.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SpeechCastMe
{
    internal sealed class FormStreamOverlay : Form
    {
        private const int Inset = 10, Gap = 6;
        private readonly Func<IList<Response>> source;
        private readonly Timer refresh = new Timer { Interval = 250 };
        private string renderedKey;
        internal bool LastUpdateSucceeded { get; private set; }

        public FormStreamOverlay(Func<IList<Response>> source, Rectangle bounds)
        {
            this.source = source;
            Text = "Speech Cast SP 配信コメント";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            TopMost = true;
            // A size set before the handle exists is reduced by the default frame
            // once the window is created; apply the requested bounds again.
            Load += delegate { Bounds = bounds; };
            ShowInTaskbar = true;
            refresh.Tick += delegate { RenderIfChanged(); };
            Shown += delegate { RenderIfChanged(); refresh.Start(); };
            Move += delegate { Push(); };
            Resize += delegate { renderedKey = null; RenderIfChanged(); };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Layered (per-pixel alpha), click-through, never takes focus.
                cp.ExStyle |= 0x80000 | 0x20 | 0x08000000;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { refresh.Dispose(); if (frame != null) frame.Dispose(); }
            base.Dispose(disposing);
        }

        private Bitmap frame;

        private void RenderIfChanged()
        {
            var responses = source();
            var config = FormMain.UserConfig;
            if (config == null || responses == null || ClientSize.Width < 1 || ClientSize.Height < 1) return;
            var last = responses.Count > 0 ? responses[responses.Count - 1] : null;
            string key = string.Join("|", responses.Count, last == null ? "" : last.Number + last.RawText, responses.Count > 0 ? responses[0].Number : 0,
                ClientSize, config.StreamFontName, config.StreamFontSize, config.CommentFontSize, config.CommentLayout,
                config.CommentShowNumber, config.CommentShowName, config.CommentShowDate, config.CommentShowTime, config.CommentShowID);
            if (key == renderedKey) return;
            renderedKey = key;
            var next = RenderFrame(responses, ClientSize);
            if (frame != null) frame.Dispose();
            frame = next;
            Push();
        }

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

        private static CaptionFont Style(string name, float pixels, Color color)
        {
            var style = new CaptionFont { Name = string.IsNullOrEmpty(name) ? "Meiryo" : name, Size = pixels * 72F / 96F, BorderSize = 3, Bold = false };
            style.Colors.HighForeColor = color;
            style.Colors.LowForeColor = color;
            style.Colors.BorderColor = Color.Black;
            return style;
        }

        private static int Measure(Graphics g, string text, Font font, int width, StringFormat format)
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

        private void Push()
        {
            if (frame == null || !IsHandleCreated) return;
            LastUpdateSucceeded = LayeredWindow.Update(Handle, Location, frame);
        }
    }

    internal static class LayeredWindow
    {
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct NativeSize { public int Width, Height; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct BlendFunction { public byte Operation, Flags, ConstantAlpha, Format; }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr destination, ref NativePoint location, ref NativeSize size, IntPtr source, ref NativePoint origin, int colorKey, ref BlendFunction blend, int flags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);

        internal static bool Update(IntPtr window, Point location, Bitmap bitmap)
        {
            IntPtr screen = GetDC(IntPtr.Zero), memory = CreateCompatibleDC(screen), handle = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                handle = bitmap.GetHbitmap(Color.FromArgb(0));
                previous = SelectObject(memory, handle);
                var at = new NativePoint { X = location.X, Y = location.Y };
                var origin = new NativePoint();
                var size = new NativeSize { Width = bitmap.Width, Height = bitmap.Height };
                var blend = new BlendFunction { ConstantAlpha = 255, Format = 1 };
                return UpdateLayeredWindow(window, screen, ref at, ref size, memory, ref origin, 0, ref blend, 2);
            }
            finally
            {
                if (previous != IntPtr.Zero) SelectObject(memory, previous);
                if (handle != IntPtr.Zero) DeleteObject(handle);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }
}
