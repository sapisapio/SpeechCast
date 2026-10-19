// Open a board or thread, choose the jpnkn reading mode, reopen it on start-up, 2026-10-19. GPLv2.
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private const string FastDescription =
            "スレッドを選ばずに、この板のすべてのスレに届く新着レスを到着順に受信します。"
            + "jpnknの公式の新着配信（Fast）を使うので、数秒おきに読み込むより早く届きます。"
            + "元スレへのリンクと元のレス番号も表示します。受信を始める前の過去レスは読み込みません。";
        private const string ThreadDescription =
            "従来どおりスレッドを1つ選び、過去のレスから読みます。jpnknでは新着もFastで受け取ります。";

        private void InitializeOpenBoardUi()
        {
            var open = new ToolStripMenuItem("掲示板を開く…") { Name = "openBoardMenu" };
            open.Click += delegate { ShowOpenBoard(); };
            menuStrip1.Items.Insert(0, open);
        }

        internal static bool IsJpnknInput(string input)
        {
            input = (input ?? "").Trim();
            JpnknFastTarget target;
            return input.Length > 0 && (Regex.IsMatch(input, @"^[A-Za-z0-9_\-]+$") || JpnknFastTarget.TryParse(input, true, out target));
        }

        // Returns true when the user chose to open something.
        internal bool ShowOpenBoard()
        {
            if (UserConfig == null || IsDesignPreview) return false;
            using (var dialog = new Form { Text = "掲示板を開く", Font = Font, StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(460, 330), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            {
                dialog.Controls.Add(new Label { Text = "掲示板（板）またはスレッドのURL", Left = 18, Top = 16, AutoSize = true });
                var url = new TextBox { Left = 18, Top = 38, Width = 424, Text = toolStripTextBoxURL.Text };
                dialog.Controls.Add(url);
                var jpnkn = new Panel { Left = 18, Top = 72, Width = 424, Height = 186 };
                var fast = new RadioButton { Text = "板全体 Fast で受信する", Left = 0, Top = 0, AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
                var fastNote = new Label { Text = FastDescription, Left = 18, Top = 24, Width = 404, Height = 76, ForeColor = Color.FromArgb(90, 100, 116) };
                var thread = new RadioButton { Text = "スレッドを選んで読む", Left = 0, Top = 108, AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
                var threadNote = new Label { Text = ThreadDescription, Left = 18, Top = 132, Width = 404, Height = 44, ForeColor = Color.FromArgb(90, 100, 116) };
                jpnkn.Controls.AddRange(new Control[] { fast, fastNote, thread, threadNote });
                dialog.Controls.Add(jpnkn);
                var other = new Label { Text = "板のURLならスレッド一覧が開きます。スレッドのURLならそのまま読み込みます。",
                    Left = 18, Top = 76, Width = 424, Height = 40, ForeColor = Color.FromArgb(90, 100, 116) };
                dialog.Controls.Add(other);
                var reopen = new CheckBox { Text = "次回起動時にこの掲示板を自動で開く（自動更新ON）", Left = 18, Top = 264, AutoSize = true, Checked = UserConfig.OpenLastOnStartup };
                dialog.Controls.Add(reopen);
                var ok = new Button { Text = "開く", DialogResult = DialogResult.OK, Left = 258, Top = 292, Width = 88 };
                var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Left = 354, Top = 292, Width = 88 };
                dialog.Controls.Add(ok); dialog.Controls.Add(cancel); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
                bool threadUrl = Regex.IsMatch(url.Text, @"/read\.cgi/");
                fast.Checked = UserConfig.JpnknBoardMode && !threadUrl;
                thread.Checked = !fast.Checked;
                EventHandler sync = delegate {
                    bool isJpnkn = IsJpnknInput(url.Text);
                    jpnkn.Visible = isJpnkn;
                    other.Visible = !isJpnkn;
                    ok.Enabled = url.Text.Trim().Length > 0;
                };
                url.TextChanged += sync;
                sync(null, EventArgs.Empty);
                if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                UserConfig.OpenLastOnStartup = reopen.Checked;
                string target = url.Text.Trim();
                UserConfig.JpnknBoardMode = IsJpnknInput(target) && fast.Checked;
                // A bare jpnkn board ID becomes its board URL so the thread list can open.
                if (!UserConfig.JpnknBoardMode && Regex.IsMatch(target, @"^[A-Za-z0-9_\-]+$")) target = "https://bbs.jpnkn.com/" + target + "/";
                UpdateFeedModeUi();
                toolStripTextBoxURL.Text = target;
                GetFromURL();
                return true;
            }
        }

        // Start-up: reopen the last board or thread (auto update turns on once it
        // loads); with nothing saved yet, ask which board to open.
        private void OpenLastOnStartup()
        {
            if (IsDesignPreview || UserConfig == null) return;
            if (Environment.GetCommandLineArgs().Length > 1) return; // a URL argument was opened already
            if (string.IsNullOrWhiteSpace(UserConfig.URL)) { ShowOpenBoard(); return; }
            if (!UserConfig.OpenLastOnStartup) return;
            toolStripTextBoxURL.Text = UserConfig.URL;
            UpdateFeedModeUi();
            GetFromURL();
        }
    }
}
