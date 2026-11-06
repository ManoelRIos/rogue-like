using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Vento do mundo. A direção gira devagar ao longo do dia; a força muda com rajadas que viajam pelo
    /// campo (a grama "ondula" quando uma rajada passa). O mesmo cálculo roda no shader (vértices) e aqui
    /// (folhas caindo, fumaça, poeira), então tudo se mexe junto.
    /// Também empurra a grama ao redor de quem anda por ela (jogadores e criaturas).
    /// </summary>
    public static class Wind
    {
        public static Vector2 Dir = new(0.8f, 0.6f);
        public static float Strength = 1;
        public static float Time;
        static readonly Vector4[] pushers = new Vector4[4];
        static readonly List<Vector4> pending = new();

        /// <summary>Vento em 3D para partículas (m/s), incluindo a rajada local.</summary>
        public static Vector3 At(Vector3 p)
        {
            float g = Gust(p.x, p.z);
            float s = Strength * (0.5f + g * 1.6f);
            return new Vector3(Dir.x * s, 0, Dir.y * s);
        }

        /// <summary>0..1: quanta rajada está passando aqui agora (igual ao shader).</summary>
        public static float Gust(float x, float z)
        {
            // faixas de rajada que andam a favor do vento (~12 m/s), quebradas na transversal
            float along = x * Dir.x + z * Dir.y, across = -x * Dir.y + z * Dir.x;
            float w = Mathf.Sin(along * 0.16f - Time * 1.9f + 0.9f * Mathf.Sin(across * 0.07f + Time * 0.13f));
            float m = 0.6f + 0.4f * Mathf.Sin(across * 0.05f - Time * 0.07f + along * 0.01f);
            float n = (0.5f + 0.5f * w) * m;
            n = Mathf.Clamp01((n - 0.35f) / 0.6f);
            return n * n * (3 - 2 * n);
        }

        /// <summary>Quem está pisando na grama neste quadro (posição + raio).</summary>
        public static void Push(Vector3 p, float radius) { if (pending.Count < 4) pending.Add(new Vector4(p.x, p.y, p.z, radius)); }

        public static void Update(float dt, float darkness)
        {
            Time += dt;
            // direção passeia devagar; à noite o vento fica mais forte e irregular
            float a = 0.65f + Mathf.Sin(Time * 0.004f) * 1.2f + (Mathf.PerlinNoise(Time * 0.01f, 3) - 0.5f) * 0.8f;
            Dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Strength = Mathf.Lerp(0.7f, 1.5f, Mathf.PerlinNoise(Time * 0.03f, 0)) * (1 + darkness * 0.5f);

            for (int i = 0; i < 4; i++) pushers[i] = i < pending.Count ? pending[i] : new Vector4(0, -9999, 0, 0.001f);
            pending.Clear();
            Shader.SetGlobalVectorArray("_FlorestaPushers", pushers);
            Shader.SetGlobalVector("_FlorestaWindDir", new Vector4(Dir.x, Dir.y, Strength, 0));
            Shader.SetGlobalFloat("_FlorestaWind", Strength);
            Shader.SetGlobalFloat("_FlorestaTime", Time);
        }
    }
}
