// Compile against SpeechCastSP.exe and run from the same directory. GPLv2.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpeechCastMe;

class UiRegressionTests
{
    static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        Console.WriteLine("PASS " + description);
    }
    [STAThread]
    static void Main()
    {
        var type = typeof(FormMain).Assembly.GetType("SpeechCastMe.Response");
        type.GetField("Style").SetValue(null, Enum.Parse(type.GetNestedType("BBSStyle"), "jbbs"));
        object response = Activator.CreateInstance(type, true);
        type.GetMethod("SetRawText").Invoke(response, new object[] { "720<>名前<><>2026/10/18(日) 12:34:56.789<>一行目<br>二行目 https://example.com/path?q=1&amp;x=2 &gt;&gt;719<>title<>id" });
        string html = (string)type.GetField("Html").GetValue(response);
        Check(html.Contains("name=\"res720\""), "response anchor retained");
        Check(html.Contains("720:"), "response number retained");
        Check(html.Contains("一行目\n二行目"), "line breaks retained");
        Check(html.Contains("href=\"https://example.com/path?q=1&amp;x=2\""), "HTTPS and query escaping retained");
        Check(html.Contains("href=\"http://res/719\""), "reply link retained");
        Check(html.Contains("2026/10/18(日)</span>") && html.Contains(">12:34</span>") && html.Contains("ID:id<span class=\"count\">"), "date time and ID emitted separately");
        string sample = html;
        type.GetMethod("SetRawText").Invoke(response, new object[] { "721<><><>today<>安全<img src=x onerror=alert(1)>テキスト<>title<>id" });
        html = (string)type.GetField("Html").GetValue(response);
        Check(!html.Contains("<img"), "board markup cannot inject an image handler");

        var config = new UserConfig();
        config.Initialize();
        config.CommentLayout = "plain";
        config.CommentTheme = "dark";
        config.CommentBackground = "#123456";
        config.CommentFontSize = 22;
        config.CommentShowTime = true;
        config.CommentShowID = true;
        var serializer = new XmlSerializer(typeof(UserConfig));
        using (var writer = new StringWriter())
        {
            serializer.Serialize(writer, config);
            var restored = (UserConfig)serializer.Deserialize(new StringReader(writer.ToString()));
            Check(restored.CommentLayout == "plain" && restored.CommentTheme == "dark" && restored.CommentBackground == "#123456", "appearance preferences round-trip");
            Check(restored.CommentFontSize == 22 && restored.CommentShowTime && restored.CommentShowID && !restored.CommentShowDate, "independent fields and font size round-trip");
        }
        var old = (UserConfig)serializer.Deserialize(new StringReader("<UserConfig />"));
        Check(old.CommentLayout == "compact" && old.CommentTheme == "light", "older settings receive defaults");
        var legacyAudio = (UserConfig)serializer.Deserialize(new StringReader("<UserConfig><SpeakMode>true</SpeakMode><PlaySoundNewResponse>true</PlaySoundNewResponse><NavigationSound>true</NavigationSound></UserConfig>"));
        legacyAudio.SupplementSettings();
        Check(legacyAudio.AudioMode == AudioNotificationMode.Speech && !legacyAudio.PlaySoundNewResponse && !legacyAudio.NavigationSound, "legacy simultaneous audio settings migrate to speech and suppress browser clicks");
        foreach (AudioNotificationMode mode in Enum.GetValues(typeof(AudioNotificationMode)))
        {
            config.AudioMode = mode;
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, config);
                var restored = (UserConfig)serializer.Deserialize(new StringReader(writer.ToString()));
                Check(restored.AudioMode == mode && !(restored.SpeakMode && restored.PlaySoundNewResponse), "exclusive audio mode persists: " + mode);
            }
        }
        FormMain.UserConfig = config;
        config.CaptionFont.Name = "Yu Gothic UI";
        using (var bitmap = new Bitmap(640, 140, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            FormCaption.DrawCaption(graphics, new Rectangle(10, 10, 620, 120), "なめらかな文字 ABC 720", config.CaptionFont, false);
            int partial = 0, opaque = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int alpha = bitmap.GetPixel(x, y).A;
                    if (alpha > 0 && alpha < 255) partial++;
                    if (alpha == 255) opaque++;
                }
            Check(partial > 100 && opaque > 100, "caption has antialiased alpha edges and opaque text");
            Check(bitmap.GetPixel(639,139).A == 0, "caption background remains transparent");
        }
        BundledFontChecks(config);
        using (var caption = new FormCaption())
        using (var bitmap = new Bitmap(680, 170, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            FormCaption.Instance = caption;
            caption.Bounds = new Rectangle(-3000, -3000, 680, 170);
            config.HideCaptionTitle = false;
            caption.setTitle("日本語のスレッド名 — 描・田・四角");
            caption.CaptionText = "描画・田・四角の輪郭 ABC 720";
            caption.Show();
            caption.Refresh();
            var success = typeof(FormCaption).GetProperty("LastAlphaUpdateSucceeded", BindingFlags.NonPublic | BindingFlags.Instance);
            Check((bool)success.GetValue(caption, null), "caption alpha frame is accepted by Windows");
            typeof(FormCaption).GetMethod("PaintCaptionContent", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(caption, new object[] { caption, new PaintEventArgs(graphics, new Rectangle(0,0,680,170)) });
            var header = caption.drawRect;
            Check(header.Top == 3 && header.Height <= 22 && header.Right <= caption.ClientSize.Width, "compact caption title has bounded width and padding");
            caption.setTitle(null);
            Check((string)typeof(FormCaption).GetField("titleText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(caption) == "SpeechcastSP", "unloaded caption uses the application name");
            caption.setTitle("日本語のスレッド名 [5/10]");
            Check((string)typeof(FormCaption).GetField("titleText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(caption) == "日本語のスレッド名 [5/10]", "loaded caption contains only thread name and counts");
            graphics.Clear(Color.Transparent);
            typeof(FormCaption).GetMethod("PaintCaptionContent", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(caption, new object[] { caption, new PaintEventArgs(graphics, new Rectangle(0,0,680,170)) });
            bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "caption-sp-render.png"), ImageFormat.Png);
            caption.Close();
        }
        BrowserChecks(sample);
        FastParsingChecks();
        ToolbarChecks();
        SoundChecks();
        SpeechChecks();
    }

    static T Pump<T>(Task<T> task, int milliseconds = 15000)
    {
        var watch = Stopwatch.StartNew();
        while (!task.IsCompleted && watch.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(5); }
        if (!task.IsCompleted) throw new Exception("timed out");
        return task.Result;
    }

    static bool SpeakAndWait(SpeechEngine engine, string text, int milliseconds = 15000)
    {
        bool done = false, cancelled = false;
        EventHandler<SpeechCompletedArgs> handler = (s, e) => { done = true; cancelled = e.Cancelled; };
        engine.SpeakCompleted += handler;
        engine.SpeakAsync(text);
        var watch = Stopwatch.StartNew();
        while (!done && watch.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(5); }
        engine.SpeakCompleted -= handler;
        return done && !cancelled;
    }

    // Minimal VOICEVOX-compatible engine: /speakers, /audio_query, /synthesis.
    static void ServeFakeVoicevox(TcpListener listener, List<string> requests)
    {
        byte[] wav = File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", "Windows Ding.wav"));
        try
        {
            while (true)
            {
                using (var client = listener.AcceptTcpClient())
                using (var stream = client.GetStream())
                {
                    var head = new List<byte>();
                    while (head.Count < 4 || Encoding.ASCII.GetString(head.Skip(head.Count - 4).ToArray()) != "\r\n\r\n") { int b = stream.ReadByte(); if (b < 0) break; head.Add((byte)b); }
                    string header = Encoding.UTF8.GetString(head.ToArray());
                    int length = 0;
                    foreach (string line in header.Split(new[] { "\r\n" }, StringSplitOptions.None))
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) length = int.Parse(line.Substring(15).Trim());
                    var body = new byte[length];
                    for (int read = 0; read < length; ) read += stream.Read(body, read, length - read);
                    string path = header.Split(' ')[1];
                    lock (requests) requests.Add(path + " " + Encoding.UTF8.GetString(body));
                    byte[] content; string type = "application/json; charset=utf-8";
                    if (path.StartsWith("/speakers")) content = Encoding.UTF8.GetBytes("[{\"name\":\"ずんだもん\",\"speaker_uuid\":\"z\",\"styles\":[{\"name\":\"ノーマル\",\"id\":3},{\"name\":\"ハミング\",\"id\":3001,\"type\":\"sing\"}]}]");
                    else if (path.StartsWith("/audio_query")) content = Encoding.UTF8.GetBytes("{\"accent_phrases\":[],\"speedScale\":1.0,\"pitchScale\":0.0,\"volumeScale\":1.0,\"outputSamplingRate\":24000}");
                    else { content = wav; type = "audio/wav"; }
                    byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: " + type + "\r\nContent-Length: " + content.Length + "\r\nConnection: close\r\n\r\n");
                    stream.Write(response, 0, response.Length);
                    stream.Write(content, 0, content.Length);
                }
            }
        }
        catch (Exception) { } // listener stopped
    }

    static void SpeechChecks()
    {
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        using (var engine = new SpeechEngine())
        {
            var names = engine.VoiceNames;
            Check(names.Contains("Microsoft Haruka Desktop") && names.Contains("Microsoft Ayumi") && names.Contains("Microsoft Ichiro") && !names.Contains("Microsoft Haruka"), "Windows built-in voices are listed with SAPI voices without a duplicate Haruka");
            Check(names.IndexOf("Microsoft Haruka Desktop") < names.IndexOf("Microsoft Ayumi"), "SAPI voices stay first so Haruka remains the default");
            engine.Volume = 0;
            Check(engine.SelectVoice("Microsoft Ayumi") && SpeakAndWait(engine, "テストです"), "Windows built-in voice speaks and completes");
            Check(engine.SelectVoice("Microsoft Haruka Desktop") && SpeakAndWait(engine, "テストです"), "SAPI voice still speaks and completes");
        }
        var saved = SpeechEngine.EngineEndpoints.ToList();
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var requests = new List<string>();
        new Thread(() => ServeFakeVoicevox(listener, requests)) { IsBackground = true }.Start();
        SpeechEngine.EngineEndpoints.Clear();
        SpeechEngine.EngineEndpoints.Add(new KeyValuePair<string, string>("VOICEVOX", "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port));
        try
        {
            using (var engine = new SpeechEngine())
            {
                string zundamon = "VOICEVOX:ずんだもん（ノーマル）";
                Check(Pump(engine.RefreshEngineVoicesAsync()) && engine.VoiceNames.Contains(zundamon) && !engine.VoiceNames.Any(n => n.Contains("ハミング")), "running VOICEVOX voices are discovered with talk styles only");
                Check(!Pump(engine.RefreshEngineVoicesAsync()), "unchanged VOICEVOX voice list reports no change");
                engine.Volume = 0; engine.Rate = 10;
                int progress = 0;
                engine.SpeakProgress += (s, e) => progress = Math.Max(progress, e.CharacterPosition);
                Check(engine.SelectVoice(zundamon) && SpeakAndWait(engine, "ずんだもんなのだ") && progress == 8, "VOICEVOX voice synthesizes plays and advances the caption");
                string synthesis;
                lock (requests) synthesis = requests.Last(r => r.StartsWith("/synthesis"));
                Check(synthesis.StartsWith("/synthesis?speaker=3 ") && synthesis.Contains("\"speedScale\":2"), "speaking rate is sent as VOICEVOX speed");
                int completions = 0; bool cancelled = false;
                engine.SpeakCompleted += (s, e) => { completions++; cancelled = e.Cancelled; };
                engine.SpeakAsync("止めるのだ");
                engine.SpeakAsyncCancelAll();
                for (int i = 0; i < 100; i++) { Application.DoEvents(); Thread.Sleep(5); }
                Check(completions == 1 && cancelled && engine.State == System.Speech.Synthesis.SynthesizerState.Ready, "cancelling a VOICEVOX request completes once as cancelled");
                listener.Stop();
                string failure = null;
                engine.Failed += message => failure = message;
                Check(SpeakAndWait(engine, "落ちたのだ") && failure != null && failure.Contains("Windowsの音声"), "VOICEVOX failure falls back to the Windows voice");
            }
        }
        finally
        {
            listener.Stop();
            SpeechEngine.EngineEndpoints.Clear();
            SpeechEngine.EngineEndpoints.AddRange(saved);
        }
    }

    static object Script(WebBrowser browser, string name, params object[] args) { return browser.Document.InvokeScript(name, args); }

    static object Field(object owner, string name) { return owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner); }

    static void ToolbarChecks()
    {
        Environment.SetEnvironmentVariable("SPEECHCAST_UI_PREVIEW", "1");
        using (var form = new FormMain())
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-3000,-3000);
            form.Show();
            var browser = (WebBrowser)Field(form, "webBrowser");
            var watch = Stopwatch.StartNew();
            while (((System.Collections.IList)Field(form,"responses")).Count == 0 && watch.ElapsedMilliseconds < 15000) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
            Check(form.AutoUpdate && Convert.ToInt32(Script(browser,"scrollTop")) > 0, "successful initial load enables updates and jumps to latest");
            form.AutoUpdate = false;
            Script(browser,"scrollToY",0);
            form.AutoUpdate = true;
            Check(Convert.ToInt32(Script(browser,"scrollTop")) > 0, "enabling updates immediately scrolls to latest");
            var play = (ToolStrip)Field(form,"myToolStripPlay");
            var voice = (ToolStrip)Field(form,"myToolStripVoice");
            var url = (ToolStrip)Field(form,"myToolStripUrl");
            Check(((ToolStripItem)Field(form,"toolStripButtonZoomInResView")).Alignment == ToolStripItemAlignment.Right && ((ToolStripItem)Field(form,"toolStripButtonZoomInResView")).Bounds.Right >= play.ClientSize.Width - 16 && ((ToolStripItem)Field(form,"toolStripButtonZoomOutnResView")).Bounds.Left < ((ToolStripItem)Field(form,"toolStripButtonZoomInResView")).Bounds.Left, "comment zoom is anchored to the actual right edge");
            Check(play.Visible && voice.Visible && url.Visible, "voice play and URL rows are visible by default");
            Check(((ToolStripItem)Field(form,"toolStripComboBoxSelectVoice")).Owner == voice && ((ToolStripItem)Field(form,"toolStripTrackBarVoiceVolume")).Owner == voice, "voice selector and volume remain on voice row");
            Check(((ToolStripItem)Field(form,"toolStripButtonAutoUpdate")).Owner == play && ((ToolStripItem)Field(form,"toolStripButtonMoveBottom")).Owner == play, "update and viewer controls share play row");
            var autoUpdate = (ToolStripButton)Field(form,"toolStripButtonAutoUpdate");
            var autoScroll = (ToolStripButton)Field(form,"toolStripButtonAutoScroll");
            Func<string, int> index = name => play.Items.IndexOf((ToolStripItem)Field(form,name));
            Check(index("toolStripButtonThreads") == 0 && index("toolStripButtonResponse") == 1 && play.Items[2] is ToolStripSeparator, "thread list and write stay first, then a separator");
            Check(play.Items.IndexOf(autoUpdate) == 3 && play.Items.IndexOf(autoScroll) == 4 && index("toolStripButtonMoveBottom") == 5 && play.Items[6] is ToolStripSeparator && index("toolStripButtonFirst") == 7 && autoScroll.Text == "自動送り" && autoScroll.Enabled, "auto update, auto-advance and jump to latest come before playback");
            Func<string, int> left = name => ((ToolStripItem)Field(form,name)).Bounds.Left;
            Check(left("toolStripButtonTurbo") < left("toolStripButtonZoomOutnResView"), "playback controls come before comment zoom");
            bool advancing = FormMain.UserConfig.AutoAdvance;
            autoScroll.PerformClick();
            Check(FormMain.UserConfig.AutoAdvance != advancing && autoScroll.Checked == FormMain.UserConfig.AutoAdvance, "auto-advance button toggles the setting");
            autoScroll.PerformClick();
            ReadingChecks(form, browser);
            StreamChecks(form);
            OpenBoardChecks(form);
            TelopChecks(form);
            foreach (var name in new[] { "toolStripButtonCaption", "toolStripButtonShowCaptionImmediately", "toolStripButtonBorder", "toolStripButtonOpenResCaptionForm", "toolStripButtonFirst", "toolStripButtonMoveBottom" })
            {
                var button = (ToolStripButton)typeof(FormMain).GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(form);
                Check(button.Image != null && button.DisplayStyle == ToolStripItemDisplayStyle.Image && button.ToolTipText.Length > 1, "drawn icon with a tooltip: " + name);
            }
            Check(voice.Renderer is ReaderToolbarRenderer && play.Renderer is ReaderToolbarRenderer, "toolbars use the reader renderer");
            using (var strip = new ToolStrip { Renderer = new ReaderToolbarRenderer(), BackColor = Color.White, Size = new Size(80, 30) })
            using (var bitmap = new Bitmap(80, 30))
            {
                var on = new ToolStripButton("ON") { Checked = true, AutoSize = false, Size = new Size(40, 26) };
                strip.Items.Add(on);
                strip.PerformLayout();
                strip.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));
                Color fill = bitmap.GetPixel(on.Bounds.Left + 4, on.Bounds.Top + on.Bounds.Height / 2);
                Check(fill.ToArgb() == ReaderToolbarRenderer.CheckedFill.ToArgb(), "checked buttons are filled pale yellow");
            }
            Check(url.Items.Count == 5 && url.Items[0] == Field(form,"feedModeSelector") && url.Items[1] == Field(form,"toolStripTextBoxURL") && ((ToolStripItem)Field(form,"toolStripTextBoxThreadKeyword")).Owner == url && ((ToolStripItem)Field(form,"toolStripButtonAutoNextThread")).Owner == url, "URL row contains source mode URL keyword and toggle");
            Check(autoUpdate.Text == "自動更新" && autoUpdate.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText && ((ToolStripButton)Field(form,"toolStripButtonTurbo")).Text == "ターボ", "auto update is labelled and turbo keeps its label");
            var modes = (ToolStripComboBox)Field(form,"audioModeSelector");
            Check(modes.Owner == voice && modes.Items.Count == 3 && ((ToolStripItem)Field(form,"toolStripButtonClickSound")).Owner == null, "audio selector replaces independent toggles and removes reload sound");
            modes.SelectedIndex = (int)AudioNotificationMode.Sound;
            Check(FormMain.UserConfig.PlaySoundNewResponse && !FormMain.UserConfig.SpeakMode && !((ToolStripItem)Field(form,"toolStripTrackBarVoiceVolume")).Enabled && ((ToolStripItem)Field(form,"toolStripTrackBarPlayVolume")).Enabled, "sound mode enables only notification volume");
            typeof(FormMain).GetProperty("communicationStatusString", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(form,"polling countdown",null);
            Check(((ToolStripStatusLabel)Field(form,"toolStripStatusLabelCommunication")).Text.Contains("登録"), "missing sound configuration message remains visible through polling status updates");
            // Exercise the actual acquisition notification entry point with missing
            // files so mode and history guards are checked without network/audio.
            var previewFlag = typeof(FormMain).GetField("IsDesignPreview", BindingFlags.NonPublic | BindingFlags.Instance);
            var notify = typeof(FormMain).GetMethod("NotifyNewResponses", BindingFlags.NonPublic | BindingFlags.Instance);
            var status = (ToolStripStatusLabel)Field(form,"toolStripStatusLabelCommunication");
            previewFlag.SetValue(form,false);
            try
            {
                status.Text = "sentinel";
                notify.Invoke(form,new object[] {false,0,60});
                notify.Invoke(form,new object[] {true,60,60});
                Check(status.Text == "sentinel", "initial history and unchanged updates never trigger notification");
                notify.Invoke(form,new object[] {true,60,61});
                Check(status.Text.Contains("有効な音声"), "new reply acquisition reaches sound notification independently of reading position");
            }
            finally { previewFlag.SetValue(form,true); }
            modes.SelectedIndex = (int)AudioNotificationMode.Speech;
            Check(FormMain.UserConfig.SpeakMode && !FormMain.UserConfig.PlaySoundNewResponse, "switching to speech disables notification sound");
            modes.SelectedIndex = (int)AudioNotificationMode.Silent;
            Check(!FormMain.UserConfig.SpeakMode && !FormMain.UserConfig.PlaySoundNewResponse, "silent mode disables both outputs");
            using (var settings = new FormSettings())
            {
                settings.SetUserConfig(FormMain.UserConfig);
                ((ComboBox)Field(settings,"audioModeSelector")).SelectedIndex = (int)AudioNotificationMode.Sound;
                settings.GetUserConfig(FormMain.UserConfig);
                Check(FormMain.UserConfig.AudioMode == AudioNotificationMode.Sound && !((Control)Field(settings,"checkBoxPlaySoundSync")).Visible, "settings use the same exclusive modes without speech synchronization");
            }
            form.Width = 240;
            Application.DoEvents();
            Check(form.Width == 240, "reorganized toolbar still permits narrow window");
            BoardFeedChecks(form,browser);
            form.Close();
        }
        Environment.SetEnvironmentVariable("SPEECHCAST_UI_PREVIEW", null);
    }

    static void BundledFontChecks(UserConfig config)
    {
        var bundledType = typeof(FormMain).Assembly.GetType("SpeechCastMe.BundledFont");
        var family = (FontFamily)bundledType.GetProperty("Family", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
        string familyName = (string)bundledType.GetField("FamilyName", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        Check(family != null && family.GetName(0x0409) == familyName, "bundled caption font loads from the executable");
        var fresh = new UserConfig();
        fresh.Initialize();
        Check(fresh.CaptionFont.Name == familyName && !fresh.CaptionFont.Bold && fresh.AACaptionFont.Name == "MS UI Gothic", "new settings default to the bundled font and keep the AA font");
        Check(fresh.CaptionFont.Colors.HighForeColor.ToArgb() == Color.White.ToArgb() && fresh.CaptionFont.Colors.LowForeColor.ToArgb() == Color.White.ToArgb() && fresh.CaptionFont.Colors.BorderColor.ToArgb() == Color.Black.ToArgb(), "new settings default to white text with a black border");
        using (var font = fresh.CaptionFont.CreateFont())
            Check(font.FontFamily.Equals(family), "caption uses the bundled family, not an installed copy");
        fresh.CaptionFont.Bold = true;
        using (var font = fresh.CaptionFont.CreateFont())
            Check(font.FontFamily.Equals(family), "bold request on the bundled font does not throw");
        fresh.CaptionFont.Bold = false;
        var previous = FormMain.UserConfig;
        FormMain.UserConfig = fresh;
        using (var bitmap = new Bitmap(640, 140, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            FormCaption.DrawCaption(graphics, new Rectangle(10, 10, 620, 120), "内蔵フォント 描画・田 ABC 720", fresh.CaptionFont, false);
            int white = 0, black = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color c = bitmap.GetPixel(x, y);
                    if (c.A == 255 && c.R > 240 && c.G > 240 && c.B > 240) white++;
                    if (c.A == 255 && c.R < 16 && c.G < 16 && c.B < 16) black++;
                }
            Check(white > 500 && black > 500, "default caption renders white text with a black border");
            bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "caption-bundled-render.png"), ImageFormat.Png);
        }
        FormMain.UserConfig = previous;
    }

    static JpnknFastMessage FastMessage(string thread, int number, string board = "sapio")
    {
        JpnknFastMessage message;
        string json = "{\"bbsid\":\"" + board + "\",\"threadkey\":\"" + thread + "\",\"no\":\"" + number + "\",\"body\":\"名無し<>sage<>2026/10/19(月) 12:34:56 ID:abc<>本文 &gt;&gt;49 <img src=x><>\"}";
        if (!JpnknFastMessage.TryParse("bbs/" + board,json,out message)) throw new Exception("Fast fixture failed");
        return message;
    }

    static void FastParsingChecks()
    {
        JpnknFastTarget target;
        Check(JpnknFastTarget.TryParse("sapio",true,out target) && target.Topic == "bbs/sapio" && target.Thread == null, "board ID alone selects a complete board subscription");
        Check(JpnknFastTarget.TryParse("https://bbs.jpnkn.com/sapio/",true,out target) && target.Board == "sapio", "board URL accepted without thread selection");
        Check(JpnknFastTarget.TryParse("https://bbs.jpnkn.com/test/read.cgi/sapio/1234567890/",true,out target) && target.Thread == null, "board mode ignores a pasted thread path and subscribes to its board");
        Check(JpnknFastTarget.TryParse("https://bbs.jpnkn.com/test/read.cgi/sapio/1234567890/",false,out target) && target.Thread == "1234567890", "normal mode retains the chosen thread filter");
        Check(!JpnknFastTarget.TryParse("https://bbs.jpnkn.com.evil.test/sapio/",true,out target) && !JpnknFastTarget.TryParse("bbs/+",true,out target) && !JpnknFastTarget.TryParse("#",true,out target), "unrelated hosts and broad MQTT wildcards rejected");
        JpnknFastMessage message;
        Check(!JpnknFastMessage.TryParse("bbs/other", "{\"bbsid\":\"sapio\",\"threadkey\":\"100\",\"no\":1,\"body\":\"a<>b<>c<>d<>\"}",out message), "payload board must match the subscribed topic");
        Check(!JpnknFastMessage.TryParse("bbs/sapio","{broken",out message) && !JpnknFastMessage.TryParse("bbs/sapio","{}",out message), "malformed and incomplete Fast payloads ignored");
        var config = new UserConfig { JpnknBoardMode = true };
        var serializer = new XmlSerializer(typeof(UserConfig));
        using (var writer = new StringWriter())
        {
            serializer.Serialize(writer,config);
            Check(((UserConfig)serializer.Deserialize(new StringReader(writer.ToString()))).JpnknBoardMode, "board-wide source mode persists in settings");
        }
    }

    static void BoardFeedChecks(FormMain form, WebBrowser browser)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var modes = (ToolStripComboBox)Field(form,"feedModeSelector");
        modes.SelectedIndex = 1;
        Check(FormMain.UserConfig.JpnknBoardMode && !((ToolStripItem)Field(form,"toolStripTextBoxThreadKeyword")).Available, "board mode hides next-thread controls and needs only a board input");
        JpnknFastTarget target; JpnknFastTarget.TryParse("sapio",true,out target);
        typeof(FormMain).GetMethod("BeginBoardFeed",flags).Invoke(form,new object[] {target});
        var replies = (System.Collections.IList)Field(form,"responses");
        var apply = typeof(FormMain).GetMethod("ApplyFastMessage",flags);
        Check(replies.Count == 0 && form.CurrentResNumber == 1 && form.AutoUpdate, "board feed starts empty with acquisition enabled");
        apply.Invoke(form,new object[] {FastMessage("100",50)});
        apply.Invoke(form,new object[] {FastMessage("200",50)});
        apply.Invoke(form,new object[] {FastMessage("100",50)});
        Check(replies.Count == 2 && form.CurrentResNumber == 1, "different threads append independently of playback cursor and duplicates are excluded");
        var type = replies[0].GetType();
        string html = (string)type.GetField("Html").GetValue(replies[0]);
        Check((int)type.GetField("Number").GetValue(replies[0]) == 1 && (int)type.GetProperty("DisplayNumber").GetValue(replies[0],null) == 50 && html.Contains("50:</span>"), "original response numbers are separate from playback queue positions");
        Check(html.Contains("スレ 100") && html.Contains("https://bbs.jpnkn.com/test/read.cgi/sapio/100/49") && !html.Contains("<img"), "board rows identify source threads preserve reply links and escape markup");
        for (int i = 1; i <= 40; i++) apply.Invoke(form,new object[] {FastMessage("300",i)});
        Script(browser,"scrollToY",300);
        int position = Convert.ToInt32(Script(browser,"scrollTop"));
        apply.Invoke(form,new object[] {FastMessage("300",41)});
        Check(Convert.ToInt32(Script(browser,"scrollTop")) == position && form.CurrentResNumber == 1, "board arrivals do not force manual scroll or playback position");
        form.StopSpeaking();
        int before = replies.Count;
        apply.Invoke(form,new object[] {FastMessage("400",1)});
        Check(form.AutoUpdate && replies.Count == before + 1 && !FormMain.UserConfig.AutoAdvance, "stop turns auto-advance off while board-wide acquisition keeps running");
        form.SetAutoAdvance(true);
        Check((int)typeof(FormMain).GetProperty("CurrentResponseLimit",flags).GetValue(form,null) == int.MaxValue, "board feed does not stop at a single-thread 1000-response boundary");
        form.AutoUpdate = false;
        before = replies.Count;
        apply.Invoke(form,new object[] {FastMessage("400",2)});
        Check(replies.Count == before, "turning acquisition off stops new board arrivals");
        form.AutoUpdate = true;
        before = replies.Count;
        JpnknFastMessage shortBody;
        Check(JpnknFastMessage.TryParse("bbs/sapio","{\"bbsid\":\"sapio\",\"threadkey\":\"500\",\"no\":1,\"body\":\"名無し<>sage<>2026/10/19 ID:a<>末尾区切りなし\"}",out shortBody), "Fast body without a trailing field parses");
        apply.Invoke(form,new object[] {shortBody});
        Check(replies.Count == before + 1, "Fast body without a trailing field is displayed");
        BoardTrimChecks(form,browser,replies,apply);
        typeof(FormMain).GetMethod("ClearFastSession",flags).Invoke(form,null);
        JpnknFastTarget.TryParse("https://bbs.jpnkn.com/test/read.cgi/sapio/100/",false,out target);
        typeof(FormMain).GetField("fastTarget",flags).SetValue(form,target);
        replies.Clear();
        apply.Invoke(form,new object[] {FastMessage("200",1)});
        apply.Invoke(form,new object[] {FastMessage("100",1)});
        apply.Invoke(form,new object[] {FastMessage("100",3)});
        Check(replies.Count == 1 && (bool)Field(form,"fastNeedsRecovery"), "thread mode filters other threads and requests recovery on a missing response");
        apply.Invoke(form,new object[] {FastMessage("100",2)});
        apply.Invoke(form,new object[] {FastMessage("100",1)});
        Check(replies.Count == 2, "thread mode appends sequential replies once without duplicate replay");
        form.StopSpeaking();
        apply.Invoke(form,new object[] {FastMessage("100",3)});
        Check(form.AutoUpdate && !FormMain.UserConfig.AutoAdvance && replies.Count == 3, "stop in thread mode turns auto-advance off but keeps auto update on");
        form.SetAutoAdvance(true);
        form.AutoUpdate = false; form.AutoUpdate = true;
        FastSnapshotChecks(form,browser,replies,apply);
    }

    static void TelopChecks(FormMain form)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        Func<string, object> get = name => typeof(FormMain).GetField(name, flags).GetValue(form);
        var split = (SplitContainer)get("splitContainerResCaption");
        var inputButton = (ToolStripButton)get("toolStripButtonOpenResCaptionForm");
        var toggleInput = typeof(FormMain).GetMethod("ToggleTelopInput", flags);
        split.Panel2.Enabled = true;
        toggleInput.Invoke(form, null);
        Application.DoEvents();
        Check(!split.Panel2Collapsed && split.Panel2.Height >= 20 && inputButton.Checked, "free telop input opens on the first click");
        toggleInput.Invoke(form, null);
        Check(split.Panel2Collapsed && !inputButton.Checked, "free telop input closes on the next click");

        var textBox = (TextBox)get("textBoxDefaultCaption");
        textBox.Text = "配信中です";
        Check(FormMain.UserConfig.CaptionTextBuffer == "配信中です", "free telop text is kept in the settings");
        form.AutoUpdate = false; form.AutoUpdate = true;
        Check(form.CaptionTextBuffer == "", "the main caption no longer shows the free telop text between replies");

        var render = form.GetType().Assembly.GetType("SpeechCastMe.FormFreeTelop").GetMethod("RenderFrame", BindingFlags.NonPublic | BindingFlags.Static);
        using (var frame = (Bitmap)render.Invoke(null, new object[] { "配信中です", new Size(400, 120) }))
        using (var empty = (Bitmap)render.Invoke(null, new object[] { "", new Size(400, 120) }))
        {
            int white = 0, black = 0, visible = 0;
            for (int y = 0; y < 120; y++) for (int x = 0; x < 400; x++)
            {
                Color c = frame.GetPixel(x, y);
                if (c.A == 255 && c.R > 240 && c.G > 240 && c.B > 240) white++;
                if (c.A == 255 && c.R < 20 && c.G < 20 && c.B < 20) black++;
                if (empty.GetPixel(x, y).A != 0) visible++;
            }
            Check(white > 200 && black > 200 && visible == 0, "free telop draws white outlined text and nothing when empty");
            frame.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "telop-frame.png"), ImageFormat.Png);
        }

        var onScreen = new Rectangle(Screen.PrimaryScreen.WorkingArea.Left + 60, Screen.PrimaryScreen.WorkingArea.Top + 60, 500, 140);
        FormMain.UserConfig.TelopBounds = onScreen;
        typeof(FormMain).GetMethod("ToggleTelopWindow", flags).Invoke(form, null);
        for (int i = 0; i < 30; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
        var window = (Form)get("telopWindow");
        int exStyle = GetWindowLong(window.Handle, -20);
        Check(window.Visible && window.Bounds == onScreen && (exStyle & 0x80000) != 0 && (exStyle & 0x20) != 0 && ((ToolStripButton)get("telopButton")).Checked && ((ToolStripButton)get("telopToggle")).Checked,
            "free telop opens as its own transparent click-through window");
        Check((bool)window.GetType().GetProperty("LastUpdateSucceeded", flags).GetValue(window, null), "free telop window accepts its alpha frame");
        Check(((ToolStripLabel)get("telopSizeLabel")).Text == FormMain.UserConfig.TelopFontSize + "px" && ((ToolStripDropDownButton)get("telopFontButton")).Text.Length > 0, "telop size and font are shown from start-up");
        int size = FormMain.UserConfig.TelopFontSize;
        typeof(FormMain).GetMethod("ChangeTelopSize", flags).Invoke(form, new object[] { 1 });
        Check(FormMain.UserConfig.TelopFontSize == size + 1 && ((ToolStripLabel)get("telopSizeLabel")).Text == (size + 1) + "px", "telop size steps by one pixel and shows it");
        typeof(FormMain).GetMethod("ChangeTelopSize", flags).Invoke(form, new object[] { -1 });
        var fontButton = (ToolStripDropDownButton)get("telopFontButton");
        FormMain.UserConfig.TelopFontName = "MS PGothic";
        fontButton.DropDownItems[0].PerformClick();
        Check(FormMain.UserConfig.TelopFontName == "GenEi M Gothic v2 Black" && fontButton.Text == "源暎エムゴ（内蔵）", "the font menu switches back to the bundled font in one click");
        var toolbar = (ToolStrip)((SplitContainer)get("splitContainerpkCaption")).Panel2.Controls["telopToolbar"];
        Check(toolbar != null && toolbar.Renderer is ReaderToolbarRenderer && ((ToolStripButton)get("telopToggle")).Image != null, "the telop row uses the same toolbar style and icons");
        ((ToolStripButton)get("telopToggle")).PerformClick();
        Application.DoEvents();
        Check(get("telopWindow") == null && FormMain.UserConfig.TelopBounds == onScreen && !((ToolStripButton)get("telopButton")).Checked, "the panel toggle closes the free telop and keeps its position");
        textBox.Text = "";
    }

    static void OpenBoardChecks(FormMain form)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var isJpnkn = typeof(FormMain).GetMethod("IsJpnknInput", BindingFlags.NonPublic | BindingFlags.Static);
        Func<string, bool> jp = s => (bool)isJpnkn.Invoke(null, new object[] { s });
        Check(jp("sapio") && jp("https://bbs.jpnkn.com/sapio/") && jp("https://bbs.jpnkn.com/test/read.cgi/sapio/123/") && !jp("https://jbbs.shitaraba.net/bbs/read.cgi/game/1/2/") && !jp(""), "jpnkn boards and threads are recognised for the mode choice");
        Check(new UserConfig().OpenLastOnStartup, "the last board reopens on start-up by default");
        Check(((MenuStrip)Field(form, "menuStrip1")).Items["openBoardMenu"] != null, "open board menu is on the menu bar");
        bool preview = (bool)typeof(FormMain).GetField("IsDesignPreview", flags).GetValue(form);
        typeof(FormMain).GetField("IsDesignPreview", flags).SetValue(form, false);
        bool jpnknShown = false, otherShown = false, otherHidden = false;
        var timer = new System.Windows.Forms.Timer { Interval = 200 };
        timer.Tick += delegate {
            Form dialog = null;
            foreach (Form open in Application.OpenForms) if (open.Text == "掲示板を開く") dialog = open;
            if (dialog == null) return;
            timer.Stop();
            TextBox url = null; Panel panel = null; Label other = null;
            foreach (Control c in dialog.Controls) { if (c is TextBox) url = (TextBox)c; else if (c is Panel) panel = (Panel)c; else if (c is Label && c.Text.StartsWith("板のURL")) other = (Label)c; }
            url.Text = "https://bbs.jpnkn.com/sapio/";
            jpnknShown = panel.Visible; otherHidden = !other.Visible;
            url.Text = "https://jbbs.shitaraba.net/game/42885/";
            otherShown = other.Visible && !panel.Visible;
            dialog.DialogResult = DialogResult.Cancel;
        };
        timer.Start();
        bool opened = (bool)typeof(FormMain).GetMethod("ShowOpenBoard", flags).Invoke(form, null);
        timer.Dispose();
        typeof(FormMain).GetField("IsDesignPreview", flags).SetValue(form, preview);
        Check(!opened && jpnknShown && otherHidden && otherShown, "the open dialog offers Fast or thread reading only for jpnkn");
    }

    static void StreamChecks(FormMain form)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var stream = (ToolStripItem)((MenuStrip)Field(form, "menuStrip1")).Items["streamModeButton"];
        Check(stream is ToolStripButton && stream.Alignment == ToolStripItemAlignment.Right && stream.Text == "配信コメント", "stream comments button sits on the menu bar");
        typeof(FormMain).GetMethod("ToggleStreamMode", flags).Invoke(form, null);
        for (int i = 0; i < 40; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
        var overlay = (Form)Field(form, "streamOverlay");
        Check(overlay != null && overlay.Visible && form.Visible && ((ToolStripButton)stream).Checked, "stream comments open in their own window while the main window stays usable");
        int exStyle = GetWindowLong(overlay.Handle, -20);
        Check((exStyle & 0x80000) != 0 && (exStyle & 0x20) != 0, "stream window is layered and click-through");
        Check((bool)overlay.GetType().GetProperty("LastUpdateSucceeded", flags).GetValue(overlay, null), "stream window accepts its per-pixel alpha frame");
        var render = overlay.GetType().GetMethod("RenderFrame", BindingFlags.NonPublic | BindingFlags.Static);
        var responses = Field(form, "responses");
        using (var frame = (Bitmap)render.Invoke(null, new object[] { responses, new Size(400, 300) }))
        {
            int clear = 0, white = 0, black = 0;
            for (int y = 0; y < frame.Height; y++) for (int x = 0; x < frame.Width; x++)
            {
                Color c = frame.GetPixel(x, y);
                if (c.A == 0) clear++;
                else if (c.A == 255 && c.R > 240 && c.G > 240 && c.B > 240) white++;
                else if (c.A == 255 && c.R < 20 && c.G < 20 && c.B < 20) black++;
            }
            Check(clear > frame.Width * frame.Height / 4 && white > 300 && black > 300, "comments render white with a black outline over a transparent background");
            frame.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stream-frame.png"), ImageFormat.Png);
        }
        bool showNumber = FormMain.UserConfig.CommentShowNumber;
        FormMain.UserConfig.CommentShowNumber = false;
        using (var without = (Bitmap)render.Invoke(null, new object[] { responses, new Size(400, 300) }))
        {
            FormMain.UserConfig.CommentShowNumber = true;
            using (var with = (Bitmap)render.Invoke(null, new object[] { responses, new Size(400, 300) }))
            {
                bool differs = false;
                for (int y = 0; y < 300 && !differs; y += 3) for (int x = 0; x < 400 && !differs; x += 3) differs = with.GetPixel(x, y) != without.GetPixel(x, y);
                Check(differs, "stream window follows the comment field settings");
            }
        }
        FormMain.UserConfig.CommentShowNumber = showNumber;
        var defaults = new UserConfig();
        Check(defaults.CommentFontName == "MS PGothic" && defaults.StreamFontName == "GenEi M Gothic v2 Black" && defaults.StreamFontSize == 24 && defaults.CommentFontSize == 12 && defaults.CommentShowTime && defaults.CommentSeparators, "comment view defaults to MS PGothic and stream comments to the bundled font");
        string streamFont = FormMain.UserConfig.StreamFontName;
        using (var bundled = (Bitmap)render.Invoke(null, new object[] { responses, new Size(400, 300) }))
        {
            FormMain.UserConfig.StreamFontName = "MS PGothic";
            using (var other = (Bitmap)render.Invoke(null, new object[] { responses, new Size(400, 300) }))
            {
                bool differs = false;
                for (int y = 0; y < 300 && !differs; y += 3) for (int x = 0; x < 400 && !differs; x += 3) differs = bundled.GetPixel(x, y) != other.GetPixel(x, y);
                Check(differs, "stream comments use their own font setting");
            }
        }
        FormMain.UserConfig.StreamFontName = streamFont;
        overlay.Location = new Point(overlay.Left + 7, overlay.Top + 5);
        var moved = overlay.Bounds;
        typeof(FormMain).GetMethod("ToggleStreamMode", flags).Invoke(form, null);
        Application.DoEvents();
        Check(Field(form, "streamOverlay") == null && form.Visible && !((ToolStripButton)stream).Checked && FormMain.UserConfig.StreamOverlayBounds == moved, "closing stream comments remembers the window position");
        var onScreen = new Rectangle(Screen.PrimaryScreen.WorkingArea.Left + 50, Screen.PrimaryScreen.WorkingArea.Top + 50, 420, 260);
        FormMain.UserConfig.StreamOverlayBounds = onScreen;
        for (int i = 0; i < 3; i++)
        {
            typeof(FormMain).GetMethod("ToggleStreamMode", flags).Invoke(form, null);
            for (int j = 0; j < 20; j++) { Application.DoEvents(); System.Threading.Thread.Sleep(5); }
            typeof(FormMain).GetMethod("ToggleStreamMode", flags).Invoke(form, null);
        }
        Check(FormMain.UserConfig.StreamOverlayBounds == onScreen, "reopening stream comments does not shrink the window");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd, int index);

    static void ColourChecks(WebBrowser browser)
    {
        var type = typeof(FormMain).Assembly.GetType("SpeechCastMe.Response");
        type.GetField("Style").SetValue(null, Enum.Parse(type.GetNestedType("BBSStyle"), "jbbs"));
        Script(browser, "clearRes");
        string[] raw = { "901<>名無し<>sage<>2026/10/19 12:00<>最初<>t<>AAA", "902<>名無し<><>2026/10/19 12:01<>&gt;&gt;901 だね<>t<>AAA",
            "903<>名無し<>age<>2026/10/19 12:02<>&gt;&gt;901<>t<>BBB", "904<>名無し<><>2026/10/19 12:03<>&gt;&gt;901<>t<>CCC" };
        foreach (string line in raw)
        {
            object response = Activator.CreateInstance(type, true);
            type.GetMethod("SetRawText").Invoke(response, new object[] { line });
            Script(browser, "addRes", (string)type.GetField("Html").GetValue(response));
        }
        Func<string, string> js = code => Convert.ToString(Script(browser, "eval", code));
        Check(js("document.getElementsByClassName('number')[0].className").Contains("ref3") && js("document.getElementsByClassName('number')[1].className").IndexOf("ref") < 0, "reply numbers turn red after three replies and stay blue without any");
        Check(js("document.getElementsByClassName('identity')[0].className").Contains("id2") && js("document.getElementsByClassName('identity')[0].innerText") == "ID:AAA(2)" && js("document.getElementsByClassName('identity')[2].innerText") == "ID:BBB(1)", "IDs show their post count and colour from two posts");
        Check(!new UserConfig().CommentShowID, "IDs are hidden by default since most boards have none");
        Check(js("document.getElementsByClassName('author')[0].className").Contains("sage") && js("document.getElementsByClassName('author')[2].className").Contains("mail") && js("document.getElementsByClassName('author')[1].className") == "field author", "names are coloured by mail field like the PeerCast viewer");
        Script(browser, "clearRes");
    }

    static void ReadingChecks(FormMain form, WebBrowser browser)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        Func<HtmlElement, bool> isCurrent = div => (" " + div.GetAttribute("className") + " " + div.GetAttribute("class") + " ").Contains(" current ");
        Func<string> current = () => {
            foreach (HtmlElement div in browser.Document.GetElementsByTagName("div"))
                if (isCurrent(div)) return div.GetElementsByTagName("a")[0].GetAttribute("name");
            return null;
        };
        typeof(FormMain).GetMethod("MarkSpoken", flags).Invoke(form, new object[] { 5 });
        var list = (System.Collections.IList)Field(form, "responses");
        Func<int, string> anchorOf = position => "res" + list[position - 1].GetType().GetField("Number").GetValue(list[position - 1]);
        Check(current() == anchorOf(5), "the reply being read is highlighted in the comment view");
        typeof(FormMain).GetMethod("MarkSpoken", flags).Invoke(form, new object[] { 6 });
        int highlighted = 0;
        foreach (HtmlElement div in browser.Document.GetElementsByTagName("div")) if (isCurrent(div)) highlighted++;
        Check(current() == anchorOf(6) && highlighted == 1, "only one reply is highlighted at a time");
        HtmlElement row = null;
        foreach (HtmlElement anchor in browser.Document.GetElementsByTagName("a")) if (anchor.GetAttribute("name") == anchorOf(6)) row = anchor.Parent;
        Check(row.OffsetRectangle.Left == 0 && Convert.ToInt32(Script(browser, "eval", "parseInt((document.getElementsByClassName('current')[0].currentStyle||{}).paddingLeft,10)")) >= 8, "highlight bar sits at the row's left edge, clear of the number");
        FormMain.UserConfig.CommentSeparators = false;
        typeof(FormMain).GetMethod("ApplyReaderAppearance", flags).Invoke(form, null);
        bool noLines = ((string)Script(browser, "eval", "document.body.className")).Contains("no-lines");
        FormMain.UserConfig.CommentSeparators = true;
        typeof(FormMain).GetMethod("ApplyReaderAppearance", flags).Invoke(form, null);
        Check(noLines && !((string)Script(browser, "eval", "document.body.className")).Contains("no-lines"), "reply separator lines can be turned off and on");
        HtmlElement target = null;
        foreach (HtmlElement anchor in browser.Document.GetElementsByTagName("a")) if (anchor.GetAttribute("name") == anchorOf(9)) target = anchor.Parent;
        bool preview = (bool)typeof(FormMain).GetField("IsDesignPreview", flags).GetValue(form);
        typeof(FormMain).GetField("IsDesignPreview", flags).SetValue(form, false);
        try
        {
            target.InvokeMember("click");
            for (int i = 0; i < 50; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(5); }
            Check((int)typeof(FormMain).GetField("lastSpokenNumber", flags).GetValue(form) == 9 && current() == anchorOf(9), "clicking a comment reads that reply and moves the highlight");
            form.StopSpeakingCore();
        }
        finally { typeof(FormMain).GetField("IsDesignPreview", flags).SetValue(form, preview); }
        int count = ((System.Collections.IList)Field(form, "responses")).Count;
        form.SetAutoAdvance(false);
        typeof(FormMain).GetField("lastSpokenNumber", flags).SetValue(form, 9);
        form.CurrentResNumber = 10;
        typeof(FormMain).GetMethod("UpdateUnreadStatus", flags).Invoke(form, null);
        var unread = (ToolStripStatusLabel)Field(form, "unreadStatusLabel");
        Check(unread.Available && unread.Text == "未読 " + (count - 9), "with auto-advance off the unread count is shown");
        form.SetAutoAdvance(true);
        Check(!unread.Available, "unread count hides when auto-advance is on");
        int total = ((System.Collections.IList)Field(form, "responses")).Count;
        typeof(FormMain).GetMethod("MarkSpoken", flags).Invoke(form, new object[] { total });
        typeof(FormMain).GetField("speakingCompletedTime", flags).SetValue(form, DateTime.Now);
        var idle = typeof(FormMain).GetMethod("ClearHighlightWhenIdle", flags);
        idle.Invoke(form, new object[] { DateTime.Now.AddSeconds(3) });
        bool kept = current() != null;
        idle.Invoke(form, new object[] { DateTime.Now.AddSeconds(15) });
        Check(kept && current() == null, "highlight stays briefly after the last reply, then clears");
        typeof(FormMain).GetMethod("MarkSpoken", flags).Invoke(form, new object[] { 3 });
        idle.Invoke(form, new object[] { DateTime.Now.AddSeconds(60) });
        Check(current() != null, "highlight stays while unread replies remain");
        ColourChecks(browser);
        foreach (object response in (System.Collections.IList)Field(form, "responses"))
            Script(browser, "addRes", (string)response.GetType().GetField("Html").GetValue(response));
    }

    static void BoardTrimChecks(FormMain form, WebBrowser browser, System.Collections.IList replies, MethodInfo apply)
    {
        var list = (ListView)Field(form,"listViewResponses");
        int number = 1;
        while (replies.Count < 2600) apply.Invoke(form,new object[] {FastMessage("600",number++)});
        form.CurrentResNumber = 2501;
        object current = replies[2500];
        for (int remaining = 3001 - replies.Count; remaining > 0; remaining--) apply.Invoke(form,new object[] {FastMessage("600",number++)});
        var type = current.GetType();
        Check(replies.Count == 2000 && form.CurrentResNumber == 1500 && replies[form.CurrentResNumber - 1] == current, "board feed drops read comments past the limit and keeps the playback position");
        Check(list.Items.Count == replies.Count && browser.Document.GetElementById("resArea").Children.Count == replies.Count, "list and view stay in step with trimmed comments");
        Check((int)type.GetField("Number").GetValue(replies[0]) == 1 && ((string)type.GetField("Html").GetValue(replies[0])).Contains("name=\"res1\""), "trimmed comments are renumbered");
        Script(browser,"scrollToY",0);
        Check(Convert.ToInt32(Script(browser,"getResTop",1500)) > Convert.ToInt32(Script(browser,"getResTop",1)), "view anchors follow renumbered queue positions");
    }

    static void FastSnapshotChecks(FormMain form, WebBrowser browser, System.Collections.IList replies, MethodInfo apply)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        for (int i = 3; i <= 60; i++) apply.Invoke(form,new object[] {FastMessage("100",i)});
        Script(browser,"scrollToY",300);
        int position = Convert.ToInt32(Script(browser,"scrollTop"));
        form.CurrentResNumber = 2;
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        string origin = "http://127.0.0.1:" + port;
        ((ToolStripTextBox)Field(form,"toolStripTextBoxURL")).Text = origin + "/test/read.cgi/sapio/100/";
        typeof(FormMain).GetField("oldUrl",flags).SetValue(form,origin + "/sapio/dat/100.dat");
        FormMain.UserConfig.UseDefaultProxy = false;
        FormMain.UserConfig.OutputDebugLog = true;
        string requestHeaders = "";
        var server = System.Threading.Tasks.Task.Run(delegate {
            using (var client = listener.AcceptTcpClient())
            using (var stream = client.GetStream())
            {
                var reader = new StreamReader(stream,System.Text.Encoding.ASCII,false,1024,true);
                string line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine())) requestHeaders += line + "\n";
                var body = new System.Text.StringBuilder();
                for (int i = 1; i <= 61; i++) body.Append("name<>sage<>2026/10/19 12:34:56 ID:a<>reply " + i + "<>title\n");
                byte[] data = System.Text.Encoding.ASCII.GetBytes(body.ToString());
                byte[] headers = System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: " + data.Length + "\r\nConnection: close\r\n\r\n");
                stream.Write(headers,0,headers.Length); stream.Write(data,0,data.Length);
            }
        });
        var previousContext = System.Threading.SynchronizationContext.Current;
        System.Threading.SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        try
        {
            var fetch = (System.Threading.Tasks.Task)typeof(FormMain).GetMethod("GetFromURLNext",flags).Invoke(form,null);
            var watch = Stopwatch.StartNew();
            while (!fetch.IsCompleted && watch.ElapsedMilliseconds < 15000) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
            Check(fetch.IsCompleted,"Fast recovery HTTP request completes without blocking UI");
            fetch.GetAwaiter().GetResult();
            server.GetAwaiter().GetResult();
            if (replies.Count != 61) Console.WriteLine("Recovery diagnostics: replies=" + replies.Count + " headers=" + requestHeaders + " log=" + ((TextBox)Field(form,"textBoxLog")).Text);
            Check(!requestHeaders.Contains("Range:") && replies.Count == 61, "Fast recovery uses a full authoritative snapshot instead of a stale DAT range");
            Check(Convert.ToInt32(Script(browser,"eval","document.getElementById('resArea').children.length")) == 61, "snapshot recovery rebuilds all visible replies exactly once");
            Check(form.CurrentResNumber == 2 && Convert.ToInt32(Script(browser,"scrollTop")) == position, "snapshot recovery preserves reading and scroll positions");
        }
        finally { listener.Stop(); System.Threading.SynchronizationContext.SetSynchronizationContext(previousContext); }
    }

    static void SoundChecks()
    {
        string wave = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notification-test.wav");
        try
        {
            using (var w = new BinaryWriter(File.Create(wave)))
            {
                int rate = 16000, samples = rate / 4, bytes = samples * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
                w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                for (int i = 0; i < samples; i++) w.Write((short)(Math.Sin(i * Math.PI * 2 * 440 / rate) * 8000));
            }
            using (var player = new NotificationSoundPlayer())
            {
                int opened = 0, ended = 0, failures = 0;
                string error = null;
                player.Opened += delegate { opened++; };
                player.Ended += delegate { ended++; };
                player.Failed += message => { failures++; error = message; };
                var watch = Stopwatch.StartNew();
                Check(player.Play(wave, 0) && watch.ElapsedMilliseconds < 1000, "notification starts asynchronously without blocking the UI");
                while (ended == 0 && failures == 0 && watch.ElapsedMilliseconds < 12000) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
                Check(opened == 1 && ended == 1 && !player.IsBusy, "real WAV opens plays silently and releases player on completion: " + error);
                Check(player.Play(wave, 0), "notification can replay after completion");
                player.Stop();
                Check(!player.IsBusy, "stopping during load cancels notification safely");
                Check(!player.Play(wave + ".missing", 0) && failures == 1 && !player.IsBusy, "missing sound file reports failure without a wait loop");
                File.WriteAllText(wave, "not an audio file");
                watch.Restart();
                player.Play(wave, 0);
                while (failures < 2 && watch.ElapsedMilliseconds < 12000) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
                Check(failures == 2 && !player.IsBusy, "invalid audio reports decoder failure and releases the player");
            }
        }
        finally { File.Delete(wave); }
    }

    static void BrowserChecks(string sample)
    {
        using (var host = new Form { ShowInTaskbar = false, Location = new Point(-3000,-3000), StartPosition = FormStartPosition.Manual, ClientSize = new Size(300,260) })
        using (var browser = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = false })
        {
            host.Controls.Add(browser);
            host.Show();
            bool loaded = false;
            browser.DocumentCompleted += delegate { loaded = true; };
            var resources = typeof(FormMain).Assembly.GetType("SpeechCastMe.Properties.Resources");
            browser.DocumentText = (string)resources.GetProperty("resView", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
            var watch = Stopwatch.StartNew();
            while (!loaded && watch.ElapsedMilliseconds < 15000) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
            Check(loaded, "embedded browser loads actual resource");
            for (int i = 0; i < 60; i++) Script(browser, "addRes", sample.Replace("res720", "res" + i));
            Script(browser, "scrollToY", 500);
            int before = Convert.ToInt32(Script(browser, "scrollTop"));
            Script(browser, "scrollByY", 3);
            Check(Convert.ToInt32(Script(browser, "scrollTop")) == before + 3, "auto-scroll advances from actual position rather than returning to top");
            before = Convert.ToInt32(Script(browser, "scrollTop"));
            Script(browser, "addRes", sample);
            Check(Convert.ToInt32(Script(browser, "scrollTop")) == before, "new response preserves scroll position");
            Script(browser, "setZoom", 1);
            before = Convert.ToInt32(Script(browser, "scrollTop"));
            Script(browser, "setZoom", 1);
            Check(Convert.ToInt32(Script(browser, "scrollTop")) == before, "repeated zoom update does not jump");
            Script(browser, "setAppearance", "two", "dark", "", 20, "Meiryo", true, true, true, true, true);
            Check(browser.Document.Body.GetAttribute("className").Contains("two-line"), "two-line layout selected");
            Check(browser.Document.Body.GetAttribute("className").Contains("show-time") && browser.Document.Body.GetAttribute("className").Contains("show-date"), "metadata toggles applied");
            Check(Convert.ToInt32(Script(browser, "scrollTop")) > 0, "appearance change keeps visible response rather than top");
            Script(browser, "setAppearance", "one", "light", "", 14, "Meiryo", false, false, false, true, false);
            string classes = browser.Document.Body.GetAttribute("className");
            Check(classes.Contains("one-line") && classes.Contains("show-time") && !classes.Contains("show-number") && !classes.Contains("show-date") && !classes.Contains("show-id"), "time-only one-line layout works independently");
            Script(browser, "scrollBottom");
            Check(Convert.ToInt32(Script(browser, "scrollTop")) > 1000, "latest-response scroll reaches document end");
            browser.Width = 220;
            Application.DoEvents();
            Check(browser.Document.Body.ScrollRectangle.Width <= browser.ClientSize.Width + 30, "narrow comment layout wraps rather than overflowing");
            host.Close();
        }
    }
}
