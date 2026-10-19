// Reading controls: auto-advance, current-reply highlight, click to read, 2026-10-19. GPLv2.
// 自動更新 fetches new replies; 自動送り (auto-advance) reads on to the next
// reply and keeps it in view. With auto-advance OFF the reader steps at their
// own pace with ⏩ / ⏪ or by clicking a comment, and new replies keep arriving.
using System;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        // Queue position of the reply most recently started; highlighted in the view.
        private int lastSpokenNumber;
        private bool highlightShown;
        // The highlight goes away this long after the latest reply has been read.
        private static readonly TimeSpan HighlightLinger = TimeSpan.FromSeconds(10);
        private ToolStripStatusLabel unreadStatusLabel;

        private void InitializeReadingUi()
        {
            unreadStatusLabel = new ToolStripStatusLabel { Name = "unreadStatusLabel", Available = false,
                ToolTipText = "自動送りOFFの間に届いた、まだ読んでいないレスの数" };
            statusStrip1.Items.Insert(1, unreadStatusLabel);
        }

        public void SetAutoAdvance(bool enabled)
        {
            if (UserConfig != null) UserConfig.AutoAdvance = enabled;
            toolStripButtonAutoScroll.Checked = enabled;
            if (enabled) HighlightSpoken(true);
            UpdateUnreadStatus();
        }

        private void MarkSpoken(int number)
        {
            lastSpokenNumber = number;
            HighlightSpoken(true);
        }

        private void HighlightSpoken(bool follow)
        {
            if (webBrowser.Document == null) return;
            // View anchors carry Response.Number; the queue position is its index + 1.
            int anchor = lastSpokenNumber > 0 && lastSpokenNumber <= responses.Count ? responses[lastSpokenNumber - 1].Number : 0;
            webBrowser.Document.InvokeScript("setCurrent", new object[] { anchor, follow });
            highlightShown = anchor > 0;
        }

        // ▶ reads the current reply again.
        private void ReplayCurrentResponse()
        {
            if (IsDesignPreview || responses.Count == 0) return;
            int number = lastSpokenNumber > 0 && lastSpokenNumber <= responses.Count
                ? lastSpokenNumber : Math.Max(1, Math.Min(CurrentResNumber, responses.Count));
            StartSpeaking(number);
        }

        private void ReadClickedResponse(string value)
        {
            int anchor;
            if (!int.TryParse(value, out anchor)) return;
            int position = responses.FindIndex(r => r.Number == anchor) + 1;
            if (position > 0) StartSpeaking(position);
        }

        // Called from the reading timer: once everything has been read and nothing
        // new arrived for a while, drop the yellow highlight.
        private void ClearHighlightWhenIdle(DateTime now)
        {
            if (!highlightShown || isSpeaking || lastSpokenNumber < responses.Count) return;
            if (now - speakingCompletedTime < HighlightLinger) return;
            if (webBrowser.Document != null) webBrowser.Document.InvokeScript("setCurrent", new object[] { 0, false });
            highlightShown = false;
        }

        private void UpdateUnreadStatus()
        {
            if (unreadStatusLabel == null || UserConfig == null) return;
            int read = Math.Max(lastSpokenNumber, CurrentResNumber - 1);
            int unread = UserConfig.AutoAdvance ? 0 : Math.Max(0, responses.Count - read);
            unreadStatusLabel.Available = unread > 0;
            string text = "未読 " + unread;
            if (unreadStatusLabel.Text != text) unreadStatusLabel.Text = text;
        }
    }
}
