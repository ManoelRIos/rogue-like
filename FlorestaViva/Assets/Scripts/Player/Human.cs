using System;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Peregrino (personagem original, visual suave e estilizado): esqueleto completo (quadril, coluna, peito,
    /// pescoço, cabeça, ombro→cotovelo→mão, coxa→joelho→pé), túnica em painéis que reagem às pernas e ao vento,
    /// cachecol de pano com física, bolsa com mola e cajado com cristal que acende à noite.
    /// </summary>
    public class HumanRig
    {
        public Transform root, pelvis, spine, chest, neck, head, pack, scarf, tool, staffTip;
        public Vector3[] robeDir;
        public Mesh skirt;
        public Vector3[] skirtV;
        public const int SkirtCols = 24, SkirtRows = 4;
        const float SkirtLen = 0.4f;

        /// <summary>Monta a saia: para cada coluna ao redor do quadril, abre no ângulo interpolado dos 10 setores.</summary>
        public void UpdateSkirt(float[] flareDeg)
        {
            for (int j = 0; j < SkirtCols; j++)
            {
                float u = j / (float)SkirtCols * 10f;
                int i0 = Mathf.FloorToInt(u) % 10, i1 = (i0 + 1) % 10;
                float f = Mathf.Lerp(flareDeg[i0], flareDeg[i1], u - Mathf.Floor(u)) * Mathf.Deg2Rad;
                float a = j / (float)SkirtCols * Mathf.PI * 2;
                var d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var top = new Vector3(d.x * 0.158f, -0.05f, d.z * 0.132f);
                var outward = new Vector3(d.x, 0, d.z * 0.9f);
                for (int k = 0; k < SkirtRows + 2; k++)
                {
                    float t = Mathf.Min(1, k / (float)SkirtRows) * SkirtLen;
                    var p = top + outward * (Mathf.Sin(f) * t + t * 0.12f) + Vector3.down * (Mathf.Cos(f) * t);
                    if (k == SkirtRows + 1) p += Vector3.up * 0.045f + outward * 0.004f;      // barra colorida (dobra para dentro)
                    skirtV[k * SkirtCols + j] = p;
                }
            }
            skirt.vertices = skirtV;
            skirt.RecalculateNormals();
            skirt.RecalculateBounds();
        }
        public Light staffLight;
        public Material crystalMat;
        public ClothRibbon ribbon;
        public readonly Transform[] thigh = new Transform[2], shin = new Transform[2], foot = new Transform[2],
            upper = new Transform[2], fore = new Transform[2], hand = new Transform[2];
        public Renderer[] renderers, firstPerson;   // na 1ª pessoa só antebraços, mãos e ferramenta aparecem
        public const float Thigh = 0.40f, Shin = 0.38f, Ankle = 0.075f, HipHeight = 0.86f;
        public static readonly Vector3 HipL = new(-0.095f, -0.03f, 0), HipR = new(0.095f, -0.03f, 0);

        static readonly Color Skin = Col.Hex("#eec0a0"), SkinD = Col.Hex("#d49a7c"), Blush = Col.Hex("#f0a0a0"), Boot = Col.Hex("#6a4a36"), Sole = Col.Hex("#3a2a20"),
            Pants = Col.Hex("#4e5068"), Wrap = Col.Hex("#d8c8a8"), Robe = Col.Hex("#e8dcc4"), Sash = Col.Hex("#ec7a52"),
            Satchel = Col.Hex("#a8784c"), Crystal = Col.Hex("#9af6ff"), Eye = Col.Hex("#2a2030");

        static Transform Node(string n, Transform parent, Vector3 pos)
        {
            var t = new GameObject(n).transform;
            t.SetParent(parent, false); t.localPosition = pos;
            return t;
        }

        static Transform Part(string n, Transform parent, MeshBuilder mb, Vector3 pos)
        {
            var go = Mats.Object(n, mb.ToMesh(n), Mats.LowPoly, parent);
            go.transform.localPosition = pos;
            return go.transform;
        }

        public static HumanRig Build(Color cloak, Transform parent)
        {
            // Peregrino: túnica clara até abaixo do joelho (em painéis que balançam), capuz macio com ponta,
            // gola/manto na cor do jogador, faixa coral na cintura, mangas largas, bolsa na cintura,
            // cachecol comprido de pano (física) e cajado nas costas com cristal que brilha.
            var r = new HumanRig();
            var rng = new Rng(3);
            var robe = Robe; var robeD = Color.Lerp(Robe, Col.Hex("#b8a888"), 0.55f);
            var mantle = cloak; var mantleD = Color.Lerp(cloak, Color.black, 0.25f);
            r.root = Node("Peregrino", parent, Vector3.zero);

            // ---------------- quadril, saia da túnica e pernas
            r.pelvis = Node("quadril", r.root, new Vector3(0, HipHeight, 0));
            var mb = new MeshBuilder();
            mb.Cylinder(new Vector3(0, -0.09f, 0), 0.16f, 0.15f, 0.155f, 10, robe, 0, 0, 0, rng);
            mb.Cylinder(new Vector3(0, 0.03f, 0), 0.07f, 0.165f, 0.16f, 10, Sash, 0, 0, 0, rng);            // faixa
            mb.Scale(new Vector3(1, 1, 0.82f));
            Part("quadril_m", r.pelvis, mb, Vector3.zero);
            // ponta da faixa caindo do lado
            mb = new MeshBuilder();
            mb.Box(new Vector3(0.12f, -0.26f, 0.06f), new Vector3(0.17f, 0.06f, 0.09f), Sash);
            Part("faixa", r.pelvis, mb, Vector3.zero);
            // bolsa do peregrino
            r.pack = Node("bolsa", r.pelvis, new Vector3(-0.17f, -0.02f, -0.02f));
            mb = new MeshBuilder { SmoothBlobs = 0.6f };
            mb.Blob(new Vector3(-0.03f, -0.1f, 0), new Vector3(0.07f, 0.09f, 0.1f), Satchel, 0.04f, rng, 1);
            mb.Box(new Vector3(-0.1f, -0.06f, -0.08f), new Vector3(-0.09f, 0.0f, 0.08f), Col.Hex("#6a4a30"));
            Part("bolsa_m", r.pack, mb, Vector3.zero);
            // saia da túnica: uma malha contínua (sem vãos) que o animador deforma a cada quadro —
            // 10 "setores" com mola abrem com a perna que passa, com a corrida, o vento e as quedas
            r.robeDir = new Vector3[10];
            for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2; r.robeDir[i] = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); }
            r.skirt = new Mesh { name = "saia" };
            r.skirt.MarkDynamic();
            r.skirtV = new Vector3[SkirtCols * (SkirtRows + 2)];
            var sc = new Color[r.skirtV.Length];
            var tri = new System.Collections.Generic.List<int>();
            for (int k = 0; k < SkirtRows + 2; k++)
                for (int j = 0; j < SkirtCols; j++)
                {
                    float tt = Mathf.Min(1, k / (float)SkirtRows);
                    sc[k * SkirtCols + j] = k > SkirtRows ? Col.A(mantleD, 0) : k == SkirtRows ? Col.A(mantle, 0) : Col.A(Color.Lerp(robe, robeD, tt), 0);
                    if (k > 0) { int a0 = (k - 1) * SkirtCols + j, a1 = (k - 1) * SkirtCols + (j + 1) % SkirtCols, b0 = k * SkirtCols + j, b1 = k * SkirtCols + (j + 1) % SkirtCols; tri.Add(a0); tri.Add(b0); tri.Add(a1); tri.Add(a1); tri.Add(b0); tri.Add(b1); }
                }
            r.skirt.vertices = r.skirtV; r.skirt.colors = sc; r.skirt.triangles = tri.ToArray();
            r.UpdateSkirt(new float[10]);
            Mats.Object("saia", r.skirt, Mats.LowPoly, r.pelvis);

            for (int s = 0; s < 2; s++)
            {
                string side = s == 0 ? "E" : "D";
                r.thigh[s] = Node("coxa" + side, r.pelvis, s == 0 ? HipL : HipR);
                mb = new MeshBuilder();
                mb.Cylinder(new Vector3(0, -Thigh, 0), Thigh + 0.02f, 0.058f, 0.08f, 8, Pants, 0, 0, 0, rng);
                Part("coxa_m", r.thigh[s], mb, Vector3.zero);
                r.shin[s] = Node("canela" + side, r.thigh[s], new Vector3(0, -Thigh, 0));
                mb = new MeshBuilder();
                mb.Blob(Vector3.zero, Vector3.one * 0.062f, Pants, 0.03f, rng, 0);
                mb.Cylinder(new Vector3(0, -Shin, 0), Shin, 0.048f, 0.06f, 8, Pants, 0, 0, 0, rng);
                mb.Cylinder(new Vector3(0, -Shin, 0), 0.17f, 0.058f, 0.064f, 8, Boot, 0, 0, 0, rng);
                mb.Cylinder(new Vector3(0, -Shin + 0.16f, 0), 0.035f, 0.07f, 0.068f, 8, Wrap, 0, 0, 0, rng);   // faixa da bota
                Part("canela_m", r.shin[s], mb, Vector3.zero);
                r.foot[s] = Node("pe" + side, r.shin[s], new Vector3(0, -Shin, 0));
                mb = new MeshBuilder { SmoothBlobs = 0.5f };
                mb.Blob(new Vector3(0, -Ankle + 0.035f, 0.045f), new Vector3(0.055f, 0.038f, 0.11f), Boot, 0.03f, rng, 1);
                mb.Box(new Vector3(-0.052f, -Ankle, -0.06f), new Vector3(0.052f, -Ankle + 0.012f, 0.155f), Sole);
                Part("pe_m", r.foot[s], mb, Vector3.zero);
            }

            // ---------------- tronco: túnica clara + manto curto nos ombros
            r.spine = Node("coluna", r.pelvis, new Vector3(0, 0.05f, 0));
            mb = new MeshBuilder();
            mb.Cylinder(Vector3.zero, 0.26f, 0.15f, 0.158f, 10, robe, 0, 0, 0, rng);
            mb.Scale(new Vector3(1, 1, 0.8f));
            for (int k = 0; k < mb.v.Count; k++) { var q = mb.v[k]; mb.Soften(k, new Vector3(q.x, 0, q.z / 0.64f).normalized, 0.8f); }
            Part("barriga", r.spine, mb, Vector3.zero);

            r.chest = Node("peito", r.spine, new Vector3(0, 0.25f, 0));
            mb = new MeshBuilder();
            mb.Cylinder(Vector3.zero, 0.24f, 0.158f, 0.19f, 10, robe, 0, 0, 0, rng);
            mb.Scale(new Vector3(1, 1, 0.74f));
            for (int k = 0; k < mb.v.Count; k++) { var q = mb.v[k]; mb.Soften(k, new Vector3(q.x, 0.1f, q.z / 0.55f).normalized, 0.8f); }
            int c0 = mb.v.Count;
            // manto: gola larga e arredondada caindo sobre os ombros
            mb.Cylinder(new Vector3(0, 0.12f, 0), 0.2f, 0.27f, 0.12f, 12, mantle, 0, 0, 0, rng);
            mb.Cylinder(new Vector3(0, 0.08f, 0), 0.045f, 0.275f, 0.27f, 12, mantleD, 0, 0, 0, rng);
            for (int k = c0; k < mb.v.Count; k++) { var q = mb.v[k]; mb.Soften(k, new Vector3(q.x, 0.9f, q.z).normalized, 0.85f); }
            mb.Scale(new Vector3(1, 1, 0.85f), c0);
            // alça do cajado atravessando o peito
            mb.Box(new Vector3(-0.15f, 0.02f, 0.12f), new Vector3(0.15f, 0.045f, 0.145f), Col.Hex("#6a4a30"));
            Part("peito_m", r.chest, mb, Vector3.zero);

            // cajado nas costas (diagonal) com cristal
            var staff = Node("cajado", r.chest, new Vector3(0, 0.12f, -0.17f));
            staff.localRotation = Quaternion.Euler(0, 0, 28);
            mb = new MeshBuilder();
            mb.Cylinder(new Vector3(0, -0.62f, 0), 1.25f, 0.022f, 0.026f, 6, Col.Hex("#8a5a3a"), 0, 0, 0, rng);
            mb.Cylinder(new Vector3(0, 0.6f, 0), 0.08f, 0.04f, 0.03f, 6, Col.Hex("#c89a50"), 0, 0, 0, rng);       // anel
            Part("cajado_m", staff, mb, Vector3.zero);
            var cm = new MeshBuilder();
            cm.Cylinder(new Vector3(0, 0.66f, 0), 0.09f, 0.0f, 0.045f, 5, Crystal, 0, 0, 0, rng, false);
            cm.Cylinder(new Vector3(0, 0.75f, 0), 0.11f, 0.045f, 0.0f, 5, Crystal, 0, 0, 0, rng, false);
            r.crystalMat = new Material(Mats.Glow) { name = "cristal" };
            var cg = Mats.Object("cristal", cm.ToMesh("cristal"), r.crystalMat, staff, false);
            r.staffTip = cg.transform;
            var lg = new GameObject("luz_cajado");
            lg.transform.SetParent(staff, false); lg.transform.localPosition = new Vector3(0, 0.74f, 0);
            r.staffLight = lg.AddComponent<Light>();
            r.staffLight.type = LightType.Point; r.staffLight.color = Col.Hex("#8ff0ff"); r.staffLight.range = 7; r.staffLight.intensity = 0; r.staffLight.shadows = LightShadows.None;

            // ---------------- pescoço e cabeça (capuz macio com ponta que cai para trás)
            r.neck = Node("pescoco", r.chest, new Vector3(0, 0.29f, 0));
            mb = new MeshBuilder();
            mb.Cylinder(Vector3.zero, 0.08f, 0.05f, 0.046f, 8, Skin, 0, 0, 0, rng);
            Part("pescoco_m", r.neck, mb, Vector3.zero);
            r.head = Node("cabeca", r.neck, new Vector3(0, 0.07f, 0.005f));
            mb = new MeshBuilder { SmoothBlobs = 0.85f };
            mb.Blob(new Vector3(0, 0.1f, 0.012f), new Vector3(0.092f, 0.112f, 0.1f), Skin, 0.0f, rng, 1);           // rosto
            mb.Blob(new Vector3(0, 0.125f, -0.04f), new Vector3(0.132f, 0.148f, 0.13f), mantle, 0.0f, rng, 1);      // capuz
            mb.Blob(new Vector3(0, 0.05f, -0.035f), new Vector3(0.12f, 0.06f, 0.12f), mantleD, 0.0f, rng, 0);       // borda do capuz no pescoço
            mb.SmoothBlobs = 0;
            int h0 = mb.v.Count;
            mb.Cylinder(new Vector3(0, 0.2f, -0.12f), 0.24f, 0.07f, 0.0f, 7, mantle, 0, 0, 0, rng);                // ponta do capuz
            for (int k = h0; k < mb.v.Count; k++) { var q = mb.v[k] - new Vector3(0, 0.2f, -0.12f); mb.Soften(k, new Vector3(q.x, 0.4f, q.z).normalized, 0.8f); }
            for (int k = h0; k < mb.v.Count; k++)                                                                  // ponta curvada para trás
            {
                var q = mb.v[k]; float up = Mathf.Max(0, q.y - 0.2f);
                mb.v[k] = new Vector3(q.x, 0.2f + up * 0.35f, q.z - up * 1.1f);                  // cai para trás, macia
            }
            mb.Box(new Vector3(-0.05f, 0.105f, 0.1f), new Vector3(-0.022f, 0.13f, 0.112f), Eye);                  // olhos
            mb.Box(new Vector3(0.022f, 0.105f, 0.1f), new Vector3(0.05f, 0.13f, 0.112f), Eye);
            mb.Box(new Vector3(-0.011f, 0.07f, 0.104f), new Vector3(0.011f, 0.1f, 0.122f), SkinD);                 // nariz
            mb.Blob(new Vector3(-0.055f, 0.08f, 0.085f), new Vector3(0.02f, 0.012f, 0.01f), Blush, 0, rng, 0);    // bochechas
            mb.Blob(new Vector3(0.055f, 0.08f, 0.085f), new Vector3(0.02f, 0.012f, 0.01f), Blush, 0, rng, 0);
            Part("cabeca_m", r.head, mb, Vector3.zero);

            // ---------------- braços com mangas largas
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1 : 1;
                string side = s == 0 ? "E" : "D";
                r.upper[s] = Node("braco" + side, r.chest, new Vector3(sx * 0.19f, 0.215f, 0));
                mb = new MeshBuilder { SmoothBlobs = 0.7f };
                mb.Blob(Vector3.zero, new Vector3(0.075f, 0.07f, 0.075f), mantle, 0.03f, rng, 0);
                mb.SmoothBlobs = 0;
                mb.Cylinder(new Vector3(0, -0.28f, 0), 0.28f, 0.055f, 0.062f, 8, robe, 0, 0, 0, rng);
                Part("braco_m", r.upper[s], mb, Vector3.zero);
                r.fore[s] = Node("antebraco" + side, r.upper[s], new Vector3(0, -0.28f, 0));
                mb = new MeshBuilder();
                mb.Blob(Vector3.zero, Vector3.one * 0.056f, robe, 0.03f, rng, 0);
                mb.Cylinder(new Vector3(0, -0.24f, 0), 0.24f, 0.075f, 0.055f, 8, robe, 0, 0, 0, rng);              // manga abre no punho
                mb.Cylinder(new Vector3(0, -0.245f, 0), 0.03f, 0.079f, 0.077f, 8, mantle, 0, 0, 0, rng);
                Part("antebraco_m", r.fore[s], mb, Vector3.zero);
                r.hand[s] = Node("mao" + side, r.fore[s], new Vector3(0, -0.25f, 0));
                mb = new MeshBuilder { SmoothBlobs = 0.7f };
                mb.Blob(new Vector3(0, -0.045f, 0.008f), new Vector3(0.04f, 0.055f, 0.03f), Skin, 0.03f, rng, 0);
                mb.Blob(new Vector3(sx * -0.03f, -0.032f, 0.025f), new Vector3(0.017f, 0.028f, 0.017f), Skin, 0.03f, rng, 0);
                Part("mao_m", r.hand[s], mb, Vector3.zero);
            }

            // ponto onde o cachecol de pano (simulado) se prende: atrás do pescoço
            r.scarf = Node("cachecol", r.chest, new Vector3(0.05f, 0.27f, -0.15f));                               // na superfície das costas
            mb = new MeshBuilder { SmoothBlobs = 0.7f };
            mb.Blob(new Vector3(-0.05f, 0.02f, 0.13f), new Vector3(0.12f, 0.05f, 0.11f), Sash, 0.03f, rng, 1);    // volta no pescoço
            Part("cachecol_m", r.scarf, mb, Vector3.zero);

            r.tool = Mats.Object("ferramenta", Rig.ToolMesh(0), Mats.LowPoly, r.hand[1]).transform;
            r.tool.localPosition = new Vector3(0, -0.06f, 0.01f);
            r.tool.localRotation = Quaternion.Euler(80, 0, 0);
            r.ribbon = new ClothRibbon(r, Sash, Color.Lerp(Sash, Col.Hex("#b0503a"), 0.6f));
            r.renderers = r.root.GetComponentsInChildren<Renderer>(true);
            var fp = new System.Collections.Generic.List<Renderer>();
            fp.AddRange(r.fore[0].GetComponentsInChildren<Renderer>(true));
            fp.AddRange(r.fore[1].GetComponentsInChildren<Renderer>(true));
            r.firstPerson = fp.ToArray();
            return r;
        }

        public int ToolIndex;
        public void SetTool(int i)
        {
            ToolIndex = i;
            tool.GetComponent<MeshFilter>().sharedMesh = Rig.ToolMesh(i);
        }

        /// <summary>Como a ferramenta fica na mão: carregada pendurada (cabeça para baixo) e virada para frente só no golpe.</summary>
        public void PoseTool(float action, float dt)
        {
            float use = action > 0 ? Mathf.Clamp01(action / 0.15f) * Mathf.Clamp01((1 - action) / 0.15f) : 0;
            toolBlend = Mathf.MoveTowards(toolBlend, use, dt * 8f);
            var carry = ToolIndex <= 1 ? Quaternion.Euler(158, 0, 0) : Quaternion.Euler(10, 0, 0);
            var swing = ToolIndex <= 1 ? Quaternion.Euler(80, 0, 0) : Quaternion.Euler(10, 0, 0);
            tool.localRotation = Quaternion.Slerp(carry, swing, Ease.Smooth(toolBlend));
        }
        float toolBlend;
    }

    /// <summary>
    /// Animação procedural do Andarilho. Nada de clipes prontos: os pés são posicionados por IK em cima do
    /// chão de verdade (sem patinar, sobem e descem morros), o quadril sobe e desce no ritmo do passo,
    /// o corpo inclina ao acelerar e nas curvas, os braços balançam contra as pernas, a cabeça olha para
    /// onde a câmera aponta, e capa/mochila/cachecol seguem com molas. Parado, ele respira, troca o peso
    /// de perna e dá passinhos para acompanhar quando você gira no lugar.
    /// </summary>
    public class HumanAnimator
    {
        readonly HumanRig r;
        public Action<int, Vector3> OnFootstep;
        public Action<Vector3, float> OnLand;

        float phase, time, runW, airW, prevYaw, turnRate, glanceT, glanceYaw;
        Spring moveS, runS, pelvisY, land, lean, bank, headYaw, headPitch, packS, packSide, airS, actS;
        readonly Spring[] robeS = new Spring[10];
        readonly float[] thighAngle = new float[2];
        readonly float[] flares = new float[10];
        Vector3 prevVel, accel;
        bool wasGrounded = true;
        readonly bool[] stance = { true, true };
        readonly Vector3[] lockW = new Vector3[2], stepFrom = new Vector3[2];
        readonly float[] stepT = { 1, 1 };
        readonly bool[] locked = new bool[2];
        readonly Vector3[] footW = new Vector3[2];
        readonly float[] footPitch = new float[2];
        float fallSpeed;
        Vector3 lastRoot;
        bool started;

        /// <summary>Recoloca os pés embaixo do corpo (início do jogo, renascer, teleporte).</summary>
        public void Snap()
        {
            for (int i = 0; i < 2; i++) { footW[i] = r.root.TransformPoint(new Vector3(i == 0 ? -0.1f : 0.1f, 0, 0)); locked[i] = false; stepT[i] = 1; }
            pelvisY.x = HumanRig.HipHeight; pelvisY.v = 0; land = default;
            prevVel = Vector3.zero; accel = Vector3.zero;
            prevYaw = r.root.eulerAngles.y;
            lastRoot = r.root.position;
            wasGrounded = true; fallSpeed = 0;
        }

        public HumanAnimator(HumanRig rig)
        {
            r = rig;
            pelvisY.x = HumanRig.HipHeight;
            for (int i = 0; i < 2; i++) { footW[i] = r.root.TransformPoint(new Vector3(i == 0 ? -0.1f : 0.1f, 0, 0)); }
            prevYaw = r.root.eulerAngles.y;
        }

        static float GroundY(Vector3 p, float fallback, out Vector3 normal)
        {
            if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out var hit, 2.6f, 1 << Layers.Ground, QueryTriggerInteraction.Ignore))
            {
                normal = hit.normal;
                return hit.point.y;
            }
            normal = Vector3.up;
            return fallback;
        }

        /// <param name="vel">velocidade do corpo (m/s, mundo)</param>
        /// <param name="look">para onde a câmera olha (mundo)</param>
        /// <param name="action">0..1 do golpe com a ferramenta (0 = nenhum)</param>
        public void Tick(float dt, Vector3 vel, bool grounded, Vector3 look, Tool tool, float action)
        {
            if (dt <= 0) return;
            time += dt;
            var root = r.root;
            if (!started || (root.position - lastRoot).sqrMagnitude > 9) { Snap(); started = true; }
            lastRoot = root.position;
            var flat = new Vector3(vel.x, 0, vel.z);
            float speed = flat.magnitude;
            accel = Vector3.Lerp(accel, (vel - prevVel) / dt, 1 - Mathf.Exp(-dt * 10));
            prevVel = vel;
            var accLocal = root.InverseTransformDirection(accel);
            float yaw = root.eulerAngles.y;
            turnRate = Mathf.Lerp(turnRate, Mathf.DeltaAngle(prevYaw, yaw) / dt, 1 - Mathf.Exp(-dt * 8));
            prevYaw = yaw;

            float moveW = Mathf.Clamp01(moveS.Step(Mathf.Clamp01(speed / 1.4f), 4f, 1f, dt));
            runW = Mathf.Clamp01(runS.Step(Mathf.InverseLerp(3.8f, 6.0f, speed), 2.5f, 1f, dt));
            airW = Mathf.Clamp01(airS.Step(grounded ? 0 : 1, 5f, 0.9f, dt));

            // ---------------- ritmo do passo: cadência sobe com a velocidade; passada = distância que o pé fica no chão
            float freq = 1.5f + 0.32f * speed;
            float duty = Mathf.Lerp(0.62f, 0.37f, runW);
            float stride = Mathf.Min(speed * duty / freq, 0.8f);
            phase = Ease.Frac(phase + freq * dt * Mathf.Max(moveW, 0.0f));

            // ---------------- aterrissagem
            if (!grounded) fallSpeed = Mathf.Max(fallSpeed, -vel.y);
            if (grounded && !wasGrounded)
            {
                land.v -= Mathf.Clamp(fallSpeed * 0.35f, 0.4f, 3.5f);
                OnLand?.Invoke(root.position, fallSpeed);
                fallSpeed = 0;
            }
            wasGrounded = grounded;
            float landDip = land.Step(0, 3.2f, 0.45f, dt);

            // ---------------- quadril: sobe e desce, balança para o lado da perna de apoio, gira com a passada
            float bobAmp = Mathf.Lerp(0.028f, -0.05f, runW) * moveW;
            float bob = Mathf.Cos(4 * Mathf.PI * (phase - duty * 0.5f)) * bobAmp;
            float sway = -Mathf.Cos(2 * Mathf.PI * (phase - duty * 0.5f)) * 0.024f * (1 - runW) * moveW;
            float idleShift = Mathf.Sin(time * 0.37f) * 0.022f * (1 - moveW);
            float twist = Mathf.Cos(2 * Mathf.PI * phase) * 7f * moveW;

            // pés: alvo de cada perna (local ao corpo), depois chão de verdade por raycast
            float hipLimit = float.MaxValue;     // o quadril desce o necessário para o pé de apoio alcançar o chão
            var targetLocal = new Vector3[2];
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.1f : 0.1f;
                float p = Ease.Frac(phase + (i == 0 ? 0 : 0.5f));
                bool inStance = p < duty;
                float z, lift = 0;
                if (inStance) z = Mathf.Lerp(stride * 0.5f, -stride * 0.5f, p / duty);
                else
                {
                    float t = (p - duty) / (1 - duty);
                    z = Mathf.Lerp(-stride * 0.5f, stride * 0.5f, Ease.Smooth(t));
                    lift = Mathf.Sin(t * Mathf.PI) * (0.09f + 0.13f * runW) * Mathf.Clamp01(stride * 3f);
                }
                // passo detectado: pé tocou o chão
                if (inStance && !stance[i] && moveW > 0.35f && grounded) OnFootstep?.Invoke(i, footW[i]);
                stance[i] = inStance;

                var gait = new Vector3(x, lift, z + 0.04f * runW);

                // parado: pé "preso" no mundo; se o corpo girar/andar demais, dá um passinho
                var rest = new Vector3(x, 0, i == 0 ? 0.03f : -0.03f);
                var restW = root.TransformPoint(rest);
                if (moveW > 0.4f || !grounded) locked[i] = false;
                else if (!locked[i] && moveW < 0.15f) { locked[i] = true; lockW[i] = footW[i]; stepT[i] = 1; }
                if (locked[i])
                {
                    var other = 1 - i;
                    if (stepT[i] >= 1 && stepT[other] >= 0.6f && (Flat(lockW[i] - restW).magnitude > 0.2f))
                    { stepT[i] = 0; stepFrom[i] = lockW[i]; }
                    if (stepT[i] < 1)
                    {
                        stepT[i] = Mathf.Min(1, stepT[i] + dt / 0.28f);
                        lockW[i] = Vector3.Lerp(stepFrom[i], restW, Ease.Smooth(stepT[i]));
                        if (stepT[i] >= 1) OnFootstep?.Invoke(i, lockW[i]);
                    }
                }
                var idleLocal = locked[i] ? root.InverseTransformPoint(lockW[i]) : rest;
                float idleLift = locked[i] && stepT[i] < 1 ? Mathf.Sin(stepT[i] * Mathf.PI) * 0.07f : 0;
                idleLocal.y = idleLift;

                var local = Vector3.Lerp(idleLocal, gait, moveW);
                var w = root.TransformPoint(new Vector3(local.x, 0, local.z));
                float gy = GroundY(w, root.position.y, out var n);
                var nL = root.InverseTransformDirection(n);
                footPitch[i] = Mathf.Lerp(footPitch[i], Mathf.Atan2(nL.z, nL.y) * Mathf.Rad2Deg, 1 - Mathf.Exp(-dt * 12));
                float groundLocal = gy - root.position.y;
                local.y = groundLocal + local.y;
                targetLocal[i] = local;
                if (inStance || locked[i] || moveW < 0.5f)
                {
                    const float reach = (HumanRig.Thigh + HumanRig.Shin) * 0.985f;
                    float dz = local.z, dx = local.x - (i == 0 ? HumanRig.HipL.x : HumanRig.HipR.x);
                    hipLimit = Mathf.Min(hipLimit, groundLocal + HumanRig.Ankle + Mathf.Sqrt(Mathf.Max(0.01f, reach * reach - dz * dz - dx * dx)) - HumanRig.HipL.y);
                }
            }

            float crouch = runW * 0.05f + airW * 0.02f;
            float pyT = Mathf.Min(HumanRig.HipHeight - crouch + bob, hipLimit);
            if (!grounded) pyT = HumanRig.HipHeight - crouch;
            float py = pelvisY.Step(Mathf.Max(pyT, 0.35f), 9f, 0.8f, dt) + landDip;
            r.pelvis.localPosition = new Vector3(sway + idleShift, py, 0);
            float bankT = Mathf.Clamp(-turnRate * speed * 0.005f, -14, 14);
            float bk = bank.Step(bankT, 2f, 0.8f, dt);
            r.pelvis.localRotation = Quaternion.Euler(0, twist, bk);

            // ---------------- pernas (IK de dois ossos no espaço do quadril)
            for (int i = 0; i < 2; i++)
            {
                Vector3 tgt;
                if (airW > 0.01f)
                {
                    // no ar: uma perna à frente, joelhos dobrados
                    var air = new Vector3(i == 0 ? -0.1f : 0.1f, py - (i == 0 ? 0.55f : 0.45f) + (vel.y < 0 ? -0.08f : 0), i == 0 ? 0.14f : -0.12f);
                    tgt = Vector3.Lerp(targetLocal[i], air, airW);
                }
                else tgt = targetLocal[i];
                // o tornozelo fica acima da sola
                var ankleW = root.TransformPoint(tgt) + Vector3.up * HumanRig.Ankle;
                footW[i] = root.TransformPoint(new Vector3(tgt.x, tgt.y, tgt.z));
                SolveLeg(i, r.pelvis.InverseTransformPoint(ankleW), Mathf.Lerp(footPitch[i], 0, airW) + ToePitch(i, duty) * moveW);
            }

            // ---------------- tronco: inclina para frente ao correr e acelerar, para trás ao frear
            float leanT = 3f + runW * 11f + Mathf.Clamp(accLocal.z * 1.4f, -9, 12) + airW * 6f;
            float ln = lean.Step(leanT, 2.2f, 0.85f, dt);
            float breathe = Mathf.Sin(time * 1.7f) * 1.4f * (1 - moveW * 0.6f);

            // ---------------- ação da ferramenta (quadros-chave)
            float a = action;
            float actOn = actS.Step(a > 0 ? 1 : 0, 9f, 1f, dt);
            float spineX = 0, spineY = 0, pDip = 0;
            float uRx, uRz, fRx, hRz = 0, uLx, uLz, fLx;
            float fwdL = -Mathf.Cos(2 * Mathf.PI * phase), fwdR = -fwdL;
            float armAmp = Mathf.Lerp(20f, 55f, runW) * moveW;
            float elbowBase = Mathf.Lerp(10f, 88f, runW) * moveW + 9f * (1 - moveW);
            uLx = -fwdL * armAmp + airW * -35f; uRx = -fwdR * armAmp + airW * -35f;
            fLx = -(elbowBase + 12f * Mathf.Max(0, fwdL) * moveW); fRx = -(elbowBase + 12f * Mathf.Max(0, fwdR) * moveW);
            uLz = -5f - runW * 4f - airW * 25f + Mathf.Sin(time * 1.7f) * 1.5f * (1 - moveW);
            uRz = 5f + runW * 4f + airW * 25f - Mathf.Sin(time * 1.7f) * 1.5f * (1 - moveW);

            if (a > 0)
            {
                switch (tool)
                {
                    case Tool.Machado:
                    case Tool.Enxada:
                    {
                        bool hoe = tool == Tool.Enxada;
                        float up = hoe ? -128 : -162, down = hoe ? -32 : -52;
                        uRx = Ease.Key(a, 0, uRx, 0.36f, up, 0.5f, down, 0.72f, down + 4, 1, uRx);
                        fRx = Ease.Key(a, 0, fRx, 0.36f, -78, 0.5f, -8, 0.72f, -14, 1, fRx);
                        uRz = Ease.Key(a, 0, uRz, 0.36f, -6, 0.5f, -14, 1, uRz);
                        // a mão esquerda segura o cabo junto
                        uLx = Ease.Key(a, 0, uLx, 0.36f, up + 12, 0.5f, down + 6, 0.72f, down + 8, 1, uLx);
                        fLx = Ease.Key(a, 0, fLx, 0.36f, -82, 0.5f, -22, 0.72f, -26, 1, fLx);
                        uLz = Ease.Key(a, 0, uLz, 0.36f, 22, 0.5f, 30, 0.72f, 28, 1, uLz);
                        spineX = Ease.Key(a, 0, 0, 0.36f, -11, 0.5f, hoe ? 26 : 19, 0.72f, hoe ? 22 : 15, 1, 0);
                        spineY = Ease.Key(a, 0, 0, 0.36f, 14, 0.5f, -6, 1, 0);
                        pDip = Ease.Key(a, 0, 0, 0.36f, 0.01f, 0.5f, -0.06f, 0.72f, -0.05f, 1, 0);
                        break;
                    }
                    case Tool.Sementes:
                        uRx = Ease.Key(a, 0, uRx, 0.3f, -58, 0.62f, -72, 1, uRx);
                        uRz = Ease.Key(a, 0, uRz, 0.3f, 32, 0.62f, -38, 1, uRz);
                        fRx = Ease.Key(a, 0, fRx, 0.3f, -46, 0.62f, -18, 1, fRx);
                        spineY = Ease.Key(a, 0, 0, 0.3f, 12, 0.62f, -14, 1, 0);
                        spineX = Ease.Key(a, 0, 0, 0.4f, 9, 1, 0);
                        break;
                    case Tool.Regador:
                        uRx = Ease.Key(a, 0, uRx, 0.22f, -52, 0.82f, -50, 1, uRx);
                        fRx = Ease.Key(a, 0, fRx, 0.22f, -22, 0.82f, -18, 1, fRx);
                        hRz = Ease.Key(a, 0, 0, 0.25f, 0, 0.45f, -62, 0.8f, -62, 1, 0);
                        spineX = Ease.Key(a, 0, 0, 0.3f, 10, 0.8f, 10, 1, 0);
                        break;
                }
            }

            r.spine.localRotation = Quaternion.Euler(ln * 0.55f + spineX * 0.5f, -twist * 0.6f + spineY * 0.5f, -bk * 0.25f);
            r.chest.localRotation = Quaternion.Euler(ln * 0.45f + breathe + spineX * 0.5f, -twist * 0.9f + spineY * 0.5f, 0);
            if (pDip != 0) r.pelvis.localPosition += new Vector3(0, pDip * actOn, 0);

            // ---------------- cabeça: olha para onde a câmera aponta; parado, às vezes olha em volta
            var lookFlat = new Vector3(look.x, 0, look.z);
            float yawRel = lookFlat.sqrMagnitude > 0.01f ? Vector3.SignedAngle(root.forward, lookFlat, Vector3.up) : 0;
            glanceT -= dt;
            if (glanceT <= 0) { glanceT = UnityEngine.Random.Range(3f, 7f); glanceYaw = moveW < 0.1f && UnityEngine.Random.value < 0.5f ? UnityEngine.Random.Range(-45f, 45f) : 0; }
            if (Mathf.Abs(yawRel) > 100) yawRel = 0;              // olhando para trás: a cabeça não torce o pescoço
            float hy = headYaw.Step(Mathf.Clamp(yawRel * 0.8f + glanceYaw * (1 - moveW), -70, 70), 2.5f, 0.9f, dt);
            float lookPitch = -Mathf.Asin(Mathf.Clamp(look.normalized.y, -1, 1)) * Mathf.Rad2Deg;
            float hp = headPitch.Step(Mathf.Clamp(lookPitch * 0.6f, -35, 40), 2.5f, 0.9f, dt);
            float stab = -(ln + breathe + spineX) * 0.8f;        // a cabeça compensa a inclinação do corpo
            r.neck.localRotation = Quaternion.Euler(hp * 0.35f + stab * 0.4f, hy * 0.4f + twist * 0.5f, 0);
            r.head.localRotation = Quaternion.Euler(hp * 0.65f + stab * 0.6f, hy * 0.6f + twist * 0.4f, -bk * 0.3f);

            // ---------------- braços
            r.upper[0].localRotation = Quaternion.Euler(uLx, 0, uLz);
            r.upper[1].localRotation = Quaternion.Euler(uRx, 0, uRz);
            r.fore[0].localRotation = Quaternion.Euler(fLx, 0, 0);
            r.fore[1].localRotation = Quaternion.Euler(fRx, 0, 0);
            r.hand[0].localRotation = Quaternion.Euler(-8, 0, 0);
            r.hand[1].localRotation = Quaternion.Euler(-8, 0, hRz);

            // ---------------- túnica: cada painel abre com a perna que passa por ele, com a velocidade (vento relativo) e ao cair
            var velL = root.InverseTransformDirection(vel);
            var windL = root.InverseTransformDirection(Wind.At(root.position));
            float thL = -thighAngle[0], thR = -thighAngle[1];       // + = perna para frente
            for (int i = 0; i < 10; i++)
            {
                var d = r.robeDir[i];
                float leg = d.x < 0 ? thL : thR;
                float sideW = Mathf.Clamp01(Mathf.Abs(d.x) * 1.2f + 0.2f);
                float push = d.z > 0 ? Mathf.Max(0, leg - 12) * d.z : Mathf.Max(0, -leg) * -d.z;                  // perna empurra (descontando o joelho dobrado em pé)
                float rel = -(velL.x * d.x + velL.z * d.z) + (windL.x * d.x + windL.z * d.z) * 0.5f;           // "vento" da corrida
                float flare = 3f + push * 0.55f * sideW + Mathf.Clamp(rel * 4f, -6, 24) + Mathf.Clamp(-vel.y * 5f, -4, 35)
                            + Ease.Noise(time * 2.2f + i * 1.7f, i) * 2f * (0.3f + Wind.Strength * 0.4f);
                flares[i] = robeS[i].Step(flare, 3.2f, 0.5f, dt);
            }
            r.UpdateSkirt(flares);
            float packT = bob * 120f + landDip * 60f + accLocal.z * -0.8f;
            r.pack.localRotation = Quaternion.Euler(packS.Step(packT, 3f, 0.3f, dt), 0, packSide.Step(-velL.x * 4 + bk * 0.5f, 2.5f, 0.35f, dt));
            r.PoseTool(action, dt);
            r.ribbon?.Step(dt, vel);
        }

        /// <summary>Ponta do pé: sobe no começo do balanço e empurra o chão no fim do apoio.</summary>
        float ToePitch(int i, float duty)
        {
            float p = Ease.Frac(phase + (i == 0 ? 0 : 0.5f));
            if (p < duty) return Mathf.Clamp01((p / duty - 0.7f) / 0.3f) * 22f;     // empurra (ponta para baixo)
            float t = (p - duty) / (1 - duty);
            return -Mathf.Sin(t * Mathf.PI) * 14f;                                    // ponta para cima
        }

        void SolveLeg(int i, Vector3 ankleInPelvis, float footPitchDeg)
        {
            var hip = r.thigh[i].localPosition;
            var t = ankleInPelvis - hip;
            const float L1 = HumanRig.Thigh, L2 = HumanRig.Shin;
            float roll = Mathf.Atan2(t.x, -t.y) * Mathf.Rad2Deg * 0.8f;
            float d = Mathf.Clamp(new Vector2(t.z, t.y).magnitude, 0.12f, L1 + L2 - 0.002f);
            float a = Mathf.Atan2(t.z, -t.y);
            float b = Mathf.Acos(Mathf.Clamp((L1 * L1 + d * d - L2 * L2) / (2 * L1 * d), -1, 1));
            float knee = Mathf.PI - Mathf.Acos(Mathf.Clamp((L1 * L1 + L2 * L2 - d * d) / (2 * L1 * L2), -1, 1));
            float thighX = -(a + b) * Mathf.Rad2Deg, shinX = knee * Mathf.Rad2Deg;
            r.thigh[i].localRotation = Quaternion.Euler(thighX, 0, roll);
            thighAngle[i] = thighX;
            r.shin[i].localRotation = Quaternion.Euler(shinX, 0, 0);
            r.foot[i].localRotation = Quaternion.Euler(-(thighX + shinX) + footPitchDeg, 0, -roll);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
