using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// O mapa: terreno low-poly com morros, um rio sinuoso, montanhas nas bordas
    /// e a clareira da fazenda no meio. Tudo vem de uma semente (igual para todos no co-op).
    /// </summary>
    public class World
    {
        public const float Half = 160f;          // mapa de 320 x 320 m
        public const float Cell = 2f;            // tamanho do triângulo do terreno
        public const int ChunkCells = 32;
        public const float WaterLevel = 0f;
        public static readonly Vector3 BaseCenter = new(0, 0, -34);
        public const float BaseRadius = 20f;

        public readonly int Seed;
        public readonly Noise noise;
        public Transform Root;

        public World(int seed)
        {
            Seed = seed;
            noise = new Noise(seed);
        }

        // ------------------------------------------------------------------ forma do terreno
        public float RiverZ(float x) => Mathf.Sin(x * 0.018f + Seed % 7) * 34f + Mathf.Sin(x * 0.051f) * 8f + 26f;
        public float RiverDist(float x, float z) => Mathf.Abs(z - RiverZ(x));

        public float Height(float x, float z)
        {
            float h = noise.Fbm(x, z, 4, 0.0085f) * 26f - 7f;
            h += noise.Fbm(x, z, 2, 0.05f, 3) * 2.2f;                       // relevo miúdo
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            h += Mathf.SmoothStep(0, 1, Mathf.InverseLerp(118f, Half, edge)) * 30f;   // serra na borda (limite do mapa)
            // rio
            float d = RiverDist(x, z);
            if (d < 16f) h = Mathf.Lerp(h, -2.6f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(16f, 4f, d)));
            // clareira da fazenda (plana)
            float b = Vector2.Distance(new Vector2(x, z), new Vector2(BaseCenter.x, BaseCenter.z));
            if (b < BaseRadius + 10f) h = Mathf.Lerp(h, 2.2f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(BaseRadius + 10f, BaseRadius - 4f, b)));
            return h;
        }

        public float ForestDensity(float x, float z) => noise.Fbm(x, z, 3, 0.022f, 5);

        // ------------------------------------------------------------------ altura EXATA da malha do terreno
        // O terreno é feito de triângulos (com vértices levemente deslocados); a função Height é a curva
        // "ideal". Para nada flutuar nem afundar, tudo que é colocado no chão usa Ground(), que lê o
        // triângulo de verdade embaixo do ponto.
        float[] gx, gy, gz;
        int gn;

        void EnsureGrid()
        {
            if (gx != null) return;
            int n = Mathf.RoundToInt(Half * 2 / Cell);
            gn = n + 1;
            gx = new float[gn * gn]; gy = new float[gn * gn]; gz = new float[gn * gn];
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float wx = -Half + i * Cell, wz = -Half + j * Cell;
                    if (!(i == 0 || j == 0 || i == n || j == n))
                    {
                        wx += (Hash(i, j) - 0.5f) * Cell * 0.35f;
                        wz += (Hash(j + 913, i) - 0.5f) * Cell * 0.35f;
                    }
                    int k = j * gn + i;
                    gx[k] = wx; gz[k] = wz; gy[k] = Height(wx, wz);
                }
        }

        Vector3 GV(int i, int j) { int k = j * gn + i; return new Vector3(gx[k], gy[k], gz[k]); }

        static bool InTri(float x, float z, Vector3 a, Vector3 b, Vector3 c, out float y)
        {
            float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            y = 0;
            if (Mathf.Abs(d) < 1e-8f) return false;
            float l1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / d;
            float l2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / d;
            float l3 = 1 - l1 - l2;
            const float e = -1e-4f;
            if (l1 < e || l2 < e || l3 < e) return false;
            y = l1 * a.y + l2 * b.y + l3 * c.y;
            return true;
        }

        /// <summary>Altura do chão exatamente como desenhado (triângulo embaixo do ponto).</summary>
        public float Ground(float x, float z)
        {
            EnsureGrid();
            int n = gn - 1;
            int ci = Mathf.FloorToInt((x + Half) / Cell), cj = Mathf.FloorToInt((z + Half) / Cell);
            for (int r = 0; r < 9; r++)
            {
                int i = ci + (r == 0 ? 0 : (r - 1) % 3 - 1), j = cj + (r == 0 ? 0 : (r - 1) / 3 - 1);
                if (i < 0 || j < 0 || i >= n || j >= n) continue;
                var a = GV(i, j); var b = GV(i + 1, j); var c = GV(i + 1, j + 1); var d = GV(i, j + 1);
                bool flip = ((i + j) & 1) == 0;
                float y;
                if (flip) { if (InTri(x, z, a, d, c, out y) || InTri(x, z, a, c, b, out y)) return y; }
                else { if (InTri(x, z, a, d, b, out y) || InTri(x, z, b, d, c, out y)) return y; }
            }
            return Height(x, z);
        }

        /// <summary>O ponto mais baixo do chão num círculo (para troncos e pedras não ficarem com a borda no ar).</summary>
        public float GroundMin(Vector3 p, float radius)
        {
            float m = Ground(p.x, p.z);
            for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * Mathf.PI * 2;
                m = Mathf.Min(m, Ground(p.x + Mathf.Cos(a) * radius, p.z + Mathf.Sin(a) * radius));
            }
            return m;
        }

        public Vector3 Normal(float x, float z)
        {
            const float e = 0.8f;
            float hl = Height(x - e, z), hr = Height(x + e, z), hd = Height(x, z - e), hu = Height(x, z + e);
            return new Vector3(hl - hr, 2 * e, hd - hu).normalized;
        }

        public bool InBase(Vector3 p) => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(BaseCenter.x, BaseCenter.z)) < BaseRadius;

        // ------------------------------------------------------------------ cores do chão
        // paleta "Omno": verdes saturados puxando para o amarelo na luz e para o turquesa na mata, pedras lilases
        static readonly Color Sand = Col.Hex("#ead39a"), Mud = Col.Hex("#8a6a4a"), GrassA = Col.Hex("#64ac48"), GrassB = Col.Hex("#93c853"),
            ForestFloor = Col.Hex("#3c7a50"), Rock = Col.Hex("#9a90a6"), RockD = Col.Hex("#7a708c"), Meadow = Col.Hex("#b0d05c"), Path = Col.Hex("#b08a5a");

        Color GroundColor(Vector3 p, Vector3 n, Rng rng)
        {
            float slope = 1 - n.y;
            float forest = ForestDensity(p.x, p.z);
            Color c = Color.Lerp(GrassA, GrassB, noise.Perlin(p.x * 0.07f, p.z * 0.07f, 6));
            c = Color.Lerp(c, ForestFloor, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.45f, 0.62f, forest)));
            if (InBase(p)) c = Color.Lerp(Meadow, GrassB, noise.Perlin(p.x * 0.2f, p.z * 0.2f, 7) * 0.6f);
            // trilha da fazenda até o rio
            float px = Mathf.Abs(p.x - Mathf.Sin(p.z * 0.06f) * 4f);
            if (px < 1.6f && p.z > BaseCenter.z + BaseRadius - 6 && p.z < RiverZ(p.x) - 8) c = Color.Lerp(c, Path, 0.85f);
            if (p.y < 1.1f) c = Color.Lerp(c, Sand, Mathf.InverseLerp(1.1f, 0.2f, p.y));
            if (p.y < -0.4f) c = Color.Lerp(Sand, Mud, Mathf.InverseLerp(-0.4f, -2.2f, p.y));
            if (slope > 0.35f || p.y > 16f) c = Color.Lerp(c, Color.Lerp(Rock, RockD, rng.Value), Mathf.InverseLerp(0.35f, 0.55f, Mathf.Max(slope, (p.y - 16f) / 12f)));
            return rng.Vary(c, 0.02f);
        }

        // ------------------------------------------------------------------ construção
        public void Build()
        {
            Root = new GameObject("Mundo").transform;
            EnsureGrid();
            int n = Mathf.RoundToInt(Half * 2 / Cell);
            int chunks = Mathf.CeilToInt(n / (float)ChunkCells);
            for (int cz = 0; cz < chunks; cz++)
                for (int cx = 0; cx < chunks; cx++)
                    BuildChunk(cx, cz, n);
            BuildWater();
        }

        /// <summary>
        /// Água só onde o terreno fica abaixo do nível (rio e poças), em triângulos irregulares para
        /// as ondas virarem facetas. A cor do vértice guarda a direção da correnteza (rg) para o shader.
        /// </summary>
        void BuildWater()
        {
            var mb = new MeshBuilder();
            const float S = 2.5f;
            int n = Mathf.RoundToInt(Half * 2 / S);
            Vector3 P(int i, int j)
            {
                float x = -Half + i * S + (Hash(i * 3, j) - 0.5f) * S * 0.45f, z = -Half + j * S + (Hash(j * 5, i) - 0.5f) * S * 0.45f;
                return new Vector3(x, WaterLevel, z);
            }
            Color Flow(Vector3 p)
            {
                // tangente do rio: derivada de RiverZ; perto do centro corre mais
                float e = 0.5f, dz = (RiverZ(p.x + e) - RiverZ(p.x - e)) / (2 * e);
                var d = new Vector2(1, dz).normalized;
                float k = Mathf.Clamp01(1 - RiverDist(p.x, p.z) / 12f);
                return new Color(0.5f + d.x * 0.5f * k, 0.5f + d.y * 0.5f * k, 0, 0);
            }
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    var a = P(i, j); var b = P(i + 1, j); var c = P(i + 1, j + 1); var d = P(i, j + 1);
                    // só onde há água por perto (algum canto abaixo do nível)
                    float lo = Mathf.Min(Mathf.Min(Height(a.x, a.z), Height(b.x, b.z)), Mathf.Min(Height(c.x, c.z), Height(d.x, d.z)));
                    if (lo > WaterLevel + 0.25f) continue;
                    mb.Tri(a, d, c, Flow(a), Flow(d), Flow(c));
                    mb.Tri(a, c, b, Flow(a), Flow(c), Flow(b));
                }
            if (mb.v.Count == 0) return;
            var go = Mats.Object("Agua", mb.ToMesh("agua"), Mats.Water, Root, false);
            go.GetComponent<Renderer>().receiveShadows = false;
        }

        void BuildChunk(int cx, int cz, int n)
        {
            var mb = new MeshBuilder();
            var rng = new Rng(Seed ^ (cx * 7919 + cz * 104729));
            int x0 = cx * ChunkCells, z0 = cz * ChunkCells;
            for (int z = z0; z < Mathf.Min(n, z0 + ChunkCells); z++)
                for (int x = x0; x < Mathf.Min(n, x0 + ChunkCells); x++)
                {
                    Vector3 P(int i, int j) => GV(i, j);     // mesmos vértices que Ground() usa
                    var a = P(x, z); var b = P(x + 1, z); var c = P(x + 1, z + 1); var d = P(x, z + 1);
                    bool flip = ((x + z) & 1) == 0;
                    if (flip) { AddTri(mb, a, d, c, rng); AddTri(mb, a, c, b, rng); }
                    else { AddTri(mb, a, d, b, rng); AddTri(mb, b, d, c, rng); }
                }
            var go = Mats.Object($"Terreno_{cx}_{cz}", mb.ToMesh("terreno"), Mats.LowPoly, Root);
            go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            go.layer = Layers.Ground;
        }

        void AddTri(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Rng rng)
        {
            var n = Vector3.Cross(b - a, c - a).normalized;
            var col = GroundColor((a + b + c) / 3f, n, rng);
            mb.Tri(a, b, c, Col.A(col, 0));
            // facetas bem de leve: a normal puxa para a curva "ideal" do morro
            int k = mb.v.Count - 3;
            mb.Soften(k, Normal(a.x, a.z), 0.55f); mb.Soften(k + 1, Normal(b.x, b.z), 0.55f); mb.Soften(k + 2, Normal(c.x, c.z), 0.55f);
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        /// <summary>Altura do chão por raycast (inclui detalhes que a função não sabe, como construções).</summary>
        public float GroundAt(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, 200, p.z), Vector3.down, out var hit, 400, 1 << Layers.Ground)) return hit.point.y;
            return Ground(p.x, p.z);
        }
    }

    public static class Layers
    {
        public const int Default = 0;
        public const int Ground = 8;      // camadas de usuário 8+ existem em qualquer projeto
        public const int Gather = 9;
        public const int Creature = 10;
    }
}
