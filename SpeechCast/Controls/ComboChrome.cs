// Paints a flat combo box's edge and drop-down button in the dark window theme;
// the flat style otherwise draws them in white system colors, 2026-10-19. GPLv2.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpeechCastMe.Controls
{
    internal sealed class ComboChrome : NativeWindow
    {
        private const int WM_PAINT = 0x000F;
        private readonly ComboBox box;
        private WindowTheme palette;

        public ComboChrome(ComboBox box)
        {
            this.box = box;
            if (box.IsHandleCreated) AssignHandle(box.Handle);
            box.HandleCreated += delegate { AssignHandle(box.Handle); };
            box.HandleDestroyed += delegate { ReleaseHandle(); };
        }

        // null leaves the flat style's own drawing (the light theme).
        internal WindowTheme Palette
        {
            get { return palette; }
            set { palette = value; box.Invalidate(); }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_PAINT && palette != null) PaintChrome();
        }

        private void PaintChrome()
        {
            int width = box.ClientSize.Width, height = box.ClientSize.Height;
            if (width < 8 || height < 8) return;
            int button = SystemInformation.HorizontalScrollBarArrowWidth;
            bool hot = box.Enabled && box.ClientRectangle.Contains(box.PointToClient(Control.MousePosition));
            using (var g = Graphics.FromHwnd(box.Handle))
            using (var edge = new Pen(hot || box.Focused ? palette.Separator : palette.Border))
            using (var inner = new Pen(palette.Field))
            using (var fill = new SolidBrush(hot ? palette.HoverFill : palette.Field))
            {
                g.DrawRectangle(inner, 1, 1, width - 3, height - 3);
                g.DrawRectangle(edge, 0, 0, width - 1, height - 1);
                var area = new Rectangle(width - button - 1, 1, button, height - 2);
                g.FillRectangle(fill, area);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float cx = area.Left + area.Width / 2F, cy = area.Top + area.Height / 2F;
                using (var arrow = new Pen(box.Enabled ? palette.Ink : palette.MutedText, 1.5F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    g.DrawLines(arrow, new[] { new PointF(cx - 3.5F, cy - 1.5F), new PointF(cx, cy + 2F), new PointF(cx + 3.5F, cy - 1.5F) });
            }
        }
    }
}
