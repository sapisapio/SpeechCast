// Stream comments window (opened next to the main window) and its hover bar, 2026-10-19. GPLv2.
// The overlay is click-through, so a small separate bar appears while the mouse
// is over it: 閉じる, 移動 (drag) and サイズ (drag). The main window stays usable.
// The bar is its own window and never shows up in a window capture of the overlay.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private FormStreamOverlay streamOverlay;
        private ToolStripButton streamButton;
        private Form streamControls;
        private Timer streamHover;

        internal bool IsStreamMode { get { return streamOverlay != null; } }

        internal void ToggleStreamMode()
        {
            if (IsStreamMode) ExitStreamMode(); else EnterStreamMode();
        }

        private void EnterStreamMode()
        {
            if (IsStreamMode || UserConfig == null) return;
            Rectangle bounds = UserConfig.StreamOverlayBounds;
            if (bounds.Width < 80 || bounds.Height < 60 || !OnAnyScreen(bounds))
                bounds = webBrowser.RectangleToScreen(webBrowser.ClientRectangle);
            streamOverlay = new FormStreamOverlay(() => responses, bounds);
            streamOverlay.FormClosed += delegate { if (streamOverlay != null) ExitStreamMode(); };
            streamControls = CreateStreamControls();
            streamHover = new Timer { Interval = 150 };
            streamHover.Tick += delegate { UpdateStreamControls(); };
            streamOverlay.Show();
            streamHover.Start();
            if (streamButton != null) streamButton.Checked = true;
        }

        private void ExitStreamMode()
        {
            if (!IsStreamMode) return;
            var overlay = streamOverlay;
            streamOverlay = null;
            UserConfig.StreamOverlayBounds = overlay.Bounds;
            streamHover.Stop(); streamHover.Dispose(); streamHover = null;
            streamControls.Close(); streamControls.Dispose(); streamControls = null;
            if (!overlay.IsDisposed) { overlay.Close(); overlay.Dispose(); }
            if (streamButton != null) streamButton.Checked = false;
        }

        private static bool OnAnyScreen(Rectangle bounds)
        {
            foreach (var screen in Screen.AllScreens) if (screen.WorkingArea.IntersectsWith(bounds)) return true;
            return false;
        }

        private Form CreateStreamControls()
        {
            var bar = new QuietForm { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, TopMost = true, StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(30, 41, 59), Size = new Size(236, 30), Font = new Font("Yu Gothic UI", 9F), Text = "配信モード" };
            var back = BarButton("閉じる", 0);
            back.Click += delegate { ExitStreamMode(); };
            var move = BarButton("✥ 移動", 78);
            var size = BarButton("◢ サイズ", 156);
            DragWith(move, (start, delta) => streamOverlay.Location = new Point(start.X + delta.X, start.Y + delta.Y));
            DragWith(size, (start, delta) => streamOverlay.Size = new Size(Math.Max(120, start.Width + delta.X), Math.Max(80, start.Height + delta.Y)));
            bar.Controls.AddRange(new Control[] { back, move, size });
            return bar;
        }

        // Hovering must not take focus away from the game or the browser underneath.
        private sealed class QuietForm : Form
        {
            protected override bool ShowWithoutActivation { get { return true; } }
            protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= 0x80 | 0x08000000; return cp; } }
        }

        private static Label BarButton(string text, int left)
        {
            return new Label { Text = text, Left = left + 2, Top = 2, Width = 76, Height = 26, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White, BackColor = Color.FromArgb(51, 65, 85), Cursor = Cursors.Hand };
        }

        // Drag handler: the callback receives the overlay bounds at mouse-down and the cursor offset.
        private void DragWith(Control handle, Action<Rectangle, Point> apply)
        {
            Point origin = Point.Empty;
            Rectangle start = Rectangle.Empty;
            bool dragging = false;
            handle.MouseDown += (s, e) => { if (e.Button != MouseButtons.Left || streamOverlay == null) return; dragging = true; origin = Cursor.Position; start = streamOverlay.Bounds; };
            handle.MouseMove += (s, e) => { if (dragging && streamOverlay != null) apply(start, new Point(Cursor.Position.X - origin.X, Cursor.Position.Y - origin.Y)); };
            handle.MouseUp += (s, e) => { dragging = false; };
        }

        // The overlay ignores the mouse; show the bar while the cursor is over it.
        private void UpdateStreamControls()
        {
            if (streamOverlay == null || streamControls == null) return;
            var area = streamOverlay.Bounds;
            bool hovering = area.Contains(Cursor.Position) || (streamControls.Visible && streamControls.Bounds.Contains(Cursor.Position))
                || Control.MouseButtons != MouseButtons.None && streamControls.Visible;
            if (hovering)
            {
                streamControls.Location = new Point(Math.Max(area.Left, area.Right - streamControls.Width - 4), area.Top + 4);
                if (!streamControls.Visible) streamControls.Show();
            }
            else if (streamControls.Visible) streamControls.Hide();
        }
    }
}
