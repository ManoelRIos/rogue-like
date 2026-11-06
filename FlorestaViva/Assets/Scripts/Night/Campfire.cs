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
        readonly Transform[] tongues = new Transform[8];
        readonly float[] tongueSeed = new float[8];
        readonly Material[] tongueMat = new Material[8];
        readonly Material emberMat, glowMat;
        readonly Transform glow;
        readonly AudioSource crackle;
        float smokeT, popT, flick;

        public Campfire(World w, Transform parent)
        {
            Pos = World.BaseCenter; Pos.y = w.Ground(Pos.x, Pos.z);
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

            // chamas: várias "línguas" de fogo, cada uma com seu ritmo, + núcleo brilhante
            flames = new GameObject("chamas").transform;
            flames.SetParent(root, false);
            flames.position = Pos + Vector3.up * 0.08f;
            for (int i = 0; i < tongues.Length; i++)
            {
                var m = new MeshBuilder();
                float h = i == 0 ? 1.25f : rng.Range(0.5f, 1.0f), r = i == 0 ? 0.3f : rng.Range(0.1f, 0.2f);
                Tongue(m, h, r, i == 0 ? 7 : 5, rng);
                tongueMat[i] = new Material(Mats.Fire) { name = "lingua" };
                tongueSeed[i] = rng.Range(0, 10);
                tongueMat[i].SetFloat("_Seed", tongueSeed[i]);
                var go = Mats.Object("lingua", m.ToMesh("lingua"), tongueMat[i], flames, false);
                float ang = i * 2.4f, rad = i == 0 ? 0 : rng.Range(0.12f, 0.36f);
                go.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, 0, Mathf.Sin(ang) * rad);
                go.transform.localRotation = Quaternion.Euler(rng.Range(-8, 8), rng.Range(0, 360), rng.Range(-8, 8));
                tongues[i] = go.transform;
            }
            // brasas: carvões no centro e pontas dos troncos em brasa (pulsam)
            emberMat = new Material(Mats.Glow) { name = "brasas" };
            var em = new MeshBuilder();
            for (int i = 0; i < 14; i++)
            {
                float a = rng.Range(0, 6.28f), d = rng.Range(0, 0.42f);
                em.Blob(new Vector3(Mathf.Cos(a) * d, 0.06f, Mathf.Sin(a) * d), new Vector3(0.07f, 0.04f, 0.06f) * rng.Range(0.7f, 1.3f), Color.Lerp(Col.Hex("#ff4a10"), Col.Hex("#ffa030"), rng.Value), 0.2f, rng, 0);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * Mathf.PI * 2 + 0.4f;
                em.Blob(new Vector3(Mathf.Cos(a) * 0.12f, 0.14f, Mathf.Sin(a) * 0.12f), new Vector3(0.06f, 0.05f, 0.06f), Col.Hex("#ff6018"), 0.15f, rng, 0);
            }
            Mats.Object("brasas", em.ToMesh("brasas"), emberMat, root, false).transform.position = Pos;
            // halo de calor (bolha aditiva) que respira com o fogo
            glowMat = new Material(Mats.SoftAdd) { name = "halo" };
            var gm = new MeshBuilder();
            gm.Blob(Vector3.zero, Vector3.one * 0.5f, Col.Hex("#ff7a28"), 0.05f, rng, 1);
            glow = Mats.Object("halo", gm.ToMesh("halo"), glowMat, root, false).transform;
            glow.position = Pos + Vector3.up * 0.45f;

            var lg = new GameObject("luz_fogueira");
            lg.transform.SetParent(root, false);
            lg.transform.position = Pos + Vector3.up * 1.4f;
            light = lg.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Col.Hex("#ff9a48");
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            light.shadowBias = 0.05f;

            crackle = Audio.Loop3D("crackle", Pos + Vector3.up * 0.4f, 2, 26);

            BuildCamp(w, root, rng);
        }

        /// <summary>Língua de fogo: cone torcido; alfa do vértice = altura (o shader usa para a cor e o tremor).</summary>
        static void Tongue(MeshBuilder m, float h, float r, int sides, Rng rng)
        {
            const int rings = 4;
            var prev = new Vector3[sides];
            for (int k = 0; k <= rings; k++)
            {
                float y = k / (float)rings;
                float rad = r * (1 - y) * (1 - y * 0.3f) + 0.005f;
                float bend = y * y * 0.12f;
                var ring = new Vector3[sides];
                for (int i = 0; i < sides; i++)
                {
                    float a = i / (float)sides * Mathf.PI * 2 + y * 1.4f;
                    ring[i] = new Vector3(Mathf.Cos(a) * rad + bend, y * h, Mathf.Sin(a) * rad);
                }
                if (k > 0)
                    for (int i = 0; i < sides; i++)
                    {
                        int j = (i + 1) % sides;
                        float y0 = (k - 1) / (float)rings;
                        var c0 = new Color(1, 1, 1, y0); var c1 = new Color(1, 1, 1, y);
                        m.Tri(prev[i], ring[j], prev[j], c0, c1, c0);
                        m.Tri(prev[i], ring[i], ring[j], c0, c1, c1);
                    }
                prev = ring;
            }
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
                p.y = w.Ground(p.x, p.z);
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
            float t = Time.time;
            // tremulação em camadas (lenta + rápida) e "engasgos" ocasionais
            float target = Mathf.PerlinNoise(t * 2.2f, 0.3f) * 0.45f + Mathf.PerlinNoise(t * 9f, 4f) * 0.35f + Mathf.PerlinNoise(t * 23f, 9f) * 0.2f;
            flick = Mathf.Lerp(flick, target, 1 - Mathf.Exp(-dt * 20));
            light.enabled = Lit;
            light.intensity = (0.8f + k * 1.5f) * (0.65f + flick * 0.7f) * Mathf.Lerp(0.5f, 1.25f, darkness);
            light.range = Mathf.Lerp(6f, 19f, k) * (0.9f + flick * 0.2f);
            // a luz "dança": as sombras dos troncos e das pessoas mexem junto
            light.transform.position = Pos + new Vector3(Mathf.PerlinNoise(t * 3, 1) - 0.5f, 1.3f + flick * 0.25f, Mathf.PerlinNoise(t * 3, 7) - 0.5f) * 0.12f + Vector3.up * 1.25f;
            light.shadows = darkness > 0.25f ? LightShadows.Soft : LightShadows.None;
            light.color = Color.Lerp(Col.Hex("#ff7a30"), Col.Hex("#ffb060"), flick);

            float s = Lit ? Mathf.Lerp(0.3f, 1.15f, k) : 0;
            flames.localScale = Vector3.one * s;
            for (int i = 0; i < tongues.Length; i++)
            {
                float n = Mathf.PerlinNoise(t * (2.5f + i * 0.3f), tongueSeed[i]);
                tongues[i].localScale = new Vector3(0.85f + n * 0.3f, 0.55f + n * 0.9f, 0.85f + n * 0.3f);
                tongueMat[i].SetColor("_Color", new Color(1, 1, 1, Mathf.Clamp01(0.55f + n * 0.6f) * (0.75f + darkness * 0.25f)));
            }
            glow.localScale = Vector3.one * (Lit ? (0.9f + flick * 0.5f) * Mathf.Lerp(0.6f, 1.6f, k) : 0);
            glowMat.SetColor("_Color", new Color(1, 0.55f, 0.2f, (0.08f + flick * 0.08f) * (0.4f + darkness)));
            // brasas continuam vermelhas um tempo mesmo com o fogo apagando
            float ember = Mathf.Clamp01(Fuel / 10f) * (0.55f + Mathf.PerlinNoise(t * 1.3f, 2) * 0.45f);
            emberMat.SetColor("_Color", new Color(1, 1, 1, ember));
            if (crackle) crackle.volume = Lit ? 0.25f + k * 0.6f : 0;

            if (!Lit) return;
            // fumaça subindo e indo com o vento (mais visível de dia)
            smokeT -= dt;
            if (smokeT <= 0)
            {
                smokeT = Mathf.Lerp(0.35f, 0.12f, k);
                Fx.Smoke(Pos + Vector3.up * (0.9f + k * 0.7f), 0.25f + k * 0.25f, Mathf.Lerp(0.32f, 0.12f, darkness));
            }
            // faíscas: fluxo contínuo + estouros com estalo
            if (Random.value < dt * (3 + k * 8)) Fx.Sparks(Pos + Vector3.up * 0.5f, 1, 0.8f + k);
            popT -= dt;
            if (popT <= 0)
            {
                popT = Random.Range(1.5f, 5f);
                Fx.Sparks(Pos + Vector3.up * 0.35f, Random.Range(6, 14), 1.3f + k);
                Audio.PlayAt("pop", Pos, 0.5f + k * 0.4f);
            }
        }
    }
}
