using UnityEngine;

namespace Floresta
{
    /// <summary>Mola amortecida (movimento secundário: capa, mochila, quadril, cabeça).</summary>
    public struct Spring
    {
        public float x, v;
        /// <param name="freq">frequência em Hz (rigidez)</param>
        /// <param name="damp">1 = sem quicar, &lt;1 = quica</param>
        public float Step(float target, float freq, float damp, float dt)
        {
            float w = freq * 6.2832f;
            // sub-passos para ficar estável mesmo com quadros lentos
            int n = Mathf.Max(1, Mathf.CeilToInt(dt / 0.008f));
            float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                v += (-2f * damp * w * v - w * w * (x - target)) * h;
                x += v * h;
            }
            return x;
        }
    }

    public struct Spring3
    {
        public Vector3 x, v;
        public Vector3 Step(Vector3 target, float freq, float damp, float dt)
        {
            float w = freq * 6.2832f;
            int n = Mathf.Max(1, Mathf.CeilToInt(dt / 0.008f));
            float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                v += (-2f * damp * w * v - w * w * (x - target)) * h;
                x += v * h;
            }
            return x;
        }
    }

    public static class Ease
    {
        public static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        public static float In(float t) { t = Mathf.Clamp01(t); return t * t * t; }
        public static float Out(float t) { t = 1 - Mathf.Clamp01(t); return 1 - t * t * t; }
        public static float Frac(float x) => x - Mathf.Floor(x);

        /// <summary>Curva por quadros-chave: pares (tempo, valor) em ordem; interpola suave entre eles.</summary>
        public static float Key(float t, params float[] kv)
        {
            if (t <= kv[0]) return kv[1];
            for (int i = 0; i + 3 < kv.Length; i += 2)
                if (t <= kv[i + 2])
                    return Mathf.Lerp(kv[i + 1], kv[i + 3], Smooth((t - kv[i]) / Mathf.Max(1e-4f, kv[i + 2] - kv[i])));
            return kv[kv.Length - 1];
        }

        /// <summary>Ruído suave 1D (-1..1) para tiques e respiração irregular.</summary>
        public static float Noise(float x, float seed = 0) => Mathf.PerlinNoise(x, seed * 13.37f) * 2 - 1;
    }
}
