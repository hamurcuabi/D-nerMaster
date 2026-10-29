using UnityEngine;

namespace DonerMaster
{
    /// <summary>
    /// Synthesises every placeholder sound at startup so the prototype has full audio feedback
    /// with zero asset files. Swap any of these for recorded clips later (see AudioManager).
    /// </summary>
    public static class ProceduralAudio
    {
        const int Rate = 44100;
        static System.Random _rng = new System.Random(1234);

        static float Noise() => (float)(_rng.NextDouble() * 2.0 - 1.0);

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Normalize(float[] d, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
            float k = peak / max;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }

        /// <summary>Crossfade the tail into the head so the clip loops without a click.</summary>
        static float[] MakeLoop(float[] d, float fadeSeconds)
        {
            int fade = Mathf.Min((int)(fadeSeconds * Rate), d.Length / 3);
            int len = d.Length - fade;
            var o = new float[len];
            for (int i = 0; i < len; i++) o[i] = d[i];
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                o[i] = d[i] * k + d[len + i] * (1f - k);
            }
            return o;
        }

        public static AudioClip Sizzle()
        {
            int n = (int)(2.6f * Rate);
            var d = new float[n];
            float hpY = 0, hpX = 0, lp = 0, mod = 0.5f, pop = 0;
            for (int i = 0; i < n; i++)
            {
                float x = Noise();
                hpY = 0.96f * (hpY + x - hpX); hpX = x;          // high-pass: frying hiss
                lp += (hpY - lp) * 0.55f;                         // soften the very top
                if (i % 400 == 0) mod = Mathf.Lerp(mod, 0.35f + (float)_rng.NextDouble() * 0.65f, 0.6f);
                if (_rng.NextDouble() < 0.0009) pop = (float)(_rng.NextDouble() * 0.9 + 0.3) * (_rng.NextDouble() < 0.5 ? -1f : 1f);
                pop *= 0.93f;                                    // fat crackles
                d[i] = lp * 0.35f * mod + pop * Noise();
            }
            d = MakeLoop(d, 0.25f);
            Normalize(d, 0.6f);
            return Make("sizzle", d);
        }

        public static AudioClip Slice()
        {
            int n = (int)(0.16f * Rate);
            var d = new float[n];
            float lp = 0, hpY = 0, hpX = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * 28f);
                float x = Noise();
                lp += (x - lp) * 0.35f;
                hpY = 0.9f * (hpY + lp - hpX); hpX = lp;
                // a soft "fleshy" body under the blade scrape
                float body = Mathf.Sin(2f * Mathf.PI * (180f - t * 600f) * t) * Mathf.Exp(-t * 40f) * 0.5f;
                d[i] = (hpY * 0.9f + body) * env;
            }
            Normalize(d, 0.7f);
            return Make("slice", d);
        }

        public static AudioClip Pop()
        {
            int n = (int)(0.1f * Rate);
            var d = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(560f, 170f, t / 0.1f);
                phase += 2f * Mathf.PI * f / Rate;
                d[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 38f) * Mathf.Min(1f, t / 0.002f);
            }
            Normalize(d, 0.6f);
            return Make("pop", d);
        }

        public static AudioClip Paper()
        {
            int n = (int)(0.38f * Rate);
            var d = new float[n];
            float lp = 0, gate = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                if (i % 700 == 0) gate = (float)_rng.NextDouble();
                float x = Noise();
                lp += (x - lp) * 0.25f;
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.38f) * Mathf.PI);
                d[i] = lp * env * (0.3f + gate * 0.7f);
            }
            Normalize(d, 0.45f);
            return Make("paper", d);
        }

        public static AudioClip Coin()
        {
            int n = (int)(0.55f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float a = Mathf.Sin(2f * Mathf.PI * 1568f * t) * Mathf.Exp(-t * 9f);
                float t2 = t - 0.07f;
                float b = t2 > 0 ? Mathf.Sin(2f * Mathf.PI * 2093f * t2) * Mathf.Exp(-t2 * 7f) : 0f;
                float click = t < 0.004f ? Noise() * (1f - t / 0.004f) : 0f;
                d[i] = (a * 0.6f + b * 0.7f + click * 0.3f) * Mathf.Min(1f, t / 0.001f);
            }
            Normalize(d, 0.55f);
            return Make("coin", d);
        }

        public static AudioClip Ding()
        {
            int n = (int)(0.8f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 5.5f) * Mathf.Min(1f, t / 0.002f);
                d[i] = (Mathf.Sin(2f * Mathf.PI * 1046.5f * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * 2093f * t) + 0.15f * Mathf.Sin(2f * Mathf.PI * 3139f * t)) * env;
            }
            Normalize(d, 0.5f);
            return Make("ding", d);
        }

        public static AudioClip Bell()
        {
            int n = (int)(0.9f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float a = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * Mathf.Exp(-t * 4f);
                float t2 = t - 0.16f;
                float b = t2 > 0 ? Mathf.Sin(2f * Mathf.PI * 1046.5f * t2) * Mathf.Exp(-t2 * 4f) : 0f;
                d[i] = (a + b) * Mathf.Min(1f, t / 0.002f);
            }
            Normalize(d, 0.35f);
            return Make("bell", d);
        }

        public static AudioClip Buzz()
        {
            int n = (int)(0.24f * Rate);
            var d = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 115f * t));
                lp += (sq - lp) * 0.08f;
                float env = Mathf.Min(1f, t / 0.005f) * Mathf.Clamp01((0.24f - t) / 0.05f);
                d[i] = lp * env;
            }
            Normalize(d, 0.4f);
            return Make("buzz", d);
        }

        public static AudioClip Tick()
        {
            int n = (int)(0.05f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                d[i] = (Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.6f + Noise() * 0.4f) * Mathf.Exp(-t * 120f);
            }
            Normalize(d, 0.35f);
            return Make("tick", d);
        }

        public static AudioClip Whoosh()
        {
            int n = (int)(0.3f * Rate);
            var d = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float cutoff = Mathf.Lerp(0.02f, 0.3f, Mathf.Sin(k * Mathf.PI));
                lp += (Noise() - lp) * cutoff;
                d[i] = lp * Mathf.Sin(k * Mathf.PI);
            }
            Normalize(d, 0.45f);
            return Make("whoosh", d);
        }

        public static AudioClip Mmm()
        {
            int n = (int)(0.55f * Rate);
            var d = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = 205f + Mathf.Sin(t * 2f * Mathf.PI * 5f) * 6f + t * 30f;
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.55f) * Mathf.PI);
                d[i] = (Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2f) + 0.12f * Mathf.Sin(phase * 3f)) * env;
            }
            Normalize(d, 0.45f);
            return Make("mmm", d);
        }

        public static AudioClip Grumble()
        {
            int n = (int)(0.45f * Rate);
            var d = new float[n];
            float phase = 0, lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = 105f - t * 60f + Mathf.Sin(t * 2f * Mathf.PI * 9f) * 8f;
                phase += f / Rate;
                float saw = 2f * (phase - Mathf.Floor(phase)) - 1f;
                lp += (saw - lp) * 0.12f;
                float env = Mathf.Min(1f, t / 0.02f) * Mathf.Clamp01((0.45f - t) / 0.12f);
                d[i] = lp * env;
            }
            Normalize(d, 0.45f);
            return Make("grumble", d);
        }

        public static AudioClip StreetAmbience()
        {
            int n = (int)(6f * Rate);
            var d = new float[n];
            float brown = 0, lp = 0, swell = 0;
            for (int i = 0; i < n; i++)
            {
                brown = Mathf.Clamp(brown + Noise() * 0.02f, -1f, 1f) * 0.999f;
                lp += (brown - lp) * 0.05f;
                float t = i / (float)Rate;
                swell = 0.6f + 0.4f * Mathf.Sin(t * 0.9f) * Mathf.Sin(t * 0.37f + 1f);
                d[i] = lp * swell;
            }
            d = MakeLoop(d, 0.8f);
            Normalize(d, 0.35f);
            return Make("street", d);
        }
    }
}
