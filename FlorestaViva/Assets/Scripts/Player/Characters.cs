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

        static readonly Color Skin = Col.Hex("#e0b08a"), Boot = Col.Hex("#3a2a1e"), Pants = Col.Hex("#4a4038"),
            Scarf = Col.Hex("#d8b04a"), Pack = Col.Hex("#7a5a36"), Eye = Col.Hex("#1a1416");

        public static Rig Wanderer(Color cloak, Transform parent = null)
        {
            var r = new Rig { root = new GameObject("Andarilho").transform };
            if (parent) r.root.SetParent(parent, false);
            var rng = new Rng(3);
            var cloakD = Color.Lerp(cloak, Color.black, 0.3f);

            r.body = new GameObject("corpo").transform; r.body.SetParent(r.root, false); r.body.localPosition = new Vector3(0, 0.75f, 0);
            // tronco + manto
            var mb = new MeshBuilder();
            mb.Cylinder(new Vector3(0, -0.05f, 0), 0.7f, 0.36f, 0.26f, 7, cloak, 0, 0, 0, rng);
            mb.Cylinder(new Vector3(0, 0.58f, 0), 0.1f, 0.3f, 0.2f, 7, Scarf, 0, 0, 0, rng);
            mb.Box(new Vector3(-0.22f, 0.12f, -0.42f), new Vector3(0.22f, 0.55f, -0.22f), Pack, rng);        // mochila
            mb.Box(new Vector3(-0.24f, 0.05f, -0.4f), new Vector3(0.24f, 0.12f, -0.18f), Color.Lerp(Pack, Color.black, 0.3f), rng);
            Part("tronco", r.body, mb.ToMesh("tronco"), Vector3.zero);

            // cabeça com capuz e olhos
            mb = new MeshBuilder();
            mb.Blob(Vector3.zero, new Vector3(0.27f, 0.27f, 0.27f), Skin, 0.04f, rng, 1);
            mb.Blob(new Vector3(0, 0.05f, -0.05f), new Vector3(0.33f, 0.33f, 0.33f), cloakD, 0.05f, rng, 1);
            mb.Cylinder(new Vector3(0, 0.25f, -0.12f), 0.25f, 0.12f, 0.0f, 5, cloakD, 0, 0, 0, rng);       // ponta do capuz
            mb.Box(new Vector3(-0.12f, 0.02f, 0.24f), new Vector3(-0.05f, 0.1f, 0.28f), Eye);
            mb.Box(new Vector3(0.05f, 0.02f, 0.24f), new Vector3(0.12f, 0.1f, 0.28f), Eye);
            // recorta a frente do capuz: um "rosto" claro por cima
            mb.Blob(new Vector3(0, -0.02f, 0.12f), new Vector3(0.22f, 0.22f, 0.16f), Skin, 0.03f, rng, 1);
            mb.Box(new Vector3(-0.12f, 0.0f, 0.26f), new Vector3(-0.05f, 0.09f, 0.29f), Eye);
            mb.Box(new Vector3(0.05f, 0.0f, 0.26f), new Vector3(0.12f, 0.09f, 0.29f), Eye);
            r.head = Part("cabeca", r.body, mb.ToMesh("cabeca"), new Vector3(0, 0.92f, 0));

            // braços e pernas (pivô em cima)
            Mesh Limb(float len, float rad, Color c, Color end)
            {
                var m = new MeshBuilder();
                m.Cylinder(new Vector3(0, -len, 0), len, rad * 0.85f, rad, 6, c, 0, 0, 0, rng);
                m.Blob(new Vector3(0, -len, 0.02f), Vector3.one * rad * 1.15f, end, 0.05f, rng, 0);
                return m.ToMesh("membro");
            }
            r.armL = Part("bracoE", r.body, Limb(0.5f, 0.09f, cloak, Skin), new Vector3(-0.36f, 0.55f, 0));
            r.armR = Part("bracoD", r.body, Limb(0.5f, 0.09f, cloak, Skin), new Vector3(0.36f, 0.55f, 0));
            r.legL = Part("pernaE", r.root, Limb(0.6f, 0.11f, Pants, Boot), new Vector3(-0.14f, 0.72f, 0));
            r.legR = Part("pernaD", r.root, Limb(0.6f, 0.11f, Pants, Boot), new Vector3(0.14f, 0.72f, 0));

            // ferramenta na mão direita (troca com 1-4)
            r.tool = Part("ferramenta", r.armR, ToolMesh(0), new Vector3(0, -0.55f, 0.05f));
            r.tool.localRotation = Quaternion.Euler(80, 0, 0);
            r.renderers = r.root.GetComponentsInChildren<Renderer>(true);
            return r;
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

        /// <summary>Animação procedural: caminhada (fase/intensidade), golpe (0..1), corrida.</summary>
        public void Animate(float phase, float walk, float swing, bool creature = false)
        {
            float s = Mathf.Sin(phase) * walk;
            if (legL) legL.localRotation = Quaternion.Euler(s * 40, 0, 0);
            if (legR) legR.localRotation = Quaternion.Euler(-s * 40, 0, 0);
            if (creature)
            {
                armL.localRotation = Quaternion.Euler(-30 - s * 25 - swing * 100, 0, -8);
                armR.localRotation = Quaternion.Euler(-30 + s * 25 - swing * 100, 0, 8);
                body.localRotation = Quaternion.Euler(18 + Mathf.Sin(phase * 0.5f) * 6, 0, Mathf.Sin(phase * 0.7f) * 8);
                head.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 7) * 6 * (1 - walk), Mathf.Sin(Time.time * 1.3f) * 25, 0);
                body.localPosition = new Vector3(0, 1.4f + Mathf.Abs(Mathf.Cos(phase)) * walk * 0.08f, 0);
                return;
            }
            armL.localRotation = Quaternion.Euler(-s * 35, 0, -6);
            // golpe com a ferramenta: ergue e desce rápido
            float sw = swing > 0 ? Mathf.Sin(swing * Mathf.PI) : 0;
            armR.localRotation = Quaternion.Euler(s * 35 * (1 - sw) - sw * 140 + (swing > 0.5f ? 60 * (swing - 0.5f) : 0), 0, 6);
            body.localPosition = new Vector3(0, 0.75f + Mathf.Abs(Mathf.Cos(phase)) * walk * 0.05f, 0);
            body.localRotation = Quaternion.Euler(walk * 5, 0, 0);
        }
    }
}
