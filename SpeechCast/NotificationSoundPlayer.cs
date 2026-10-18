// Notification playback lifecycle, 2026-10-19. GPLv2.
using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;

namespace SpeechCastMe
{
    public sealed class NotificationSoundPlayer : IDisposable
    {
        private MediaPlayer player;
        private readonly Timer loadTimer = new Timer { Interval = 10000 };
        public event Action<string> Failed;
        public event Action Opened;
        public event Action Ended;
        public bool IsBusy { get { return player != null; } }

        public NotificationSoundPlayer()
        {
            loadTimer.Tick += delegate { Fail("音声ファイルの読み込みがタイムアウトしました。"); };
        }

        // Called on the UI thread. Keep the player alive until playback ends;
        // never block the dispatcher waiting for NaturalDuration.
        public bool Play(string path, int volume)
        {
            Stop();
            try
            {
                if (!File.Exists(path)) throw new FileNotFoundException("音声ファイルが見つかりません。", path);
                player = new MediaPlayer();
                player.MediaOpened += OnOpened;
                player.MediaEnded += OnEnded;
                player.MediaFailed += OnFailed;
                player.Volume = Math.Max(0, Math.Min(100, volume)) / 100.0;
                loadTimer.Start();
                player.Open(new Uri(Path.GetFullPath(path)));
                return true;
            }
            catch (Exception ex) { Fail(ex.Message); return false; }
        }

        private void OnOpened(object sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, player)) return;
            loadTimer.Stop();
            player.Play();
            if (Opened != null) Opened();
        }

        private void OnEnded(object sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, player)) return;
            Stop();
            if (Ended != null) Ended();
        }

        private void OnFailed(object sender, System.Windows.Media.ExceptionEventArgs e)
        {
            if (ReferenceEquals(sender, player)) Fail(e.ErrorException.Message);
        }

        private void Fail(string message)
        {
            Stop();
            if (Failed != null) Failed(message);
        }

        public void Stop()
        {
            loadTimer.Stop();
            if (player == null) return;
            player.MediaOpened -= OnOpened;
            player.MediaEnded -= OnEnded;
            player.MediaFailed -= OnFailed;
            player.Close();
            player = null;
        }

        public void Dispose() { Stop(); loadTimer.Dispose(); }
    }
}
