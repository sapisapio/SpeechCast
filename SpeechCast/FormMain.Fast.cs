// jpnkn Fast integration and board-wide feed, 2026-10-19. GPLv2.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        // jpnkn only, on the status bar: shows the Fast state and picks how replies arrive (remembered in the settings).
        private ToolStripDropDownButton fastModeButton;
        private ToolStripMenuItem fastThreadItem, fastBoardItem, httpItem;
        private string fastState; // "Fast 受信中" etc. while a Fast session exists
        public const string FastThreadMode = "fast", FastBoardMode = "board", HttpMode = "http";
        private JpnknFastTarget fastTarget;
        private JpnknFastClient fastClient;
        private int fastGeneration;
        private bool fastConnected, syncingFeedMode, loadingThread;
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
            fastModeButton = new ToolStripDropDownButton { Name = "fastModeButton", DropDownDirection = ToolStripDropDownDirection.AboveRight,
                DisplayStyle = ToolStripItemDisplayStyle.Text, Available = false, Margin = new Padding(0, 1, 14, 1),
                ToolTipText = "jpnkn の新着の受け取り方" };
            fastThreadItem = new Controls.DescribedMenuItem("Fastスレッド（標準）", "jpnkn の Fast 配信で、このスレの新着をすぐに受け取ります。", delegate { SetFeedMode(FastThreadMode); });
            fastBoardItem = new Controls.DescribedMenuItem("Fast板全部", "板の全スレの新着を届いた順に流します。過去のレスは出ず、次スレへの移動も要りません。", delegate { SetFeedMode(FastBoardMode); });
            httpItem = new Controls.DescribedMenuItem("従来の方式", "設定した間隔でスレを読み直して、新着を確かめます。", delegate { SetFeedMode(HttpMode); });
            fastModeButton.DropDownItems.AddRange(new ToolStripItem[] { fastThreadItem, fastBoardItem, httpItem });
            // The menu sizes itself to the titles; make room for the explanations underneath.
            fastModeButton.DropDown.Opening += delegate {
                int width = 0;
                foreach (ToolStripItem item in fastModeButton.DropDownItems) width = Math.Max(width, item.GetPreferredSize(Size.Empty).Width);
                fastModeButton.DropDown.MinimumSize = new Size(width + 8, 0);
            };
            statusStrip1.Items.Insert(1, fastModeButton);
            toolStripTextBoxURL.TextChanged += delegate { UpdateFastButton(); };
        }

        internal string FeedMode
        {
            get { return UserConfig == null ? FastThreadMode : UserConfig.JpnknBoardMode ? FastBoardMode : UserConfig.JpnknHttpOnly ? HttpMode : FastThreadMode; }
        }

        // Takes effect at once: thread <-> board reloads; Fast <-> 従来 keeps the replies and
        // only changes how new ones arrive.
        internal void SetFeedMode(string mode)
        {
            if (UserConfig == null || mode == FeedMode) return;
            bool boardChanged = (mode == FastBoardMode) != UserConfig.JpnknBoardMode;
            UserConfig.JpnknBoardMode = mode == FastBoardMode;
            if (mode != FastBoardMode) UserConfig.JpnknHttpOnly = mode == HttpMode;
            UpdateFeedModeUi();
            if (IsDesignPreview) return;
            if (boardChanged)
            {
                StopSpeakingCore(); AutoUpdate = false; ClearFastSession();
                if (toolStripTextBoxURL.Text.Trim().Length > 0) GetFromURL();
                return;
            }
            if (mode == HttpMode)
            {
                StopFastClient();
                fastTarget = null;
                fastPending.Clear();
            }
            else
            {
                JpnknFastTarget target;
                if (responses.Count > 0 && JpnknFastTarget.TryParse(toolStripTextBoxURL.Text, false, out target))
                {
                    fastTarget = target;
                    fastNeedsRecovery = true; // catch up over HTTP once connected
                    EnsureFastSubscription();
                }
            }
            UpdateFastButton();
        }

        private void UpdateFeedModeUi()
        {
            if (UserConfig == null) return;
            toolStripLabel7.Available = toolStripTextBoxThreadKeyword.Available = toolStripButtonAutoNextThread.Available = !UserConfig.JpnknBoardMode;
            // Board feeds have no single destination thread for the old writer.
            toolStripButtonResponse.Enabled = toolStripButtonThreads.Enabled = !IsDesignPreview && !UserConfig.JpnknBoardMode;
            toolStripButtonResponse.ToolTipText = UserConfig.JpnknBoardMode ? "投稿はコメントの元スレへのリンクから行ってください" : "レスを書く";
            toolStripTextBoxURL.ToolTipText = UserConfig.JpnknBoardMode
                ? "jpnknの掲示板URLまたは板ID（Enterで板全部の新着受信を開始）"
                : "スレッドURL（Enterで読み込み）";
            myToolStripUrl.PerformLayout();
            UpdateFastButton();
        }

        // Shown only for jpnkn: the session state while one runs, otherwise the chosen mode.
        private void UpdateFastButton()
        {
            if (fastModeButton == null || UserConfig == null) return;
            string mode = FeedMode;
            fastThreadItem.Checked = mode == FastThreadMode;
            fastBoardItem.Checked = mode == FastBoardMode;
            httpItem.Checked = mode == HttpMode;
            fastModeButton.Available = fastTarget != null || UserConfig.JpnknBoardMode || IsJpnknInput(toolStripTextBoxURL.Text);
            fastModeButton.Text = fastTarget != null && fastState != null ? fastState
                : mode == HttpMode ? "従来の方式" : mode == FastBoardMode ? "Fast板全部" : "Fastスレッド";
        }

        private void SetFastState(string state, string detail)
        {
            fastState = state;
            if (fastModeButton != null) fastModeButton.ToolTipText = detail ?? "jpnkn の新着の受け取り方";
            UpdateFastButton();
        }

        private void StopFastClient()
        {
            fastGeneration++;
            if (fastClient != null) fastClient.Dispose();
            fastClient = null;
            fastConnected = false;
            SetFastState(null, null);
        }

        private void ClearFastSession()
        {
            StopFastClient();
            fastTarget = null;
            fastPending.Clear(); fastSeen.Clear(); fastSeenOrder.Clear();
            fastExpectedNumber = 0;
            fastNeedsRecovery = false;
            // A new thread or board starts without a highlighted reply.
            lastSpokenNumber = 0;
            if (webBrowser.Document != null) webBrowser.Document.InvokeScript("setCurrent", new object[] { 0, false });
            UpdateFastButton();
        }

        private void OnFastAcquisitionChanged(bool enabled)
        {
            if (fastTarget == null) return;
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
            SetFastState("Fast 接続中…", null);
            fastClient.ConnectionChanged += (connected, message) => PostFastEvent(generation, delegate {
                fastConnected = connected;
                SetFastState(connected ? "Fast 受信中" : "Fast 再接続待ち", message);
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
            if (UserConfig != null && UserConfig.JpnknHttpOnly) return; // 従来の方式: HTTP polling only
            JpnknFastTarget target;
            if (JpnknFastTarget.TryParse(toolStripTextBoxURL.Text, false, out target)) fastTarget = target;
            UpdateFastButton();
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
            threadTitle = target.Board + " — Fast板全部";
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
            lastSpokenNumber = Math.Max(0, lastSpokenNumber - count);
            HighlightSpoken(false);
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
