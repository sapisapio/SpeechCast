// Sounds for the timer gadget: the bundled alarm and school chime, or, when those files are
// missing, synthesized stand-ins (the Westminster Quarters, public domain, and a "pi-pi-pi"), 2026-10-19. GPLv2.
using System;
using System.IO;

namespace SpeechCastMe
{
    internal static class GadgetSounds
    {
        private const int Rate = 44100;

        // The bundled sound next to the program (Sounds\alarm.mp3, Sounds\chime.mp3); without it,
        // a WAV synthesized here and written on first use.
        internal static string PathFor(string sound)
        {
            string bundled = Path.Combine(System.Windows.Forms.Application.StartupPath, "Sounds", (sound == GadgetTimer.ChimeSound ? "chime" : "alarm") + ".mp3");
            if (File.Exists(bundled)) return bundled;
            string folder = Path.Combine(Path.GetTempPath(), "SpeechCastSP");
            string path = Path.Combine(folder, sound + "-v1.wav");
            if (File.Exists(path)) return path;
            Directory.CreateDirectory(folder);
            float[] samples = sound == GadgetTimer.ChimeSound ? Chime() : Beep();
            WriteWav(path, samples);
            return path;
        }

        // キーンコーンカーンコーン: E C D G, G D E C, struck like a bell.
        private static float[] Chime()
        {
            double[] notes = { 329.63, 261.63, 293.66, 196.00, 196.00, 293.66, 329.63, 261.63 };
            const double step = 0.78, gap = 0.5;
            double length = notes.Length * step + gap + 3.2;
            var samples = new float[(int)(length * Rate)];
            for (int i = 0; i < notes.Length; i++)
            {
                double at = i * step + (i >= 4 ? gap : 0);
                Bell(samples, at, notes[i], i == notes.Length - 1 ? 3.2 : 2.2);
            }
            Normalize(samples, 0.7f);
            return samples;
        }

        // A bell: a few inharmonic partials, each fading out at its own rate.
        private static void Bell(float[] samples, double at, double frequency, double ring)
        {
            double[] partials = { 1.0, 2.0, 2.76, 4.07, 5.4 };
            double[] levels = { 1.0, 0.55, 0.35, 0.18, 0.1 };
            double[] decay = { 1.0, 1.6, 2.4, 3.4, 4.5 };
            int start = (int)(at * Rate), count = (int)(ring * Rate);
            for (int n = 0; n < count && start + n < samples.Length; n++)
            {
                double t = n / (double)Rate;
                double attack = Math.Min(1.0, t / 0.004);
                double value = 0;
                for (int p = 0; p < partials.Length; p++)
                    value += levels[p] * Math.Exp(-t * decay[p] * 1.6) * Math.Sin(2 * Math.PI * frequency * partials[p] * t);
                samples[start + n] += (float)(value * attack);
            }
        }

        // ピピピ … ピピピ: three short beeps, four times.
        private static float[] Beep()
        {
            const double tone = 2000, on = 0.08, off = 0.07, pause = 0.5;
            double set = 3 * (on + off) + pause;
            var samples = new float[(int)(4 * set * Rate)];
            for (int s = 0; s < 4; s++)
                for (int b = 0; b < 3; b++)
                {
                    int start = (int)((s * set + b * (on + off)) * Rate), count = (int)(on * Rate);
                    for (int n = 0; n < count; n++)
                    {
                        double t = n / (double)Rate;
                        double edge = Math.Min(1.0, Math.Min(t, on - t) / 0.004); // no clicks
                        samples[start + n] = (float)(0.45 * edge * Math.Sin(2 * Math.PI * tone * t));
                    }
                }
            return samples;
        }

        private static void Normalize(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float v in samples) max = Math.Max(max, Math.Abs(v));
            if (max <= 0f) return;
            float gain = peak / max;
            for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
        }

        private static void WriteWav(string path, float[] samples)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                int bytes = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' }); writer.Write(36 + bytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' }); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(Rate); writer.Write(Rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' }); writer.Write(bytes);
                foreach (float v in samples) writer.Write((short)Math.Max(short.MinValue, Math.Min(short.MaxValue, v * 32767f)));
            }
        }
    }
}
