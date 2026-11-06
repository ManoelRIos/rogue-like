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
        static AudioSource[] pool, pool3d;
        static int next, next3d;
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

        /// <summary>Ruído só numa faixa de frequência (lo..hi Hz).</summary>
        static void Band(float[] b, float dur, float vol, float lo, float hi, float delay = 0, float attack = 0.002f, float release = 2f)
        {
            int s0 = (int)(delay * SR), n = (int)(dur * SR);
            float l1 = 0, l2 = 0, a1 = Mathf.Clamp01(hi / SR * 6.28f), a2 = Mathf.Clamp01(lo / SR * 6.28f);
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float x = (float)rnd.NextDouble() * 2 - 1;
                l1 += (x - l1) * a1; l2 += (l1 - l2) * a2;
                float k = i / (float)n;
                b[s0 + i] += (l1 - l2) * vol * 3f * Mathf.Pow(1 - k, release) * Mathf.Min(1, i / (SR * attack));
            }
        }

        /// <summary>Tom com vibrato (pássaros, coruja).</summary>
        static void Whistle(float[] b, float f0, float f1, float dur, float vol, float delay, float vibHz = 0, float vibAmt = 0)
        {
            int s0 = (int)(delay * SR), n = (int)(dur * SR);
            double ph = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float k = i / (float)n;
                float f = Mathf.Lerp(f0, f1, k) * (1 + Mathf.Sin(i / (float)SR * vibHz * 6.28f) * vibAmt);
                ph += f / SR;
                float env = Mathf.Sin(k * Mathf.PI);
                b[s0 + i] += Mathf.Sin((float)(ph % 1) * 6.2832f) * vol * env * env;
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
            // sons com posição no mundo (passos, árvores caindo, bichos ao longe)
            pool3d = new AudioSource[20];
            for (int i = 0; i < pool3d.Length; i++)
            {
                var go = new GameObject("som3d");
                go.transform.SetParent(host.transform, false);
                pool3d[i] = Spatial(go.AddComponent<AudioSource>(), 2, 70);
            }

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

            // ---- sons novos: corpo, árvores, bichos, fogo e água
            b = Buf(0.35f); Band(b, 0.3f, 0.6f, 40, 500); Tone(b, 90, 50, 0.2f, 0.5f); Make("land", b);
            b = Buf(0.5f); Band(b, 0.35f, 0.5f, 600, 5000); for (int i = 0; i < 4; i++) Tone(b, 700 + i * 260, 1400 + i * 300, 0.05f, 0.08f, 0.04f + i * 0.06f); Make("splash", b);
            b = Buf(1.6f);
            for (int i = 0; i < 3; i++) Tone(b, 140 - i * 20, 95 - i * 15, 0.55f, 0.18f, i * 0.42f, 2);       // rangido da madeira
            Band(b, 1.5f, 0.12f, 300, 1800, 0, 0.3f, 1); Make("creak", b);
            b = Buf(1.6f); Tone(b, 70, 32, 1.2f, 0.8f); Band(b, 1.2f, 0.7f, 30, 380, 0, 0.004f, 3); Band(b, 0.9f, 0.35f, 800, 4000, 0.03f, 0.01f, 2.5f); Make("thud", b);
            b = Buf(1.2f); Band(b, 1.1f, 0.4f, 1500, 7000, 0, 0.15f, 1.5f); Make("leaves", b);
            b = Buf(0.6f); Whistle(b, 2600, 3300, 0.18f, 0.25f, 0); Whistle(b, 3300, 2500, 0.22f, 0.25f, 0.24f); Make("bird1", b);
            b = Buf(0.9f); for (int i = 0; i < 9; i++) Whistle(b, 3800, 4300, 0.05f, 0.18f, i * 0.07f); Make("bird2", b);
            b = Buf(0.8f); for (int i = 0; i < 4; i++) Whistle(b, 4200 - i * 400, 3000 - i * 300, 0.12f, 0.2f, i * 0.16f); Make("bird3", b);
            b = Buf(0.8f); for (int i = 0; i < 9; i++) { Band(b, 0.03f, 0.5f, 900, 4000, i * 0.055f, 0.001f, 4); Tone(b, 520, 380, 0.03f, 0.2f, i * 0.055f, 1); } Make("knock", b);
            b = Buf(1.6f); Whistle(b, 390, 360, 0.45f, 0.45f, 0, 5, 0.01f); Whistle(b, 370, 340, 0.75f, 0.45f, 0.62f, 5, 0.015f); Make("owl", b);
            b = Buf(0.6f); for (int i = 0; i < 2; i++) for (int k = 0; k < 6; k++) Tone(b, 210, 170, 0.03f, 0.22f, i * 0.22f + k * 0.028f, 1); Make("frog", b);
            b = Buf(0.3f); Band(b, 0.06f, 0.9f, 1200, 9000, 0, 0.0005f, 5); Band(b, 0.05f, 0.5f, 800, 6000, 0.07f, 0.0005f, 5); Make("twig", b);
            b = Buf(3.5f); Band(b, 3.4f, 0.35f, 120, 900, 0, 1.2f, 1.2f); Make("gust", b);
            b = Buf(0.25f); Band(b, 0.08f, 0.7f, 900, 8000, 0, 0.0005f, 4); Tone(b, 300, 120, 0.05f, 0.3f); Make("pop", b);
            // fogo crepitando (loop): ronco grave + estalinhos
            b = Buf(6f); Band(b, 6f, 0.12f, 60, 500, 0, 1f, 0.01f);
            for (int i = 0; i < 70; i++) Band(b, 0.02f, (float)rnd.NextDouble() * 0.35f + 0.1f, 1500, 9000, (float)rnd.NextDouble() * 5.9f, 0.0005f, 4);
            Make("crackle", b, true);
            // rio (loop): água corrente + bolhas
            b = Buf(8f); Band(b, 8f, 0.22f, 300, 2400, 0, 1f, 0.01f); Band(b, 8f, 0.1f, 2500, 7000, 0, 1f, 0.01f);
            for (int i = 0; i < 40; i++) { float d = (float)rnd.NextDouble() * 7.8f, f = 500 + (float)rnd.NextDouble() * 900; Tone(b, f, f * 1.6f, 0.04f, 0.05f, d); }
            Make("river", b, true);

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

        static AudioSource Spatial(AudioSource s, float min, float max)
        {
            s.playOnAwake = false;
            s.spatialBlend = 1;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = min; s.maxDistance = max;
            s.dopplerLevel = 0;
            return s;
        }

        /// <summary>Toca um som num ponto do mundo (ouve-se de onde veio).</summary>
        public static void PlayAt(string n, Vector3 at, float vol = 0.7f, float minGap = 0.02f, float maxDist = 70)
        {
            if (pool3d == null || !clips.TryGetValue(n, out var c)) return;
            float t = Time.unscaledTime;
            if (last.TryGetValue(n, out float l) && t - l < minGap) return;
            last[n] = t;
            var s = pool3d[next3d]; next3d = (next3d + 1) % pool3d.Length;
            s.transform.position = at;
            s.maxDistance = maxDist;
            s.pitch = 1 + Random.Range(-0.08f, 0.08f);
            s.PlayOneShot(c, vol);
        }

        /// <summary>Som contínuo preso num lugar (fogueira, rio).</summary>
        public static AudioSource Loop3D(string n, Vector3 at, float min, float max)
        {
            if (!clips.TryGetValue(n, out var c)) return null;
            var go = new GameObject("loop_" + n);
            go.transform.position = at;
            var s = Spatial(go.AddComponent<AudioSource>(), min, max);
            s.clip = c; s.loop = true; s.volume = 0.8f; s.Play();
            return s;
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
