// Free telop: the input area under the comment view and its own overlay window, 2026-10-19. GPLv2.
// Replaces the old 代替字幕 (text shown on the main caption between replies, which
// mixed with incoming replies). The main caption now only reads replies.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private FormFreeTelop telopWindow;
        private OverlayHoverBar telopBar;
        private ToolStripButton telopButton;
        private ToolStripButton telopToggle;
        private ToolStripLabel telopSizeLabel;
        private ToolStripDropDownButton telopFontButton;

        internal bool IsTelopOpen { get { return telopWindow != null; } }

        private void InitializeTelopUi()
        {
            telopButton = new ToolStripButton("自由テロップ") { Name = "telopButton", Alignment = ToolStripItemAlignment.Right,
                DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = "自由テロップの窓を開く / 閉じる（透過・OBSのウィンドウキャプチャ用）" };
            telopButton.Click += delegate { ToggleTelopWindow(); };
            menuStrip1.Items.Add(telopButton);

            // The input area: the text box plus a toolbar in the same style as the main
            // rows: window toggle | size − 40px ＋ | font menu.
            foreach (Control old in splitContainerpkCaption.Panel2.Controls) old.Visible = false;
            var row = new ToolStrip { Name = "telopToolbar", Dock = DockStyle.Fill, GripStyle = ToolStripGripStyle.Hidden, Renderer = toolbarRenderer,
                BackColor = BackColor, Font = new Font("Yu Gothic UI", 9F), Padding = new Padding(4, 2, 4, 2), CanOverflow = true };
            telopToggle = new ToolStripButton { Name = "telopToggle" };
            SetIconButton(telopToggle, "altcaption", "自由テロップの窓を開く / 閉じる（OBSのウィンドウキャプチャ用）");
            SetLabelledIcon(telopToggle, "テロップ窓");
            telopToggle.Click += delegate { ToggleTelopWindow(); };
            var smaller = new ToolStripButton { Name = "telopSmaller" };
            SetIconButton(smaller, "zoomout", "テロップの文字を小さく（1px）");
            smaller.Click += delegate { ChangeTelopSize(-1); };
            telopSizeLabel = new ToolStripLabel { Name = "telopSizeLabel", AutoSize = false, Width = 44, TextAlign = ContentAlignment.MiddleCenter, ToolTipText = "テロップの文字の大きさ" };
            var larger = new ToolStripButton { Name = "telopLarger" };
            SetIconButton(larger, "zoomin", "テロップの文字を大きく（1px）");
            larger.Click += delegate { ChangeTelopSize(1); };
            telopFontButton = new ToolStripDropDownButton { Name = "telopFont", ToolTipText = "テロップのフォント" };
            telopFontButton.Image = CreateThemedIcon(telopFontButton, "font");
            telopFontButton.ImageScaling = ToolStripItemImageScaling.None;
            telopFontButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            telopFontButton.DropDownItems.Add("源暎エムゴ v2 Black（内蔵）", null, delegate { UserConfig.TelopFontName = BundledFont.FamilyName; SyncTelopUi(); });
            telopFontButton.DropDownItems.Add("ほかのフォントを選ぶ…", null, delegate { ChooseTelopFont(); });
            row.Items.AddRange(new ToolStripItem[] { telopToggle, new ToolStripSeparator(), smaller, telopSizeLabel, larger, new ToolStripSeparator(), telopFontButton });
            splitContainerpkCaption.Panel2.Controls.Add(row);
            SyncTelopUi();
            splitContainerpkCaption.Panel2MinSize = 0;
            textBoxDefaultCaption.Font = new Font("Yu Gothic UI", 10F);
            toolStripStatusLabelDefaultCaptionButton.Text = "自由テロップ入力欄";
            toolStripButtonOpenResCaptionForm.ToolTipText = "自由テロップの入力欄を開く / 閉じる";
            SizeChanged += delegate { FitTelopRow(); };
        }

        private void FitTelopRow()
        {
            int rowHeight = 34;
            int distance = splitContainerpkCaption.Height - splitContainerpkCaption.SplitterWidth - rowHeight;
            if (distance > 20) splitContainerpkCaption.SplitterDistance = distance;
        }

        // The input area under the comment view: one click opens it, the next closes it.
        private void ToggleTelopInput()
        {
            bool open = splitContainerResCaption.Panel2Collapsed || splitContainerResCaption.Panel2.Height < 20;
            if (open)
            {
                splitContainerResCaption.Panel2Collapsed = false;
                splitContainerResCaption.IsSplitterFixed = false;
                splitContainerResCaption.SplitterDistance = Math.Max(40, splitContainerResCaption.Height - 130);
                FitTelopRow();
                textBoxDefaultCaption.Focus();
            }
            else
            {
                splitContainerResCaption.Panel2Collapsed = true;
                webBrowser.Focus();
            }
            toolStripButtonOpenResCaptionForm.Checked = open;
        }

        internal void ToggleTelopWindow()
        {
            if (IsTelopOpen) CloseTelopWindow(); else OpenTelopWindow();
        }

        private void OpenTelopWindow()
        {
            if (IsTelopOpen || UserConfig == null) return;
            Rectangle bounds = UserConfig.TelopBounds;
            if (bounds.Width < 80 || bounds.Height < 40 || !OnAnyScreen(bounds))
            {
                var area = webBrowser.RectangleToScreen(webBrowser.ClientRectangle);
                bounds = new Rectangle(area.Left, area.Bottom - 120, area.Width, 120);
            }
            telopWindow = new FormFreeTelop(() => UserConfig == null ? "" : UserConfig.CaptionTextBuffer, bounds);
            telopWindow.FormClosed += delegate { if (telopWindow != null) CloseTelopWindow(); };
            telopBar = new OverlayHoverBar(telopWindow, CloseTelopWindow);
            telopWindow.Show();
            SyncTelopUi();
        }

        private void CloseTelopWindow()
        {
            if (!IsTelopOpen) return;
            var window = telopWindow;
            telopWindow = null;
            UserConfig.TelopBounds = window.Bounds;
            telopBar.Dispose(); telopBar = null;
            if (!window.IsDisposed) { window.Close(); window.Dispose(); }
            SyncTelopUi();
        }

        private void SyncTelopUi()
        {
            if (telopButton != null) telopButton.Checked = IsTelopOpen;
            if (telopToggle != null) telopToggle.Checked = IsTelopOpen;
            if (UserConfig != null && telopSizeLabel != null)
            {
                telopSizeLabel.Text = UserConfig.TelopFontSize + "px";
                string name = string.IsNullOrEmpty(UserConfig.TelopFontName) ? BundledFont.FamilyName : UserConfig.TelopFontName;
                telopFontButton.Text = name == BundledFont.FamilyName ? "源暎エムゴ（内蔵）" : name;
            }
        }

        // Same step as the comment zoom buttons: one pixel at a time.
        internal void ChangeTelopSize(int delta)
        {
            if (UserConfig == null) return;
            UserConfig.TelopFontSize = Math.Max(10, Math.Min(160, UserConfig.TelopFontSize + delta));
            SyncTelopUi();
        }

        private void ChooseTelopFont()
        {
            string name = string.IsNullOrEmpty(UserConfig.TelopFontName) ? BundledFont.FamilyName : UserConfig.TelopFontName;
            int px = Math.Max(10, UserConfig.TelopFontSize);
            // The dialog cannot list the bundled font; start it from a common installed one.
            using (var preview = new Font(name == BundledFont.FamilyName ? "Meiryo" : name, px, GraphicsUnit.Pixel))
            using (var chooser = new FontDialog { Font = preview, FontMustExist = true, ShowEffects = false })
            {
                if (!ShowFontDialog(chooser, this)) return;
                UserConfig.TelopFontName = chooser.Font.FontFamily.Name;
                UserConfig.TelopFontSize = Math.Max(10, Math.Min(160, (int)Math.Round(chooser.Font.SizeInPoints * 96F / 72F)));
            }
            SyncTelopUi();
        }
    }
}
