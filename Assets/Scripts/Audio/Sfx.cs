using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>Efeitos sonoros sintetizados na inicialização (sem arquivos de áudio).</summary>
    public static class Sfx
    {
        const int SR = 22050;
        static readonly Dictionary<string, AudioClip> clips = new();
        static readonly Dictionary<string, float> last = new();
        static AudioSource[] sources;
        static int next;
        public static float Volume = 0.55f;

        enum Wave { Square, Saw, Tri, Sine }

        class Synth
        {
            public float[] buf;
            public Synth(float seconds) { buf = new float[Mathf.CeilToInt(seconds * SR)]; }

            public Synth Tone(Wave w, float f0, float f1, float dur, float vol, float delay = 0)
            {
                int s0 = (int)(delay * SR), n = (int)(dur * SR);
                double ph = 0;
                for (int i = 0; i < n && s0 + i < buf.Length; i++)
                {
                    float k = i / (float)n;
                    float f = f0 * Mathf.Pow(Mathf.Max(20, f1) / f0, k);
                    ph += f / SR;
                    float p = (float)(ph % 1.0);
                    float v = w switch
                    {
                        Wave.Square => p < 0.5f ? 1 : -1,
                        Wave.Saw => p * 2 - 1,
                        Wave.Tri => 1 - 4 * Mathf.Abs(p - 0.5f),
                        _ => Mathf.Sin(p * Mathf.PI * 2),
                    };
                    float env = Mathf.Pow(1 - k, 1.6f) * Mathf.Min(1, i / (SR * 0.003f));
                    buf[s0 + i] += v * vol * env;
                }
                return this;
            }

            public Synth Noise(float dur, float vol, float cutoff, float delay = 0, bool high = false)
            {
                int s0 = (int)(delay * SR), n = (int)(dur * SR);
                float lp = 0, a = Mathf.Clamp01(cutoff / SR * 6.28f);
                var rnd = new System.Random(n);
                for (int i = 0; i < n && s0 + i < buf.Length; i++)
                {
                    float x = (float)rnd.NextDouble() * 2 - 1;
                    lp += (x - lp) * a;
                    float v = high ? x - lp : lp;
                    float k = i / (float)n;
                    buf[s0 + i] += v * vol * Mathf.Pow(1 - k, 2f) * (high ? 1 : 2.2f);
                }
                return this;
            }

            public AudioClip Make(string name)
            {
                for (int i = 0; i < buf.Length; i++) buf[i] = Mathf.Clamp(buf[i], -1, 1);
                var c = AudioClip.Create(name, buf.Length, 1, SR, false);
                c.SetData(buf, 0);
                return c;
            }
        }

        public static void Init(GameObject host)
        {
            if (sources != null) return;
            sources = new AudioSource[16];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = host.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0;
            }
            void Add(string n, Synth s) => clips[n] = s.Make(n);

            Add("shoot", new Synth(0.1f).Tone(Wave.Square, 900, 220, 0.07f, 0.25f).Noise(0.05f, 0.25f, 3000));
            Add("shotgun", new Synth(0.25f).Noise(0.22f, 0.6f, 1500).Tone(Wave.Saw, 200, 60, 0.15f, 0.3f));
            Add("heavy", new Synth(0.32f).Noise(0.3f, 0.6f, 900).Tone(Wave.Saw, 320, 40, 0.25f, 0.35f));
            Add("laser", new Synth(0.08f).Tone(Wave.Saw, 1800, 600, 0.06f, 0.18f));
            Add("bubble", new Synth(0.12f).Tone(Wave.Sine, 300, 900, 0.1f, 0.35f));
            Add("enemyShoot", new Synth(0.08f).Tone(Wave.Tri, 520, 300, 0.07f, 0.16f));
            Add("hit", new Synth(0.06f).Tone(Wave.Square, 260, 120, 0.05f, 0.2f));
            Add("enemyDie", new Synth(0.28f).Noise(0.25f, 0.45f, 800).Tone(Wave.Square, 400, 80, 0.2f, 0.2f));
            Add("hurt", new Synth(0.4f).Tone(Wave.Saw, 300, 70, 0.35f, 0.5f).Noise(0.3f, 0.5f, 600));
            Add("roll", new Synth(0.18f).Noise(0.16f, 0.35f, 500));
            Add("reload", new Synth(0.2f).Tone(Wave.Square, 1200, 1100, 0.03f, 0.15f).Tone(Wave.Square, 700, 650, 0.04f, 0.15f, 0.12f));
            Add("empty", new Synth(0.05f).Tone(Wave.Square, 200, 180, 0.04f, 0.15f));
            Add("blank", new Synth(0.65f).Tone(Wave.Sine, 120, 1400, 0.5f, 0.6f).Noise(0.6f, 0.4f, 4000, 0, true));
            Add("pickup", new Synth(0.16f).Tone(Wave.Square, 880, 880, 0.06f, 0.18f).Tone(Wave.Square, 1320, 1320, 0.08f, 0.18f, 0.06f));
            Add("coin", new Synth(0.11f).Tone(Wave.Square, 1500, 1500, 0.04f, 0.12f).Tone(Wave.Square, 2000, 2000, 0.06f, 0.12f, 0.04f));
            var heal = new Synth(0.35f); for (int i = 0; i < 3; i++) heal.Tone(Wave.Tri, new[] { 523f, 659f, 784f }[i], new[] { 523f, 659f, 784f }[i], 0.12f, 0.3f, i * 0.07f);
            Add("heal", heal);
            Add("door", new Synth(0.32f).Noise(0.3f, 0.4f, 300).Tone(Wave.Square, 90, 60, 0.25f, 0.25f));
            var chest = new Synth(0.45f); float[] cn = { 392, 523, 659, 784, 1046 }; for (int i = 0; i < 5; i++) chest.Tone(Wave.Square, cn[i], cn[i], 0.1f, 0.18f, i * 0.06f);
            Add("chest", chest);
            Add("explode", new Synth(0.55f).Noise(0.5f, 0.8f, 400).Tone(Wave.Sine, 120, 30, 0.45f, 0.6f));
            Add("crate", new Synth(0.2f).Noise(0.18f, 0.5f, 900));
            var boss = new Synth(1.3f); float[] bn = { 110, 98, 82 }; for (int i = 0; i < 3; i++) boss.Tone(Wave.Saw, bn[i], bn[i] * 0.98f, 0.5f, 0.35f, i * 0.35f);
            Add("boss", boss);
            var stairs = new Synth(0.5f); float[] sn = { 784, 659, 523, 392 }; for (int i = 0; i < 4; i++) stairs.Tone(Wave.Tri, sn[i], sn[i], 0.14f, 0.3f, i * 0.09f);
            Add("stairs", stairs);
            var vic = new Synth(1f); float[] vn = { 523, 659, 784, 1046, 784, 1046 }; for (int i = 0; i < 6; i++) vic.Tone(Wave.Square, vn[i], vn[i], 0.18f, 0.2f, i * 0.13f);
            Add("victory", vic);
            Add("buy", new Synth(0.16f).Tone(Wave.Square, 1000, 1000, 0.05f, 0.18f).Tone(Wave.Square, 1500, 1500, 0.1f, 0.18f, 0.05f));
            Add("deny", new Synth(0.16f).Tone(Wave.Square, 150, 120, 0.15f, 0.2f));
            Add("select", new Synth(0.08f).Tone(Wave.Square, 660, 990, 0.06f, 0.15f));
        }

        public static void Play(string name, float minGap = 0.03f)
        {
            if (sources == null || !clips.TryGetValue(name, out var clip)) return;
            float now = Time.unscaledTime;
            if (last.TryGetValue(name, out float l) && now - l < minGap) return;
            last[name] = now;
            var s = sources[next];
            next = (next + 1) % sources.Length;
            s.pitch = 1f + Random.Range(-0.04f, 0.04f);
            s.PlayOneShot(clip, Volume);
        }
    }
}
