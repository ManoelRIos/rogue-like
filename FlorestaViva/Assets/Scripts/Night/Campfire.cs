using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// A fogueira no centro da clareira: é o coração da base.
    /// Gasta lenha (mais rápido à noite). Enquanto queima forte, a luz espanta os Vultos.
    /// Se apagar, a base fica indefesa — defender a base = manter a chama viva.
    /// </summary>
    public class Campfire
    {
        public readonly Vector3 Pos;
        public float Fuel = 70;              // 0..100
        public const float MaxFuel = 100;
        public bool Lit => Fuel > 0;
        /// <summary>Raio em que os Vultos não entram (cresce com a lenha).</summary>
        public float SafeRadius => Lit ? Mathf.Lerp(3.5f, 11f, Fuel / MaxFuel) : 0;
        public float Drain;                  // dreno extra causado por criaturas por perto

        readonly Light light;
        readonly Transform flames;
        readonly Material sparkMat;
        readonly Mesh sparkMesh;
        readonly Vector3[] sparks = new Vector3[24];
        readonly float[] sparkLife = new float[24];
        readonly Matrix4x4[] sparkM = new Matrix4x4[24];

        public Campfire(World w, Transform parent)
        {
            Pos = World.BaseCenter; Pos.y = w.Height(Pos.x, Pos.z);
            var rng = new Rng(77);
            var root = new GameObject("Base").transform; root.SetParent(parent, false);

            // anel de pedras + lenha
            var mb = new MeshBuilder();
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2;
                mb.Blob(new Vector3(Mathf.Cos(a) * 0.85f, 0.12f, Mathf.Sin(a) * 0.85f), new Vector3(0.24f, 0.17f, 0.22f), rng.Vary(Col.Hex("#7a7670"), 0.06f), 0.15f, rng, 0);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * Mathf.PI * 2 + 0.4f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var m = new MeshBuilder();
                m.Cylinder(Vector3.zero, 1.0f, 0.08f, 0.07f, 5, Col.Hex("#5a3a24"), 0, 0, 0, rng);
                AddRotated(mb, m, d * 0.35f + Vector3.up * 0.05f, Quaternion.LookRotation(-d) * Quaternion.Euler(60, 0, 0));
            }
            mb.Blob(new Vector3(0, 0.05f, 0), new Vector3(0.45f, 0.08f, 0.45f), Col.Hex("#2a2420"), 0.2f, rng, 0);
            var ring = Mats.Object("fogueira", mb.ToMesh("fogueira"), Mats.LowPoly, root);
            ring.transform.position = Pos;

            // chamas (brilho aditivo, sem sombra)
            mb = new MeshBuilder();
            mb.Cylinder(new Vector3(0, 0.1f, 0), 1.1f, 0.42f, 0f, 6, Col.Hex("#ff6a1a"), 0, 0, 0, rng, false);
            mb.Cylinder(new Vector3(0.1f, 0.1f, 0.05f), 0.8f, 0.28f, 0f, 5, Col.Hex("#ffb030"), 0, 0, 0.5f, rng, false);
            mb.Cylinder(new Vector3(-0.08f, 0.1f, -0.05f), 0.55f, 0.18f, 0f, 5, Col.Hex("#ffe080"), 0, 0, 1f, rng, false);
            flames = Mats.Object("chamas", mb.ToMesh("chamas"), Mats.Glow, root, false).transform;
            flames.position = Pos;

            var lg = new GameObject("luz_fogueira");
            lg.transform.SetParent(root, false);
            lg.transform.position = Pos + Vector3.up * 1.4f;
            light = lg.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Col.Hex("#ff9a48");
            light.shadows = LightShadows.Hard;
            light.shadowStrength = 0.7f;

            sparkMat = new Material(Mats.Glow) { name = "faiscas", enableInstancing = true };
            mb = new MeshBuilder();
            mb.Blob(Vector3.zero, Vector3.one * 0.035f, Col.Hex("#ffb050"), 0, rng, 0);
            sparkMesh = mb.ToMesh("faisca");
            for (int i = 0; i < sparks.Length; i++) sparkLife[i] = rng.Value;

            BuildCamp(w, root, rng);
        }

        static void AddRotated(MeshBuilder dst, MeshBuilder src, Vector3 offset, Quaternion rot)
        {
            for (int i = 0; i < src.v.Count; i += 3)
                dst.Tri(offset + rot * src.v[i], offset + rot * src.v[i + 1], offset + rot * src.v[i + 2], src.c[i], src.c[i + 1], src.c[i + 2]);
        }

        /// <summary>Acampamento: barraca, troncos para sentar, pilha de lenha e estacas ao redor da clareira.</summary>
        void BuildCamp(World w, Transform root, Rng rng)
        {
            GameObject Put(string n, MeshBuilder m, Vector3 p, float yaw, bool solid, float r = 0.6f)
            {
                var go = Mats.Object(n, m.ToMesh(n), Mats.LowPoly, root);
                p.y = w.Height(p.x, p.z);
                go.transform.position = p; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                if (solid) { var c = go.AddComponent<SphereCollider>(); c.radius = r; c.center = Vector3.up * r * 0.6f; }
                return go;
            }
            // barraca de lona
            var mb = new MeshBuilder();
            Color canvas = Col.Hex("#b49a6a"), canvasD = Col.Hex("#8a7450");
            Vector3 a = new(-1.2f, 0, -1.4f), b = new(1.2f, 0, -1.4f), c = new(1.2f, 0, 1.4f), d = new(-1.2f, 0, 1.4f);
            Vector3 t0 = new(0, 1.7f, -1.5f), t1 = new(0, 1.7f, 1.5f);
            mb.Quad(a, t0, t1, d, canvas); mb.Quad(c, t1, t0, b, canvasD);
            mb.Tri(a, b, t0, canvasD);
            mb.Tri(d, t1, c, Col.Hex("#2a2018"));                // entrada escura
            mb.Box(new Vector3(-0.04f, 0, 1.45f), new Vector3(0.04f, 1.85f, 1.53f), Col.Hex("#5a3a24"));
            var tent = Put("barraca", mb, Pos + new Vector3(-5.5f, 0, -3.5f), 35, false);
            var bc = tent.AddComponent<BoxCollider>(); bc.size = new Vector3(2.4f, 1.6f, 2.8f); bc.center = new Vector3(0, 0.8f, 0);

            // troncos para sentar
            for (int i = 0; i < 3; i++)
            {
                float ang = i * 120 + 20;
                var dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                var log = new MeshBuilder();
                var m = new MeshBuilder();
                m.Cylinder(new Vector3(0, 0, -0.9f), 1.8f, 0.24f, 0.22f, 7, Col.Hex("#6a4a30"), 0, 0, 0, rng);
                AddRotated(log, m, new Vector3(0, 0.24f, 0), Quaternion.Euler(90, 0, 0));
                Put("tronco_assento", log, Pos + dir * 2.6f, ang + 90, true, 0.5f);
            }
            // pilha de lenha
            mb = new MeshBuilder();
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 4 - y; x++)
                {
                    var m = new MeshBuilder();
                    m.Cylinder(new Vector3(0, 0, -0.6f), 1.2f, 0.16f, 0.16f, 6, rng.Vary(Col.Hex("#7a5236"), 0.05f), 0, 0, 0, rng);
                    AddRotated(mb, m, new Vector3((x - (3 - y) * 0.5f) * 0.33f, 0.16f + y * 0.28f, 0), Quaternion.Euler(90, 0, 0));
                }
            Put("lenha", mb, Pos + new Vector3(3.6f, 0, -4.2f), -20, true, 0.8f);

            // estacas marcando a borda da clareira (com lanternas apagadas para o futuro)
            for (int i = 0; i < 28; i++)
            {
                float ang = i / 28f * 360;
                if (Mathf.Abs(Mathf.DeltaAngle(ang, 0)) < 12) continue;          // abertura (trilha para o norte)
                var dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                var st = new MeshBuilder();
                st.Cylinder(Vector3.zero, rng.Range(1.1f, 1.6f), 0.09f, 0.0f, 5, rng.Vary(Col.Hex("#6a4a2e"), 0.05f), 0, 0, 0, rng, false);
                var p = Pos + dir * (World.BaseRadius - 1.5f);
                Put("estaca", st, p, rng.Range(0, 360), false).transform.rotation = Quaternion.Euler(rng.Range(-8, 8), 0, rng.Range(-8, 8));
            }
        }

        public bool AddWood()
        {
            if (Fuel >= MaxFuel - 1) return false;
            Fuel = Mathf.Min(MaxFuel, Fuel + 14);
            return true;
        }

        public void Update(float dt, float darkness)
        {
            Fuel = Mathf.Max(0, Fuel - dt * (0.05f + darkness * 0.12f + Drain));
            Drain = 0;
            float k = Fuel / MaxFuel;
            float flick = Mathf.PerlinNoise(Time.time * 6f, 0.3f) * 0.35f + Mathf.PerlinNoise(Time.time * 17f, 4f) * 0.15f;
            light.enabled = Lit;
            light.intensity = (0.9f + k * 1.4f) * (0.8f + flick) * Mathf.Lerp(0.6f, 1.2f, darkness);
            light.range = Mathf.Lerp(6f, 18f, k) * (0.92f + flick * 0.2f);
            float s = Lit ? Mathf.Lerp(0.35f, 1.15f, k) : 0;
            flames.localScale = new Vector3(s * (0.95f + flick * 0.2f), s * (0.85f + flick * 0.5f), s * (0.95f + flick * 0.2f));
            flames.rotation = Quaternion.Euler(0, Time.time * 40, 0);

            if (!Lit) return;
            // faíscas subindo
            int n = 0;
            for (int i = 0; i < sparks.Length; i++)
            {
                sparkLife[i] += dt * (0.5f + (i % 5) * 0.12f);
                if (sparkLife[i] >= 1) { sparkLife[i] = 0; sparks[i] = Pos + new Vector3(Random.Range(-0.3f, 0.3f), 0.4f, Random.Range(-0.3f, 0.3f)); }
                float l = sparkLife[i];
                var p = sparks[i] + new Vector3(Mathf.Sin(l * 7 + i) * 0.3f, l * 3.5f * (0.4f + k), Mathf.Cos(l * 5 + i) * 0.3f);
                sparkM[n++] = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * (1 - l));
            }
            sparkMat.SetColor("_Color", new Color(1, 1, 1, 0.5f + darkness));
            Graphics.DrawMeshInstanced(sparkMesh, 0, sparkMat, sparkM, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }
    }
}
