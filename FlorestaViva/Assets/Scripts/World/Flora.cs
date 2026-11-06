using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    public enum Item { Madeira, Pedra, Frutinha, Cogumelo, Fibra, Flor, SementeCenoura, SementeAbobora, Cenoura, Abobora }

    /// <summary>Árvore, pedra ou arbusto que pode ser coletado (machado/mão).</summary>
    public class Gatherable : MonoBehaviour
    {
        public Item drop;
        public int hp = 4, amount = 3;
        public bool fells;            // árvores tombam
        public float respawn = 240f;  // segundos para voltar (arbustos/pedras)
        public Mesh stumpMesh;
        float shake, deadT = -1;
        Quaternion baseRot;
        Vector3 basePos;
        int maxHp;
        Renderer rend;
        Collider col;

        void Awake()
        {
            baseRot = transform.rotation; basePos = transform.position; maxHp = hp;
            rend = GetComponent<Renderer>(); col = GetComponent<Collider>();
        }

        /// <summary>Recebe um golpe; devolve quantos itens caíram.</summary>
        public int Hit(Vector3 from)
        {
            if (deadT >= 0) return 0;
            hp--;
            shake = 0.25f;
            Audio.Play(drop == Item.Madeira ? "chop" : drop == Item.Pedra ? "mine" : "rustle");
            if (hp > 0) return drop == Item.Madeira ? 1 : 0;
            deadT = 0;
            if (col) col.enabled = false;
            if (fells)
            {
                Audio.Play("fall");
                fallDir = Vector3.Cross(Vector3.up, (transform.position - from).normalized);
            }
            return amount;
        }

        Vector3 fallDir;

        void Update()
        {
            float dt = Time.deltaTime;
            if (shake > 0)
            {
                shake -= dt;
                transform.rotation = baseRot * Quaternion.Euler(Mathf.Sin(Time.time * 60) * shake * 8, 0, Mathf.Cos(Time.time * 55) * shake * 8);
                if (shake <= 0 && deadT < 0) transform.rotation = baseRot;
            }
            if (deadT >= 0)
            {
                deadT += dt;
                if (fells && deadT < 1.4f)
                {
                    float k = Mathf.Min(1, deadT / 1.2f);
                    transform.rotation = Quaternion.AngleAxis(k * k * 88f, fallDir) * baseRot;
                }
                else if (deadT < 1.8f) transform.localScale = Vector3.one * scale0 * Mathf.Max(0.01f, 1 - (deadT - (fells ? 1.4f : 0)) * 3f);
                if (rend && deadT > (fells ? 1.6f : 0.35f)) rend.enabled = false;
                if (deadT > respawn) Regrow();
            }
        }

        void Regrow()
        {
            deadT = -1; hp = maxHp;
            transform.SetPositionAndRotation(basePos, baseRot);
            transform.localScale = Vector3.one * scale0;
            if (rend) rend.enabled = true;
            if (col) col.enabled = true;
        }

        public float scale0 = 1;
    }

    /// <summary>
    /// Gera e espalha a vegetação: pinheiros, carvalhos, bétulas, árvores mortas,
    /// arbustos com frutinhas, cogumelos, flores, pedras e muita grama balançando.
    /// </summary>
    public class Flora
    {
        readonly World W;
        readonly Rng rng;
        readonly Transform root;
        readonly Dictionary<string, List<Mesh>> meshes = new();
        // grama/flores desenhadas por instância (sem objeto nem colisão)
        readonly List<(Mesh mesh, List<Matrix4x4> list)> instanced = new();
        public readonly List<Vector3> FireflySpots = new();
        public readonly List<Vector3> ForestEdge = new();   // pontos de onde as criaturas saem à noite

        public Flora(World w)
        {
            W = w;
            rng = new Rng(w.Seed * 31 + 7);
            root = new GameObject("Vegetacao").transform;
            root.SetParent(w.Root, false);
            BuildMeshes();
            Scatter();
        }

        // ------------------------------------------------------------------ malhas das espécies
        static readonly Color Bark = Col.Hex("#5a3d28"), BarkD = Col.Hex("#3e2a1c"), Birch = Col.Hex("#ddd6c4"),
            Pine = Col.Hex("#2f5a34"), PineL = Col.Hex("#4b7f3e"), Oak = Col.Hex("#4f7d2f"), OakL = Col.Hex("#8fb84a"),
            Autumn = Col.Hex("#c7782f"), AutumnL = Col.Hex("#e6b34a"), BirchLeaf = Col.Hex("#7fae3c"), BirchLeafL = Col.Hex("#c4d860"),
            Dead = Col.Hex("#4a3e36"), Bush = Col.Hex("#3e6e2c"), BushL = Col.Hex("#69a03c"), Berry = Col.Hex("#c22a3c"),
            Stone = Col.Hex("#8a8578"), StoneL = Col.Hex("#b0aa9a"), Moss = Col.Hex("#5a7a34"), Grass = Col.Hex("#6f9c3c"), GrassL = Col.Hex("#a6c855"),
            Cap = Col.Hex("#b8342c"), CapGlow = Col.Hex("#5fd6c8"), Stem = Col.Hex("#e8dcc0");

        void Add(string k, Mesh m) { if (!meshes.TryGetValue(k, out var l)) meshes[k] = l = new List<Mesh>(); l.Add(m); }

        void BuildMeshes()
        {
            for (int v = 0; v < 4; v++)
            {
                var r = new Rng(W.Seed + v * 101);
                // pinheiro: tronco + 3-4 cones empilhados
                var mb = new MeshBuilder();
                float h = r.Range(7, 10);
                mb.Cylinder(Vector3.zero, h * 0.4f, 0.32f, 0.22f, 6, Bark, 0, 0.05f, 0, r);
                int tiers = r.Int(3, 4);
                for (int i = 0; i < tiers; i++)
                {
                    float y = h * 0.22f + i * h * 0.19f, rad = (2.4f - i * 0.5f) * r.Range(0.9f, 1.1f);
                    mb.Cylinder(new Vector3(0, y, 0), h * 0.32f, rad, 0.05f, 7, Color.Lerp(Pine, PineL, i / (float)tiers), 0.25f + i * 0.15f, 0.5f + i * 0.15f, r.Range(0, 3), r);
                }
                Add("pinheiro", mb.ToMesh("pinheiro"));

                // carvalho: tronco grosso + copa de várias bolhas (às vezes outonal)
                mb = new MeshBuilder();
                h = r.Range(3.2f, 4.2f);
                mb.Cylinder(Vector3.zero, h, 0.55f, 0.35f, 7, Bark, 0, 0.05f, 0, r);
                mb.Cylinder(new Vector3(0, h * 0.7f, 0), 1.5f, 0.22f, 0.12f, 5, BarkD, 0.05f, 0.15f, 0.6f, r);
                bool autumn = v == 3;
                for (int i = 0; i < 4; i++)
                {
                    var c = new Vector3(r.Range(-1.3f, 1.3f), h + r.Range(0.6f, 2.0f), r.Range(-1.3f, 1.3f));
                    mb.Blob(c, Vector3.one * r.Range(1.6f, 2.3f), autumn ? Autumn : Oak, 0.18f, r, 1, 0.3f, 0.7f, autumn ? AutumnL : OakL);
                }
                Add("carvalho", mb.ToMesh("carvalho"));

                // bétula: tronco claro com manchas + copa alta
                mb = new MeshBuilder();
                h = r.Range(5, 7);
                mb.Cylinder(Vector3.zero, h, 0.22f, 0.16f, 6, Birch, 0, 0.1f, 0, r);
                for (int i = 0; i < 4; i++) mb.Box(new Vector3(-0.23f, 0.8f + i * 1.2f, -0.05f), new Vector3(-0.15f, 1.0f + i * 1.2f, 0.12f), BarkD);
                mb.Blob(new Vector3(0, h + 0.4f, 0), new Vector3(1.4f, 2.4f, 1.4f), BirchLeaf, 0.15f, r, 1, 0.35f, 0.8f, BirchLeafL);
                Add("betula", mb.ToMesh("betula"));

                // árvore morta (galhos retorcidos) — bonita de dia, assustadora à noite
                mb = new MeshBuilder();
                h = r.Range(4, 6);
                mb.Cylinder(Vector3.zero, h, 0.35f, 0.08f, 5, Dead, 0, 0.05f, 0, r);
                for (int i = 0; i < 4; i++)
                {
                    float y = h * r.Range(0.45f, 0.85f), ang = r.Range(0, 6.28f);
                    var dir = new Vector3(Mathf.Cos(ang), r.Range(0.4f, 0.9f), Mathf.Sin(ang)).normalized;
                    var p0 = new Vector3(0, y, 0);
                    var p1 = p0 + dir * r.Range(1.2f, 2.2f);
                    Branch(mb, p0, p1, 0.12f, Dead);
                }
                Add("morta", mb.ToMesh("morta"));

                // arbusto com frutinhas
                mb = new MeshBuilder();
                mb.Blob(new Vector3(0, 0.6f, 0), new Vector3(1.1f, 0.75f, 1.1f), Bush, 0.2f, r, 1, 0.2f, 0.45f, BushL);
                for (int i = 0; i < 7; i++)
                {
                    var p = new Vector3(r.Range(-0.8f, 0.8f), r.Range(0.5f, 1.2f), r.Range(-0.8f, 0.8f));
                    mb.Blob(p, Vector3.one * 0.12f, Berry, 0.1f, r, 0, 0.4f, 0.4f);
                }
                Add("arbusto", mb.ToMesh("arbusto"));

                // pedra com musgo
                mb = new MeshBuilder();
                mb.Blob(new Vector3(0, 0.35f, 0), new Vector3(r.Range(0.8f, 1.3f), r.Range(0.5f, 0.8f), r.Range(0.8f, 1.2f)), Stone, 0.22f, r, 1, 0, 0, StoneL);
                mb.Blob(new Vector3(0.1f, 0.75f, 0), new Vector3(0.6f, 0.12f, 0.5f), Moss, 0.2f, r, 0, 0, 0);
                Add("pedra", mb.ToMesh("pedra"));

                // cogumelos (alguns brilham à noite)
                mb = new MeshBuilder();
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3(r.Range(-0.3f, 0.3f), 0, r.Range(-0.3f, 0.3f));
                    float s = r.Range(0.6f, 1.1f);
                    mb.Cylinder(p, 0.25f * s, 0.05f * s, 0.04f * s, 5, Stem, 0, 0.1f, 0, r);
                    mb.Cylinder(p + Vector3.up * 0.22f * s, 0.12f * s, 0.18f * s, 0.02f, 6, v == 2 ? CapGlow : Cap, 0.1f, 0.15f, 0, r);
                }
                Add("cogumelo", mb.ToMesh("cogumelo"));

                // tufo de grama (lâminas finas que balançam bastante)
                mb = new MeshBuilder();
                for (int i = 0; i < 6; i++)
                {
                    float a = r.Range(0, 6.28f);
                    var b = new Vector3(r.Range(-0.25f, 0.25f), 0, r.Range(-0.25f, 0.25f));
                    var side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.06f;
                    var tip = b + new Vector3(r.Range(-0.15f, 0.15f), r.Range(0.35f, 0.6f), r.Range(-0.15f, 0.15f));
                    mb.Tri(b - side, tip, b + side, Col.A(Grass, 0), Col.A(GrassL, 1), Col.A(Grass, 0));
                }
                Add("grama", mb.ToMesh("grama"));

                // flor (cores variadas)
                mb = new MeshBuilder();
                Color[] petals = { Col.Hex("#f2d24a"), Col.Hex("#e86a8a"), Col.Hex("#f4f0e6"), Col.Hex("#8a6ae0") };
                var pc = petals[v];
                mb.Cylinder(Vector3.zero, 0.4f, 0.02f, 0.02f, 3, Grass, 0, 0.6f, 0, r, false);
                for (int i = 0; i < 5; i++)
                {
                    float a = i / 5f * 6.28f;
                    var c0 = new Vector3(0, 0.42f, 0);
                    mb.Tri(c0, c0 + new Vector3(Mathf.Cos(a) * 0.14f, 0.03f, Mathf.Sin(a) * 0.14f), c0 + new Vector3(Mathf.Cos(a + 0.6f) * 0.14f, 0.03f, Mathf.Sin(a + 0.6f) * 0.14f), Col.A(pc, 0.7f));
                }
                Add("flor", mb.ToMesh("flor"));
            }
        }

        static void Branch(MeshBuilder mb, Vector3 a, Vector3 b, float r, Color c)
        {
            var dir = (b - a);
            var side = Vector3.Cross(dir, Vector3.up).normalized * r;
            var up2 = Vector3.Cross(dir, side).normalized * r;
            mb.Quad(a - side, b - side * 0.3f, b + side * 0.3f, a + side, Col.A(c, 0.2f));
            mb.Quad(a - up2, b - up2 * 0.3f, b + up2 * 0.3f, a + up2, Col.A(c, 0.2f));
        }

        // ------------------------------------------------------------------ distribuição
        void Scatter()
        {
            const float step = 3.4f;
            var grass = new List<Matrix4x4>[4]; var flowers = new List<Matrix4x4>[4];
            for (int i = 0; i < 4; i++) { grass[i] = new List<Matrix4x4>(); flowers[i] = new List<Matrix4x4>(); }
            for (float z = -World.Half + 4; z < World.Half - 4; z += step)
                for (float x = -World.Half + 4; x < World.Half - 4; x += step)
                {
                    var p = new Vector3(x + rng.Range(-1.4f, 1.4f), 0, z + rng.Range(-1.4f, 1.4f));
                    p.y = W.Height(p.x, p.z);
                    if (p.y < 0.3f) continue;                    // água
                    var n = W.Normal(p.x, p.z);
                    if (n.y < 0.72f) { if (rng.Chance(0.25f)) Place("pedra", p, n, rng.Range(0.8f, 1.8f), Item.Pedra, 5, 3); continue; }
                    float dens = W.ForestDensity(p.x, p.z);
                    bool inBase = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(World.BaseCenter.x, 0, World.BaseCenter.z)) < World.BaseRadius + 4;
                    bool nearRiver = W.RiverDist(p.x, p.z) < 7;

                    if (!inBase && dens > 0.47f && rng.Chance(Mathf.InverseLerp(0.47f, 0.62f, dens) * 0.85f + 0.1f))
                    {
                        string sp = p.y > 9 ? "pinheiro" : rng.Chance(0.45f) ? "pinheiro" : rng.Chance(0.5f) ? "carvalho" : rng.Chance(0.75f) ? "betula" : "morta";
                        Place(sp, p, Vector3.up, rng.Range(0.8f, 1.35f), Item.Madeira, sp == "morta" ? 3 : 5, sp == "morta" ? 2 : 4, true);
                        if (dens > 0.55f && rng.Chance(0.2f)) ForestEdge.Add(p);
                        continue;
                    }
                    if (!inBase && dens > 0.42f && dens < 0.5f && rng.Chance(0.15f)) ForestEdge.Add(p);
                    if (!inBase && rng.Chance(dens > 0.4f ? 0.12f : 0.05f)) { Place("arbusto", p, n, rng.Range(0.8f, 1.2f), Item.Frutinha, 2, 3); continue; }
                    if (!inBase && rng.Chance(0.04f)) { Place("pedra", p, n, rng.Range(0.6f, 1.3f), Item.Pedra, 4, 2); continue; }
                    if (!inBase && dens > 0.45f && rng.Chance(0.08f)) { Place("cogumelo", p, n, rng.Range(0.8f, 1.4f), Item.Cogumelo, 1, 2); }
                    // grama e flores (instanciadas)
                    int tufts = inBase ? 2 : nearRiver ? 4 : 3;
                    for (int k = 0; k < tufts; k++)
                    {
                        var q = p + new Vector3(rng.Range(-1.6f, 1.6f), 0, rng.Range(-1.6f, 1.6f));
                        q.y = W.Height(q.x, q.z);
                        if (q.y < 0.4f) continue;
                        var m = Matrix4x4.TRS(q, Quaternion.Euler(0, rng.Range(0, 360f), 0), Vector3.one * rng.Range(0.8f, 1.5f));
                        grass[rng.Int(0, 3)].Add(m);
                    }
                    if (rng.Chance(dens < 0.45f ? 0.35f : 0.08f))
                    {
                        var q = p + new Vector3(rng.Range(-1.4f, 1.4f), 0, rng.Range(-1.4f, 1.4f));
                        q.y = W.Height(q.x, q.z);
                        if (q.y > 0.5f) flowers[rng.Int(0, 3)].Add(Matrix4x4.TRS(q, Quaternion.Euler(0, rng.Range(0, 360f), 0), Vector3.one * rng.Range(0.8f, 1.3f)));
                    }
                    if ((nearRiver || dens > 0.5f) && rng.Chance(0.12f)) FireflySpots.Add(p + Vector3.up);
                }
            for (int i = 0; i < 4; i++)
            {
                instanced.Add((meshes["grama"][i], grass[i]));
                instanced.Add((meshes["flor"][i], flowers[i]));
            }
        }

        void Place(string kind, Vector3 p, Vector3 normal, float scale, Item drop, int hp, int amount, bool tree = false)
        {
            var list = meshes[kind];
            var mesh = list[rng.Int(0, list.Count - 1)];
            var go = Mats.Object(kind, mesh, Mats.LowPoly, root);
            var up = Vector3.Slerp(Vector3.up, normal, tree ? 0.15f : 0.6f);
            go.transform.SetPositionAndRotation(p - Vector3.up * 0.05f, Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0, rng.Range(0, 360f), 0));
            go.transform.localScale = Vector3.one * scale;
            go.layer = Layers.Gather;
            if (tree)
            {
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = kind == "carvalho" ? 0.55f : 0.35f; cap.height = 4f; cap.center = new Vector3(0, 2, 0);
            }
            else
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = kind == "pedra" ? 0.8f : kind == "arbusto" ? 0.9f : 0.4f; sc.center = new Vector3(0, 0.4f, 0);
                if (kind == "cogumelo") sc.isTrigger = true;
            }
            var g = go.AddComponent<Gatherable>();
            g.drop = drop; g.hp = hp; g.amount = amount; g.fells = tree; g.scale0 = scale;
            g.respawn = tree ? 600 : 200;
        }

        /// <summary>Desenha grama e flores perto da câmera (instanciado, sem objetos).</summary>
        public void Draw(Vector3 cam)
        {
            var buf = new Matrix4x4[1023];
            foreach (var (mesh, list) in instanced)
            {
                int n = 0;
                foreach (var m in list)
                {
                    var p = new Vector3(m.m03, m.m13, m.m23);
                    if ((p - cam).sqrMagnitude > 70 * 70) continue;
                    buf[n++] = m;
                    if (n == buf.Length) { Graphics.DrawMeshInstanced(mesh, 0, Mats.LowPoly, buf, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, true); n = 0; }
                }
                if (n > 0) Graphics.DrawMeshInstanced(mesh, 0, Mats.LowPoly, buf, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, true);
            }
        }
    }
}
