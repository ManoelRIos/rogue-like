using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Toques de atmosfera "Omno": rastros de vento (fitas brancas que serpenteiam a favor do vento perto de você)
    /// e feixes de sol atravessando a mata nas horas de luz baixa. Tudo numa malha só, refeita a cada quadro.
    /// </summary>
    public class Atmosphere
    {
        class Trail { public Vector3 start; public float age, life, phase, len, speed; }

        readonly World world;
        readonly List<Trail> trails = new();
        readonly Mesh mesh;
        readonly List<Vector3> v = new();
        readonly List<Color> c = new();
        readonly List<int> t = new();
        float spawnT;

        public Atmosphere(World w)
        {
            world = w;
            mesh = new Mesh { name = "atmosfera" };
            mesh.MarkDynamic();
            var go = Mats.Object("atmosfera", mesh, Mats.Ribbon, w.Root, false);
            go.GetComponent<Renderer>().receiveShadows = false;
        }

        public void Update(float dt, Camera cam, float darkness, float time, Vector3 sunDir)
        {
            v.Clear(); c.Clear(); t.Clear();
            var cp = cam.transform.position;
            float day = 1 - darkness;

            // ---------------- rastros de vento (mais com vento forte; quase nenhum na noite fechada)
            spawnT -= dt * (0.6f + Wind.Strength * 1.4f) * Mathf.Lerp(0.15f, 1f, day);
            if (spawnT <= 0 && trails.Count < 9)
            {
                spawnT = Random.Range(0.4f, 1.2f);
                var d = Random.insideUnitCircle * 22f;
                var s = new Vector3(cp.x + d.x, 0, cp.z + d.y);
                s.y = world.Ground(s.x, s.z) + Random.Range(0.6f, 5f);
                trails.Add(new Trail { start = s, life = Random.Range(2.2f, 3.8f), phase = Random.Range(0, 6.28f), len = Random.Range(0.7f, 1.3f), speed = Random.Range(4.5f, 7f) });
            }
            var wd = new Vector3(Wind.Dir.x, 0, Wind.Dir.y);
            var perp = new Vector3(-wd.z, 0, wd.x);
            for (int i = trails.Count - 1; i >= 0; i--)
            {
                var tr = trails[i];
                tr.age += dt;
                if (tr.age >= tr.life) { trails.RemoveAt(i); continue; }
                float env = Mathf.Sin(tr.age / tr.life * Mathf.PI);
                Vector3 P(float tau) => tr.start + wd * tau * tr.speed + perp * Mathf.Sin(tau * 1.9f + tr.phase) * 1.1f + Vector3.up * (Mathf.Sin(tau * 1.3f + tr.phase * 2) * 0.5f + tau * 0.15f);
                const int S = 18;
                float head = tr.age, tail = Mathf.Max(0, tr.age - tr.len);
                int b0 = v.Count;
                for (int k = 0; k <= S; k++)
                {
                    float u = k / (float)S, tau = Mathf.Lerp(tail, head, u);
                    var p = P(tau);
                    var nx = P(tau + 0.02f) - p;
                    var w = Vector3.Cross(nx.normalized, (p - cp).normalized).normalized * 0.025f * (0.4f + Mathf.Sin(u * Mathf.PI));
                    float a = env * Mathf.Sin(u * Mathf.PI) * 0.55f * day + env * Mathf.Sin(u * Mathf.PI) * 0.12f;
                    var col = new Color(1f, 0.98f, 0.92f, a);
                    v.Add(p - w); v.Add(p + w); c.Add(col); c.Add(col);
                    if (k > 0) { int q = b0 + (k - 1) * 2; t.Add(q); t.Add(q + 2); t.Add(q + 1); t.Add(q + 1); t.Add(q + 2); t.Add(q + 3); }
                }
            }

            // ---------------- feixes de sol na mata (manhã e fim de tarde, quando o sol está baixo)
            float low = Mathf.Clamp01(1 - Mathf.Abs(sunDir.y - 0.35f) / 0.3f) * day * (sunDir.y > 0.05f ? 1 : 0);
            if (low > 0.02f)
            {
                var sunCol = Color.Lerp(new Color(1f, 0.92f, 0.7f), new Color(1f, 0.72f, 0.45f), 1 - Mathf.Clamp01(sunDir.y * 2.5f));
                const float C = 9f;
                int cx = Mathf.FloorToInt(cp.x / C), cz = Mathf.FloorToInt(cp.z / C);
                for (int z = cz - 4; z <= cz + 4; z++)
                    for (int x = cx - 4; x <= cx + 4; x++)
                    {
                        float hsh = Hash(x * 12.7f + z * 3.1f);
                        if (hsh > 0.35f) continue;
                        float wx = x * C + Hash(x + z * 7.1f) * C, wz = z * C + Hash(z + x * 5.3f) * C;
                        if (world.ForestDensity(wx, wz) < 0.5f) continue;
                        float g = world.Ground(wx, wz);
                        var bottom = new Vector3(wx, g, wz);
                        var top = bottom + sunDir.normalized * 16f;
                        var axis = (top - bottom).normalized;
                        var side = Vector3.Cross(axis, (bottom + top) * 0.5f - cp).normalized * (0.8f + hsh * 3f);
                        float dist = Vector3.Distance(cp, bottom);
                        float a = low * 0.085f * Mathf.Clamp01(dist / 6f) * (0.6f + 0.4f * Mathf.Sin(time * 0.3f + hsh * 20));
                        int b0 = v.Count;
                        // 3 segmentos: some nas pontas, mais forte no meio
                        for (int k = 0; k <= 3; k++)
                        {
                            float u = k / 3f;
                            var p = Vector3.Lerp(bottom, top, u);
                            var col = new Color(sunCol.r, sunCol.g, sunCol.b, a * Mathf.Sin(u * Mathf.PI) * (k == 0 ? 0.3f : 1));
                            v.Add(p - side); v.Add(p + side); c.Add(col); c.Add(col);
                            if (k > 0) { int q = b0 + (k - 1) * 2; t.Add(q); t.Add(q + 2); t.Add(q + 1); t.Add(q + 1); t.Add(q + 2); t.Add(q + 3); }
                        }
                    }
            }

            mesh.Clear();
            if (v.Count == 0) return;
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
        }

        static float Hash(float x) { float s = Mathf.Sin(x * 12.9898f) * 43758.5453f; return s - Mathf.Floor(s); }
    }
}
