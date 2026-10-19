// Windows 10+ built-in (OneCore) voices such as Ayumi and Ichiro, 2026-10-19. GPLv2.
// These ship with Windows but are not exposed through SAPI 5 / System.Speech.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

namespace SpeechCastMe
{
    internal static class OneCoreVoices
    {
        // Callers wrap these in try/catch: on systems without WinRT the type load
        // fails when the method is compiled, which happens at the call site.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static List<KeyValuePair<string, string>> List()
        {
            return SpeechSynthesizer.AllVoices.Select(v => new KeyValuePair<string, string>(v.DisplayName, v.Id)).ToList();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static async Task<byte[]> SynthesizeAsync(string id, string text, double speed)
        {
            VoiceInformation voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == id);
            if (voice == null) throw new InvalidOperationException("音声が見つかりません。");
            using (var synthesizer = new SpeechSynthesizer { Voice = voice })
            {
                try { synthesizer.Options.SpeakingRate = Math.Max(0.5, Math.Min(6.0, speed)); }
                catch (Exception) { } // Options need Windows 10 1709 or later.
                using (SpeechSynthesisStream stream = await synthesizer.SynthesizeTextToStreamAsync(text))
                using (var reader = new DataReader(stream))
                {
                    var bytes = new byte[stream.Size];
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                    return bytes;
                }
            }
        }
    }
}
