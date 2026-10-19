// Audio mode controls, 2026-10-19. GPLv2.
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private readonly NotificationSoundPlayer notificationPlayer = new NotificationSoundPlayer();
        private readonly Random notificationRandom = new Random();
        private ToolStripComboBox audioModeSelector;
        private bool syncingAudioMode;
        private DateTime audioMessageUntil;
        // In AudioNotificationMode order.
        internal static readonly object[] AudioModeNames = { "読み上げ", "着信音", "無音", "読み上げ＋着信音" };

        private bool AudioSpeaks { get { return UserConfig.AudioMode == AudioNotificationMode.Speech || UserConfig.AudioMode == AudioNotificationMode.SpeechAndSound; } }
        private bool AudioRings { get { return UserConfig.AudioMode == AudioNotificationMode.Sound || UserConfig.AudioMode == AudioNotificationMode.SpeechAndSound; } }
        // 読み上げ＋着信音: the reading waits until the sound has finished.
        private bool WaitingForSound { get { return UserConfig != null && UserConfig.AudioMode == AudioNotificationMode.SpeechAndSound && notificationPlayer.IsBusy; } }

        private void ShowAudioMessage(string message)
        {
            audioMessageUntil = DateTime.UtcNow.AddSeconds(8);
            toolStripStatusLabelCommunication.Text = message;
            toolStripStatusLabelCommunication.ToolTipText = message;
        }

        private void InitializeAudioUi()
        {
            foreach (var item in new ToolStripItem[] { toolStripButtonSpeech, toolStripButtonPlaySoundNewResponse, toolStripButtonClickSound })
                myToolStripVoice.Items.Remove(item);
            audioModeSelector = new ToolStripComboBox { Name = "audioModeSelector", DropDownStyle = ComboBoxStyle.DropDownList,
                AutoSize = false, Width = 112, ToolTipText = "通知方法：読み上げ / 着信音 / 無音 / 読み上げ＋着信音（鳴り終わってから読む）" };
            audioModeSelector.Items.AddRange(AudioModeNames);
            myToolStripVoice.Items.Insert(0, audioModeSelector);
            audioModeSelector.SelectedIndexChanged += delegate {
                if (syncingAudioMode || UserConfig == null || audioModeSelector.SelectedIndex < 0) return;
                UserConfig.AudioMode = (AudioNotificationMode)audioModeSelector.SelectedIndex;
                notificationPlayer.Stop();
                SyncAudioModeUi();
                if (AudioRings && !UserConfig.NewResponseSoundFilePathes.Any(File.Exists))
                    ShowAudioMessage("設定 → 着信音 で音声ファイルを登録してください");
            };
            notificationPlayer.Failed += message => { ShowAudioMessage("着信音：" + message); AddLog("Notification: {0}", message); };
            // Suppress the embedded browser's navigation click regardless of legacy settings.
            CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_PROCESS, true);
        }

        internal void SyncAudioModeUi()
        {
            if (UserConfig == null || audioModeSelector == null) return;
            syncingAudioMode = true;
            audioModeSelector.SelectedIndex = (int)UserConfig.AudioMode;
            syncingAudioMode = false;
            bool speech = AudioSpeaks;
            bool sound = AudioRings;
            toolStripComboBoxSelectVoice.Enabled = speech;
            toolStripTextBoxVoiceVolume.Enabled = toolStripTrackBarVoiceVolume.Enabled = speech;
            toolStripTextBoxPlayVolume.Enabled = toolStripTrackBarPlayVolume.Enabled = sound;
            toolStripButtonSpeech.Checked = speech;
            toolStripButtonPlaySoundNewResponse.Checked = sound;
            synthesizer.Volume = speech ? UserConfig.SpeakingVolume : 0;
        }

        // Notify once per successful update containing new replies. Initial history,
        // manual replays and updates without new replies do not produce a sound.
        private void NotifyNewResponses(bool next, int previousCount, int currentCount)
        {
            if (!next || currentCount <= previousCount || !AudioRings || IsDesignPreview) return;
            if (notificationPlayer.IsBusy) return;
            var files = UserConfig.NewResponseSoundFilePathes.Where(File.Exists).ToArray();
            if (files.Length == 0)
            {
                ShowAudioMessage("着信音：設定で有効な音声ファイルを登録してください");
                return;
            }
            notificationPlayer.Play(files[notificationRandom.Next(files.Length)], UserConfig.PlayVolume);
        }

    }
}
