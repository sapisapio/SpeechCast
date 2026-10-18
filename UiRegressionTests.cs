// Compile against SpeechCastSP.exe and run from the same directory. GPLv2.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Diagnostics;
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
        ToolbarChecks();
        SoundChecks();
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
            Check(url.Items.Count == 4 && url.Items[0] == Field(form,"toolStripTextBoxURL") && ((ToolStripItem)Field(form,"toolStripTextBoxThreadKeyword")).Owner == url && ((ToolStripItem)Field(form,"toolStripButtonAutoNextThread")).Owner == url, "URL row starts with field and contains keyword and toggle");
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
            form.Close();
        }
        Environment.SetEnvironmentVariable("SPEECHCAST_UI_PREVIEW", null);
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
