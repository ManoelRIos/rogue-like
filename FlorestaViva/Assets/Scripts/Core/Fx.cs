using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Partículas leves desenhadas por instância (sem GameObjects): lascas de madeira, folhas caindo,
    /// poeira, fumaça, faíscas, respingos e gotas. Cada tipo tem física simples própria:
    /// gravidade, arrasto do ar, vento, quique no chão e "planar" das folhas.
    /// </summary>
    public static class Fx
    {
        struct P
        {
            public Vector3 pos, vel, spin;
            public Quaternion rot;
            public float life, max, size, grow, gravity, drag, windK, bounce, flutter, seed;
            public Vector4 color;
            public bool grounded;
        }

        class Kind
        {
            public Mesh[] meshes;
            public Material mat;
            public bool tinted;              // usa cor por instância (materiais Soft)
            public bool fadeAlpha = true;    // some pelo alfa (senão encolhe)
            public readonly List<P>[] items;
            public Kind(Mesh[] m, Material mat, bool tinted) { meshes = m; this.mat = mat; this.tinted = tinted; items = new List<P>[m.Length]; for (int i = 0; i < m.Length; i++) items[i] = new List<P>(); }
        }

        static Kind chips, stones, leaves, dust, smoke, sparks, drops, berries;
        static readonly Matrix4x4[] mats = new Matrix4x4[1023];
        static readonly Vector4[] cols = new Vector4[1023];
        static MaterialPropertyBlock mpb;
        public static World World;
        static float dropT;
        static Kind[] all;

        public const int LeafPine = 0, LeafOak = 1, LeafAutumn = 2, LeafBirch = 3, LeafBush = 4, LeafPink = 5;

        public static void Init(World w)
        {
            World = w;
            if (chips != null) return;
            mpb = new MaterialPropertyBlock();
            var rng = new Rng(55);
            Mesh Box(Color c, float sx, float sy, float sz)
            {
                var mb = new MeshBuilder();
                mb.Box(new Vector3(-sx, -sy, -sz), new Vector3(sx, sy, sz), c, rng);
                return mb.ToMesh("lasca");
            }
            Mesh Leaf(Color c)
            {
                var mb = new MeshBuilder();
                mb.Tri(new Vector3(0, 0, -0.06f), new Vector3(0.035f, 0, 0), new Vector3(0, 0.004f, 0.06f), Col.A(c, 0));
                mb.Tri(new Vector3(0, 0, -0.06f), new Vector3(0, 0.004f, 0.06f), new Vector3(-0.035f, 0, 0), Col.A(Color.Lerp(c, Color.black, 0.12f), 0));
                return mb.ToMesh("folha");
            }
            Mesh Puff(int detail)
            {
                var mb = new MeshBuilder();
                mb.Blob(Vector3.zero, Vector3.one * 0.5f, Color.white, 0.18f, rng, detail);
                return mb.ToMesh("bolha");
            }
            chips = new Kind(new[] { Box(Col.Hex("#c8a070"), 0.035f, 0.012f, 0.05f), Box(Col.Hex("#8a5c38"), 0.03f, 0.015f, 0.04f), Box(Col.Hex("#e0c090"), 0.025f, 0.01f, 0.045f) }, Mats.LowPoly, false) { fadeAlpha = false };
            stones = new Kind(new[] { Box(Col.Hex("#8a8578"), 0.04f, 0.03f, 0.035f), Box(Col.Hex("#a8a294"), 0.03f, 0.025f, 0.03f) }, Mats.LowPoly, false) { fadeAlpha = false };
            leaves = new Kind(new[] { Leaf(Col.Hex("#3f6e3a")), Leaf(Col.Hex("#6a9a38")), Leaf(Col.Hex("#d88a34")), Leaf(Col.Hex("#b8d050")), Leaf(Col.Hex("#4e8a34")), Leaf(Col.Hex("#f4a8cc")) }, Mats.LowPoly, false) { fadeAlpha = false };
            berries = new Kind(new[] { Box(Col.Hex("#c22a3c"), 0.03f, 0.03f, 0.03f) }, Mats.LowPoly, false) { fadeAlpha = false };
            dust = new Kind(new[] { Puff(0) }, Mats.Soft, true);
            smoke = new Kind(new[] { Puff(1) }, Mats.Soft, true);
            sparks = new Kind(new[] { Puff(0) }, Mats.SoftAdd, true);
            drops = new Kind(new[] { Puff(0) }, Mats.Soft, true);
            all = new[] { chips, stones, leaves, berries, dust, smoke, sparks, drops };
        }

        static float Ground(Vector3 p) => World != null ? Mathf.Max(World.Ground(p.x, p.z), World.WaterLevel - 0.05f) : -999;

        static void Add(Kind k, int variant, P p)
        {
            var list = k.items[Mathf.Clamp(variant, 0, k.meshes.Length - 1)];
            if (list.Count >= 1000) list.RemoveAt(0);
            p.max = p.life;
            p.seed = Random.value * 100;
            if (p.rot.x == 0 && p.rot.y == 0 && p.rot.z == 0 && p.rot.w == 0) p.rot = Random.rotation;
            list.Add(p);
        }

        // ------------------------------------------------------------------ emissores
        /// <summary>Lascas de madeira saltando do golpe do machado.</summary>
        public static void Chips(Vector3 at, Vector3 outDir, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var v = (outDir.normalized * Random.Range(1.5f, 3.5f) + Random.insideUnitSphere * 1.4f + Vector3.up * Random.Range(1.2f, 3f));
                Add(chips, Random.Range(0, 3), new P { pos = at + Random.insideUnitSphere * 0.08f, vel = v, spin = Random.insideUnitSphere * 720, life = Random.Range(2.5f, 4f), size = Random.Range(0.8f, 1.4f), gravity = 9.8f, drag = 0.4f, bounce = 0.3f });
            }
        }

        public static void Stones(Vector3 at, Vector3 outDir, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var v = outDir.normalized * Random.Range(1f, 2.5f) + Random.insideUnitSphere * 1.5f + Vector3.up * Random.Range(1f, 2.5f);
                Add(stones, Random.Range(0, 2), new P { pos = at + Random.insideUnitSphere * 0.1f, vel = v, spin = Random.insideUnitSphere * 500, life = Random.Range(2f, 3.5f), size = Random.Range(0.6f, 1.6f), gravity = 9.8f, drag = 0.2f, bounce = 0.25f });
            }
        }

        public static void Berries(Vector3 at, int n)
        {
            for (int i = 0; i < n; i++)
                Add(berries, 0, new P { pos = at + Random.insideUnitSphere * 0.4f, vel = Random.insideUnitSphere * 1.5f + Vector3.up * 1.5f, spin = Random.insideUnitSphere * 300, life = 2.5f, size = 1, gravity = 9.8f, drag = 0.3f, bounce = 0.4f });
        }

        /// <summary>Folhas que se soltam e caem planando, levadas pelo vento.</summary>
        public static void Leaves(Vector3 at, float radius, int n, int variant, float burst = 0)
        {
            if (variant < 0) return;
            for (int i = 0; i < n; i++)
            {
                var off = Random.insideUnitSphere * radius;
                Add(leaves, variant, new P
                {
                    pos = at + off,
                    vel = off.normalized * burst * Random.Range(0.5f, 1.2f) + Vector3.up * burst * 0.5f,
                    spin = new Vector3(Random.Range(-200, 200), Random.Range(-300, 300), Random.Range(-200, 200)),
                    life = Random.Range(5f, 9f), size = Random.Range(0.8f, 1.5f),
                    gravity = 2.2f, drag = 2.6f, windK = 0.9f, flutter = Random.Range(0.8f, 1.6f), bounce = 0,
                });
            }
        }

        public static void Dust(Vector3 at, int n, float size)
        {
            for (int i = 0; i < n; i++)
            {
                var d = Random.insideUnitCircle;
                Add(dust, 0, new P
                {
                    pos = at + new Vector3(d.x, 0.05f, d.y) * size * 0.5f,
                    vel = new Vector3(d.x, 0.3f, d.y) * size * Random.Range(0.8f, 2f),
                    life = Random.Range(0.9f, 1.8f), size = size * Random.Range(0.25f, 0.5f), grow = size * 0.8f,
                    gravity = -0.15f, drag = 2.5f, windK = 0.5f, color = new Vector4(0.62f, 0.55f, 0.42f, 0.42f),
                });
            }
        }

        /// <summary>Fumaça da fogueira: sobe, abre e é levada pelo vento.</summary>
        public static void Smoke(Vector3 at, float size, float alpha)
        {
            float g = Random.Range(0.42f, 0.58f);
            Add(smoke, 0, new P
            {
                pos = at + new Vector3(Random.Range(-0.15f, 0.15f), 0, Random.Range(-0.15f, 0.15f)),
                vel = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.8f, 1.3f), Random.Range(-0.15f, 0.15f)),
                spin = Random.insideUnitSphere * 30, life = Random.Range(4.5f, 7f), size = size, grow = size * 0.55f,
                gravity = -0.25f, drag = 0.6f, windK = 0.55f, color = new Vector4(g, g, g * 1.02f, alpha),
            });
        }

        public static void Sparks(Vector3 at, int n, float power = 1)
        {
            for (int i = 0; i < n; i++)
                Add(sparks, 0, new P
                {
                    pos = at + Random.insideUnitSphere * 0.2f,
                    vel = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(1.5f, 3.5f) * power, Random.Range(-0.6f, 0.6f)),
                    life = Random.Range(0.8f, 2.2f), size = Random.Range(0.035f, 0.07f), gravity = -0.4f, drag = 0.8f, windK = 0.7f, flutter = 2.5f,
                    color = new Vector4(1f, Random.Range(0.45f, 0.75f), 0.2f, 1),
                });
        }

        public static void Splash(Vector3 at)
        {
            for (int i = 0; i < 7; i++)
            {
                var d = Random.insideUnitCircle;
                Add(drops, 0, new P { pos = at + Vector3.up * 0.05f, vel = new Vector3(d.x * 1.2f, Random.Range(1.5f, 2.8f), d.y * 1.2f), life = 0.7f, size = Random.Range(0.04f, 0.08f), gravity = 9.8f, drag = 0.2f, color = new Vector4(0.8f, 0.92f, 1f, 0.75f) });
            }
        }

        /// <summary>Filete do regador (limitado por tempo, pode chamar todo quadro).</summary>
        public static void Drops(Vector3 at, Vector3 dir)
        {
            dropT -= Time.deltaTime;
            if (dropT > 0) return;
            dropT = 0.02f;
            Add(drops, 0, new P { pos = at, vel = dir.normalized * 1.3f + Random.insideUnitSphere * 0.15f, life = 0.8f, size = Random.Range(0.025f, 0.045f), gravity = 9.8f, drag = 0.1f, color = new Vector4(0.75f, 0.9f, 1f, 0.8f) });
        }

        // ------------------------------------------------------------------ simulação + desenho
        public static void Update(float dt, Camera cam)
        {
            if (chips == null || dt <= 0) return;
            float t = Time.time;
            foreach (var k in all)
                for (int m = 0; m < k.meshes.Length; m++)
                {
                    var list = k.items[m];
                    int n = 0;
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var p = list[i];
                        p.life -= dt;
                        if (p.life <= 0) { list.RemoveAt(i); continue; }
                        if (!p.grounded)
                        {
                            var wind = p.windK > 0 ? Wind.At(p.pos) * p.windK : Vector3.zero;
                            p.vel += Vector3.down * p.gravity * dt;
                            p.vel += (wind - p.vel) * Mathf.Clamp01(p.drag * dt);
                            if (p.flutter > 0)   // folha planando: vai e volta de lado
                            {
                                float f = Mathf.Sin(t * 3.1f * p.flutter + p.seed) * p.flutter;
                                p.vel.x += Mathf.Cos(p.seed) * f * dt * 4; p.vel.z += Mathf.Sin(p.seed) * f * dt * 4;
                            }
                            p.pos += p.vel * dt;
                            p.rot = Quaternion.Euler(p.spin * dt) * p.rot;
                            float g = Ground(p.pos);
                            if (p.pos.y < g + 0.01f && p.gravity > 0)
                            {
                                p.pos.y = g + 0.01f;
                                if (p.bounce > 0 && p.vel.y < -1.2f) { p.vel.y = -p.vel.y * p.bounce; p.vel.x *= 0.5f; p.vel.z *= 0.5f; p.spin *= 0.5f; }
                                else
                                {
                                    p.grounded = true; p.vel = Vector3.zero;
                                    // assenta deitado
                                    var e = p.rot.eulerAngles; p.rot = Quaternion.Euler(k == leaves ? 0 : e.x, e.y, k == leaves ? 0 : e.z);
                                    if (k == drops) p.life = 0.05f;
                                }
                            }
                        }
                        float age = 1 - p.life / p.max;
                        float s = p.size + p.grow * age * p.max;
                        if (!k.fadeAlpha) s *= Mathf.Clamp01(p.life / 0.6f);   // sólidos encolhem no fim
                        list[i] = p;
                        mats[n] = Matrix4x4.TRS(p.pos, p.rot, Vector3.one * s);
                        if (k.tinted)
                        {
                            var c = p.color;
                            float fadeIn = Mathf.Clamp01(age * 8), fadeOut = Mathf.Clamp01(p.life / (p.max * 0.6f));
                            c.w *= fadeIn * fadeOut;
                            cols[n] = c;
                        }
                        n++;
                        if (n == mats.Length) { Draw(k, m, n); n = 0; }
                    }
                    if (n > 0) Draw(k, m, n);
                }
        }

        static void Draw(Kind k, int m, int n)
        {
            if (k.tinted) { mpb.Clear(); mpb.SetVectorArray("_Color", cols); Graphics.DrawMeshInstanced(k.meshes[m], 0, k.mat, mats, n, mpb, UnityEngine.Rendering.ShadowCastingMode.Off, false); }
            else Graphics.DrawMeshInstanced(k.meshes[m], 0, k.mat, mats, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, true);
        }
    }
}
