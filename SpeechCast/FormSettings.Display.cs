// Settings tabs for what the UI refresh added (comment view, stream comments, gadgets) and the
// thread-name band on the 字幕 tab, so every option lives in this one dialog, 2026-10-20. GPLv2.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormSettings
    {
        // 表示
        private RadioButton themeLight, themeDark, themeColor;
        private Button themeColorButton;
        private string themeBackground;
        private ComboBox commentLayout;
        private CheckBox[] commentFields;
        private CheckBox commentSeparators;
        private Button commentFontButton;
        private string commentFontName;
        private NumericUpDown commentFontSize;
        // 配信コメント
        private Button streamFontButton;
        private string streamFontName;
        private ComboBox streamSize, streamSkin;
        private CheckBox[] streamFields;
        private CheckBox streamIntro;
        private static readonly int[] StreamSizes = { 0, 14, 16, 18, 20, 24, 28, 32, 40, 48 };
        // ガジェット
        private RadioButton gadgetDark, gadgetLight;
        private TrackBar gadgetVolume;
        private Label gadgetVolumeText;
        // ショートカット
        private CheckBox hotkeysOn;
        private TextBox[] hotkeyBoxes;
        private Keys[] hotkeyKeys;
        // 通信: reopen the last thread at start-up
        private CheckBox reopenLast;
        // 字幕: the thread name band
        private CheckBox titleShown, titleEdge;
        private string titleBack, titleText, titleBorder;
        private Button titleBackButton, titleTextButton, titleBorderButton;

        private static readonly string[] FieldNames = { "レス番号", "名前", "日付", "時間", "ID" };

        // Called from the constructor after InitializeComponent.
        private void InitializeDisplayTabs()
        {
            ClientSize = new Size(600, 520);
            tabControl1.Multiline = true;
            // No longer used: the embedded YouTube player and the auto-scroll speed; the bookmark
            // option moves to 表示. Notes that only applied to SpeechCastMe or the old 代替字幕 go too.
            tabControl1.TabPages.Remove(tabPageBrowser);
            checkBoxHideCaptionTitle.Visible = false;
            label24.Visible = false;

            var display = Page("表示");
            int y = 12;
            display.Controls.Add(Caption("コメント欄の配色（ツールバーとメニューも同じ色になります）", 12, y)); y += 22;
            themeLight = new RadioButton { Text = "明るい", Left = 20, Top = y, AutoSize = true };
            themeDark = new RadioButton { Text = "暗い", Left = 100, Top = y, AutoSize = true };
            themeColor = new RadioButton { Text = "テーマカラー", Left = 170, Top = y, AutoSize = true };
            themeColorButton = new Button { Text = "色…", Left = 280, Top = y - 4, Width = 70, Height = 26, TextImageRelation = TextImageRelation.ImageBeforeText };
            themeColorButton.Click += delegate {
                using (var dialog = new ColorDialog { FullOpen = true, Color = FormFreeTelop.ParseColor(themeBackground, Color.FromArgb(31, 38, 48)) })
                    if (dialog.ShowDialog(this) == DialogResult.OK) { themeBackground = Html(dialog.Color); themeColor.Checked = true; Swatch(themeColorButton, dialog.Color); }
            };
            display.Controls.AddRange(new Control[] { themeLight, themeDark, themeColor, themeColorButton });
            y += 36;
            display.Controls.Add(Caption("コメントの並べ方", 12, y + 4));
            commentLayout = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = 130, Top = y, Width = 260 };
            commentLayout.Items.AddRange(new object[] { "1行：表示項目と本文を横並び", "2行：表示項目の下に本文" });
            display.Controls.Add(commentLayout);
            y += 36;
            display.Controls.Add(Caption("表示項目", 12, y));
            commentFields = Checks(display, 20, y + 20);
            y += 52;
            commentSeparators = new CheckBox { Text = "レスごとに区切り線を引く", Left = 20, Top = y, AutoSize = true };
            display.Controls.Add(commentSeparators);
            y += 34;
            display.Controls.Add(Caption("本文のフォント", 12, y + 6));
            commentFontButton = new Button { Left = 130, Top = y, Width = 220, Height = 28 };
            commentFontButton.Click += delegate { var name = ChooseFont(commentFontName, (int)commentFontSize.Value); if (name != null) { commentFontName = name; ShowFontName(commentFontButton, name); } };
            var commentBundled = new Button { Text = "内蔵フォント", Left = 356, Top = y, Width = 100, Height = 28 };
            commentBundled.Click += delegate { commentFontName = BundledFont.FamilyName; ShowFontName(commentFontButton, commentFontName); };
            display.Controls.AddRange(new Control[] { commentFontButton, commentBundled });
            y += 36;
            display.Controls.Add(Caption("本文の文字サイズ（px）", 12, y + 4));
            commentFontSize = new NumericUpDown { Left = 170, Top = y, Width = 70, Minimum = 10, Maximum = 40 };
            display.Controls.Add(commentFontSize);
            y += 40;
            checkBoxEnableMoveBottomFromBookmarks.Parent = display;
            checkBoxEnableMoveBottomFromBookmarks.Location = new Point(20, y);

            var stream = Page("配信");
            y = 12;
            stream.Controls.Add(Caption("OBS のウィンドウキャプチャ用のコメント窓", 12, y)); y += 28;
            stream.Controls.Add(Caption("フォント", 12, y + 6));
            streamFontButton = new Button { Left = 130, Top = y, Width = 220, Height = 28 };
            streamFontButton.Click += delegate { var name = ChooseFont(streamFontName, 24); if (name != null) { streamFontName = name; ShowFontName(streamFontButton, name); } };
            var streamBundled = new Button { Text = "内蔵フォント", Left = 356, Top = y, Width = 100, Height = 28 };
            streamBundled.Click += delegate { streamFontName = BundledFont.FamilyName; ShowFontName(streamFontButton, streamFontName); };
            stream.Controls.AddRange(new Control[] { streamFontButton, streamBundled });
            y += 38;
            stream.Controls.Add(Caption("文字サイズ", 12, y + 4));
            streamSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = 130, Top = y, Width = 160 };
            foreach (int px in StreamSizes) streamSize.Items.Add(px == 0 ? "コメント欄と同じ" : px + " px");
            stream.Controls.Add(streamSize);
            y += 36;
            stream.Controls.Add(Caption("表示項目", 12, y));
            streamFields = Checks(stream, 20, y + 20);
            y += 54;
            stream.Controls.Add(Caption("スキン", 12, y + 4));
            streamSkin = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = 130, Top = y, Width = 160 };
            foreach (var entry in StreamTemplates.Catalog) streamSkin.Items.Add(entry[1]);
            stream.Controls.Add(streamSkin);
            y += 36;
            streamIntro = new CheckBox { Text = "新着をスライドで表示する", Left = 20, Top = y, AutoSize = true };
            stream.Controls.Add(streamIntro);
            y += 32;
            stream.Controls.Add(Hint("メニューバーの「表示設定」からも同じ設定をすぐ切り替えられます。", 12, y));

            var gadget = Page("ガジェット");
            y = 12;
            gadget.Controls.Add(Caption("時計とタイマーの窓（ステータスバーの「ガジェット」から開きます）", 12, y)); y += 28;
            gadget.Controls.Add(Caption("色", 12, y + 2));
            gadgetDark = new RadioButton { Text = "ダーク", Left = 130, Top = y, AutoSize = true };
            gadgetLight = new RadioButton { Text = "ライト", Left = 210, Top = y, AutoSize = true };
            gadget.Controls.AddRange(new Control[] { gadgetDark, gadgetLight });
            y += 36;
            gadget.Controls.Add(Caption("音量", 12, y + 6));
            gadgetVolume = new TrackBar { Minimum = 0, Maximum = 100, TickStyle = TickStyle.None, Left = 124, Top = y, Width = 220 };
            gadgetVolumeText = new Label { Left = 350, Top = y + 6, Width = 40 };
            gadgetVolume.ValueChanged += delegate { gadgetVolumeText.Text = gadgetVolume.Value.ToString(); };
            gadget.Controls.AddRange(new Control[] { gadgetVolume, gadgetVolumeText });
            y += 44;
            var testAlarm = new Button { Text = "アラームを試す", Left = 20, Top = y, Width = 130, Height = 28 };
            var testChime = new Button { Text = "チャイムを試す", Left = 158, Top = y, Width = 130, Height = 28 };
            var stop = new Button { Text = "止める", Left = 296, Top = y, Width = 80, Height = 28 };
            testAlarm.Click += delegate { FormMain.Instance.TestGadgetSound(GadgetTimer.BeepSound, gadgetVolume.Value); };
            testChime.Click += delegate { FormMain.Instance.TestGadgetSound(GadgetTimer.ChimeSound, gadgetVolume.Value); };
            stop.Click += delegate { FormMain.Instance.StopGadgetTest(); };
            FormClosed += delegate { FormMain.Instance.StopGadgetTest(); };
            gadget.Controls.AddRange(new Control[] { testAlarm, testChime, stop });
            y += 40;
            gadget.Controls.Add(Hint("アラームは止めるまで鳴ります（タイマーの「開始/停止」か「リセット」で止まります）。", 12, y));

            var keys = Page("ショートカット");
            y = 12;
            hotkeysOn = new CheckBox { Text = "ショートカットキーを有効にする（ゲームなど別の窓を使っているときも効きます）", Left = 12, Top = y, AutoSize = true };
            keys.Controls.Add(hotkeysOn);
            y += 30;
            hotkeyBoxes = new TextBox[FormMain.HotkeyActions.Length];
            hotkeyKeys = new Keys[FormMain.HotkeyActions.Length];
            for (int i = 0; i < hotkeyBoxes.Length; i++)
            {
                int index = i;
                keys.Controls.Add(Caption((string)FormMain.HotkeyActions[i][1], 24, y + 4));
                // Click the box and press the keys (with Ctrl / Alt / Shift if wanted).
                var box = new TextBox { Left = 250, Top = y, Width = 150, ReadOnly = true, BackColor = SystemColors.Window, ShortcutsEnabled = false };
                box.PreviewKeyDown += (s2, e) => { e.IsInputKey = true; };
                box.KeyDown += (s2, e) => {
                    e.SuppressKeyPress = true; e.Handled = true;
                    var code = e.KeyCode;
                    if (code == Keys.ControlKey || code == Keys.Menu || code == Keys.ShiftKey) return;
                    hotkeyKeys[index] = code == Keys.Escape || code == Keys.Back || code == Keys.Delete ? Keys.None : e.KeyData;
                    box.Text = FormMain.HotkeyText(hotkeyKeys[index]);
                };
                var clear = new Button { Text = "なし", Left = 406, Top = y - 1, Width = 50, Height = 24 };
                clear.Click += delegate { hotkeyKeys[index] = Keys.None; box.Text = FormMain.HotkeyText(Keys.None); };
                hotkeyBoxes[i] = box;
                keys.Controls.AddRange(new Control[] { box, clear });
                y += 30;
            }
            var defaults = new Button { Text = "既定に戻す", Left = 24, Top = y + 4, Width = 100, Height = 26 };
            defaults.Click += delegate {
                for (int i = 0; i < hotkeyKeys.Length; i++) { hotkeyKeys[i] = (Keys)FormMain.HotkeyActions[i][2]; hotkeyBoxes[i].Text = FormMain.HotkeyText(hotkeyKeys[i]); }
            };
            keys.Controls.Add(defaults);
            y += 40;
            keys.Controls.Add(Hint("欄をクリックしてキーを押すと変えられます（Esc / Delete で「なし」）。\r\n登録したキーは Windows 全体でこのアプリが受け取るので、ゲーム側には届かなくなります。\r\nゲームで F キーを使うなら、Ctrl+Alt+F11 のような組み合わせが安全です。", 12, y));
            y += 58;
            if (FormMain.Instance != null && FormMain.Instance.HotkeyFailures.Count > 0)
                keys.Controls.Add(new Label { Text = "ほかのアプリが使っていて登録できなかったキー：" + string.Join("、", FormMain.Instance.HotkeyFailures.ToArray()),
                    Left = 12, Top = y, Width = 520, Height = 34, ForeColor = Color.Firebrick });

            reopenLast = new CheckBox { Text = "前回開いていたスレッドを起動時に開く（自動更新 ON）", Left = 20, Top = 220, AutoSize = true };
            tabPage3.Controls.Add(reopenLast);

            // 字幕: the thread name shown at the caption's top-left.
            // Right of 字幕表示位置の微調整, above the preview.
            var title = new GroupBox { Text = "左上のスレッド名", Left = 226, Top = 104, Width = 316, Height = 92 };
            titleShown = new CheckBox { Text = "表示する", Left = 10, Top = 22, AutoSize = true };
            titleEdge = new CheckBox { Text = "枠線を付ける", Left = 110, Top = 22, AutoSize = true };
            titleBackButton = ColorButton("帯の色", 8, 52, () => titleBack, v => titleBack = v, "#1F2937");
            titleTextButton = ColorButton("文字の色", 108, 52, () => titleText, v => titleText = v, "#EEF2F7");
            titleBorderButton = ColorButton("枠線の色", 208, 52, () => titleBorder, v => titleBorder = v, "#FFFFFF");
            title.Controls.AddRange(new Control[] { titleShown, titleEdge, titleBackButton, titleTextButton, titleBorderButton });
            tabPage2.Controls.Add(title);

            // In order: 音声 字幕 AAモード 表示 配信コメント ガジェット 通信 NGWord 読み方 着信音 その他.
            // (TabPages.Insert is ignored before the control has a handle, so rebuild the list.)
            var pages = new List<TabPage>();
            foreach (TabPage page in tabControl1.TabPages)
            {
                pages.Add(page);
                if (page == tabPage7) pages.AddRange(new[] { display, stream, gadget });
                if (page == tabPage4) pages.Insert(pages.Count - 1, keys); // ショートカット before その他
            }
            tabControl1.TabPages.Clear();
            tabControl1.TabPages.AddRange(pages.ToArray());
        }

        // Selects a tab by its title (表示, 配信コメント, ガジェット ...).
        public void SelectTab(string title)
        {
            foreach (TabPage page in tabControl1.TabPages) if (page.Text == title) tabControl1.SelectedTab = page;
        }

        private void SetDisplaySettings(UserConfig c)
        {
            themeBackground = c.CommentBackground;
            themeColor.Checked = !string.IsNullOrEmpty(themeBackground);
            themeDark.Checked = !themeColor.Checked && c.CommentTheme == "dark";
            themeLight.Checked = !themeColor.Checked && !themeDark.Checked;
            Swatch(themeColorButton, FormFreeTelop.ParseColor(themeBackground, Color.FromArgb(31, 38, 48)));
            commentLayout.SelectedIndex = c.CommentLayout == "two" || c.CommentLayout == "named" ? 1 : 0;
            SetChecks(commentFields, c.CommentShowNumber, c.CommentShowName, c.CommentShowDate, c.CommentShowTime, c.CommentShowID);
            commentSeparators.Checked = c.CommentSeparators;
            commentFontName = string.IsNullOrEmpty(c.CommentFontName) ? "MS PGothic" : c.CommentFontName;
            ShowFontName(commentFontButton, commentFontName);
            commentFontSize.Value = Math.Max(10, Math.Min(40, c.CommentFontSize));

            streamFontName = string.IsNullOrEmpty(c.StreamFontName) ? BundledFont.FamilyName : c.StreamFontName;
            ShowFontName(streamFontButton, streamFontName);
            int sizeIndex = Array.IndexOf(StreamSizes, c.StreamFontSize);
            if (sizeIndex < 0) { streamSize.Items.Add(c.StreamFontSize + " px"); sizeIndex = streamSize.Items.Count - 1; }
            streamSize.SelectedIndex = sizeIndex;
            SetChecks(streamFields, c.StreamShowNumber, c.StreamShowName, c.StreamShowDate, c.StreamShowTime, c.StreamShowID);
            streamSkin.SelectedIndex = Math.Max(0, Array.FindIndex(StreamTemplates.Catalog, e => e[0] == c.StreamTemplate));
            streamIntro.Checked = c.StreamIntro;

            gadgetLight.Checked = c.GadgetTheme == "light";
            gadgetDark.Checked = !gadgetLight.Checked;
            gadgetVolume.Value = Math.Max(0, Math.Min(100, c.GadgetVolume));
            gadgetVolumeText.Text = gadgetVolume.Value.ToString();

            reopenLast.Checked = c.OpenLastOnStartup;
            hotkeysOn.Checked = c.HotkeysEnabled;
            for (int i = 0; i < hotkeyKeys.Length; i++)
            {
                hotkeyKeys[i] = FormMain.HotkeyFor(c, (string)FormMain.HotkeyActions[i][0]);
                hotkeyBoxes[i].Text = FormMain.HotkeyText(hotkeyKeys[i]);
            }
            titleShown.Checked = !c.HideCaptionTitle;
            titleEdge.Checked = c.CaptionTitleBorderOn;
            titleBack = c.CaptionTitleBack; titleText = c.CaptionTitleText; titleBorder = c.CaptionTitleBorder;
            Swatch(titleBackButton, FormFreeTelop.ParseColor(titleBack, ColorTranslator.FromHtml("#1F2937")));
            Swatch(titleTextButton, FormFreeTelop.ParseColor(titleText, ColorTranslator.FromHtml("#EEF2F7")));
            Swatch(titleBorderButton, FormFreeTelop.ParseColor(titleBorder, Color.White));
        }

        private void GetDisplaySettings(UserConfig c)
        {
            c.CommentTheme = themeDark.Checked ? "dark" : themeColor.Checked
                ? (FormFreeTelop.ParseColor(themeBackground, Color.Black).GetBrightness() < 0.5F ? "dark" : "light") : "light";
            c.CommentBackground = themeColor.Checked ? themeBackground ?? "" : "";
            c.CommentLayout = commentLayout.SelectedIndex == 1 ? "two" : "one";
            c.CommentShowNumber = commentFields[0].Checked; c.CommentShowName = commentFields[1].Checked; c.CommentShowDate = commentFields[2].Checked;
            c.CommentShowTime = commentFields[3].Checked; c.CommentShowID = commentFields[4].Checked;
            c.CommentSeparators = commentSeparators.Checked;
            c.CommentFontName = commentFontName;
            c.CommentFontSize = (int)commentFontSize.Value;

            c.StreamFontName = streamFontName;
            int index = streamSize.SelectedIndex;
            if (index >= 0 && index < StreamSizes.Length) c.StreamFontSize = StreamSizes[index];
            c.StreamShowNumber = streamFields[0].Checked; c.StreamShowName = streamFields[1].Checked; c.StreamShowDate = streamFields[2].Checked;
            c.StreamShowTime = streamFields[3].Checked; c.StreamShowID = streamFields[4].Checked;
            c.StreamTemplate = StreamTemplates.Catalog[Math.Max(0, streamSkin.SelectedIndex)][0];
            c.StreamIntro = streamIntro.Checked;

            c.GadgetTheme = gadgetLight.Checked ? "light" : "dark";
            c.GadgetVolume = gadgetVolume.Value;

            c.OpenLastOnStartup = reopenLast.Checked;
            c.HotkeysEnabled = hotkeysOn.Checked;
            c.Hotkeys = new List<string>();
            for (int i = 0; i < hotkeyKeys.Length; i++) c.Hotkeys.Add(FormMain.HotkeyActions[i][0] + "=" + (int)hotkeyKeys[i]);
            c.HideCaptionTitle = !titleShown.Checked;
            c.CaptionTitleBorderOn = titleEdge.Checked;
            c.CaptionTitleBack = titleBack; c.CaptionTitleText = titleText; c.CaptionTitleBorder = titleBorder;
        }

        private TabPage Page(string text) { return new TabPage(text) { UseVisualStyleBackColor = true, Padding = new Padding(3) }; }

        private static Label Caption(string text, int left, int top) { return new Label { Text = text, Left = left, Top = top, AutoSize = true }; }

        private static Label Hint(string text, int left, int top) { return new Label { Text = text, Left = left, Top = top, AutoSize = true, ForeColor = SystemColors.GrayText }; }

        private static CheckBox[] Checks(Control parent, int left, int top)
        {
            var boxes = new CheckBox[FieldNames.Length];
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i] = new CheckBox { Text = FieldNames[i], Left = left + i * 84, Top = top, AutoSize = true };
                parent.Controls.Add(boxes[i]);
            }
            return boxes;
        }

        private static void SetChecks(CheckBox[] boxes, params bool[] values) { for (int i = 0; i < boxes.Length; i++) boxes[i].Checked = values[i]; }

        private Button ColorButton(string text, int left, int top, Func<string> get, Action<string> set, string fallback)
        {
            var button = new Button { Text = text, Left = left, Top = top, Width = 96, Height = 28, TextImageRelation = TextImageRelation.ImageBeforeText };
            button.Click += delegate {
                using (var dialog = new ColorDialog { FullOpen = true, Color = FormFreeTelop.ParseColor(get(), ColorTranslator.FromHtml(fallback)) })
                    if (dialog.ShowDialog(this) == DialogResult.OK) { set(Html(dialog.Color)); Swatch(button, dialog.Color); }
            };
            return button;
        }

        private static string Html(Color color) { return ColorTranslator.ToHtml(Color.FromArgb(color.R, color.G, color.B)); }

        private static void Swatch(Button button, Color color)
        {
            var swatch = new Bitmap(14, 14);
            using (var g = Graphics.FromImage(swatch))
            using (var fill = new SolidBrush(color)) { g.FillRectangle(fill, 0, 0, 14, 14); g.DrawRectangle(Pens.Gray, 0, 0, 13, 13); }
            if (button.Image != null) button.Image.Dispose();
            button.Image = swatch;
        }

        private static void ShowFontName(Button button, string name)
        {
            button.Text = name == BundledFont.FamilyName ? "源暎エムゴ v2 Black（内蔵）" : name;
        }

        // The font dialog cannot list the bundled font; it starts from an installed one then.
        private string ChooseFont(string current, int pixels)
        {
            string start = string.IsNullOrEmpty(current) || current == BundledFont.FamilyName ? "Meiryo" : current;
            try
            {
                using (var preview = new Font(start, Math.Max(8, pixels), FontStyle.Regular, GraphicsUnit.Pixel))
                using (var chooser = new FontDialog { Font = preview, FontMustExist = true, ShowEffects = false })
                    return chooser.ShowDialog(this) == DialogResult.OK ? chooser.Font.FontFamily.Name : null;
            }
            catch (ArgumentException)
            {
                MessageBox.Show(this, "このフォントは使用できません。TrueType形式（.ttf / .ttc）のフォントを選んでください。", "フォント", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
        }
    }
}
