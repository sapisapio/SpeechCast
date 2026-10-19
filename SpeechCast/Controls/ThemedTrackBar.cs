// A track bar that keeps the native control's behavior but paints itself in the
// window theme's colors through NM_CUSTOMDRAW, 2026-10-19. GPLv2.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpeechCastMe.Controls
{
    internal sealed class ThemedTrackBar : TrackBar
    {
        private WindowTheme palette;
        private bool recreated;

        // null keeps the native look.
        internal WindowTheme Palette
        {
            get { return palette; }
            set
            {
                palette = value;
                // NM_CUSTOMDRAW goes to the window the control was created under; a tool strip
                // host starts under WinForms' parking window, so the first theme starts it over.
                if (value != null && !recreated && IsHandleCreated) { recreated = true; RecreateHandle(); }
                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (palette != null && !recreated) { recreated = true; BeginInvoke(new Action(RecreateHandle)); }
        }

        // The dotted focus rectangle reads as a stray frame around the drawn slider.
        protected override bool ShowFocusCues { get { return palette == null && base.ShowFocusCues; } }

        [StructLayout(LayoutKind.Sequential)]
        private struct NMHDR { public IntPtr hwndFrom; public IntPtr idFrom; public int code; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct NMCUSTOMDRAW { public NMHDR hdr; public int dwDrawStage; public IntPtr hdc; public RECT rc; public IntPtr dwItemSpec; public int uItemState; public IntPtr lItemlParam; }

        private const int OCM_NOTIFY = 0x204E, NM_CUSTOMDRAW = -12;
        private const int CDDS_PREPAINT = 0x1;
        private const int CDRF_SKIPDEFAULT = 0x4;
        private const int TBM_GETTHUMBRECT = 0x0419, TBM_GETCHANNELRECT = 0x041A;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, ref RECT lParam);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == OCM_NOTIFY && palette != null)
            {
                var header = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));
                if (header.code == NM_CUSTOMDRAW)
                {
                    var draw = (NMCUSTOMDRAW)Marshal.PtrToStructure(m.LParam, typeof(NMCUSTOMDRAW));
                    if (draw.dwDrawStage == CDDS_PREPAINT)
                    {
                        // Paint the whole control at once, so no part of the native drawing is left behind.
                        using (var g = Graphics.FromHdc(draw.hdc)) PaintAll(g);
                        m.Result = (IntPtr)CDRF_SKIPDEFAULT;
                        return;
                    }
                }
            }
            base.WndProc(ref m);
        }

        // The native control repaints only the old and new thumb; the round knob is wider than that.
        protected override void OnValueChanged(EventArgs e)
        {
            if (palette != null) Invalidate();
            base.OnValueChanged(e);
        }

        private Rectangle NativeRect(int message)
        {
            var rect = new RECT();
            SendMessage(Handle, message, IntPtr.Zero, ref rect);
            return Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
        }

        private void PaintAll(Graphics g)
        {
            using (var back = new SolidBrush(BackColor)) g.FillRectangle(back, ClientRectangle);
            var channel = NativeRect(TBM_GETCHANNELRECT);
            var thumb = NativeRect(TBM_GETTHUMBRECT);
            if (thumb.Width <= 0 || thumb.Height <= 0) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Level with the neighboring toolbar icons (the strip's middle), kept inside the native
            // thumb so dragging the knob still grabs it.
            float cx = thumb.Left + thumb.Width / 2F;
            float cy = Parent != null ? Parent.ClientSize.Height / 2F - Top : thumb.Top + thumb.Height / 2F;
            cy = Math.Max(thumb.Top + 6F, Math.Min(thumb.Bottom - 6F, cy));

            // A thin rounded track through the knob's center, filled up to the knob.
            var track = new RectangleF(channel.Left, cy - 2F, Math.Max(4F, channel.Width), 4F);
            using (var path = ToolbarIcons.RoundRect(track.X, track.Y, track.Width, track.Height, 2F))
            using (var brush = new SolidBrush(palette.Border)) g.FillPath(brush, path);
            float filled = cx - track.X;
            if (filled > 2F)
                using (var path = ToolbarIcons.RoundRect(track.X, track.Y, filled, track.Height, 2F))
                using (var brush = new SolidBrush(palette.Ink)) g.FillPath(brush, path);

            float size = Math.Min(13F, Height - 2F);
            var knob = new RectangleF(cx - size / 2F, cy - size / 2F, size, size);
            // A white knob in every theme; the edge keeps it visible on a light toolbar.
            using (var brush = new SolidBrush(Enabled ? Color.White : palette.MutedText)) g.FillEllipse(brush, knob);
            using (var edge = new Pen(palette.IsDark ? palette.Window : palette.Separator, 1F)) g.DrawEllipse(edge, knob);
        }
    }
}
