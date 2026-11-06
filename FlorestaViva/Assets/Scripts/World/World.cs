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

        public Vector3 Normal(float x, float z)
        {
            const float e = 0.8f;
            float hl = Height(x - e, z), hr = Height(x + e, z), hd = Height(x, z - e), hu = Height(x, z + e);
            return new Vector3(hl - hr, 2 * e, hd - hu).normalized;
        }

        public bool InBase(Vector3 p) => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(BaseCenter.x, BaseCenter.z)) < BaseRadius;

        // ------------------------------------------------------------------ cores do chão
        static readonly Color Sand = Col.Hex("#c9b27a"), Mud = Col.Hex("#6b5236"), GrassA = Col.Hex("#5f8f3a"), GrassB = Col.Hex("#7aa443"),
            ForestFloor = Col.Hex("#3f5e2a"), Rock = Col.Hex("#7a7468"), RockD = Col.Hex("#5e5950"), Meadow = Col.Hex("#9cb04f"), Path = Col.Hex("#8a6e48");

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
            int n = Mathf.RoundToInt(Half * 2 / Cell);
            int chunks = Mathf.CeilToInt(n / (float)ChunkCells);
            for (int cz = 0; cz < chunks; cz++)
                for (int cx = 0; cx < chunks; cx++)
                    BuildChunk(cx, cz, n);
            // água
            var mb = new MeshBuilder();
            float W = Half;
            int seg = 40;
            for (int z = 0; z < seg; z++)
                for (int x = 0; x < seg; x++)
                {
                    float x0 = -W + x * 2 * W / seg, x1 = x0 + 2 * W / seg, z0 = -W + z * 2 * W / seg, z1 = z0 + 2 * W / seg;
                    mb.Quad(new Vector3(x0, WaterLevel, z0), new Vector3(x0, WaterLevel, z1), new Vector3(x1, WaterLevel, z1), new Vector3(x1, WaterLevel, z0), Color.white);
                }
            Mats.Object("Agua", mb.ToMesh("agua"), Mats.Water, Root, false);
        }

        void BuildChunk(int cx, int cz, int n)
        {
            var mb = new MeshBuilder();
            var rng = new Rng(Seed ^ (cx * 7919 + cz * 104729));
            int x0 = cx * ChunkCells, z0 = cz * ChunkCells;
            for (int z = z0; z < Mathf.Min(n, z0 + ChunkCells); z++)
                for (int x = x0; x < Mathf.Min(n, x0 + ChunkCells); x++)
                {
                    Vector3 P(int i, int j)
                    {
                        float wx = -Half + i * Cell, wz = -Half + j * Cell;
                        // pequeno deslocamento determinístico para quebrar a grade (menos "quadriculado")
                        float jx = (Hash(i, j) - 0.5f) * Cell * 0.35f, jz = (Hash(j + 913, i) - 0.5f) * Cell * 0.35f;
                        if (i == 0 || j == 0 || i == n || j == n) { jx = 0; jz = 0; }
                        wx += jx; wz += jz;
                        return new Vector3(wx, Height(wx, wz), wz);
                    }
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
            return Height(p.x, p.z);
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
