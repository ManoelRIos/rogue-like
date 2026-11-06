using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Personagens low-poly originais montados por código (partes separadas para animar).
    /// Andarilho = jogador (cor do manto muda por jogador no co-op).
    /// Vulto = criatura da noite: alta, magra, braços longos e olhos que brilham.
    /// </summary>
    public class Rig
    {
        public Transform root, body, head, armL, armR, legL, legR, tool;
        public Renderer[] renderers;

        static Transform Part(string n, Transform parent, Mesh m, Vector3 pos, Material mat = null)
        {
            var go = Mats.Object(n, m, mat ? mat : Mats.LowPoly, parent);
            go.transform.localPosition = pos;
            return go.transform;
        }


        static Mesh[] tools;
        static readonly Color Wood = Col.Hex("#7a5232"), Iron = Col.Hex("#9aa0a8");

        /// <summary>0 machado, 1 enxada, 2 saco de sementes, 3 regador.</summary>
        public static Mesh ToolMesh(int i)
        {
            if (tools == null)
            {
                tools = new Mesh[4];
                var rng = new Rng(8);
                var mb = new MeshBuilder();
                mb.Box(new Vector3(-0.025f, -0.05f, -0.025f), new Vector3(0.025f, 0.65f, 0.025f), Wood);
                mb.Box(new Vector3(-0.03f, 0.48f, 0.02f), new Vector3(0.03f, 0.66f, 0.22f), Iron);
                tools[0] = mb.ToMesh("machado");
                mb = new MeshBuilder();
                mb.Box(new Vector3(-0.02f, -0.1f, -0.02f), new Vector3(0.02f, 0.85f, 0.02f), Wood);
                mb.Box(new Vector3(-0.09f, 0.78f, 0.0f), new Vector3(0.09f, 0.84f, 0.2f), Iron);
                tools[1] = mb.ToMesh("enxada");
                mb = new MeshBuilder();
                mb.Blob(new Vector3(0, -0.02f, 0.05f), new Vector3(0.13f, 0.15f, 0.12f), Col.Hex("#c8a878"), 0.08f, rng, 1);
                mb.Cylinder(new Vector3(0, 0.1f, 0.05f), 0.06f, 0.06f, 0.08f, 6, Col.Hex("#a8885a"), 0, 0, 0, rng);
                mb.Box(new Vector3(-0.07f, 0.1f, -0.02f), new Vector3(0.07f, 0.12f, 0.12f), Col.Hex("#6a4a2a"));
                tools[2] = mb.ToMesh("sementes");
                mb = new MeshBuilder();
                var tin = Col.Hex("#7a9aa8");
                mb.Cylinder(new Vector3(0, -0.05f, 0.08f), 0.24f, 0.11f, 0.1f, 8, tin, 0, 0, 0, rng);
                mb.Cylinder(new Vector3(0, 0.05f, 0.18f), 0.3f, 0.03f, 0.02f, 5, tin, 0, 0, 0, rng);   // bico
                mb.Box(new Vector3(-0.015f, 0.19f, -0.02f), new Vector3(0.015f, 0.25f, 0.18f), Col.Hex("#5a7a88"));
                tools[3] = mb.ToMesh("regador");
            }
            return tools[Mathf.Clamp(i, 0, 3)];
        }

        public void SetTool(int i)
        {
            if (tool == null) return;
            tool.GetComponent<MeshFilter>().sharedMesh = ToolMesh(i);
            // machado e enxada ficam apontados para a frente; saco e regador, pendurados
            tool.localRotation = i <= 1 ? Quaternion.Euler(80, 0, 0) : Quaternion.Euler(10, 0, 0);
        }

        static readonly Color Shadow = Col.Hex("#0e0b12"), ShadowL = Col.Hex("#231a2a");

        public static Rig Shade(Transform parent = null)
        {
            var r = new Rig { root = new GameObject("Vulto").transform };
            if (parent) r.root.SetParent(parent, false);
            var rng = new Rng(11);
            r.body = new GameObject("corpo").transform; r.body.SetParent(r.root, false); r.body.localPosition = new Vector3(0, 1.4f, 0);
            var mb = new MeshBuilder();
            // corpo alongado e esfarrapado
            mb.Cylinder(new Vector3(0, -1.4f, 0), 1.0f, 0.55f, 0.28f, 7, Shadow, 0.6f, 0.2f, 0, rng, false);
            mb.Cylinder(new Vector3(0, -0.4f, 0), 1.0f, 0.28f, 0.2f, 6, ShadowL, 0.1f, 0, 0, rng);
            for (int i = 0; i < 6; i++)   // farrapos na barra
            {
                float a = i / 6f * 6.28f;
                var p = new Vector3(Mathf.Cos(a) * 0.5f, -1.4f, Mathf.Sin(a) * 0.5f);
                mb.Tri(p, p + new Vector3(Mathf.Cos(a + 0.5f) * 0.2f, 0, Mathf.Sin(a + 0.5f) * 0.2f), p + new Vector3(0, -0.25f, 0), Col.A(Shadow, 1));
            }
            Part("corpo", r.body, mb.ToMesh("vulto_corpo"), Vector3.zero);
            // cabeça: crânio alongado com chifres finos
            mb = new MeshBuilder();
            mb.Blob(Vector3.zero, new Vector3(0.24f, 0.36f, 0.26f), ShadowL, 0.1f, rng, 1);
            mb.Cylinder(new Vector3(-0.15f, 0.2f, 0), 0.5f, 0.05f, 0.0f, 4, Shadow, 0, 0.3f, 0, rng);
            mb.Cylinder(new Vector3(0.15f, 0.2f, 0), 0.5f, 0.05f, 0.0f, 4, Shadow, 0, 0.3f, 0, rng);
            r.head = Part("cabeca", r.body, mb.ToMesh("vulto_cabeca"), new Vector3(0, 0.75f, 0.05f));
            // olhos que brilham (material aditivo, aparecem na neblina)
            var eyes = new MeshBuilder();
            eyes.Blob(new Vector3(-0.09f, 0.04f, 0.22f), Vector3.one * 0.05f, Col.Hex("#ff3020"), 0, rng, 0);
            eyes.Blob(new Vector3(0.09f, 0.04f, 0.22f), Vector3.one * 0.05f, Col.Hex("#ff3020"), 0, rng, 0);
            Part("olhos", r.head, eyes.ToMesh("olhos"), Vector3.zero, Mats.Glow);
            // braços muito longos com garras
            Mesh Arm()
            {
                var m = new MeshBuilder();
                m.Cylinder(new Vector3(0, -1.3f, 0), 1.3f, 0.05f, 0.08f, 5, Shadow, 0, 0, 0, rng);
                for (int i = -1; i <= 1; i++) m.Cylinder(new Vector3(i * 0.04f, -1.55f, 0.04f), 0.25f, 0.0f, 0.03f, 3, ShadowL, 0, 0, 0, rng, false);
                return m.ToMesh("vulto_braco");
            }
            r.armL = Part("bracoE", r.body, Arm(), new Vector3(-0.3f, 0.5f, 0));
            r.armR = Part("bracoD", r.body, Arm(), new Vector3(0.3f, 0.5f, 0));
            r.renderers = r.root.GetComponentsInChildren<Renderer>(true);
            return r;
        }

        // ---------------- animação do Vulto: molas soltas (braços pendurados que arrastam), tiques de cabeça
        Spring lean, sArmL, sArmR, headYaw, headRoll, sway, bob;
        float twitchT, twitchYaw, twitchRoll, phase;

        /// <param name="speed">velocidade (m/s)</param>
        /// <param name="swing">0..1 golpe com as garras</param>
        /// <param name="watched">o jogador está olhando: congela numa pose estranha</param>
        public void AnimateShade(float dt, float speed, float swing, bool watched, bool charging, bool eating)
        {
            if (dt <= 0) return;
            phase += dt * (1.2f + speed * 0.9f);
            float walk = Mathf.Clamp01(speed / 3f);
            float s = Mathf.Sin(phase * 3.1f) * walk;
            float leanT = charging ? 38 : eating ? 55 : watched ? 6 : 18;
            float ln = lean.Step(leanT, 1.6f, 0.7f, dt);
            float sw = sway.Step(Mathf.Sin(phase * 1.55f) * 9 * walk + (watched ? 0 : Ease.Noise(phase * 0.4f, 3) * 6), 1.2f, 0.5f, dt);
            body.localRotation = Quaternion.Euler(ln, 0, sw);
            body.localPosition = new Vector3(0, 1.4f + bob.Step(Mathf.Abs(Mathf.Cos(phase * 1.55f)) * walk * 0.1f - (eating ? 0.35f : 0), 3f, 0.6f, dt), 0);

            // braços: seguem atrasados (mola mole); correndo, ficam para trás; golpe = os dois de uma vez
            float trail = charging ? 55 : 0;
            float baseArm = -25 + trail - swing * 125 - (eating ? 70 : 0);
            float aL = sArmL.Step(baseArm - s * 28, 1.6f, 0.35f, dt), aR = sArmR.Step(baseArm + s * 28, 1.5f, 0.35f, dt);
            this.armL.localRotation = Quaternion.Euler(aL, 0, -8 - walk * 6);
            this.armR.localRotation = Quaternion.Euler(aR, 0, 8 + walk * 6);

            // cabeça: espasmos rápidos de vez em quando; observada, inclina e encara
            twitchT -= dt;
            if (twitchT <= 0)
            {
                twitchT = Random.Range(0.25f, 1.8f);
                twitchYaw = Random.value < 0.6f ? Random.Range(-55f, 55f) : 0;
                twitchRoll = Random.Range(-30f, 30f);
            }
            float hyT = watched ? 0 : twitchYaw, hrT = watched ? 38 : twitchRoll;
            head.localRotation = Quaternion.Euler(-ln * 0.6f + (eating ? 30 : 0), headYaw.Step(hyT, 7f, 0.55f, dt), headRoll.Step(hrT, 6f, 0.5f, dt));
        }
    }
}
