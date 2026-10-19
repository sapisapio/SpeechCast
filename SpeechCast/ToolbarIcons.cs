// Toolbar icons drawn as vectors and the toolbar renderer, 2026-10-19. GPLv2.
// One consistent line-icon set at the screen's DPI instead of font glyphs
// (GDI renders emoji monochrome and kanji glyphs read poorly as icons).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpeechCastMe
{
    internal static class ToolbarIcons
    {
        internal static readonly Color Ink = Color.FromArgb(51, 65, 85);

        // Icons are designed on a 20 x 20 grid and scaled to the requested size.
        internal static Bitmap Create(string name, int size)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            using (var pen = new Pen(Ink, 1.7F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (var brush = new SolidBrush(Ink))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.ScaleTransform(size / 20F, size / 20F);
                Draw(g, name, pen, brush);
            }
            return bitmap;
        }

        private static void Draw(Graphics g, string name, Pen pen, Brush brush)
        {
            switch (name)
            {
                case "threads":
                    foreach (float y in new[] { 5.5F, 10F, 14.5F })
                    {
                        g.FillEllipse(brush, 3.2F, y - 1.3F, 2.6F, 2.6F);
                        g.DrawLine(pen, 8F, y, 16.5F, y);
                    }
                    break;
                case "write":
                    g.DrawPolygon(pen, new[] { new PointF(4F, 16F), new PointF(4.8F, 12.4F), new PointF(12.8F, 4.4F), new PointF(15.6F, 7.2F), new PointF(7.6F, 15.2F) });
                    g.DrawLine(pen, 11F, 6.2F, 13.8F, 9F);
                    break;
                case "first":
                    g.FillRectangle(brush, 4.5F, 5F, 2F, 10F);
                    Triangle(g, brush, 15.5F, 5F, 15.5F, 15F, 7.5F, 10F);
                    break;
                case "prev":
                    Triangle(g, brush, 10F, 5F, 10F, 15F, 3F, 10F);
                    Triangle(g, brush, 17F, 5F, 17F, 15F, 10F, 10F);
                    break;
                case "stop":
                    using (var path = RoundRect(5F, 5F, 10F, 10F, 1.6F)) g.FillPath(brush, path);
                    break;
                case "play":
                    Triangle(g, brush, 6F, 4F, 6F, 16F, 16F, 10F);
                    break;
                case "next":
                    Triangle(g, brush, 3F, 5F, 3F, 15F, 10F, 10F);
                    Triangle(g, brush, 10F, 5F, 10F, 15F, 17F, 10F);
                    break;
                case "last":
                    Triangle(g, brush, 4.5F, 5F, 4.5F, 15F, 12.5F, 10F);
                    g.FillRectangle(brush, 13.5F, 5F, 2F, 10F);
                    break;
                case "update":
                    // Circular arrow: an open arc with an arrowhead at its end.
                    const float start = 300F, sweep = 285F, radius = 6F;
                    g.DrawArc(pen, 10F - radius, 10F - radius, radius * 2, radius * 2, start, sweep);
                    double end = (start + sweep) * Math.PI / 180;
                    var tip = new PointF((float)(10 + radius * Math.Cos(end)), (float)(10 + radius * Math.Sin(end)));
                    var along = new PointF((float)-Math.Sin(end), (float)Math.Cos(end));
                    var outward = new PointF((float)Math.Cos(end), (float)Math.Sin(end));
                    g.FillPolygon(brush, new[] {
                        new PointF(tip.X + along.X * 3F, tip.Y + along.Y * 3F),
                        new PointF(tip.X + outward.X * 2.6F, tip.Y + outward.Y * 2.6F),
                        new PointF(tip.X - outward.X * 2.6F, tip.Y - outward.Y * 2.6F) });
                    break;
                case "zoomout":
                case "zoomin":
                    g.DrawEllipse(pen, 3F, 3F, 10.5F, 10.5F);
                    using (var handle = new Pen(Ink, 2.4F) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLine(handle, 12.2F, 12.2F, 16.8F, 16.8F);
                    g.DrawLine(pen, 5.6F, 8.25F, 10.9F, 8.25F);
                    if (name == "zoomin") g.DrawLine(pen, 8.25F, 5.6F, 8.25F, 10.9F);
                    break;
                case "autoscroll":
                    g.DrawLines(pen, new[] { new PointF(5.5F, 5F), new PointF(10F, 9.5F), new PointF(14.5F, 5F) });
                    g.DrawLines(pen, new[] { new PointF(5.5F, 10.5F), new PointF(10F, 15F), new PointF(14.5F, 10.5F) });
                    break;
                case "bottom":
                    g.DrawLine(pen, 10F, 3F, 10F, 12.5F);
                    g.DrawLines(pen, new[] { new PointF(6F, 9F), new PointF(10F, 13F), new PointF(14F, 9F) });
                    g.DrawLine(pen, 5F, 16.5F, 15F, 16.5F);
                    break;
                case "caption":
                    using (var path = RoundRect(2.5F, 4.5F, 15F, 11F, 2.2F)) g.DrawPath(pen, path);
                    g.DrawLine(pen, 5.5F, 9F, 14.5F, 9F);
                    g.DrawLine(pen, 5.5F, 12.2F, 11.5F, 12.2F);
                    break;
                case "instant":
                    g.FillPolygon(brush, new[] { new PointF(11.8F, 2.5F), new PointF(4.8F, 11.2F), new PointF(9.3F, 11.2F),
                        new PointF(8.2F, 17.5F), new PointF(15.2F, 8.6F), new PointF(10.7F, 8.6F) });
                    break;
                case "frame":
                    using (var dashed = new Pen(Ink, 1.4F) { DashPattern = new[] { 2F, 1.6F } }) g.DrawRectangle(dashed, 4F, 4F, 12F, 12F);
                    foreach (var corner in new[] { new PointF(2.5F, 2.5F), new PointF(14.5F, 2.5F), new PointF(2.5F, 14.5F), new PointF(14.5F, 14.5F) })
                        g.FillRectangle(brush, corner.X, corner.Y, 3F, 3F);
                    break;
                case "altcaption":
                    using (var path = RoundRect(2.5F, 3.5F, 15F, 13F, 2F)) g.DrawPath(pen, path);
                    g.DrawLine(pen, 2.5F, 7F, 17.5F, 7F);
                    g.DrawLine(pen, 7F, 10F, 13F, 10F);
                    g.DrawLine(pen, 10F, 10F, 10F, 14.2F);
                    break;
                case "font":
                    g.DrawLines(pen, new[] { new PointF(4F, 16.5F), new PointF(10F, 3.5F), new PointF(16F, 16.5F) });
                    g.DrawLine(pen, 6.6F, 11.6F, 13.4F, 11.6F);
                    break;
                case "settings":
                    using (var gear = new GraphicsPath(FillMode.Alternate))
                    {
                        var points = new PointF[16];
                        for (int i = 0; i < points.Length; i++)
                        {
                            double angle = Math.PI * 2 * i / points.Length;
                            float r = i % 2 == 0 ? 7.6F : 5.6F;
                            points[i] = new PointF((float)(10 + r * Math.Cos(angle)), (float)(10 + r * Math.Sin(angle)));
                        }
                        gear.AddPolygon(points);
                        gear.AddEllipse(7.3F, 7.3F, 5.4F, 5.4F);
                        g.FillPath(brush, gear);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown icon: " + name);
            }
        }

        private static void Triangle(Graphics g, Brush brush, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            g.FillPolygon(brush, new[] { new PointF(x1, y1), new PointF(x2, y2), new PointF(x3, y3) });
        }

        internal static GraphicsPath RoundRect(float x, float y, float width, float height, float radius)
        {
            float d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Checked (ON) buttons get a pale yellow fill so their state reads at a glance.
    public sealed class ReaderToolbarRenderer : ToolStripProfessionalRenderer
    {
        public static readonly Color CheckedFill = Color.FromArgb(255, 238, 168);
        internal static readonly Color CheckedBorder = Color.FromArgb(222, 178, 52);
        private static readonly Color HoverFill = Color.FromArgb(228, 234, 242);
        private static readonly Color HoverBorder = Color.FromArgb(196, 206, 219);
        private static readonly Color PressedFill = Color.FromArgb(208, 218, 230);

        public ReaderToolbarRenderer() { RoundedEdges = false; }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (!e.Vertical || e.ToolStrip is ToolStripDropDown) { base.OnRenderSeparator(e); return; }
            int x = e.Item.Width / 2;
            using (var pen = new Pen(Color.FromArgb(160, 174, 192)))
                e.Graphics.DrawLine(pen, x, 5, x, e.Item.Height - 6);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown) { base.OnRenderToolStripBackground(e); return; }
            using (var brush = new SolidBrush(e.ToolStrip.BackColor)) e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var button = e.Item as ToolStripButton;
            if (button == null || e.ToolStrip is ToolStripDropDown) { base.OnRenderButtonBackground(e); return; }
            Color fill, border;
            if (button.Pressed) { fill = PressedFill; border = HoverBorder; }
            else if (button.Checked) { fill = button.Selected ? Color.FromArgb(255, 228, 130) : CheckedFill; border = CheckedBorder; }
            else if (button.Selected && button.Enabled) { fill = HoverFill; border = HoverBorder; }
            else return;
            var bounds = new RectangleF(0.5F, 1.5F, button.Width - 1.5F, button.Height - 3.5F);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = ToolbarIcons.RoundRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, 4F))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
