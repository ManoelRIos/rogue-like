using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>Ruído coerente com semente (mesma semente = mesmo mundo para todos no co-op).</summary>
    public class Noise
    {
        readonly Vector2[] offsets = new Vector2[8];

        public Noise(int seed)
        {
            var r = new System.Random(seed);
            for (int i = 0; i < offsets.Length; i++) offsets[i] = new Vector2(r.Next(-50000, 50000), r.Next(-50000, 50000));
        }

        public float Perlin(float x, float z, int layer = 0) => Mathf.PerlinNoise(x + offsets[layer].x, z + offsets[layer].y);

        /// <summary>Soma de oitavas (fBm) em 0..1.</summary>
        public float Fbm(float x, float z, int octaves, float scale, int layer = 0)
        {
            float a = 1, f = scale, sum = 0, norm = 0;
            for (int i = 0; i < octaves; i++)
            {
                sum += Perlin(x * f, z * f, (layer + i) % offsets.Length) * a;
                norm += a; a *= 0.5f; f *= 2.03f;
            }
            return sum / norm;
        }
    }

    public class Rng
    {
        readonly System.Random r;
        public Rng(int seed) { r = new System.Random(seed); }
        public float Value => (float)r.NextDouble();
        public float Range(float a, float b) => a + (b - a) * Value;
        public int Int(int a, int b) => r.Next(a, b + 1);
        public bool Chance(float p) => Value < p;
        public T Pick<T>(IList<T> l) => l[r.Next(l.Count)];
        public Color Vary(Color c, float amt) => new Color(
            Mathf.Clamp01(c.r + Range(-amt, amt)), Mathf.Clamp01(c.g + Range(-amt, amt)), Mathf.Clamp01(c.b + Range(-amt, amt)), c.a);
    }

    public static class Col
    {
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static Color A(Color c, float a) { c.a = a; return c; }
    }

    /// <summary>
    /// Monta malhas low-poly com sombreamento chapado (vértices não compartilhados)
    /// e cor por vértice. O alfa da cor = quanto balança com o vento.
    /// </summary>
    public class MeshBuilder
    {
        public readonly List<Vector3> v = new();
        public readonly List<Color> c = new();
        public readonly List<int> t = new();
        // normal "arredondada" por vértice (xyz) e quanto ela pesa sobre a normal da faceta (w)
        public readonly List<Vector4> soft = new();
        /// <summary>0..1: quanto as bolhas (copas, arbustos) ficam com sombreado suave em vez de facetado.</summary>
        public float SmoothBlobs = 0;

        public void Tri(Vector3 a, Vector3 b, Vector3 d, Color ca, Color cb, Color cd)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(d);
            c.Add(ca); c.Add(cb); c.Add(cd);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            soft.Add(Vector4.zero); soft.Add(Vector4.zero); soft.Add(Vector4.zero);
        }

        /// <summary>Define a normal suave de um vértice já adicionado.</summary>
        public void Soften(int index, Vector3 normal, float amount) => soft[index] = new Vector4(normal.x, normal.y, normal.z, amount);

        public void Tri(Vector3 a, Vector3 b, Vector3 d, Color col) => Tri(a, b, d, col, col, col);

        public void Quad(Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col)
        {
            Tri(a, b, d, col); Tri(a, d, e, col);
        }

        /// <summary>Cilindro/cone facetado entre duas alturas (r0 embaixo, r1 em cima).</summary>
        public void Cylinder(Vector3 basePos, float h, float r0, float r1, int sides, Color col, float sway0 = 0, float sway1 = 0, float twist = 0, Rng rng = null, bool cap = true)
        {
            var c0 = Col.A(col, sway0); var c1 = Col.A(col, sway1);
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2 + twist, a1 = ((i + 1) / (float)sides) * Mathf.PI * 2 + twist;
                var p0 = basePos + new Vector3(Mathf.Cos(a0) * r0, 0, Mathf.Sin(a0) * r0);
                var p1 = basePos + new Vector3(Mathf.Cos(a1) * r0, 0, Mathf.Sin(a1) * r0);
                var q0 = basePos + new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1);
                var q1 = basePos + new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1);
                var shade = rng != null ? rng.Vary(col, 0.025f) : col;
                var s0 = Col.A(shade, sway0); var s1 = Col.A(shade, sway1);
                if (r1 > 0.001f) { Tri(p0, q1, p1, s0, s1, s0); Tri(p0, q0, q1, s0, s1, s1); }
                else Tri(p0, q0, p1, s0, s1, s0);
                if (cap && r1 > 0.001f) Tri(basePos + Vector3.up * h, q1, q0, c1, c1, c1);
            }
        }

        /// <summary>"Bolha" facetada (copa de árvore, arbusto, pedra): icosaedro subdividido com ruído.</summary>
        public void Blob(Vector3 center, Vector3 radius, Color col, float jitter, Rng rng, int detail = 1, float sway = 0, float swayTop = 0, Color? colTop = null)
        {
            var verts = new List<Vector3>(Ico.V);
            var faces = new List<int[]>(Ico.F);
            for (int s = 0; s < detail; s++)
            {
                var nf = new List<int[]>();
                var mid = new Dictionary<long, int>();
                int Mid(int a, int b)
                {
                    long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (mid.TryGetValue(k, out int m)) return m;
                    verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
                    mid[k] = verts.Count - 1;
                    return verts.Count - 1;
                }
                foreach (var f in faces)
                {
                    int ab = Mid(f[0], f[1]), bc = Mid(f[1], f[2]), ca = Mid(f[2], f[0]);
                    nf.Add(new[] { f[0], ab, ca }); nf.Add(new[] { f[1], bc, ab }); nf.Add(new[] { f[2], ca, bc }); nf.Add(new[] { ab, bc, ca });
                }
                faces = nf;
            }
            var pts = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                var d = verts[i] * (1 + rng.Range(-jitter, jitter));
                pts[i] = center + Vector3.Scale(d, radius);
            }
            var top = colTop ?? col;
            foreach (var f in faces)
            {
                var a = pts[f[0]]; var b = pts[f[1]]; var e = pts[f[2]];
                float up = ((a + b + e) / 3f - center).y / Mathf.Max(0.01f, radius.y);
                var cc = rng.Vary(Color.Lerp(col, top, Mathf.Clamp01(up * 0.5f + 0.5f)), 0.03f);
                float sw = Mathf.Lerp(sway, swayTop, Mathf.Clamp01(up * 0.5f + 0.5f));
                Tri(a, e, b, Col.A(cc, sw), Col.A(cc, sw), Col.A(cc, sw));
                if (SmoothBlobs > 0)
                {
                    int k = v.Count - 3;
                    // normal de elipsoide: (p - centro) / raio²
                    Vector3 N(Vector3 q) => new Vector3((q.x - center.x) / (radius.x * radius.x), (q.y - center.y) / (radius.y * radius.y), (q.z - center.z) / (radius.z * radius.z)).normalized;
                    Soften(k, N(a), SmoothBlobs); Soften(k + 1, N(e), SmoothBlobs); Soften(k + 2, N(b), SmoothBlobs);
                }
            }
        }

        public void Box(Vector3 min, Vector3 max, Color col, Rng rng = null)
        {
            Vector3 a = min, b = max;
            Vector3[] p =
            {
                new(a.x, a.y, a.z), new(b.x, a.y, a.z), new(b.x, a.y, b.z), new(a.x, a.y, b.z),
                new(a.x, b.y, a.z), new(b.x, b.y, a.z), new(b.x, b.y, b.z), new(a.x, b.y, b.z),
            };
            int[][] faces = { new[] { 4, 7, 6, 5 }, new[] { 0, 1, 2, 3 }, new[] { 0, 4, 5, 1 }, new[] { 1, 5, 6, 2 }, new[] { 2, 6, 7, 3 }, new[] { 3, 7, 4, 0 } };
            foreach (var f in faces)
            {
                var cc = Col.A(rng != null ? rng.Vary(col, 0.02f) : col, 0);   // caixas não balançam ao vento
                Quad(p[f[0]], p[f[1]], p[f[2]], p[f[3]], cc);
            }
        }

        /// <summary>Escala todos os vértices já adicionados (ex.: achatar o peito na profundidade).</summary>
        public MeshBuilder Scale(Vector3 s, int from = 0)
        {
            for (int i = from; i < v.Count; i++) v[i] = Vector3.Scale(v[i], s);
            return this;
        }

        /// <summary>Copia triângulos de outro builder girados/deslocados.</summary>
        public void Add(MeshBuilder src, Vector3 offset, Quaternion rot)
        {
            for (int i = 0; i < src.v.Count; i += 3)
            {
                int k = v.Count;
                Tri(offset + rot * src.v[i], offset + rot * src.v[i + 1], offset + rot * src.v[i + 2], src.c[i], src.c[i + 1], src.c[i + 2]);
                for (int j = 0; j < 3; j++) if (src.soft[i + j].w > 0) { var n = rot * (Vector3)src.soft[i + j]; Soften(k + j, n, src.soft[i + j].w); }
            }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            bool any = false;
            foreach (var s in soft) if (s.w > 0) { any = true; break; }
            if (any)
            {
                var ns = m.normals;
                for (int i = 0; i < ns.Length && i < soft.Count; i++)
                    if (soft[i].w > 0) ns[i] = Vector3.Lerp(ns[i], new Vector3(soft[i].x, soft[i].y, soft[i].z), soft[i].w).normalized;
                m.normals = ns;
            }
            return m;
        }
    }

    static class Ico
    {
        static readonly float X = 0.525731f, Z = 0.850651f;
        public static readonly Vector3[] V =
        {
            new(-X, 0, Z), new(X, 0, Z), new(-X, 0, -Z), new(X, 0, -Z), new(0, Z, X), new(0, Z, -X),
            new(0, -Z, X), new(0, -Z, -X), new(Z, X, 0), new(-Z, X, 0), new(Z, -X, 0), new(-Z, -X, 0),
        };
        public static readonly int[][] F =
        {
            new[] { 0, 4, 1 }, new[] { 0, 9, 4 }, new[] { 9, 5, 4 }, new[] { 4, 5, 8 }, new[] { 4, 8, 1 }, new[] { 8, 10, 1 }, new[] { 8, 3, 10 },
            new[] { 5, 3, 8 }, new[] { 5, 2, 3 }, new[] { 2, 7, 3 }, new[] { 7, 10, 3 }, new[] { 7, 6, 10 }, new[] { 7, 11, 6 }, new[] { 11, 0, 6 },
            new[] { 0, 1, 6 }, new[] { 6, 1, 10 }, new[] { 9, 0, 11 }, new[] { 9, 11, 2 }, new[] { 9, 2, 5 }, new[] { 7, 2, 11 },
        };
    }

    public static class Mats
    {
        public static Material LowPoly, Grass, Water, Sky, Glow, Soft, SoftAdd, SoftUnlit, Fire;
        public static void Init()
        {
            if (LowPoly != null) return;
            LowPoly = new Material(Resources.Load<Shader>("Shaders/LowPoly")) { name = "LowPoly", enableInstancing = true };
            // grama: balança mais e some aos poucos ao longe
            Grass = new Material(LowPoly) { name = "Grama", enableInstancing = true };
            Grass.SetFloat("_WindScale", 2.2f);
            Grass.SetFloat("_FadeDist", 66f);
            Water = new Material(Resources.Load<Shader>("Shaders/Water")) { name = "Agua" };
            Sky = new Material(Resources.Load<Shader>("Shaders/Sky")) { name = "Ceu" };
            Glow = new Material(Resources.Load<Shader>("Shaders/Glow")) { name = "Brilho", enableInstancing = true };
            var soft = Resources.Load<Shader>("Shaders/Soft");
            Soft = new Material(soft) { name = "Fumaca", enableInstancing = true };
            SoftAdd = new Material(soft) { name = "Faiscas", enableInstancing = true };
            SoftAdd.SetFloat("_Src", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            SoftAdd.SetFloat("_Dst", (float)UnityEngine.Rendering.BlendMode.One);
            SoftAdd.SetFloat("_Lit", 0);
            SoftUnlit = new Material(soft) { name = "Nuvens", enableInstancing = true };
            SoftUnlit.SetFloat("_Lit", 0);
            SoftUnlit.SetFloat("_UseFog", 0);
            Fire = new Material(Resources.Load<Shader>("Shaders/Fire")) { name = "Fogo" };
        }

        public static GameObject Object(string name, Mesh mesh, Material mat, Transform parent = null, bool shadows = true)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
