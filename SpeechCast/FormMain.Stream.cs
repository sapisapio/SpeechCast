// Stream comments window, opened next to the main window, 2026-10-19. GPLv2.
// The main window stays usable; the overlay's hover bar can close, move and resize it.
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private FormStreamOverlay streamOverlay;
        private ToolStripButton streamButton;
        private OverlayHoverBar streamBar;

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
            streamBar = new OverlayHoverBar(streamOverlay, ExitStreamMode);
            streamOverlay.Show();
            if (streamButton != null) streamButton.Checked = true;
        }

        private void ExitStreamMode()
        {
            if (!IsStreamMode) return;
            var overlay = streamOverlay;
            streamOverlay = null;
            UserConfig.StreamOverlayBounds = overlay.Bounds;
            streamBar.Dispose(); streamBar = null;
            if (!overlay.IsDisposed) { overlay.Close(); overlay.Dispose(); }
            if (streamButton != null) streamButton.Checked = false;
        }

        private static bool OnAnyScreen(Rectangle bounds)
        {
            foreach (var screen in Screen.AllScreens) if (screen.WorkingArea.IntersectsWith(bounds)) return true;
            return false;
        }
    }
}
