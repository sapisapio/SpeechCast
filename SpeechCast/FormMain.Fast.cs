// jpnkn Fast integration and board-wide feed, 2026-10-19. GPLv2.
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private ToolStripComboBox feedModeSelector;
        private ToolStripStatusLabel fastStatusLabel;
        private JpnknFastTarget fastTarget;
        private JpnknFastClient fastClient;
        private int fastGeneration;
        private bool fastConnected, readingPaused, syncingFeedMode, loadingThread;
        private bool fastNeedsRecovery;
        private int fastExpectedNumber;
        private DateTime lastFastRecovery = DateTime.MinValue;
        private readonly HashSet<string> fastSeen = new HashSet<string>();
        private readonly Queue<string> fastSeenOrder = new Queue<string>();
        private const int FastSeenLimit = 20000, BoardFeedLimit = 3000, BoardFeedKeep = 2000;
        private readonly Queue<JpnknFastMessage> fastPending = new Queue<JpnknFastMessage>();
        private bool IsFastBoardFeed { get { return fastTarget != null && fastTarget.Thread == null; } }
        private int CurrentResponseLimit { get { return IsFastBoardFeed ? int.MaxValue : Response.MaxResponseCount; } }

        private void InitializeFastUi()
        {
            feedModeSelector = new ToolStripComboBox { Name = "feedModeSelector", DropDownStyle = ComboBoxStyle.DropDownList,
                AutoSize = false, Width = 108, ToolTipText = "スレッド指定 / jpnkn板全体の新着受信" };
            feedModeSelector.Items.AddRange(new object[] { "スレッド", "板全体 Fast" });
            myToolStripUrl.Items.Insert(0, feedModeSelector);
            fastStatusLabel = new ToolStripStatusLabel { Name = "fastStatusLabel", Available = false };
            statusStrip1.Items.Insert(1, fastStatusLabel);
            feedModeSelector.SelectedIndexChanged += delegate {
                if (syncingFeedMode || UserConfig == null) return;
                UserConfig.JpnknBoardMode = feedModeSelector.SelectedIndex == 1;
                if (!IsDesignPreview) { StopSpeakingCore(); AutoUpdate = false; ClearFastSession(); }
                UpdateFeedModeUi();
            };
        }

        private void UpdateFeedModeUi()
        {
            if (UserConfig == null) return;
            syncingFeedMode = true;
            feedModeSelector.SelectedIndex = UserConfig.JpnknBoardMode ? 1 : 0;
            syncingFeedMode = false;
            toolStripLabel7.Available = toolStripTextBoxThreadKeyword.Available = toolStripButtonAutoNextThread.Available = !UserConfig.JpnknBoardMode;
            // Board feeds have no single destination thread for the old writer.
            toolStripButtonResponse.Enabled = toolStripButtonThreads.Enabled = !IsDesignPreview && !UserConfig.JpnknBoardMode;
            toolStripButtonResponse.ToolTipText = UserConfig.JpnknBoardMode ? "投稿はコメントの元スレへのリンクから行ってください" : "レスを書く";
            toolStripTextBoxURL.ToolTipText = UserConfig.JpnknBoardMode
                ? "jpnknの掲示板URLまたは板ID（Enterで板全体の新着受信を開始）"
                : "スレッドURL（Enterで読み込み。jpnknはFastで新着受信）";
            myToolStripUrl.PerformLayout();
        }

        private void StopFastClient()
        {
            fastGeneration++;
            if (fastClient != null) fastClient.Dispose();
            fastClient = null;
            fastConnected = false;
            if (fastStatusLabel != null) fastStatusLabel.Text = "Fast 停止";
        }

        private void ClearFastSession()
        {
            StopFastClient();
            fastTarget = null;
            fastPending.Clear(); fastSeen.Clear(); fastSeenOrder.Clear();
            readingPaused = false;
            fastExpectedNumber = 0;
            fastNeedsRecovery = false;
            if (fastStatusLabel != null) fastStatusLabel.Available = false;
        }

        private void OnFastAcquisitionChanged(bool enabled)
        {
            if (fastTarget == null) return;
            readingPaused = false;
            if (enabled) EnsureFastSubscription(); else StopFastClient();
        }

        private void PostFastEvent(int generation, Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try { BeginInvoke(new Action(delegate { if (!IsDisposed && generation == fastGeneration && AutoUpdate) action(); })); }
            catch (InvalidOperationException) { }
        }

        private void EnsureFastSubscription()
        {
            if (fastTarget == null || fastClient != null || !AutoUpdate || IsDesignPreview) return;
            int generation = ++fastGeneration;
            fastClient = new JpnknFastClient(fastTarget.Board);
            fastStatusLabel.Available = true;
            fastStatusLabel.Text = "Fast 接続中…";
            fastClient.ConnectionChanged += (connected, message) => PostFastEvent(generation, delegate {
                fastConnected = connected;
                fastStatusLabel.Text = connected ? "Fast 受信中" : "Fast 再接続待ち";
                fastStatusLabel.ToolTipText = message;
                if (connected && !IsFastBoardFeed) { fastNeedsRecovery = true; lastFastRecovery = DateTime.MinValue; }
            });
            fastClient.ReplyReceived += message => PostFastEvent(generation, delegate {
                if (!endWebRequest) fastPending.Enqueue(message); else ApplyFastMessage(message);
            });
            fastClient.Start();
        }

        private void ConfigureThreadFast()
        {
            ClearFastSession();
            JpnknFastTarget target;
            if (JpnknFastTarget.TryParse(toolStripTextBoxURL.Text, false, out target)) fastTarget = target;
        }

        private void BeginBoardFeed(JpnknFastTarget target)
        {
            ClearFastSession();
            fastTarget = target;
            Response.Style = Response.BBSStyle.zerochan;
            responses.Clear(); listViewResponses.Items.Clear();
            if (webBrowser.Document != null) webBrowser.Document.InvokeScript("clearRes");
            oldUrl = null; rawURL = null; oldResCount = 0; datSize = 1;
            baseURL = target.BoardUrl;
            threadId = "";
            threadTitle = target.Board + " — 板全体 Fast";
            Text = "Speech Cast SP — " + threadTitle;
            endThreadWarning = false; WarningNextThread = false; openNextThread = ReadThread;
            isSpeakingWarningMessage = false;
            CurrentResNumber = 1;
            CompleteThreadLoad(true);
            EnsureFastSubscription();
        }

        private void ApplyFastMessage(JpnknFastMessage message)
        {
            if (!AutoUpdate || fastTarget == null || message.Board != fastTarget.Board ||
                (!IsFastBoardFeed && message.Thread != fastTarget.Thread)) return;
            if (IsFastBoardFeed)
            {
                if (!fastSeen.Add(message.Key)) return;
                fastSeenOrder.Enqueue(message.Key);
                while (fastSeenOrder.Count > FastSeenLimit) fastSeen.Remove(fastSeenOrder.Dequeue());
            }
            else
            {
                fastExpectedNumber = Math.Max(fastExpectedNumber, message.Number);
                if (message.Number <= responses.Count) return;
                if (message.Number != responses.Count + 1) { fastNeedsRecovery = true; return; }
            }
            int oldCount = responses.Count;
            var response = message.ToResponse(oldCount + 1, IsFastBoardFeed, threadTitle);
            if (response == null) return;
            responses.Add(response);
            listViewResponses.Items.Add(response.CreateListViewItem());
            if (webBrowser.Document != null) webBrowser.Document.InvokeScript("addRes", new object[] { response.Html });
            NotifyNewResponses(true, oldCount, responses.Count);
            if (IsFastBoardFeed) TrimBoardFeed();
        }

        // A board feed never ends, so drop already-read comments from the front
        // once it grows past the limit. Unread comments are always kept.
        private void TrimBoardFeed()
        {
            if (responses.Count <= BoardFeedLimit) return;
            int count = Math.Min(responses.Count - BoardFeedKeep, CurrentResNumber - 1);
            if (count <= 0) return;
            responses.RemoveRange(0, count);
            for (int i = 0; i < responses.Count; i++) responses[i].Renumber(i + 1);
            listViewResponses.BeginUpdate();
            for (int i = 0; i < count && listViewResponses.Items.Count > 0; i++) listViewResponses.Items.RemoveAt(0);
            listViewResponses.EndUpdate();
            if (webBrowser.Document != null) webBrowser.Document.InvokeScript("trimRes", new object[] { count });
            CurrentResNumber -= count;
        }

        private void DrainFastMessages()
        {
            while (fastPending.Count > 0) ApplyFastMessage(fastPending.Dequeue());
        }

        private async System.Threading.Tasks.Task<bool> HandleFastRecovery()
        {
            if (fastTarget == null) return false;
            if (IsFastBoardFeed) return true;
            if (!AutoUpdate || !timerTickEnabled || !endWebRequest || loadingThread) return true;
            // Fast events append directly. HTTP only repairs gaps/reconnects and
            // reconciles once a minute; disconnected clients use the old polling.
            if (!fastConnected) return false;
            double seconds = (DateTime.UtcNow - lastFastRecovery).TotalSeconds;
            if ((fastNeedsRecovery && seconds >= 3) || seconds >= 60)
            {
                lastFastRecovery = DateTime.UtcNow;
                fastNeedsRecovery = false;
                await GetFromURLNext();
            }
            return true;
        }
    }
}
