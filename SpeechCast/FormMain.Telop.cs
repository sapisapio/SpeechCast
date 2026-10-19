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
        private ToolStripTextBox telopSizeLabel; // the size in px: typed, or stepped with − ＋ / ↑ ↓ / the wheel
        private ToolStripDropDownButton telopFontButton;
        private ToolStripDropDownButton telopColorButton;
        private ToolStripMenuItem telopTextColorItem, telopOutlineColorItem;
        private readonly ToolStripMenuItem[] telopWidthItems = new ToolStripMenuItem[7];
        private static readonly int[] TelopOutlineWidths = { 0, 1, 2, 3, 4, 6, 8 };

        internal bool IsTelopOpen { get { return telopWindow != null; } }

        private void InitializeTelopUi()
        {
            telopButton = new ToolStripButton("自由テロップ") { Name = "telopButton", Alignment = ToolStripItemAlignment.Right,
                DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = "自由テロップの窓を開く / 閉じる（透過・OBSのウィンドウキャプチャ用）" };
            telopButton.Click += delegate { ToggleTelopWindow(); };
            // Not on the menu bar: the telop input row has its own 表示 / 非表示 (the button stays as state).

            // The input area: the text box plus a toolbar in the same style as the main
            // rows: window toggle | size − 40px ＋ | font menu.
            foreach (Control old in splitContainerpkCaption.Panel2.Controls) old.Visible = false;
            var row = new ToolStrip { Name = "telopToolbar", Dock = DockStyle.Fill, GripStyle = ToolStripGripStyle.Hidden, Renderer = toolbarRenderer,
                BackColor = BackColor, Font = new Font("Yu Gothic UI", 9F), Padding = new Padding(4, 2, 4, 2), CanOverflow = true };
            telopToggle = new ToolStripButton { Name = "telopToggle" };
            SetIconButton(telopToggle, "altcaption", "自由テロップの窓を開く / 閉じる（OBSのウィンドウキャプチャ用）");
            SetLabelledIcon(telopToggle, "非表示"); // 表示 while the window is open (SyncTelopUi)
            telopToggle.Click += delegate { ToggleTelopWindow(); };
            var smaller = new ToolStripButton { Name = "telopSmaller" };
            SetIconButton(smaller, "zoomout", "テロップの文字を小さく（1px）");
            smaller.Click += delegate { ChangeTelopSize(-1); };
            telopSizeLabel = new ToolStripTextBox { Name = "telopSizeLabel", AutoSize = false, Width = 40, MaxLength = 3, ToolTipText = "テロップの文字の大きさ（px）。数字を入れて Enter" };
            telopSizeLabel.TextBox.TextAlign = HorizontalAlignment.Center;
            telopSizeLabel.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            telopSizeLabel.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { CommitTelopSize(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Up) { ChangeTelopSize(1); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Down) { ChangeTelopSize(-1); e.SuppressKeyPress = true; }
            };
            telopSizeLabel.Leave += delegate { CommitTelopSize(); };
            telopSizeLabel.TextBox.MouseWheel += (s, e) => ChangeTelopSize(e.Delta > 0 ? 1 : -1);
            var pixels = new ToolStripLabel("px") { Margin = new Padding(0, 1, 2, 2) };
            var larger = new ToolStripButton { Name = "telopLarger" };
            SetIconButton(larger, "zoomin", "テロップの文字を大きく（1px）");
            larger.Click += delegate { ChangeTelopSize(1); };
            telopFontButton = new ToolStripDropDownButton { Name = "telopFont", ToolTipText = "テロップのフォント" };
            telopFontButton.Image = CreateThemedIcon(telopFontButton, "font");
            telopFontButton.ImageScaling = ToolStripItemImageScaling.None;
            telopFontButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            telopFontButton.DropDownItems.Add("源暎エムゴ v2 Black（内蔵）", null, delegate { UserConfig.TelopFontName = BundledFont.FamilyName; SyncTelopUi(); });
            telopFontButton.DropDownItems.Add("ほかのフォントを選ぶ…", null, delegate { ChooseTelopFont(); });
            // Inside and outline colors and the outline width; the swatches show the current colors.
            telopColorButton = new ToolStripDropDownButton("色・縁取り") { Name = "telopColor", DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = "テロップの文字の色と縁取り" };
            telopTextColorItem = new ToolStripMenuItem("文字の色…", null, delegate { ChooseTelopColor(true); });
            telopOutlineColorItem = new ToolStripMenuItem("縁取りの色…", null, delegate { ChooseTelopColor(false); });
            var widths = new ToolStripMenuItem("縁取りの幅");
            for (int i = 0; i < TelopOutlineWidths.Length; i++)
            {
                int width = TelopOutlineWidths[i];
                telopWidthItems[i] = new ToolStripMenuItem(width == 0 ? "なし" : width + " px", null, delegate { UserConfig.TelopOutlineWidth = width; SyncTelopUi(); });
                widths.DropDownItems.Add(telopWidthItems[i]);
            }
            telopColorButton.DropDownItems.AddRange(new ToolStripItem[] { telopTextColorItem, telopOutlineColorItem, new ToolStripSeparator(), widths });
            row.Items.AddRange(new ToolStripItem[] { telopToggle, new ToolStripSeparator(), smaller, telopSizeLabel, pixels, larger, new ToolStripSeparator(), telopFontButton, telopColorButton });
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
            if (telopToggle != null) { telopToggle.Checked = IsTelopOpen; telopToggle.Text = IsTelopOpen ? "表示" : "非表示"; }
            if (UserConfig != null && telopSizeLabel != null)
            {
                if (!telopSizeLabel.Focused) telopSizeLabel.Text = UserConfig.TelopFontSize.ToString();
                string name = string.IsNullOrEmpty(UserConfig.TelopFontName) ? BundledFont.FamilyName : UserConfig.TelopFontName;
                telopFontButton.Text = name == BundledFont.FamilyName ? "源暎エムゴ（内蔵）" : name;
                SetSwatch(telopTextColorItem, FormFreeTelop.ParseColor(UserConfig.TelopTextColor, Color.White));
                SetSwatch(telopOutlineColorItem, FormFreeTelop.ParseColor(UserConfig.TelopOutlineColor, Color.Black));
                for (int i = 0; i < TelopOutlineWidths.Length; i++) telopWidthItems[i].Checked = TelopOutlineWidths[i] == UserConfig.TelopOutlineWidth;
            }
        }

        private static void SetSwatch(ToolStripMenuItem item, Color color)
        {
            if (item == null) return;
            var swatch = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(swatch))
            using (var fill = new SolidBrush(color))
            {
                g.FillRectangle(fill, 1, 1, 14, 14);
                g.DrawRectangle(Pens.Gray, 1, 1, 13, 13);
            }
            if (item.Image != null) item.Image.Dispose();
            item.Image = swatch;
        }

        private void ChooseTelopColor(bool text)
        {
            if (UserConfig == null) return;
            var current = text ? FormFreeTelop.ParseColor(UserConfig.TelopTextColor, Color.White) : FormFreeTelop.ParseColor(UserConfig.TelopOutlineColor, Color.Black);
            using (var dialog = new ColorDialog { FullOpen = true, Color = current })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string html = ColorTranslator.ToHtml(Color.FromArgb(dialog.Color.R, dialog.Color.G, dialog.Color.B));
                if (text) UserConfig.TelopTextColor = html; else UserConfig.TelopOutlineColor = html;
            }
            SyncTelopUi();
        }

        // Same step as the comment zoom buttons: one pixel at a time.
        internal void ChangeTelopSize(int delta)
        {
            if (UserConfig == null) return;
            UserConfig.TelopFontSize = Math.Max(10, Math.Min(160, UserConfig.TelopFontSize + delta));
            telopSizeLabel.Text = UserConfig.TelopFontSize.ToString();
            SyncTelopUi();
        }

        // A typed size takes effect on Enter or leaving the field; anything else restores the current one.
        private void CommitTelopSize()
        {
            if (UserConfig == null) return;
            int typed;
            if (int.TryParse(telopSizeLabel.Text, out typed)) UserConfig.TelopFontSize = Math.Max(10, Math.Min(160, typed));
            telopSizeLabel.Text = UserConfig.TelopFontSize.ToString();
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
