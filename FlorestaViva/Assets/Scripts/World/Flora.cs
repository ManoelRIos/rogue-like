using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    public enum Item { Madeira, Pedra, Frutinha, Cogumelo, Fibra, Flor, SementeCenoura, SementeAbobora, Cenoura, Abobora }

    /// <summary>
    /// Árvore, pedra ou arbusto que pode ser coletado.
    /// Árvore: cada golpe solta lascas e algumas folhas e faz a copa balançar (mola). No último golpe ela
    /// range, começa a tombar devagar e acelera como um corpo rígido girando na base (gravidade),
    /// bate no chão, quica, levanta poeira e folhas, treme a câmera de quem está perto e afunda.
    /// Fica o toco; depois de um tempo uma muda nasce e cresce até virar árvore de novo.
    /// </summary>
    public class Gatherable : MonoBehaviour
    {
        public Item drop;
        public int hp = 4, amount = 3;
        public bool fells;            // árvores tombam
        public float respawn = 240f;  // segundos para voltar
        public Mesh stumpMesh;
        public int leafVariant = -1;  // cor das folhas que caem (Fx.Leaf*), -1 = sem folhas
        public float height = 6;      // altura aproximada (para a física da queda e de onde caem as folhas)
        public float scale0 = 1;

        enum St { Standing, Falling, Down, Gone, Growing }
        St st = St.Standing;
        Spring wobble;                // balanço da copa depois do golpe
        Vector3 wobbleAxis = Vector3.right;
        float angle, angVel, t, groundAngle;
        Quaternion baseRot;
        Vector3 basePos, fallAxis, fallDir;
        int maxHp, bounces;
        Renderer rend;
        Collider col;
        GameObject stump;

        void Awake()
        {
            baseRot = transform.rotation; basePos = transform.position; maxHp = hp;
            rend = GetComponent<Renderer>(); col = GetComponent<Collider>();
            enabled = false;          // só roda Update enquanto mexe (são milhares de plantas)
        }

        Vector3 Canopy => transform.position + transform.up * height * 0.72f;

        /// <summary>Recebe um golpe; devolve quantos itens caíram.</summary>
        public int Hit(Vector3 from)
        {
            if (st != St.Standing) return 0;
            enabled = true;
            hp--;
            var toward = transform.position - from; toward.y = 0; toward.Normalize();
            var hitPoint = transform.position + Vector3.up * (fells ? 1.0f : 0.4f) - toward * (fells ? 0.3f : 0.5f) * scale0;
            if (drop == Item.Madeira)
            {
                Audio.PlayAt("chop", hitPoint, 0.8f);
                Fx.Chips(hitPoint, -toward, 5);
                Fx.Leaves(Canopy, height * 0.2f, 3, leafVariant);
                if (fells) Audio.PlayAt("leaves", Canopy, 0.35f, 0.2f);
            }
            else if (drop == Item.Pedra) { Audio.PlayAt("mine", hitPoint, 0.8f); Fx.Stones(hitPoint, -toward, 4); Fx.Dust(hitPoint, 2, 0.5f); }
            else { Audio.PlayAt("rustle", hitPoint, 0.7f); Fx.Leaves(transform.position + Vector3.up * 0.6f, 0.6f, 4, leafVariant, 1.5f); }
            // a copa balança na direção do golpe
            wobbleAxis = Vector3.Cross(Vector3.up, toward);
            wobble.v += fells ? 26f : 60f;
            if (hp > 0) return drop == Item.Madeira ? 1 : 0;

            if (col) col.enabled = false;
            t = 0;
            if (fells)
            {
                st = St.Falling;
                fallDir = toward;
                fallAxis = Vector3.Cross(Vector3.up, fallDir);
                angle = 2f * Mathf.Deg2Rad; angVel = 0.12f;
                bounces = 0;
                // ângulo até encostar no chão (morro abaixo cai mais)
                var tip = basePos + fallDir * height * 0.9f;
                float fallDrop = basePos.y - (Fx.World != null ? Fx.World.Height(tip.x, tip.z) : basePos.y);
                groundAngle = Mathf.Clamp(90f + Mathf.Atan2(fallDrop, height * 0.9f) * Mathf.Rad2Deg, 70f, 108f) * Mathf.Deg2Rad - 0.05f;
                Audio.PlayAt("creak", transform.position + Vector3.up * 2, 0.9f);
                MakeStump();
            }
            else
            {
                st = St.Down;
                if (drop == Item.Pedra) { Fx.Stones(transform.position + Vector3.up * 0.4f, Vector3.up, 10); Fx.Dust(transform.position, 8, 1.2f); }
                else if (drop == Item.Frutinha) { Fx.Berries(transform.position + Vector3.up * 0.6f, 6); Fx.Leaves(transform.position + Vector3.up * 0.6f, 0.7f, 10, leafVariant, 2f); }
                else Fx.Leaves(transform.position + Vector3.up * 0.3f, 0.3f, 3, leafVariant, 1f);
            }
            return amount;
        }

        void MakeStump()
        {
            if (stumpMesh == null || stump != null) return;
            stump = Mats.Object("toco", stumpMesh, Mats.LowPoly, transform.parent);
            stump.transform.SetPositionAndRotation(basePos, baseRot);
            stump.transform.localScale = Vector3.one * scale0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            float w = wobble.Step(0, fells ? 1.1f : 2.5f, 0.12f, dt);
            var wob = Quaternion.AngleAxis(w * (fells ? 0.12f : 0.25f), wobbleAxis);

            switch (st)
            {
                case St.Standing:
                    transform.rotation = wob * baseRot;
                    if (Mathf.Abs(w) < 0.05f && Mathf.Abs(wobble.v) < 0.05f) { transform.rotation = baseRot; enabled = false; }
                    break;

                case St.Falling:
                {
                    // corpo rígido girando na base: α = (3g / 2L)·sen(θ)
                    float L = Mathf.Max(2f, height * 0.8f);
                    float alpha = 1.5f * 9.81f / L * Mathf.Sin(angle);
                    angVel += alpha * dt;
                    angle += angVel * dt;
                    if (angle >= groundAngle)
                    {
                        angle = groundAngle;
                        if (bounces == 0) Impact();
                        if (angVel > 0.6f && bounces < 2) { angVel = -angVel * 0.22f; bounces++; }
                        else { angVel = 0; st = St.Down; t = 0; }
                    }
                    transform.position = basePos;
                    transform.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, fallAxis) * baseRot;
                    break;
                }

                case St.Down:
                    if (fells)
                    {
                        // fica deitada um tempo e depois afunda no chão (a madeira já foi para a mochila)
                        if (t > 3.5f)
                        {
                            float k = Mathf.Clamp01((t - 3.5f) / 1.6f);
                            transform.position = basePos + Vector3.down * k * 1.4f * scale0;
                            if (k >= 1) Hide();
                        }
                    }
                    else
                    {
                        transform.rotation = wob * baseRot;
                        transform.localScale = Vector3.one * scale0 * Mathf.Max(0.01f, 1 - t * 3f);
                        if (t > 0.35f) Hide();
                    }
                    break;

                case St.Gone:
                    if (t > respawn) { st = St.Growing; t = 0; Show(); }
                    break;

                case St.Growing:
                {
                    // muda brotando: cresce rápido no começo e devagar no fim
                    float k = Ease.Out(t / (fells ? 8f : 3f));
                    transform.SetPositionAndRotation(basePos, baseRot);
                    transform.localScale = Vector3.one * scale0 * Mathf.Lerp(0.12f, 1, k);
                    if (k >= 1)
                    {
                        st = St.Standing; hp = maxHp;
                        if (col) col.enabled = true;
                        if (stump) { Destroy(stump); stump = null; }
                        transform.localScale = Vector3.one * scale0;
                        enabled = false;
                    }
                    break;
                }
            }
        }

        void Impact()
        {
            var top = basePos + fallDir * height * 0.65f;
            if (Fx.World != null) top.y = Fx.World.Height(top.x, top.z) + 0.5f;
            Audio.PlayAt("thud", top, 1f);
            Audio.PlayAt("leaves", top, 0.8f, 0.05f);
            Fx.Leaves(top + Vector3.up * 0.8f, height * 0.25f, 26, leafVariant, 2.5f);
            for (int i = 1; i <= 4; i++) Fx.Dust(basePos + fallDir * height * (i / 5f), 3, 1.1f);
            Fx.Chips(basePos + Vector3.up * 0.4f, fallDir, 4);
            // quem está perto sente o chão tremer
            var cam = Game.I != null && Game.I.Rig != null ? Game.I.Rig.cam.transform.position : top;
            float d = Vector3.Distance(cam, top);
            if (d < 22 && Game.I != null) Game.I.Rig.Shake(Mathf.Lerp(0.7f, 0.1f, d / 22f));
        }

        void Hide()
        {
            if (rend) rend.enabled = false;
            st = St.Gone; t = 0;
        }

        void Show()
        {
            if (rend) rend.enabled = true;
            transform.SetPositionAndRotation(basePos, baseRot);
            transform.localScale = Vector3.one * scale0 * 0.12f;
            angle = 0; angVel = 0;
        }
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
        // grama, capim, samambaias e flores: desenhados por instância, guardados em células de 16 m
        // (só as células perto da câmera são percorridas a cada quadro)
        const float Bucket = 16f;
        readonly List<(Mesh mesh, Dictionary<Vector2Int, List<Matrix4x4>> cells)> instanced = new();
        readonly Dictionary<string, Mesh> stumps = new();
        readonly Matrix4x4[] drawBuf = new Matrix4x4[1023];
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

                // tufo de grama: lâminas curvas em dois segmentos (base firme, ponta solta), escuro embaixo e claro na ponta
                mb = new MeshBuilder();
                var gBase = Color.Lerp(Grass, Color.black, 0.28f);
                for (int i = 0; i < 9; i++) Blade(mb, new Vector3(r.Range(-0.26f, 0.26f), 0, r.Range(-0.26f, 0.26f)), r.Range(0.3f, 0.58f), r.Range(0.035f, 0.055f), r, gBase, Color.Lerp(Grass, GrassL, r.Value), GrassL);
                Add("grama", mb.ToMesh("grama"));

                // capim alto com espiga (campos abertos e beira do rio)
                mb = new MeshBuilder();
                var straw = Col.Hex("#c8c070");
                for (int i = 0; i < 7; i++)
                {
                    var b0 = new Vector3(r.Range(-0.2f, 0.2f), 0, r.Range(-0.2f, 0.2f));
                    var tip = Blade(mb, b0, r.Range(0.7f, 1.05f), 0.04f, r, gBase, Color.Lerp(Grass, straw, 0.4f), Color.Lerp(GrassL, straw, 0.6f));
                    if (i % 2 == 0) mb.Blob(tip + Vector3.up * 0.05f, new Vector3(0.025f, 0.08f, 0.025f), straw, 0.1f, r, 0, 1f, 1f);
                }
                Add("capim", mb.ToMesh("capim"));

                // samambaia (chão da mata fechada): folhas compridas que arqueiam para fora
                mb = new MeshBuilder();
                var fern = Col.Hex("#3f7a34"); var fernL = Col.Hex("#6aa848");
                int fronds = 6 + v;
                for (int i = 0; i < fronds; i++)
                {
                    float a = i / (float)fronds * 6.28f + r.Range(-0.2f, 0.2f);
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    var side = new Vector3(-dir.z, 0, dir.x);
                    float len = r.Range(0.55f, 0.85f);
                    Vector3 prev = Vector3.zero;
                    for (int k = 1; k <= 4; k++)
                    {
                        float tt = k / 4f;
                        var pt = dir * len * tt + Vector3.up * (Mathf.Sin(tt * 2.2f) * 0.45f * len);
                        float wdt = 0.09f * Mathf.Sin(tt * Mathf.PI) + 0.02f;
                        float s0 = (k - 1) / 4f, s1 = tt;
                        var cA = Col.A(Color.Lerp(fern, fernL, s0), s0 * 0.9f); var cB = Col.A(Color.Lerp(fern, fernL, s1), s1 * 0.9f);
                        mb.Tri(prev, pt + side * wdt, pt, cA, cB, cB);
                        mb.Tri(prev, pt, pt - side * wdt, cA, cB, cB);
                        prev = pt;
                    }
                }
                Add("samambaia", mb.ToMesh("samambaia"));

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

        /// <summary>Lâmina de grama curva (2 segmentos). Devolve a ponta.</summary>
        static Vector3 Blade(MeshBuilder mb, Vector3 b, float h, float w, Rng r, Color cBase, Color cMid, Color cTip)
        {
            float a = r.Range(0, 6.28f);
            var side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * w;
            var lean = new Vector3(r.Range(-0.18f, 0.18f), 0, r.Range(-0.18f, 0.18f));
            var mid = b + lean * 0.35f + Vector3.up * h * 0.55f;
            var tip = b + lean + Vector3.up * h;
            Color A0 = Col.A(cBase, 0), A1 = Col.A(cMid, 0.4f), A2 = Col.A(cTip, 1);
            mb.Tri(b - side, mid - side * 0.6f, b + side, A0, A1, A0);
            mb.Tri(b + side, mid - side * 0.6f, mid + side * 0.6f, A0, A1, A1);
            mb.Tri(mid - side * 0.6f, tip, mid + side * 0.6f, A1, A2, A1);
            return tip;
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
            var kinds = new[] { "grama", "capim", "samambaia", "flor" };
            var cells = new Dictionary<Vector2Int, List<Matrix4x4>>[kinds.Length, 4];
            for (int k = 0; k < kinds.Length; k++) for (int i = 0; i < 4; i++) cells[k, i] = new Dictionary<Vector2Int, List<Matrix4x4>>();
            void Put(int kind, Vector3 q, float scale)
            {
                var c = new Vector2Int(Mathf.FloorToInt(q.x / Bucket), Mathf.FloorToInt(q.z / Bucket));
                var d = cells[kind, rng.Int(0, 3)];
                if (!d.TryGetValue(c, out var l)) d[c] = l = new List<Matrix4x4>();
                l.Add(Matrix4x4.TRS(q, Quaternion.Euler(rng.Range(-6, 6), rng.Range(0, 360f), rng.Range(-6, 6)), Vector3.one * scale));
            }
            bool Spot(Vector3 p, float spread, float minY, out Vector3 q)
            {
                q = p + new Vector3(rng.Range(-spread, spread), 0, rng.Range(-spread, spread));
                q.y = W.Height(q.x, q.z) - 0.02f;
                return q.y > minY;
            }
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
                    // grama, capim, samambaias e flores (instanciados)
                    bool forest = dens > 0.5f;
                    int tufts = inBase ? 4 : nearRiver ? 7 : forest ? 3 : 5;
                    for (int k = 0; k < tufts; k++) if (Spot(p, 1.7f, 0.35f, out var q)) Put(0, q, rng.Range(0.75f, 1.45f));
                    if (!inBase && (nearRiver ? rng.Chance(0.8f) : dens < 0.44f && rng.Chance(0.45f)))
                        for (int k = rng.Int(1, 3); k > 0; k--) if (Spot(p, 1.5f, 0.35f, out var q)) Put(1, q, rng.Range(0.8f, 1.25f));
                    if (forest && !inBase && rng.Chance(0.55f))
                        for (int k = rng.Int(1, 2); k > 0; k--) if (Spot(p, 1.5f, 0.4f, out var q)) Put(2, q, rng.Range(0.8f, 1.5f));
                    if (rng.Chance(dens < 0.45f ? 0.4f : 0.08f))
                        for (int k = rng.Int(1, 3); k > 0; k--) if (Spot(p, 1.4f, 0.5f, out var q)) Put(3, q, rng.Range(0.8f, 1.3f));
                    if ((nearRiver || dens > 0.5f) && rng.Chance(0.12f)) FireflySpots.Add(p + Vector3.up);
                }
            for (int k = 0; k < kinds.Length; k++)
                for (int i = 0; i < 4; i++) instanced.Add((meshes[kinds[k]][i], cells[k, i]));
        }

        void Place(string kind, Vector3 p, Vector3 normal, float scale, Item drop, int hp, int amount, bool tree = false)
        {
            var list = meshes[kind];
            int variant = rng.Int(0, list.Count - 1);
            var mesh = list[variant];
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
            g.leafVariant = kind switch
            {
                "pinheiro" => Fx.LeafPine, "carvalho" => variant == 3 ? Fx.LeafAutumn : Fx.LeafOak, "betula" => Fx.LeafBirch,
                "arbusto" => Fx.LeafBush, _ => -1,
            };
            g.height = scale * kind switch { "pinheiro" => 8.5f, "carvalho" => 5.8f, "betula" => 6.5f, "morta" => 5f, _ => 1f };
            if (tree) g.stumpMesh = Stump(kind);
        }

        /// <summary>Toco que fica no lugar da árvore cortada (anéis claros no topo, corte inclinado).</summary>
        Mesh Stump(string kind)
        {
            if (stumps.TryGetValue(kind, out var m)) return m;
            var r = new Rng(kind.Length * 17);
            float rad = kind == "carvalho" ? 0.52f : kind == "pinheiro" ? 0.3f : kind == "betula" ? 0.2f : 0.32f;
            var bark = kind == "betula" ? Birch : kind == "morta" ? Dead : Bark;
            var mb = new MeshBuilder();
            mb.Cylinder(new Vector3(0, -0.1f, 0), 0.42f, rad * 1.15f, rad, 7, bark, 0, 0, 0, r, false);
            // topo cortado: miolo claro + anel
            var top = new Vector3(0, 0.32f, 0);
            for (int i = 0; i < 7; i++)
            {
                float a0 = i / 7f * 6.28f, a1 = (i + 1) / 7f * 6.28f;
                var p0 = new Vector3(Mathf.Cos(a0) * rad, 0.32f + Mathf.Cos(a0) * 0.05f, Mathf.Sin(a0) * rad);
                var p1 = new Vector3(Mathf.Cos(a1) * rad, 0.32f + Mathf.Cos(a1) * 0.05f, Mathf.Sin(a1) * rad);
                mb.Tri(top, p1, p0, i % 2 == 0 ? Col.Hex("#e8cc98") : Col.Hex("#d8b880"));
            }
            for (int i = 0; i < 3; i++)   // raízes
            {
                float a = i / 3f * 6.28f + 0.5f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                mb.Tri(d * rad * 0.8f + Vector3.up * 0.15f, d * rad * 2.1f + Vector3.down * 0.05f, d * rad * 0.9f + new Vector3(-d.z, 0, d.x) * 0.12f, bark);
                mb.Tri(d * rad * 0.8f + Vector3.up * 0.15f, d * rad * 0.9f - new Vector3(-d.z, 0, d.x) * 0.12f, d * rad * 2.1f + Vector3.down * 0.05f, bark);
            }
            m = mb.ToMesh("toco_" + kind);
            stumps[kind] = m;
            return m;
        }

        /// <summary>Desenha grama, capim, samambaias e flores perto da câmera (instanciado, sem objetos).</summary>
        public void Draw(Vector3 cam)
        {
            const float R = 80f;
            int cx = Mathf.FloorToInt(cam.x / Bucket), cz = Mathf.FloorToInt(cam.z / Bucket), rc = Mathf.CeilToInt(R / Bucket);
            var buf = drawBuf;
            foreach (var (mesh, cells) in instanced)
            {
                int n = 0;
                for (int z = cz - rc; z <= cz + rc; z++)
                    for (int x = cx - rc; x <= cx + rc; x++)
                    {
                        if (!cells.TryGetValue(new Vector2Int(x, z), out var list)) continue;
                        float ddx = (x + 0.5f) * Bucket - cam.x, ddz = (z + 0.5f) * Bucket - cam.z;
                        if (ddx * ddx + ddz * ddz > (R + Bucket) * (R + Bucket)) continue;
                        foreach (var m in list)
                        {
                            buf[n++] = m;
                            if (n == buf.Length) { Graphics.DrawMeshInstanced(mesh, 0, Mats.Grass, buf, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, true); n = 0; }
                        }
                    }
                if (n > 0) Graphics.DrawMeshInstanced(mesh, 0, Mats.Grass, buf, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, true);
            }
        }
    }
}
