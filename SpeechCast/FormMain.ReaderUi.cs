// UI refresh, 2026-10-18. Distributed under the project's GPLv2 license.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private ToolStrip readerToolbar;
        private bool presentation;
        private bool previousListCollapsed;
        private bool previousCaptionCollapsed;
        private FormBorderStyle previousBorder;
        private ToolStripMenuItem presentationMenu;
        private readonly bool IsDesignPreview = Environment.GetEnvironmentVariable("SPEECHCAST_UI_PREVIEW") == "1";

        private void InitializeReaderUi()
        {
            Font = new Font("Yu Gothic UI", 10F);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            BackColor = Color.FromArgb(248, 250, 252);
            MinimumSize = new Size(240, 220);
            // 2026-10-18: keep the original three rows and their existing handlers.
            readerToolbar = myToolStripPlay;
            readerToolbar.Items.Clear();
            SetIconButton(toolStripButtonThreads, "threads", "スレッド一覧");
            SetIconButton(toolStripButtonResponse, "write", "レスを書く");
            SetIconButton(toolStripButtonFirst, "first", "最初のレス");
            SetIconButton(toolStripButtonPrev, "prev", "前のレスを読む");
            SetIconButton(toolStripButtonStop, "stop", "停止（読み上げを止め、自動送りをOFF。自動更新は続く）");
            var play = playButton = new ToolStripButton { Name = "playCurrentResponse" };
            SetIconButton(play, "play", "今のレスをもう一度読む");
            play.Click += delegate { ReplayCurrentResponse(); };
            SetIconButton(toolStripButtonNext, "next", "次のレスを読む");
            SetIconButton(toolStripButtonLast, "last", "最新のレスを読む");
            toolStripButtonTurbo.Text = "ターボ";
            toolStripButtonTurbo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonTurbo.ToolTipText = "ターボモード ON / OFF";
            // The switches used every broadcast lead the row: auto update (fetch
            // new replies), auto-advance (read on and follow) and jump to latest.
            SetIconButton(toolStripButtonAutoUpdate, "update", "自動更新 ON / OFF：新しいレスを取得する（ONで末尾へ移動）");
            SetLabelledIcon(toolStripButtonAutoUpdate, "自動更新");
            SetIconButton(toolStripButtonAutoScroll, "autoscroll", "自動送り ON / OFF：読み終えたら次のレスへ進む。OFFで⏩やコメントのクリックで1つずつ読む");
            SetLabelledIcon(toolStripButtonAutoScroll, "自動送り");
            SetIconButton(toolStripButtonZoomOutnResView, "zoomout", "コメント文字を小さく");
            SetIconButton(toolStripButtonZoomInResView, "zoomin", "コメント文字を大きく");
            SetIconButton(toolStripButtonMoveBottom, "bottom", "最新レスへ移動");
            readerToolbar.Items.AddRange(new ToolStripItem[] { toolStripButtonThreads, toolStripButtonResponse, new ToolStripSeparator(),
                toolStripButtonAutoUpdate, toolStripButtonAutoScroll, toolStripButtonMoveBottom,
                new ToolStripSeparator(), toolStripButtonFirst, toolStripButtonPrev, toolStripButtonStop, play,
                toolStripButtonNext, toolStripButtonLast, toolStripButtonTurbo });
            foreach (ToolStripButton button in new[] { toolStripButtonZoomInResView, toolStripButtonZoomOutnResView })
            {
                button.Alignment = ToolStripItemAlignment.Right;
                readerToolbar.Items.Add(button);
            }

            SetIconButton(toolStripButtonCaption, "caption", "テロップ表示 ON / OFF");
            SetIconButton(toolStripButtonShowCaptionImmediately, "instant", "テロップを瞬間表示 ON / OFF");
            InitializeAudioUi();
            SetIconButton(toolStripButtonBorder, "frame", "テロップの位置調整枠 ON / OFF");
            SetIconButton(toolStripButtonSettings, "settings", "設定");
            SetIconButton(toolStripButtonOpenResCaptionForm, "altcaption", "代替字幕を開く");
            toolStripLabel3.Text = "音量";
            toolStripLabel8.Text = "着信音量";
            toolStripComboBoxSelectVoice.Width = 132;
            toolStripTrackBarVoiceVolume.Width = 80;
            toolStripTrackBarPlayVolume.Width = 65;

            var appearance = new ToolStripDropDownButton("表示") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            AddAppearanceItem(appearance, "1行モード（項目と本文を横並び）", "one", null);
            AddAppearanceItem(appearance, "2行モード（項目の下に本文）", "two", null);
            appearance.DropDownItems.Add("表示項目・フォント…", null, delegate { ShowReaderAppearance(); });
            appearance.DropDownItems.Add(new ToolStripSeparator());
            AddAppearanceItem(appearance, "明るい背景", null, "light");
            AddAppearanceItem(appearance, "暗い背景", null, "dark");
            AddAppearanceItem(appearance, "ペーパー", null, "paper");
            appearance.DropDownItems.Add("背景色を選ぶ…", null, delegate {
                if (UserConfig == null) return;
                using (var dialog = new ColorDialog { FullOpen = true })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        UserConfig.CommentBackground = ColorTranslator.ToHtml(dialog.Color);
                        // Select a contrasting text palette for the chosen background.
                        UserConfig.CommentTheme = dialog.Color.GetBrightness() < 0.5F ? "dark" : "light";
                        ApplyReaderAppearance();
                    }
                }
            });
            appearance.DropDownItems.Add(new ToolStripSeparator());
            presentationMenu = new ToolStripMenuItem("コメントだけ表示  (F11)");
            presentationMenu.Click += delegate { TogglePresentation(); };
            appearance.DropDownItems.Add(presentationMenu);
            var autoScroll = new ToolStripMenuItem("自動送り");
            autoScroll.Click += delegate { toolStripButtonAutoScroll.PerformClick(); };
            appearance.DropDownOpening += delegate { autoScroll.Checked = UserConfig != null && UserConfig.AutoAdvance; };
            appearance.DropDownItems.Add(autoScroll);
            appearance.DropDownItems.Add("レス一覧の表示切替", null, delegate { toolStripButtonListToggle.PerformClick(); });
            appearance.DropDownItems.Add("代替字幕を開く", null, delegate { openResCaptionForm(); });
            menuStrip1.Items.Add(appearance);

            foreach (var strip in new ToolStrip[] { myToolStripVoice, myToolStripPlay, myToolStripBrowser, myToolStripUrl, menuStrip1 })
            {
                strip.Font = new Font("Yu Gothic UI", 9F);
                strip.BackColor = BackColor;
                strip.GripStyle = ToolStripGripStyle.Hidden;
                strip.Padding = new Padding(4, 2, 4, 2);
                strip.Renderer = toolbarRenderer;
            }
            // Remove only the URL row's old left-side controls. Enter still opens a URL.
            myToolStripUrl.Items.Clear();
            myToolStripUrl.Items.AddRange(new ToolStripItem[] { toolStripTextBoxURL, toolStripLabel7, toolStripTextBoxThreadKeyword, toolStripButtonAutoNextThread });
            InitializeFastUi();
            InitializeReadingUi();
            toolStripLabel7.Text = "次スレキーワード";
            toolStripButtonAutoNextThread.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonAutoNextThread.ToolTipText = "次スレへ自動移動 ON / OFF";
            toolStripButtonAutoNextThread.CheckedChanged += delegate { toolStripButtonAutoNextThread.Text = toolStripButtonAutoNextThread.Checked ? "ON" : "OFF"; };
            toolStripButtonAutoNextThread.Text = toolStripButtonAutoNextThread.Checked ? "ON" : "OFF";
            toolStripTextBoxThreadKeyword.AutoSize = false;
            toolStripTextBoxThreadKeyword.Width = 100;
            myToolStripUrl.Layout += delegate {
                int available = myToolStripUrl.ClientSize.Width - myToolStripUrl.Padding.Horizontal - 28 - feedModeSelector.Width - feedModeSelector.Margin.Horizontal;
                if (UserConfig != null && UserConfig.JpnknBoardMode)
                {
                    toolStripLabel7.Available = false;
                    toolStripTextBoxURL.AutoSize = false;
                    toolStripTextBoxURL.Width = Math.Max(60, available);
                    return;
                }
                int keywordWidth = Math.Max(60, Math.Min(140, available / 4));
                int keywordLabel = available >= 450 ? toolStripLabel7.Width + toolStripLabel7.Margin.Horizontal : 0;
                toolStripLabel7.Available = available >= 450;
                toolStripTextBoxThreadKeyword.Width = keywordWidth;
                toolStripTextBoxURL.AutoSize = false;
                toolStripTextBoxURL.Width = Math.Max(60, available - keywordWidth - keywordLabel - toolStripButtonAutoNextThread.Width - 12);
            };
            toolStripTextBoxURL.ToolTipText = "スレッドURL（Enterで読み込み）";
            toolStripTextBoxThreadKeyword.ToolTipText = "次スレキーワード";
            webBrowser.IsWebBrowserContextMenuEnabled = false;
            var context = new ContextMenuStrip();
            context.Items.Add("操作パネルを表示 / 非表示  (F11)", null, delegate { TogglePresentation(); });
            context.Items.Add("最新レスへ", null, delegate { toolStripButtonMoveBottom.PerformClick(); });
            context.Items.Add("表示項目・フォント…", null, delegate { ShowReaderAppearance(); });
            webBrowser.ContextMenuStrip = context;
            webBrowser.DocumentCompleted += delegate { ApplyReaderAppearance(); };
            Shown += delegate { FinishReaderUi(); };
            SetReaderChrome();
        }

        private readonly ReaderToolbarRenderer toolbarRenderer = new ReaderToolbarRenderer();

        private static int ToolbarIconSize
        {
            get { using (var screen = Graphics.FromHwnd(IntPtr.Zero)) return (int)Math.Round(18 * screen.DpiX / 96F); }
        }

        private ToolStripButton playButton;

        private static void SetLabelledIcon(ToolStripButton button, string label)
        {
            button.Text = label;
            button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.Padding = new Padding(4, 0, 6, 0);
        }

        private static void SetIconButton(ToolStripButton button, string icon, string description)
        {
            if (button.Image != null && button.Tag as string == "reader-icon") button.Image.Dispose();
            button.Image = ToolbarIcons.Create(icon, ToolbarIconSize);
            button.Tag = "reader-icon";
            button.ImageScaling = ToolStripItemImageScaling.None;
            button.Text = description;
            button.ToolTipText = description;
            button.AccessibleName = description;
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Font = new Font("Yu Gothic UI", 9F);
            button.AutoSize = true;
            button.Padding = new Padding(3, 1, 3, 1);
            button.Margin = new Padding(0, 0, 1, 0);
        }

        private void AddAppearanceItem(ToolStripDropDownButton menu, string label, string layout, string theme)
        {
            var item = new ToolStripMenuItem(label);
            item.Click += delegate {
                if (UserConfig == null) return;
                if (layout != null) UserConfig.CommentLayout = layout;
                if (theme != null) { UserConfig.CommentTheme = theme; UserConfig.CommentBackground = ""; }
                ApplyReaderAppearance();
            };
            menu.DropDownItems.Add(item);
            menu.DropDownOpening += delegate {
                if (UserConfig != null) item.Checked = layout != null ? (layout == "two" ? UserConfig.CommentLayout == "two" || UserConfig.CommentLayout == "named" : UserConfig.CommentLayout != "two" && UserConfig.CommentLayout != "named") : UserConfig.CommentTheme == theme && string.IsNullOrEmpty(UserConfig.CommentBackground);
            };
        }

        private void ApplyReaderAppearance()
        {
            if (UserConfig == null || webBrowser.Document == null) return;
            webBrowser.Document.InvokeScript("setAppearance", new object[] {
                UserConfig.CommentLayout ?? "one", UserConfig.CommentTheme ?? "light", UserConfig.CommentBackground ?? "",
                UserConfig.CommentFontSize, UserConfig.CommentFontName,
                UserConfig.CommentShowNumber, UserConfig.CommentShowName, UserConfig.CommentShowDate, UserConfig.CommentShowTime, UserConfig.CommentShowID });
            foreach (Response response in responses) response.ScrollY = -1;
        }

        private void FinishReaderUi()
        {
            SyncAudioModeUi();
            UpdateFeedModeUi();
            splitContainer1.Panel1Collapsed = true;
            splitContainerResCaption.Panel2Collapsed = true;
            // Font size now uses native pixels; old fractional CSS zoom blurred small glyphs.
            if (UserConfig != null) UserConfig.magnification = 1;
            if (UserConfig != null && UserConfig.CommentLayout == "plain") { UserConfig.CommentLayout = "one"; UserConfig.CommentShowNumber = false; }
            if (UserConfig != null && UserConfig.CommentLayout == "named") { UserConfig.CommentLayout = "two"; UserConfig.CommentShowName = true; }
            if (webBrowser.Document != null) webBrowser.Document.InvokeScript("setZoom", new object[] { 1 });
            ApplyReaderAppearance();
        }

        private void SetReaderChrome()
        {
            myToolStripVoice.Visible = !presentation;
            myToolStripPlay.Visible = !presentation;
            myToolStripBrowser.Visible = false;
            readerToolbar.Visible = !presentation;
            myToolStripUrl.Visible = !presentation;
            menuStrip1.Visible = !presentation;
            statusStrip1.Visible = !presentation;
        }

        private void TogglePresentation()
        {
            presentation = !presentation;
            if (presentation)
            {
                previousListCollapsed = splitContainer1.Panel1Collapsed;
                previousCaptionCollapsed = splitContainerResCaption.Panel2Collapsed;
                previousBorder = FormBorderStyle;
                splitContainer1.Panel1Collapsed = true;
                splitContainerResCaption.Panel2Collapsed = true;
                FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                FormBorderStyle = previousBorder;
                splitContainer1.Panel1Collapsed = previousListCollapsed;
                splitContainerResCaption.Panel2Collapsed = previousCaptionCollapsed;
            }
            presentationMenu.Checked = presentation;
            SetReaderChrome();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11 || (presentation && keyData == Keys.Escape)) { TogglePresentation(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void LoadDesignPreview()
        {
            // An offline visual test: never reads or saves the user's normal configuration.
            UserConfig = new UserConfig(System.IO.Path.Combine(Application.StartupPath, "preview.cfg"));
            UserConfig.Initialize();
            Bookmarks = new Bookmarks(System.IO.Path.Combine(Application.StartupPath, "preview-bookmarks.cfg"));
            timer.Enabled = false;
            timerWeb.Enabled = false;
            Text = "Speech Cast SP — UI プレビュー";
            Size = new Size(760, 620);
            toolStripTextBoxURL.Text = "プレビュー：ネット接続・読み上げは行いません";
            foreach (ToolStripItem item in readerToolbar.Items)
                if (item != toolStripButtonAutoUpdate && item != toolStripButtonAutoScroll && item != toolStripButtonZoomOutnResView && item != toolStripButtonZoomInResView && item != toolStripButtonMoveBottom) item.Enabled = false;
            myToolStripUrl.Enabled = false;
            toolStripMenuItemBookmarks.Enabled = false;
            toolStripMenuItemSpeech.Enabled = false;
            splitContainerResCaption.Panel2.Enabled = false;
            if (toolStripComboBoxSelectVoice.Items.Count > 0) toolStripComboBoxSelectVoice.SelectedIndex = 0;
            toolStripTrackBarVoiceVolume.Value = UserConfig.SpeakingVolume;
            toolStripTrackBarPlayVolume.Value = UserConfig.PlayVolume;
            UserConfig.CaptionFont.Name = BundledFont.FamilyName;
            UserConfig.CaptionFont.Size = 30;
            UserConfig.CaptionFont.Bold = false;
            UserConfig.HideCaptionTitle = false;
            FormCaption.Instance.Bounds = new Rectangle(80, 120, 680, 160);
            FormCaption.Instance.CaptionText = "描画・田・四角の輪郭 ABC 720";
            FormCaption.Instance.setTitle("サンプルスレッド [779/779]");
            webBrowser.DocumentCompleted += delegate {
                if (responses.Count != 0) return;
                Response.Style = Response.BBSStyle.jbbs;
                string[] samples = { "こんばんは。配信楽しみにしていました！", "文字が読みやすくなりましたね。", "長めのコメントもウィンドウの幅に合わせて折り返します。右端まで自然に使い、必要以上の余白を作りません。", "改行したコメントです。<br>２行目もそのまま表示します。", "配信用には「本文のみ」と暗い背景の組み合わせも使えます。", "レスごとに細い線で区切り、読みやすく。" };
                for (int i = 0; i < 60; i++)
                {
                    var response = new Response();
                    response.SetRawText((720 + i) + "<>名無し<><>2026/10/18(日) 12:34:56.789<>" + samples[i % samples.Length] + "<>デザイン確認<>preview" + i);
                    responses.Add(response);
                    webBrowser.Document.InvokeScript("addRes", new object[] { response.Html });
                }
                CompleteThreadLoad(true);
            };
            FinishReaderUi();
        }
    }
}
