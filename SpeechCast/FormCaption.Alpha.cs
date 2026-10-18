// 2026-10-18: per-pixel alpha caption rendering. GPLv2, see LICENSE.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormCaption
    {
        internal bool LastAlphaUpdateSucceeded { get; private set; }
        [StructLayout(LayoutKind.Sequential)]
        private struct AlphaPoint { public int X, Y; public AlphaPoint(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)]
        private struct AlphaSize { public int Width, Height; public AlphaSize(int w, int h) { Width = w; Height = h; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AlphaBlend { public byte Operation, Flags, ConstantAlpha, Format; }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr destination, ref AlphaPoint location, ref AlphaSize size, IntPtr source, ref AlphaPoint origin, int colorKey, ref AlphaBlend blend, int flags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x80000; return cp; }
        }

        private void FormCaption_Paint(object sender, PaintEventArgs e)
        {
            RenderAlphaCaption();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            Refresh();
        }

        public override void Refresh()
        {
            // Layered windows do not receive the normal initial WM_PAINT reliably.
            if (IsHandleCreated && Visible) RenderAlphaCaption();
        }

        private void RenderAlphaCaption()
        {
            if (FormMain.UserConfig == null || ClientSize.Width < 1 || ClientSize.Height < 1) return;
            using (var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var args = new PaintEventArgs(graphics, ClientRectangle))
                {
                    graphics.Clear(Color.Transparent);
                    PaintCaptionContent(this, args);
                }
                IntPtr screen = GetDC(IntPtr.Zero);
                IntPtr memory = CreateCompatibleDC(screen);
                IntPtr handle = IntPtr.Zero;
                IntPtr previous = IntPtr.Zero;
                try
                {
                    handle = bitmap.GetHbitmap(Color.FromArgb(0));
                    previous = SelectObject(memory, handle);
                    var location = new AlphaPoint(Left, Top);
                    var origin = new AlphaPoint(0, 0);
                    var size = new AlphaSize(bitmap.Width, bitmap.Height);
                    var blend = new AlphaBlend { ConstantAlpha = 255, Format = 1 };
                    LastAlphaUpdateSucceeded = UpdateLayeredWindow(Handle, screen, ref location, ref size, memory, ref origin, 0, ref blend, 2);
                    if (!LastAlphaUpdateSucceeded)
                        System.Diagnostics.Trace.WriteLine("Caption alpha update failed: " + Marshal.GetLastWin32Error());
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
}
