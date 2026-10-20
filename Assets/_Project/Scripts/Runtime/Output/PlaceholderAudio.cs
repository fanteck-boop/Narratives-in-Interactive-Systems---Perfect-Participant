using System.Collections.Generic;
using UnityEngine;

namespace PerfectParticipant
{
    // Procedural stand-in audio so the game is audible with zero recorded assets. Disclose in DRIS.
    public static class PlaceholderAudio
    {
        const int Rate = 22050;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        static AudioClip Make(string key, float[] d)
        {
            var c = AudioClip.Create(key, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Get(string key, System.Func<float[]> gen)
        {
            if (!cache.TryGetValue(key, out var c) || !c) cache[key] = c = Make(key, gen());
            return c;
        }

        // One syllable per letter, 0.06 s each (same rate as the subtitle estimate). Narrator low and buzzy, nudge high and soft.
        // ponytail: no real prosody, ignores baked silences (T3-N3), swap in recorded VO when it exists
        public static AudioClip Murmur(string id, string text, bool nudge) => Get((nudge ? "n_" : "v_") + id, () =>
        {
            int per = (int)(Rate * 0.06f);
            var d = new float[Mathf.Max(1, text.Length) * per];
            float ph = 0f, baseHz = nudge ? 330f : 110f, amp = nudge ? 0.15f : 0.3f;
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToLowerInvariant(text[i]);
                if (!char.IsLetterOrDigit(c)) continue;
                float hz = baseHz * (1f + (c % 7) * 0.09f), a = amp * ("aeiou".IndexOf(c) >= 0 ? 1f : 0.45f);
                for (int k = 0; k < per; k++)
                {
                    ph += 6.2831853f * hz / Rate;
                    float s = Mathf.Sin(ph) + (nudge ? 0f : 0.5f * Mathf.Sin(2f * ph) + 0.25f * Mathf.Sin(3f * ph));
                    d[i * per + k] = s * a * Mathf.Sin(Mathf.PI * k / per) * 0.6f;
                }
            }
            return d;
        });

        // Low thump: clipboard, door clunk. Lower hz and longer dur = heavier.
        public static AudioClip Thump(float hz, float dur) => Get("thump" + hz + "_" + dur, () =>
        {
            var d = new float[(int)(Rate * dur)];
            var r = new System.Random(3);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = (Mathf.Sin(6.2831853f * hz * t) * Mathf.Exp(-t * 9f / dur * 0.5f) + ((float)r.NextDouble() - 0.5f) * Mathf.Exp(-t * 60f)) * 0.8f;
            }
            return d;
        });

        public static AudioClip Click() => Get("click", () =>
        {
            var d = new float[(int)(Rate * 0.04f)];
            var r = new System.Random(5);
            for (int i = 0; i < d.Length; i++) d[i] = ((float)r.NextDouble() - 0.5f) * Mathf.Exp(-i * 0.004f);
            return d;
        });

        public static AudioClip Chime() => Get("chime", () =>
        {
            var d = new float[(int)(Rate * 1.4f)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float a = Mathf.Sin(6.2831853f * 880f * t) * Mathf.Exp(-t * 4f);
                float b = t > 0.15f ? Mathf.Sin(6.2831853f * 1318f * (t - 0.15f)) * Mathf.Exp(-(t - 0.15f) * 4f) : 0f;
                d[i] = (a + b) * 0.3f;
            }
            return d;
        });

        public static AudioClip Applause() => Get("applause", () =>
        {
            var d = new float[Rate * 4];
            var r = new System.Random(9);
            float env = 0f, tgt = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                if (i % 880 == 0) tgt = (float)r.NextDouble();
                env += (tgt - env) * 0.01f;
                float t = (float)i / d.Length;
                d[i] = ((float)r.NextDouble() - 0.5f) * env * Mathf.Sin(Mathf.PI * t) * 0.5f;
            }
            return d;
        });

        // Exactly 1 s, integer cycles of 100 and 200 Hz, loops clean.
        public static AudioClip Hum() => Get("hum", () =>
        {
            var d = new float[Rate];
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = (Mathf.Sin(6.2831853f * 100f * t) + 0.4f * Mathf.Sin(6.2831853f * 200f * t)) * 0.2f;
            }
            return d;
        });
    }
}
