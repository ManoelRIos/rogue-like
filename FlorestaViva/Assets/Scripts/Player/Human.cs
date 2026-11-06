using System;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Andarilho com esqueleto completo (quadril, coluna, peito, pescoço, cabeça, ombro→cotovelo→mão,
    /// coxa→joelho→pé), proporções de adulto e peças soltas (aba do casaco, mochila, cachecol) que balançam.
    /// </summary>
    public class HumanRig
    {
        public Transform root, pelvis, spine, chest, neck, head, tail, pack, scarf, tool;
        public readonly Transform[] thigh = new Transform[2], shin = new Transform[2], foot = new Transform[2],
            upper = new Transform[2], fore = new Transform[2], hand = new Transform[2];
        public Renderer[] renderers, firstPerson;   // na 1ª pessoa só antebraços, mãos e ferramenta aparecem
        public const float Thigh = 0.40f, Shin = 0.38f, Ankle = 0.075f, HipHeight = 0.86f;
        public static readonly Vector3 HipL = new(-0.095f, -0.03f, 0), HipR = new(0.095f, -0.03f, 0);

        static readonly Color Skin = Col.Hex("#d9a882"), SkinD = Col.Hex("#b98764"), Boot = Col.Hex("#3a2a1e"), Sole = Col.Hex("#1e1712"),
            Pants = Col.Hex("#4a4038"), Belt = Col.Hex("#2e2218"), Buckle = Col.Hex("#c8a050"), Scarf = Col.Hex("#d8b04a"),
            Pack = Col.Hex("#7a5a36"), PackD = Col.Hex("#5a4026"), Roll = Col.Hex("#8a6e4e"), Eye = Col.Hex("#1a1416"), Hair = Col.Hex("#4a3020");

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
            var r = new HumanRig();
            var rng = new Rng(3);
            var cloakD = Color.Lerp(cloak, Color.black, 0.3f);
            var cloakL = Color.Lerp(cloak, Color.white, 0.12f);
            r.root = Node("Andarilho", parent, Vector3.zero);

            // ---------------- quadril e pernas
            r.pelvis = Node("quadril", r.root, new Vector3(0, HipHeight, 0));
            var mb = new MeshBuilder();
            mb.Cylinder(new Vector3(0, -0.1f, 0), 0.17f, 0.15f, 0.16f, 8, Pants, 0, 0, 0, rng);
            mb.Cylinder(new Vector3(0, 0.04f, 0), 0.05f, 0.168f, 0.168f, 8, Belt, 0, 0, 0, rng);
            mb.Scale(new Vector3(1, 1, 0.8f));
            mb.Box(new Vector3(-0.035f, 0.04f, 0.125f), new Vector3(0.035f, 0.09f, 0.145f), Buckle);
            mb.Box(new Vector3(0.1f, -0.06f, 0.06f), new Vector3(0.17f, 0.05f, 0.14f), PackD, rng);            // bolsinha na cintura
            Part("quadril_m", r.pelvis, mb, Vector3.zero);
            // aba de trás do casaco (balança com mola)
            r.tail = Node("aba", r.pelvis, new Vector3(0, 0.02f, -0.12f));
            mb = new MeshBuilder();
            mb.Quad(new Vector3(-0.15f, 0, 0), new Vector3(0.15f, 0, 0), new Vector3(0.19f, -0.44f, -0.05f), new Vector3(-0.19f, -0.44f, -0.05f), cloak);
            mb.Quad(new Vector3(-0.15f, 0, 0.01f), new Vector3(-0.19f, -0.44f, -0.04f), new Vector3(0.19f, -0.44f, -0.04f), new Vector3(0.15f, 0, 0.01f), cloakD);
            Part("aba_m", r.tail, mb, Vector3.zero);

            for (int s = 0; s < 2; s++)
            {
                string side = s == 0 ? "E" : "D";
                r.thigh[s] = Node("coxa" + side, r.pelvis, s == 0 ? HipL : HipR);
                mb = new MeshBuilder();
                mb.Cylinder(new Vector3(0, -Thigh, 0), Thigh + 0.02f, 0.062f, 0.088f, 7, Pants, 0, 0, 0, rng);
                Part("coxa_m", r.thigh[s], mb, Vector3.zero);
                r.shin[s] = Node("canela" + side, r.thigh[s], new Vector3(0, -Thigh, 0));
                mb = new MeshBuilder();
                mb.Blob(Vector3.zero, Vector3.one * 0.066f, Pants, 0.05f, rng, 0);                               // joelho
                mb.Cylinder(new Vector3(0, -Shin, 0), Shin, 0.05f, 0.064f, 7, Pants, 0, 0, 0, rng);
                mb.Cylinder(new Vector3(0, -Shin, 0), 0.2f, 0.06f, 0.066f, 7, Boot, 0, 0, 0, rng);              // cano da bota
                mb.Cylinder(new Vector3(0, -Shin + 0.19f, 0), 0.03f, 0.07f, 0.07f, 7, Color.Lerp(Boot, Color.white, 0.15f), 0, 0, 0, rng);
                Part("canela_m", r.shin[s], mb, Vector3.zero);
                r.foot[s] = Node("pe" + side, r.shin[s], new Vector3(0, -Shin, 0));
                mb = new MeshBuilder();
                mb.Box(new Vector3(-0.052f, -Ankle + 0.012f, -0.065f), new Vector3(0.052f, 0.02f, 0.11f), Boot, rng);
                mb.Box(new Vector3(-0.048f, -Ankle + 0.012f, 0.1f), new Vector3(0.048f, -0.02f, 0.17f), Boot, rng);          // biqueira
                mb.Box(new Vector3(-0.055f, -Ankle, -0.07f), new Vector3(0.055f, -Ankle + 0.014f, 0.175f), Sole);
                Part("pe_m", r.foot[s], mb, Vector3.zero);
            }

            // ---------------- tronco
            r.spine = Node("coluna", r.pelvis, new Vector3(0, 0.05f, 0));
            mb = new MeshBuilder();
            mb.Cylinder(Vector3.zero, 0.26f, 0.155f, 0.165f, 8, cloak, 0, 0, 0, rng);
            mb.Scale(new Vector3(1, 1, 0.78f));
            mb.Box(new Vector3(-0.012f, 0.0f, 0.12f), new Vector3(0.012f, 0.26f, 0.135f), cloakD);                 // abotoamento
            Part("barriga", r.spine, mb, Vector3.zero);

            r.chest = Node("peito", r.spine, new Vector3(0, 0.25f, 0));
            mb = new MeshBuilder();
            mb.Cylinder(Vector3.zero, 0.25f, 0.165f, 0.205f, 8, cloak, 0, 0, 0, rng);
            mb.Cylinder(new Vector3(0, 0.25f, 0), 0.06f, 0.205f, 0.12f, 8, cloakL, 0, 0, 0, rng);                   // ombros
            mb.Scale(new Vector3(1, 1, 0.72f));
            int m0 = mb.v.Count;
            mb.Cylinder(new Vector3(0, 0.27f, 0), 0.075f, 0.115f, 0.092f, 8, Scarf, 0, 0, 0, rng);                 // cachecol
            mb.Blob(new Vector3(0.05f, 0.27f, 0.08f), new Vector3(0.05f, 0.045f, 0.04f), Scarf, 0.1f, rng, 0);       // nó
            mb.Box(new Vector3(-0.13f, 0.0f, 0.115f), new Vector3(-0.09f, 0.28f, 0.135f), PackD);                   // alças da mochila
            mb.Box(new Vector3(0.09f, 0.0f, 0.115f), new Vector3(0.13f, 0.28f, 0.135f), PackD);
            mb.Box(new Vector3(-0.012f, 0.0f, 0.13f), new Vector3(0.012f, 0.24f, 0.145f), cloakD);
            Part("peito_m", r.chest, mb, Vector3.zero);

            r.pack = Node("mochila", r.chest, new Vector3(0, 0.02f, -0.15f));
            mb = new MeshBuilder();
            mb.Box(new Vector3(-0.15f, 0, -0.13f), new Vector3(0.15f, 0.32f, 0), Pack, rng);
            mb.Box(new Vector3(-0.155f, 0.2f, -0.135f), new Vector3(0.155f, 0.33f, 0.005f), PackD, rng);            // tampa
            mb.Box(new Vector3(-0.11f, 0.04f, -0.17f), new Vector3(0.11f, 0.16f, -0.12f), PackD, rng);             // bolso
            mb.Cylinder(new Vector3(-0.19f, 0.39f, -0.07f), 0.38f, 0.06f, 0.06f, 7, Roll, 0, 0, 0, rng);           // saco de dormir
            int rollStart = mb.v.Count - 7 * 3 * 3;
            for (int i = Mathf.Max(0, rollStart); i < mb.v.Count; i++)                                              // deita o rolo (gira 90° no eixo Z)
            {
                var q = mb.v[i] - new Vector3(-0.19f, 0.39f, -0.07f);
                mb.v[i] = new Vector3(-0.19f, 0.39f, -0.07f) + new Vector3(q.y, -q.x, q.z);
            }
            Part("mochila_m", r.pack, mb, Vector3.zero);

            r.scarf = Node("cachecol", r.chest, new Vector3(0.05f, 0.29f, -0.08f));
            mb = new MeshBuilder();
            mb.Quad(new Vector3(-0.04f, 0, 0), new Vector3(0.04f, 0, 0), new Vector3(0.045f, -0.3f, -0.03f), new Vector3(-0.035f, -0.3f, -0.03f), Scarf);
            mb.Box(new Vector3(-0.045f, -0.32f, -0.04f), new Vector3(0.05f, -0.29f, -0.02f), Color.Lerp(Scarf, Color.black, 0.25f));
            Part("cachecol_m", r.scarf, mb, Vector3.zero);

            // ---------------- pescoço e cabeça
            r.neck = Node("pescoco", r.chest, new Vector3(0, 0.3f, 0));
            mb = new MeshBuilder();
            mb.Cylinder(Vector3.zero, 0.09f, 0.052f, 0.048f, 6, Skin, 0, 0, 0, rng);
            Part("pescoco_m", r.neck, mb, Vector3.zero);
            r.head = Node("cabeca", r.neck, new Vector3(0, 0.08f, 0.005f));
            mb = new MeshBuilder();
            mb.Blob(new Vector3(0, 0.11f, 0.012f), new Vector3(0.096f, 0.122f, 0.108f), Skin, 0.03f, rng, 1);       // crânio/rosto
            mb.Blob(new Vector3(0, 0.035f, 0.05f), new Vector3(0.07f, 0.045f, 0.06f), Skin, 0.03f, rng, 0);        // queixo
            mb.Blob(new Vector3(0, 0.135f, -0.05f), new Vector3(0.138f, 0.152f, 0.128f), cloakD, 0.04f, rng, 1);     // capuz
            mb.Cylinder(new Vector3(0, 0.2f, -0.14f), 0.17f, 0.06f, 0.0f, 5, cloakD, 0, 0, 0, rng);                // ponta do capuz
            mb.Box(new Vector3(-0.072f, 0.165f, 0.07f), new Vector3(0.072f, 0.205f, 0.1f), Hair, rng);              // franja
            mb.Box(new Vector3(-0.058f, 0.118f, 0.104f), new Vector3(-0.024f, 0.138f, 0.118f), Eye);                // olhos
            mb.Box(new Vector3(0.024f, 0.118f, 0.104f), new Vector3(0.058f, 0.138f, 0.118f), Eye);
            mb.Box(new Vector3(-0.062f, 0.145f, 0.1f), new Vector3(-0.02f, 0.153f, 0.114f), Hair);                 // sobrancelhas
            mb.Box(new Vector3(0.02f, 0.145f, 0.1f), new Vector3(0.062f, 0.153f, 0.114f), Hair);
            mb.Box(new Vector3(-0.013f, 0.075f, 0.11f), new Vector3(0.013f, 0.12f, 0.138f), SkinD);                 // nariz
            mb.Box(new Vector3(-0.025f, 0.052f, 0.1f), new Vector3(0.025f, 0.058f, 0.112f), SkinD);                 // boca
            Part("cabeca_m", r.head, mb, Vector3.zero);

            // ---------------- braços
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1 : 1;
                string side = s == 0 ? "E" : "D";
                r.upper[s] = Node("braco" + side, r.chest, new Vector3(sx * 0.2f, 0.225f, 0));
                mb = new MeshBuilder();
                mb.Blob(Vector3.zero, new Vector3(0.07f, 0.065f, 0.07f), cloakL, 0.05f, rng, 0);                    // ombro
                mb.Cylinder(new Vector3(0, -0.29f, 0), 0.29f, 0.05f, 0.064f, 7, cloak, 0, 0, 0, rng);
                Part("braco_m", r.upper[s], mb, Vector3.zero);
                r.fore[s] = Node("antebraco" + side, r.upper[s], new Vector3(0, -0.29f, 0));
                mb = new MeshBuilder();
                mb.Blob(Vector3.zero, Vector3.one * 0.052f, cloak, 0.05f, rng, 0);                                   // cotovelo
                mb.Cylinder(new Vector3(0, -0.25f, 0), 0.25f, 0.042f, 0.05f, 7, cloak, 0, 0, 0, rng);
                mb.Cylinder(new Vector3(0, -0.25f, 0), 0.035f, 0.05f, 0.05f, 7, cloakD, 0, 0, 0, rng);              // punho
                Part("antebraco_m", r.fore[s], mb, Vector3.zero);
                r.hand[s] = Node("mao" + side, r.fore[s], new Vector3(0, -0.25f, 0));
                mb = new MeshBuilder();
                mb.Blob(new Vector3(0, -0.05f, 0.008f), new Vector3(0.042f, 0.058f, 0.03f), Skin, 0.06f, rng, 0);
                mb.Blob(new Vector3(sx * -0.03f, -0.035f, 0.025f), new Vector3(0.018f, 0.03f, 0.018f), Skin, 0.05f, rng, 0);   // polegar
                Part("mao_m", r.hand[s], mb, Vector3.zero);
            }

            r.tool = Mats.Object("ferramenta", Rig.ToolMesh(0), Mats.LowPoly, r.hand[1]).transform;
            r.tool.localPosition = new Vector3(0, -0.06f, 0.01f);
            r.tool.localRotation = Quaternion.Euler(80, 0, 0);
            r.renderers = r.root.GetComponentsInChildren<Renderer>(true);
            var fp = new System.Collections.Generic.List<Renderer>();
            fp.AddRange(r.fore[0].GetComponentsInChildren<Renderer>(true));
            fp.AddRange(r.fore[1].GetComponentsInChildren<Renderer>(true));
            r.firstPerson = fp.ToArray();
            return r;
        }

        public void SetTool(int i)
        {
            tool.GetComponent<MeshFilter>().sharedMesh = Rig.ToolMesh(i);
            tool.localRotation = i <= 1 ? Quaternion.Euler(80, 0, 0) : Quaternion.Euler(10, 0, 0);
        }
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
        Spring moveS, runS, pelvisY, land, lean, bank, headYaw, headPitch, tailS, tailSide, packS, scarfS, airS, actS;
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

            // ---------------- peças soltas com mola
            var velL = root.InverseTransformDirection(vel);
            float tailT = Mathf.Clamp(-velL.z * 7f - accLocal.z * 1.2f + vel.y * -4f, -70, 8) + Mathf.Sin(time * 9 + phase * 6.28f) * 4 * moveW;
            float tailX = tailS.Step(-tailT, 2.2f, 0.35f, dt);
            r.tail.localRotation = Quaternion.Euler(tailX, 0, tailSide.Step(-velL.x * 6 - bk, 2f, 0.4f, dt));
            float packT = bob * 120f + landDip * 60f + accLocal.z * -0.8f;
            r.pack.localRotation = Quaternion.Euler(packS.Step(packT, 3f, 0.3f, dt), 0, 0);
            float scarfT = Mathf.Clamp(-velL.z * 9f + vel.y * -5f, -80, 5) + Ease.Noise(time * 1.3f, 2) * 8f;
            r.scarf.localRotation = Quaternion.Euler(scarfS.Step(-scarfT, 2.6f, 0.3f, dt), 0, Ease.Noise(time * 2f, 5) * 10f);
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
            r.shin[i].localRotation = Quaternion.Euler(shinX, 0, 0);
            r.foot[i].localRotation = Quaternion.Euler(-(thighX + shinX) + footPitchDeg, 0, -roll);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
