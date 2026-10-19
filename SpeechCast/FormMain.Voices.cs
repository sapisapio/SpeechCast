// Voice list: SAPI, Windows built-in and VOICEVOX-compatible engines, 2026-10-19. GPLv2.
using System;
using System.Threading.Tasks;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private bool syncingVoiceList, refreshingEngineVoices;

        private void InitializeVoiceEngines()
        {
            synthesizer.Failed += message => { ShowAudioMessage(message); AddLog("Speech: {0}", message); };
            // Pick up VOICEVOX started after this app: re-probe whenever the list opens.
            toolStripComboBoxSelectVoice.DropDown += delegate { RefreshEngineVoices(); };
        }

        // Rebuild the selector from the engine's voices and select the saved voice.
        // A saved VOICEVOX voice that is not running yet stays saved; the first
        // voice speaks meanwhile.
        private void SyncVoiceList()
        {
            syncingVoiceList = true;
            try
            {
                toolStripComboBoxSelectVoice.Items.Clear();
                foreach (string voiceName in synthesizer.VoiceNames) toolStripComboBoxSelectVoice.Items.Add(voiceName);
                int index = toolStripComboBoxSelectVoice.Items.IndexOf(UserConfig.VoiceName);
                if (index >= 0 || toolStripComboBoxSelectVoice.Items.Count > 0)
                    toolStripComboBoxSelectVoice.SelectedIndex = Math.Max(0, index);
            }
            finally { syncingVoiceList = false; }
            if (toolStripComboBoxSelectVoice.SelectedIndex >= 0 && !SpeechEngine.IsEngineVoiceName(UserConfig.VoiceName))
                UserConfig.VoiceName = toolStripComboBoxSelectVoice.SelectedItem.ToString();
        }

        private async void RefreshEngineVoices()
        {
            if (IsDesignPreview || refreshingEngineVoices || UserConfig == null) return;
            refreshingEngineVoices = true;
            try
            {
                if (await synthesizer.RefreshEngineVoicesAsync() && !IsDisposed) SyncVoiceList();
            }
            catch (Exception ex) { AddLog("Speech: {0}", ex.Message); }
            finally { refreshingEngineVoices = false; }
        }
    }
}
