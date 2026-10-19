// Speech backends behind one synthesizer-like surface, 2026-10-19. GPLv2.
// - SAPI 5 voices (System.Speech): the original behaviour, e.g. Haruka Desktop.
// - Windows 10+ built-in voices (Ayumi, Ichiro, ...), rendered to WAV.
// - A running VOICEVOX / AivisSpeech engine on this PC, found automatically.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;

namespace SpeechCastMe
{
    public sealed class SpeechProgressArgs : EventArgs
    {
        public int CharacterPosition { get; internal set; }
        public int CharacterCount { get; internal set; }
    }

    public sealed class SpeechCompletedArgs : EventArgs
    {
        public bool Cancelled { get; internal set; }
    }

    public sealed class SpeechEngine : IDisposable
    {
        private enum Kind { Sapi, OneCore, Engine }

        private sealed class Voice
        {
            public Kind Kind;
            public string Id;
            public string Url;
            public int Style;
        }

        // Label and base URL of the local engines probed for voices.
        public static readonly List<KeyValuePair<string, string>> EngineEndpoints = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("VOICEVOX", "http://127.0.0.1:50021"),
            new KeyValuePair<string, string>("AivisSpeech", "http://127.0.0.1:10101"),
        };

        private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly HttpClient probe = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        private readonly SpeechSynthesizer sapi = new SpeechSynthesizer();
        private readonly System.Windows.Threading.Dispatcher owner = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        private readonly Dictionary<string, Voice> voices = new Dictionary<string, Voice>();
        private readonly List<string> names = new List<string>();
        private readonly Queue<string> pending = new Queue<string>();
        private readonly System.Windows.Forms.Timer progressTimer = new System.Windows.Forms.Timer { Interval = 50 };
        // Never let a stuck player stop the reading queue.
        private readonly System.Windows.Forms.Timer watchdog = new System.Windows.Forms.Timer();
        private Voice current;
        private MediaPlayer player;
        private string playingFile, playingText;
        private DateTime playStart;
        private double playMilliseconds;
        private int generation, volume = 100, rate;
        private bool busy, sapiActive;

        public event EventHandler<SpeechProgressArgs> SpeakProgress;
        public event EventHandler<SpeechCompletedArgs> SpeakCompleted;
        /// <summary>音声エンジンに失敗してWindowsの音声に切り替えたときの通知</summary>
        public event Action<string> Failed;

        public SpeechEngine()
        {
            sapi.SpeakProgress += (s, e) => { if (sapiActive) RaiseProgress(e.CharacterPosition, e.CharacterCount); };
            sapi.SpeakCompleted += (s, e) => { sapiActive = sapi.State == SynthesizerState.Speaking; Raise(e.Cancelled); };
            progressTimer.Tick += delegate { ReportPlaybackProgress(); };
            watchdog.Tick += delegate {
                if (player == null) { watchdog.Stop(); return; }
                if (Failed != null) Failed("音声の再生が応答しないため、次へ進みます。");
                Finish(false);
            };
            foreach (InstalledVoice voice in sapi.GetInstalledVoices())
                if (voice.Enabled) Add(voice.VoiceInfo.Name, new Voice { Kind = Kind.Sapi, Id = voice.VoiceInfo.Name });
            try
            {
                foreach (var voice in OneCoreVoices.List())
                    // Haruka exists in both; keep the SAPI "Desktop" entry users already have.
                    if (!voices.ContainsKey(voice.Key) && !voices.ContainsKey(voice.Key + " Desktop"))
                        Add(voice.Key, new Voice { Kind = Kind.OneCore, Id = voice.Value });
            }
            catch (Exception) { } // Windows 7/8: SAPI voices only.
        }

        public IList<string> VoiceNames { get { return names.AsReadOnly(); } }
        public string CurrentVoiceName { get; private set; }

        public int Volume
        {
            get { return volume; }
            set
            {
                volume = Math.Max(0, Math.Min(100, value));
                sapi.Volume = volume;
                if (player != null) player.Volume = volume / 100.0;
            }
        }

        // SAPI scale -10..10; other engines map it to a speed multiplier.
        public int Rate
        {
            get { return rate; }
            set { rate = Math.Max(-10, Math.Min(10, value)); sapi.Rate = rate; }
        }

        public SynthesizerState State
        {
            get { return busy || pending.Count > 0 || (sapiActive && sapi.State == SynthesizerState.Speaking) ? SynthesizerState.Speaking : SynthesizerState.Ready; }
        }

        public static bool IsEngineVoiceName(string name)
        {
            return name != null && EngineEndpoints.Any(e => name.StartsWith(e.Key + ":", StringComparison.Ordinal));
        }

        public bool SelectVoice(string name)
        {
            Voice voice;
            if (name == null || !voices.TryGetValue(name, out voice)) return false;
            if (voice.Kind == Kind.Sapi) sapi.SelectVoice(voice.Id);
            current = voice;
            CurrentVoiceName = name;
            return true;
        }

        /// <summary>
        /// 起動中のVOICEVOX等から声の一覧を取り直す。一覧が変わったらtrue。
        /// </summary>
        public async Task<bool> RefreshEngineVoicesAsync()
        {
            var found = new List<KeyValuePair<string, Voice>>();
            foreach (var endpoint in EngineEndpoints.ToArray())
            {
                try
                {
                    string json = await probe.GetStringAsync(endpoint.Value + "/speakers");
                    var speakers = new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(json);
                    foreach (var speaker in speakers ?? new List<Dictionary<string, object>>())
                    {
                        var styles = speaker.ContainsKey("styles") ? speaker["styles"] as System.Collections.ArrayList : null;
                        if (styles == null) continue;
                        foreach (Dictionary<string, object> style in styles)
                        {
                            if (style.ContainsKey("type") && style["type"] as string != null && (string)style["type"] != "talk") continue;
                            string label = endpoint.Key + ":" + speaker["name"] + "（" + style["name"] + "）";
                            found.Add(new KeyValuePair<string, Voice>(label, new Voice { Kind = Kind.Engine, Url = endpoint.Value, Style = Convert.ToInt32(style["id"]) }));
                        }
                    }
                }
                catch (Exception) { } // Not running, or not a compatible engine.
            }
            var before = names.Where(n => voices[n].Kind == Kind.Engine).ToList();
            if (before.SequenceEqual(found.Select(f => f.Key))) return false;
            foreach (string name in before) { voices.Remove(name); names.Remove(name); }
            foreach (var voice in found) if (!voices.ContainsKey(voice.Key)) Add(voice.Key, voice.Value);
            if (current != null && current.Kind == Kind.Engine && !voices.ContainsKey(CurrentVoiceName)) current = null;
            return true;
        }

        public void SpeakAsync(string text)
        {
            if (current == null || current.Kind == Kind.Sapi) { SpeakWithSapi(text); return; }
            pending.Enqueue(text ?? "");
            if (!busy) StartNext();
        }

        public void SpeakAsyncCancelAll()
        {
            generation++;
            pending.Clear();
            sapi.SpeakAsyncCancelAll();
            if (busy) Finish(true);
        }

        private void SpeakWithSapi(string text)
        {
            sapiActive = true;
            sapi.SpeakAsync(text ?? "");
        }

        private async void StartNext()
        {
            if (pending.Count == 0) return;
            busy = true;
            string text = pending.Dequeue();
            int started = generation;
            Voice voice = current;
            double speed = Math.Pow(3.0, rate / 10.0);
            byte[] wav = null;
            Exception error = null;
            try
            {
                if (voice == null || voice.Kind == Kind.Sapi) throw new InvalidOperationException("音声が選択されていません。");
                wav = voice.Kind == Kind.OneCore
                    ? await OneCoreVoices.SynthesizeAsync(voice.Id, text, speed).ConfigureAwait(false)
                    : await SynthesizeWithEngineAsync(voice, text, speed).ConfigureAwait(false);
            }
            catch (Exception ex) { error = ex; }
            // Synthesis runs off the UI thread; playback (MediaPlayer is bound to its
            // creating thread), events and the SAPI fallback return to the owner thread.
            owner.BeginInvoke(new Action(delegate {
                if (started != generation) return;
                if (error == null) { Play(wav, text); return; }
                busy = false;
                if (Failed != null) Failed((voice != null && voice.Kind == Kind.Engine ? "音声エンジンに接続できません" : "音声を作成できません") + "（" + error.Message + "）。Windowsの音声で読み上げます。");
                SpeakWithSapi(text);
            }));
        }

        private static async Task<byte[]> SynthesizeWithEngineAsync(Voice voice, string text, double speed)
        {
            string query;
            using (var response = await http.PostAsync(voice.Url + "/audio_query?speaker=" + voice.Style + "&text=" + Uri.EscapeDataString(text), new StringContent("")))
            {
                response.EnsureSuccessStatusCode();
                query = await response.Content.ReadAsStringAsync();
            }
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var data = serializer.Deserialize<Dictionary<string, object>>(query);
            data["speedScale"] = Math.Max(0.5, Math.Min(2.0, speed));
            using (var body = new StringContent(serializer.Serialize(data), Encoding.UTF8, "application/json"))
            using (var response = await http.PostAsync(voice.Url + "/synthesis?speaker=" + voice.Style, body))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        private void Play(byte[] wav, string text)
        {
            string directory = Path.Combine(Path.GetTempPath(), "SpeechCastSP");
            Directory.CreateDirectory(directory);
            playingFile = Path.Combine(directory, "voice-" + Guid.NewGuid().ToString("N") + ".wav");
            File.WriteAllBytes(playingFile, wav);
            playingText = text;
            playMilliseconds = EstimateWavMilliseconds(wav);
            player = new MediaPlayer { Volume = volume / 100.0 };
            player.MediaOpened += OnOpened;
            player.MediaEnded += OnEnded;
            player.MediaFailed += OnFailed;
            watchdog.Interval = 10000;
            watchdog.Start();
            player.Open(new Uri(playingFile));
        }

        private void OnOpened(object sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, player)) return;
            if (player.NaturalDuration.HasTimeSpan) playMilliseconds = player.NaturalDuration.TimeSpan.TotalMilliseconds;
            playStart = DateTime.UtcNow;
            watchdog.Stop();
            watchdog.Interval = (int)Math.Min(int.MaxValue, Math.Max(1000, playMilliseconds) + 5000);
            watchdog.Start();
            player.Play();
            progressTimer.Start();
            ReportPlaybackProgress();
        }

        private void OnEnded(object sender, EventArgs e) { if (ReferenceEquals(sender, player)) Finish(false); }

        private void OnFailed(object sender, ExceptionEventArgs e)
        {
            if (!ReferenceEquals(sender, player)) return;
            if (Failed != null) Failed("音声を再生できません（" + e.ErrorException.Message + "）。");
            Finish(false);
        }

        // WAV playback has no word events; advance the caption with elapsed time.
        private void ReportPlaybackProgress()
        {
            if (player == null || playingText == null) return;
            double elapsed = (DateTime.UtcNow - playStart).TotalMilliseconds;
            int position = playMilliseconds <= 0 ? playingText.Length : (int)Math.Min(playingText.Length, Math.Ceiling(playingText.Length * elapsed / playMilliseconds));
            RaiseProgress(position, 0);
        }

        private void Finish(bool cancelled)
        {
            progressTimer.Stop();
            watchdog.Stop();
            if (player != null)
            {
                if (!cancelled && playingText != null) RaiseProgress(playingText.Length, 0);
                player.MediaOpened -= OnOpened;
                player.MediaEnded -= OnEnded;
                player.MediaFailed -= OnFailed;
                player.Close();
                player = null;
            }
            if (playingFile != null)
            {
                try { File.Delete(playingFile); } catch (Exception) { }
                playingFile = null;
            }
            playingText = null;
            busy = false;
            Raise(cancelled);
            if (!busy && pending.Count > 0) StartNext();
        }

        private static double EstimateWavMilliseconds(byte[] wav)
        {
            // RIFF header: byte rate at offset 28; data size approximated by file length.
            if (wav == null || wav.Length < 44) return 0;
            int byteRate = BitConverter.ToInt32(wav, 28);
            return byteRate > 0 ? (wav.Length - 44) * 1000.0 / byteRate : 0;
        }

        private void Add(string name, Voice voice)
        {
            voices[name] = voice;
            names.Add(name);
        }

        private void RaiseProgress(int position, int count)
        {
            if (SpeakProgress != null) SpeakProgress(this, new SpeechProgressArgs { CharacterPosition = position, CharacterCount = count });
        }

        private void Raise(bool cancelled)
        {
            if (SpeakCompleted != null) SpeakCompleted(this, new SpeechCompletedArgs { Cancelled = cancelled });
        }

        public void Dispose()
        {
            SpeakAsyncCancelAll();
            progressTimer.Dispose();
            watchdog.Dispose();
            sapi.Dispose();
        }
    }
}
