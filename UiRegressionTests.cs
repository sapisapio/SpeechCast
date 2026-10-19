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
        Check(html.Contains("2026/10/18(日)</span>") && html.Contains("12:34:56.789</span>") && html.Contains("ID:id</span>"), "date time and ID emitted separately");
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
            Check(((ToolStripItem)Field(form,"toolStripButtonAutoUpdate")).Alignment == ToolStripItemAlignment.Right && ((ToolStripItem)Field(form,"toolStripButtonMoveBottom")).Bounds.Right >= play.ClientSize.Width - 16, "viewer controls are anchored to the actual right edge");
            Check(play.Visible && voice.Visible && url.Visible, "voice play and URL rows are visible by default");
            Check(((ToolStripItem)Field(form,"toolStripComboBoxSelectVoice")).Owner == voice && ((ToolStripItem)Field(form,"toolStripTrackBarVoiceVolume")).Owner == voice, "voice selector and volume remain on voice row");
            Check(((ToolStripItem)Field(form,"toolStripButtonAutoUpdate")).Owner == play && ((ToolStripItem)Field(form,"toolStripButtonMoveBottom")).Owner == play, "update and viewer controls share play row");
            Check(((ToolStripItem)Field(form,"toolStripButtonAutoUpdate")).Bounds.Left < ((ToolStripItem)Field(form,"toolStripButtonZoomOutnResView")).Bounds.Left && ((ToolStripItem)Field(form,"toolStripButtonZoomOutnResView")).Bounds.Left < ((ToolStripItem)Field(form,"toolStripButtonZoomInResView")).Bounds.Left && ((ToolStripItem)Field(form,"toolStripButtonZoomInResView")).Bounds.Left < ((ToolStripItem)Field(form,"toolStripButtonMoveBottom")).Bounds.Left, "right-aligned update zoom and bottom controls keep intended order");
            Check(url.Items.Count == 5 && url.Items[0] == Field(form,"feedModeSelector") && url.Items[1] == Field(form,"toolStripTextBoxURL") && ((ToolStripItem)Field(form,"toolStripTextBoxThreadKeyword")).Owner == url && ((ToolStripItem)Field(form,"toolStripButtonAutoNextThread")).Owner == url, "URL row contains source mode URL keyword and toggle");
            Check(((ToolStripButton)Field(form,"toolStripButtonAutoUpdate")).Text == "🔽" && ((ToolStripButton)Field(form,"toolStripButtonTurbo")).Text == "ターボ", "update emoji and explicit turbo label retained");
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
        Check(form.AutoUpdate && replies.Count == before + 1 && (bool)Field(form,"fastReadingPaused"), "pausing reading keeps board-wide acquisition running");
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
        FastSnapshotChecks(form,browser,replies,apply);
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
