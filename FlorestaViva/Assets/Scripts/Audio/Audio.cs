using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>Sons sintetizados (sem arquivos): efeitos curtos + ambientes em loop de dia e de noite.</summary>
    public static class Audio
    {
        const int SR = 22050;
        static readonly Dictionary<string, AudioClip> clips = new();
        static readonly Dictionary<string, float> last = new();
        static AudioSource[] pool;
        static int next;
        static AudioSource dayAmb, nightAmb, heart;
        static System.Random rnd = new(7);

        static float[] Buf(float s) => new float[Mathf.CeilToInt(s * SR)];

        static void Tone(float[] b, float f0, float f1, float dur, float vol, float delay = 0, int wave = 0)
        {
            int s0 = (int)(delay * SR), n = (int)(dur * SR);
            double ph = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float k = i / (float)n;
                ph += (f0 + (f1 - f0) * k) / SR;
                float p = (float)(ph % 1);
                float v = wave == 0 ? Mathf.Sin(p * 6.2832f) : wave == 1 ? (p < 0.5f ? 1 : -1) : p * 2 - 1;
                b[s0 + i] += v * vol * Mathf.Pow(1 - k, 1.5f) * Mathf.Min(1, i / (SR * 0.004f));
            }
        }

        static void Noise(float[] b, float dur, float vol, float cutoff, float delay = 0, float attack = 0.002f)
        {
            int s0 = (int)(delay * SR), n = (int)(dur * SR);
            float lp = 0, a = Mathf.Clamp01(cutoff / SR * 6.28f);
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                lp += ((float)rnd.NextDouble() * 2 - 1 - lp) * a;
                float k = i / (float)n;
                b[s0 + i] += lp * vol * 2.5f * Mathf.Pow(1 - k, 2) * Mathf.Min(1, i / (SR * attack));
            }
        }

        static AudioClip Make(string n, float[] b, bool loopSafe = false)
        {
            if (loopSafe)   // cruza o fim com o começo para o loop não estalar
            {
                int f = SR / 4;
                for (int i = 0; i < f; i++) { float k = i / (float)f; b[i] = b[i] * k + b[b.Length - f + i] * (1 - k); }
                System.Array.Resize(ref b, b.Length - f);
            }
            for (int i = 0; i < b.Length; i++) b[i] = Mathf.Clamp(b[i], -1, 1);
            var c = AudioClip.Create(n, b.Length, 1, SR, false);
            c.SetData(b, 0);
            clips[n] = c;
            return c;
        }

        public static void Init(GameObject host)
        {
            if (pool != null) return;
            pool = new AudioSource[16];
            for (int i = 0; i < pool.Length; i++) { pool[i] = host.AddComponent<AudioSource>(); pool[i].playOnAwake = false; }

            float[] b;
            b = Buf(0.25f); Noise(b, 0.2f, 0.5f, 1800); Tone(b, 160, 90, 0.12f, 0.4f); Make("chop", b);
            b = Buf(0.25f); Noise(b, 0.15f, 0.4f, 4000); Tone(b, 900, 600, 0.08f, 0.25f, 0, 1); Make("mine", b);
            b = Buf(0.25f); Noise(b, 0.22f, 0.3f, 2500, 0, 0.03f); Make("rustle", b);
            b = Buf(1.2f); Noise(b, 1.1f, 0.6f, 500, 0, 0.1f); Tone(b, 90, 40, 0.9f, 0.4f, 0.2f); Make("fall", b);
            b = Buf(0.2f); Tone(b, 660, 990, 0.08f, 0.2f); Tone(b, 990, 1320, 0.08f, 0.2f, 0.07f); Make("pickup", b);
            b = Buf(0.12f); Noise(b, 0.08f, 0.25f, 700); Make("step", b);
            b = Buf(0.45f); Tone(b, 320, 90, 0.4f, 0.45f, 0, 2); Noise(b, 0.3f, 0.4f, 900); Make("hurt", b);
            b = Buf(0.9f); Tone(b, 900, 300, 0.8f, 0.3f, 0, 2); Tone(b, 1300, 500, 0.7f, 0.2f, 0.05f, 1); Noise(b, 0.8f, 0.3f, 3000); Make("screech", b);
            b = Buf(0.35f); Noise(b, 0.3f, 0.5f, 1200); Tone(b, 200, 120, 0.2f, 0.3f); Make("hit", b);
            b = Buf(0.5f); Tone(b, 523, 523, 0.15f, 0.25f); Tone(b, 659, 659, 0.15f, 0.25f, 0.12f); Tone(b, 784, 784, 0.25f, 0.25f, 0.24f); Make("eat", b);
            b = Buf(2.5f); Tone(b, 110, 104, 2.4f, 0.5f, 0, 2); Tone(b, 82, 80, 2.4f, 0.4f, 0.3f, 2); Tone(b, 55, 55, 2.4f, 0.4f, 0.6f); Make("nightfall", b);
            b = Buf(2f); for (int i = 0; i < 5; i++) Tone(b, 392 + i * 98, 392 + i * 98, 0.5f, 0.15f, i * 0.25f); Make("dawn", b);
            b = Buf(1.2f); for (int i = 0; i < 3; i++) Noise(b, 0.5f, 0.25f, 2600, i * 0.3f, 0.15f); Make("whisper", b);
            // batimento cardíaco (fica tocando quando há criatura perto)
            b = Buf(1f); Tone(b, 60, 40, 0.12f, 0.9f); Tone(b, 55, 38, 0.12f, 0.7f, 0.22f); Make("heart", b);

            // ambiente de dia: vento + passarinhos
            b = Buf(12f); Noise(b, 12f, 0.12f, 400, 0, 2f);
            for (int i = 0; i < 18; i++)
            {
                float d = (float)rnd.NextDouble() * 11f, f = 2400 + (float)rnd.NextDouble() * 1800;
                for (int k = 0; k < 3; k++) Tone(b, f, f * 1.25f, 0.06f, 0.07f, d + k * 0.09f);
            }
            var dayClip = Make("ambDay", b, true);
            // ambiente de noite: grilos + zumbido grave + rangidos
            b = Buf(14f); Tone(b, 48, 46, 14f, 0.25f); Noise(b, 14f, 0.1f, 250, 0, 3f);
            for (int i = 0; i < 120; i++) { float d = (float)rnd.NextDouble() * 13.8f; Tone(b, 4200, 4100, 0.03f, 0.03f, d, 1); }
            for (int i = 0; i < 4; i++) { float d = (float)rnd.NextDouble() * 12f; Tone(b, 180, 120, 1.2f, 0.06f, d, 2); }
            var nightClip = Make("ambNight", b, true);

            dayAmb = Loop(host, dayClip); nightAmb = Loop(host, nightClip); heart = Loop(host, clips["heart"]);
        }

        static AudioSource Loop(GameObject host, AudioClip c)
        {
            var s = host.AddComponent<AudioSource>();
            s.clip = c; s.loop = true; s.volume = 0; s.playOnAwake = false; s.Play();
            return s;
        }

        /// <summary>Mistura dos ambientes conforme a escuridão e o perigo (0..1).</summary>
        public static void Mix(float darkness, float danger)
        {
            if (dayAmb == null) return;
            dayAmb.volume = Mathf.Lerp(dayAmb.volume, (1 - darkness) * 0.5f, 0.05f);
            nightAmb.volume = Mathf.Lerp(nightAmb.volume, darkness * 0.6f, 0.05f);
            heart.volume = Mathf.Lerp(heart.volume, danger * 0.8f, 0.1f);
            heart.pitch = 0.9f + danger * 0.6f;
        }

        public static void Play(string n, float vol = 0.7f, float minGap = 0.05f)
        {
            if (pool == null || !clips.TryGetValue(n, out var c)) return;
            float t = Time.unscaledTime;
            if (last.TryGetValue(n, out float l) && t - l < minGap) return;
            last[n] = t;
            var s = pool[next]; next = (next + 1) % pool.Length;
            s.pitch = 1 + Random.Range(-0.06f, 0.06f);
            s.PlayOneShot(c, vol);
        }
    }
}
