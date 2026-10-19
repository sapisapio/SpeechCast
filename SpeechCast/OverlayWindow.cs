// Transparent overlay windows for OBS window capture, and their hover bar, 2026-10-19. GPLv2.
// OBS "Window Capture" (Windows 10 1903+ method) keeps per-pixel alpha, so these
// windows can be laid over a broadcast as is. They ignore the mouse; a small
// separate bar appears while the cursor is over one (閉じる / 移動 / サイズ) and
// never shows up in a window capture of the overlay itself.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpeechCastMe
{
    internal abstract class OverlayWindow : Form
    {
        private readonly Timer refresh = new Timer { Interval = 250 };
        private string renderedKey;
        private Bitmap frame;
        internal bool LastUpdateSucceeded { get; private set; }

        protected OverlayWindow(string title, Rectangle bounds)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            TopMost = true;
            ShowInTaskbar = true;
            Icon = AppIcon.Value;
            // A size set before the handle exists is reduced by the default frame
            // once the window is created; apply the requested bounds again.
            Load += delegate { Bounds = bounds; };
            refresh.Tick += delegate { RenderIfChanged(); };
            Shown += delegate { RenderIfChanged(); refresh.Start(); };
            Move += delegate { Push(); };
            Resize += delegate { renderedKey = null; RenderIfChanged(); };
        }

        // A string that changes whenever the picture must be redrawn; null skips drawing.
        protected abstract string RenderKey();
        protected abstract Bitmap Render(Size size);

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

        internal void RenderIfChanged()
        {
            if (ClientSize.Width < 1 || ClientSize.Height < 1) return;
            string key = RenderKey();
            if (key == null) return;
            key += "|" + ClientSize;
            if (key == renderedKey) return;
            renderedKey = key;
            var next = Render(ClientSize);
            if (frame != null) frame.Dispose();
            frame = next;
            Push();
        }

        private void Push()
        {
            if (frame == null || !IsHandleCreated) return;
            LastUpdateSucceeded = LayeredWindow.Update(Handle, Location, frame);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { refresh.Dispose(); if (frame != null) frame.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal sealed class OverlayHoverBar : IDisposable
    {
        private readonly OverlayWindow target;
        private readonly Form bar;
        private readonly Timer hover = new Timer { Interval = 150 };

        public OverlayHoverBar(OverlayWindow target, Action close)
        {
            this.target = target;
            bar = new QuietForm { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, TopMost = true, StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(30, 41, 59), Size = new Size(236, 30), Font = new Font("Yu Gothic UI", 9F), Text = target.Text };
            var closeButton = Button("閉じる", 0);
            closeButton.Click += delegate { close(); };
            var move = Button("✥ 移動", 78);
            var size = Button("◢ サイズ", 156);
            DragWith(move, (start, delta) => target.Location = new Point(start.X + delta.X, start.Y + delta.Y));
            DragWith(size, (start, delta) => target.Size = new Size(Math.Max(120, start.Width + delta.X), Math.Max(60, start.Height + delta.Y)));
            bar.Controls.AddRange(new Control[] { closeButton, move, size });
            hover.Tick += delegate { Update(); };
            hover.Start();
        }

        internal bool Visible { get { return bar.Visible; } }

        // Hovering must not take focus away from the game or the browser underneath.
        private sealed class QuietForm : Form
        {
            protected override bool ShowWithoutActivation { get { return true; } }
            protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= 0x80 | 0x08000000; return cp; } }
        }

        private static Label Button(string text, int left)
        {
            return new Label { Text = text, Left = left + 2, Top = 2, Width = 76, Height = 26, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White, BackColor = Color.FromArgb(51, 65, 85), Cursor = Cursors.Hand };
        }

        // The callback receives the overlay bounds at mouse-down and the cursor offset.
        private void DragWith(Control handle, Action<Rectangle, Point> apply)
        {
            Point origin = Point.Empty;
            Rectangle start = Rectangle.Empty;
            bool dragging = false;
            handle.MouseDown += (s, e) => { if (e.Button != MouseButtons.Left || target.IsDisposed) return; dragging = true; origin = Cursor.Position; start = target.Bounds; };
            handle.MouseMove += (s, e) => { if (dragging && !target.IsDisposed) apply(start, new Point(Cursor.Position.X - origin.X, Cursor.Position.Y - origin.Y)); };
            handle.MouseUp += (s, e) => { dragging = false; };
        }

        private void Update()
        {
            if (target.IsDisposed || !target.Visible) { if (bar.Visible) bar.Hide(); return; }
            var area = target.Bounds;
            bool hovering = area.Contains(Cursor.Position) || (bar.Visible && bar.Bounds.Contains(Cursor.Position))
                || Control.MouseButtons != MouseButtons.None && bar.Visible;
            if (hovering)
            {
                bar.Location = new Point(Math.Max(area.Left, area.Right - bar.Width - 4), area.Top + 4);
                if (!bar.Visible) bar.Show();
            }
            else if (bar.Visible) bar.Hide();
        }

        public void Dispose()
        {
            hover.Stop();
            hover.Dispose();
            bar.Close();
            bar.Dispose();
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
