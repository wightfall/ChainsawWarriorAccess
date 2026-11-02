using System;
using System.Collections.Generic;
using UnityEngine;

namespace CWAccess.Audio
{
    /// <summary>Every sound the mod can play. Each has a volume setting (Cue.Name in CWAccess.ini).</summary>
    public enum Cue
    {
        Move,
        Edge,
        Activate,
        Unavailable,
        ToggleOn,
        ToggleOff,
        Screen,
        PanelOpen,
        PanelClose,
        Choices,
        Room,
        CardTurn,
        Enemy,
        Trap,
        SafeRoom,
        Dice,
        Damage,
        Heal,
        Contamination,
        ItemGained,
        ItemLost,
        Select,
        Deselect,
        Time,
        Night,
    }

    /// <summary>Makes the cue sounds from tones and noise (no sound files) and plays them.</summary>
    internal static class CuePlayer
    {
        private const int Rate = 22050;
        private static readonly Dictionary<Cue, AudioClip> clips = new Dictionary<Cue, AudioClip>();
        private static AudioSource source;

        public static void Init(GameObject host)
        {
            try
            {
                source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                foreach (Cue c in Enum.GetValues(typeof(Cue)))
                {
                    float[] data = Synth.Make(c, Rate);
                    AudioClip clip = AudioClip.Create("CWAccess." + c, data.Length, 1, Rate, false, false);
                    clip.SetData(data, 0);
                    clips[c] = clip;
                }
                Log.Info("Sound cues ready: " + clips.Count);
            }
            catch (Exception e)
            {
                Log.Error("Sound cues unavailable: " + e.Message);
                source = null;
            }
        }

        /// <summary>Plays a cue when sound cues are on.</summary>
        public static void Play(Cue c)
        {
            if (!Settings.SoundCues.Value) return;
            PlayAt(c, Volume(c));
        }

        /// <summary>Plays a cue even when cues are off (previews in the settings and the key list).</summary>
        public static void Preview(Cue c)
        {
            PlayAt(c, Mathf.Max(Volume(c), 0.05f));
        }

        public static float Volume(Cue c)
        {
            float overall = Mathf.Clamp01(Settings.CueVolume.Value / 100f);
            float own = Settings.CueVolumes.TryGetValue(c, out Setting<int> s) ? Mathf.Clamp01(s.Value / 100f) : 1f;
            return overall * own;
        }

        private static void PlayAt(Cue c, float volume)
        {
            if (source == null || volume <= 0f || !clips.TryGetValue(c, out AudioClip clip)) return;
            try
            {
                source.PlayOneShot(clip, volume);
            }
            catch (Exception e)
            {
                Log.Error("Cue " + c + " failed: " + e.Message);
            }
        }
    }

    /// <summary>The recipe of each cue.</summary>
    internal static class Synth
    {
        private enum Wave
        {
            Sine,
            Square,
            Triangle,
            Noise,
        }

        private static readonly System.Random random = new System.Random(7);

        public static float[] Make(Cue c, int rate)
        {
            switch (c)
            {
                case Cue.Move: return Tone(rate, 1400, 1400, 0.025f, Wave.Sine, 0.35f);
                case Cue.Edge: return Tone(rate, 180, 140, 0.09f, Wave.Triangle, 0.6f);
                case Cue.Activate: return Concat(Tone(rate, 900, 900, 0.02f, Wave.Square, 0.25f), Tone(rate, 1300, 1300, 0.03f, Wave.Sine, 0.35f));
                case Cue.Unavailable: return Tone(rate, 150, 150, 0.16f, Wave.Square, 0.3f);
                case Cue.ToggleOn: return Tone(rate, 600, 1100, 0.09f, Wave.Sine, 0.45f);
                case Cue.ToggleOff: return Tone(rate, 1100, 600, 0.09f, Wave.Sine, 0.45f);
                case Cue.Screen: return Concat(Tone(rate, 660, 660, 0.06f, Wave.Sine, 0.35f), Tone(rate, 990, 990, 0.09f, Wave.Sine, 0.35f));
                case Cue.PanelOpen: return Tone(rate, 500, 900, 0.07f, Wave.Triangle, 0.4f);
                case Cue.PanelClose: return Tone(rate, 900, 500, 0.07f, Wave.Triangle, 0.4f);
                case Cue.Choices: return Concat(Tone(rate, 784, 784, 0.05f, Wave.Sine, 0.3f), Tone(rate, 1047, 1047, 0.07f, Wave.Sine, 0.3f));
                case Cue.Room: return Concat(Tone(rate, 220, 200, 0.05f, Wave.Triangle, 0.6f), Silence(rate, 0.05f), Tone(rate, 220, 200, 0.05f, Wave.Triangle, 0.6f));
                case Cue.CardTurn: return Sweep(rate, 0.14f);
                case Cue.Enemy: return Mix(Tone(rate, 110, 82, 0.35f, Wave.Square, 0.25f), Tone(rate, 116, 87, 0.35f, Wave.Square, 0.2f));
                case Cue.Trap: return Concat(Tone(rate, 880, 880, 0.08f, Wave.Square, 0.25f), Tone(rate, 660, 660, 0.08f, Wave.Square, 0.25f), Tone(rate, 880, 880, 0.08f, Wave.Square, 0.25f));
                case Cue.SafeRoom: return Concat(Tone(rate, 523, 523, 0.08f, Wave.Sine, 0.3f), Tone(rate, 659, 659, 0.08f, Wave.Sine, 0.3f), Tone(rate, 784, 784, 0.12f, Wave.Sine, 0.3f));
                case Cue.Dice: return Rattle(rate);
                case Cue.Damage: return Tone(rate, 300, 90, 0.25f, Wave.Square, 0.35f);
                case Cue.Heal: return Concat(Tone(rate, 440, 440, 0.07f, Wave.Sine, 0.35f), Tone(rate, 554, 554, 0.07f, Wave.Sine, 0.35f), Tone(rate, 659, 659, 0.1f, Wave.Sine, 0.35f));
                case Cue.Contamination: return Mix(Tone(rate, 200, 260, 0.3f, Wave.Sine, 0.3f), Noise(rate, 0.3f, 0.08f));
                case Cue.ItemGained: return Concat(Tone(rate, 700, 700, 0.05f, Wave.Triangle, 0.35f), Tone(rate, 1050, 1050, 0.08f, Wave.Triangle, 0.35f));
                case Cue.ItemLost: return Concat(Tone(rate, 1050, 1050, 0.05f, Wave.Triangle, 0.35f), Tone(rate, 700, 700, 0.08f, Wave.Triangle, 0.35f));
                case Cue.Select: return Tone(rate, 1200, 1600, 0.05f, Wave.Sine, 0.35f);
                case Cue.Deselect: return Tone(rate, 1600, 1200, 0.05f, Wave.Sine, 0.35f);
                case Cue.Time: return Concat(Tone(rate, 1000, 1000, 0.03f, Wave.Sine, 0.3f), Silence(rate, 0.08f), Tone(rate, 1000, 1000, 0.03f, Wave.Sine, 0.3f));
                case Cue.Night: return Tone(rate, 400, 200, 0.5f, Wave.Sine, 0.4f);
                default: return Tone(rate, 1000, 1000, 0.05f, Wave.Sine, 0.3f);
            }
        }

        /// <summary>A tone gliding from one frequency to another, with a short fade in and out.</summary>
        private static float[] Tone(int rate, float from, float to, float seconds, Wave wave, float volume)
        {
            int n = Mathf.Max(1, (int)(rate * seconds));
            var data = new float[n];
            double phase = 0;
            int fade = Mathf.Min(n / 4, (int)(rate * 0.006f) + 1);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                double f = from + (to - from) * t;
                phase += 2 * Math.PI * f / rate;
                float s;
                switch (wave)
                {
                    case Wave.Square: s = Math.Sin(phase) >= 0 ? 1f : -1f; break;
                    case Wave.Triangle: s = (float)(2 / Math.PI * Math.Asin(Math.Sin(phase))); break;
                    case Wave.Noise: s = (float)(random.NextDouble() * 2 - 1); break;
                    default: s = (float)Math.Sin(phase); break;
                }
                float env = 1f;
                if (i < fade) env = (float)i / fade;
                else if (i > n - fade) env = (float)(n - i) / fade;
                // A gentle decay so cues do not sound like beeps.
                env *= 1f - 0.5f * t;
                data[i] = s * env * volume;
            }
            return data;
        }

        private static float[] Noise(int rate, float seconds, float volume)
        {
            return Tone(rate, 0, 0, seconds, Wave.Noise, volume);
        }

        private static float[] Silence(int rate, float seconds)
        {
            return new float[Mathf.Max(1, (int)(rate * seconds))];
        }

        /// <summary>Noise that brightens as it goes: a card sliding over the table.</summary>
        private static float[] Sweep(int rate, float seconds)
        {
            float[] n = Noise(rate, seconds, 0.5f);
            float last = 0f;
            for (int i = 0; i < n.Length; i++)
            {
                float a = 0.05f + 0.6f * i / n.Length;
                last = last + a * (n[i] - last);
                n[i] = last;
            }
            return n;
        }

        /// <summary>A few short clicks of noise: dice rattling.</summary>
        private static float[] Rattle(int rate)
        {
            var parts = new List<float[]>();
            for (int i = 0; i < 6; i++)
            {
                parts.Add(Noise(rate, 0.018f, 0.5f - i * 0.05f));
                parts.Add(Silence(rate, 0.02f + 0.008f * i));
            }
            return Concat(parts.ToArray());
        }

        private static float[] Concat(params float[][] parts)
        {
            int total = 0;
            foreach (float[] p in parts) total += p.Length;
            var data = new float[total];
            int at = 0;
            foreach (float[] p in parts)
            {
                Array.Copy(p, 0, data, at, p.Length);
                at += p.Length;
            }
            return data;
        }

        private static float[] Mix(float[] a, float[] b)
        {
            var data = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < data.Length; i++)
            {
                float s = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
                data[i] = Mathf.Clamp(s, -1f, 1f);
            }
            return data;
        }
    }
}
