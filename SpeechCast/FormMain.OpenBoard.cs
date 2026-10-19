// Opening boards: the bookmark menu, renamed 掲示板を開く, and reopening the last thread on
// start-up (settings → 通信), 2026-10-20. GPLv2. The separate 掲示板を開く dialog duplicated
// the bookmarks, so it is gone; jpnkn opens in the last reading mode chosen on the status bar.
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private void InitializeOpenBoardUi()
        {
            toolStripMenuItemBookmarks.Text = "掲示板を開く(&B)";
            toolStripMenuItemAddBookMark.Text = "掲示板を登録(&A)";
            toolStripMenuItemEditBookmarks.Text = "掲示板リストの編集(&E)";
            // First on the menu bar, where the dialog used to be.
            menuStrip1.Items.Remove(toolStripMenuItemBookmarks);
            menuStrip1.Items.Insert(0, toolStripMenuItemBookmarks);
        }

        internal static bool IsJpnknInput(string input)
        {
            input = (input ?? "").Trim();
            JpnknFastTarget target;
            return input.Length > 0 && (Regex.IsMatch(input, @"^[A-Za-z0-9_\-]+$") || JpnknFastTarget.TryParse(input, true, out target));
        }

        // Start-up: reopen the thread (or board) that was open when the app closed, if enabled.
        private void OpenLastOnStartup()
        {
            if (IsDesignPreview || UserConfig == null) return;
            if (Environment.GetCommandLineArgs().Length > 1) return; // a URL argument was opened already
            if (!UserConfig.OpenLastOnStartup || string.IsNullOrWhiteSpace(UserConfig.URL)) return;
            toolStripTextBoxURL.Text = UserConfig.URL;
            UpdateFeedModeUi();
            GetFromURL();
        }
    }
}
