// Stream comments window, opened next to the main window, 2026-10-19. GPLv2.
// The main window stays usable; the overlay's hover bar can close, move and resize it.
using System.Collections.Generic;
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

        // 表示設定 for the stream comments: size, fields, the slide-in and the skin (StreamTemplates.Catalog).
        private void AddStreamLookItems(ToolStripItemCollection items, ToolStripDropDown owner)
        {
            // Text size: 0 follows the comment view's size.
            var size = new ToolStripMenuItem("文字サイズ");
            var sizes = new List<ToolStripMenuItem>();
            foreach (int px in new[] { 0, 14, 16, 18, 20, 24, 28, 32, 40, 48 })
            {
                int value = px;
                var item = new ToolStripMenuItem(px == 0 ? "コメント欄と同じ" : px + " px", null, delegate { if (UserConfig == null) return; UserConfig.StreamFontSize = value; RefreshStream(); }) { Tag = px };
                sizes.Add(item);
                size.DropDownItems.Add(item);
            }
            items.Add(size);
            // Fields shown above each comment, for the stream window only; ticking keeps the list open.
            var fields = new ToolStripMenuItem("表示項目");
            var fieldItems = new List<ToolStripMenuItem>();
            foreach (var field in new[] { new[] { "number", "レス番号" }, new[] { "name", "名前" }, new[] { "date", "日付" }, new[] { "time", "時間" }, new[] { "id", "ID" } })
            {
                string key = field[0];
                var item = new ToolStripMenuItem(field[1]) { Tag = key };
                // Checked = shown; set after the toggle (ItemClicked comes before Click).
                item.Click += delegate { ToggleStreamField(key); item.Checked = StreamField(key); };
                fieldItems.Add(item);
                fields.DropDownItems.Add(item);
            }
            fields.DropDown.Closing += (s, e) => { if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) e.Cancel = true; };
            items.Add(fields);
            items.Add(new ToolStripSeparator());
            var intro = new ToolStripMenuItem("新着をスライドで表示", null, delegate {
                if (UserConfig == null) return;
                UserConfig.StreamIntro = !UserConfig.StreamIntro;
            });
            items.Add(intro);
            // The skins last, in their own list: size and fields are changed far more often.
            items.Add(new ToolStripSeparator());
            var skins = new ToolStripMenuItem("スキン");
            var choices = new List<ToolStripMenuItem>();
            foreach (var entry in StreamTemplates.Catalog)
            {
                string id = entry[0];
                var item = new ToolStripMenuItem(entry[1], null, delegate { SetStreamTemplate(id); }) { Tag = id };
                choices.Add(item);
                skins.DropDownItems.Add(item);
            }
            items.Add(skins);
            owner.Opening += delegate {
                string current = UserConfig == null || string.IsNullOrEmpty(UserConfig.StreamTemplate) ? StreamTemplates.Outline : UserConfig.StreamTemplate;
                foreach (var item in choices) item.Checked = (string)item.Tag == current;
                intro.Checked = UserConfig != null && UserConfig.StreamIntro;
                foreach (var item in sizes) item.Checked = UserConfig != null && (int)item.Tag == UserConfig.StreamFontSize;
                foreach (var item in fieldItems) item.Checked = StreamField((string)item.Tag);
            };
        }

        private void SetStreamTemplate(string id)
        {
            if (UserConfig == null) return;
            UserConfig.StreamTemplate = id;
            RefreshStream();
        }

        private void RefreshStream() { if (streamOverlay != null) streamOverlay.RenderIfChanged(); }

        private bool StreamField(string key)
        {
            var c = UserConfig;
            if (c == null) return false;
            switch (key)
            {
                case "number": return c.StreamShowNumber;
                case "name": return c.StreamShowName;
                case "date": return c.StreamShowDate;
                case "time": return c.StreamShowTime;
                default: return c.StreamShowID;
            }
        }

        private void ToggleStreamField(string key)
        {
            var c = UserConfig;
            if (c == null) return;
            switch (key)
            {
                case "number": c.StreamShowNumber = !c.StreamShowNumber; break;
                case "name": c.StreamShowName = !c.StreamShowName; break;
                case "date": c.StreamShowDate = !c.StreamShowDate; break;
                case "time": c.StreamShowTime = !c.StreamShowTime; break;
                default: c.StreamShowID = !c.StreamShowID; break;
            }
            RefreshStream();
        }

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
