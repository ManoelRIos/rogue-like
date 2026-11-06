using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Vida e clima da floresta: vagalumes, pólen, bandos de pássaros, borboletas nas flores,
    /// folhas caindo na mata, névoa rasteira no amanhecer e à noite, nuvens no céu, e sons com
    /// posição no mundo (pássaros, pica-pau, coruja, sapos no rio, galhos estalando no escuro).
    /// </summary>
    public class Life
    {
        readonly Flora flora;
        readonly Mesh moteMesh, pollenMesh;
        readonly Material fireflyMat, pollenMat;
        readonly Matrix4x4[] buf = new Matrix4x4[1023];
        readonly List<Bird> birds = new();

        class Bird { public Transform t, wl, wr; public Vector3 center; public float r, h, speed, ph; }
        class Butterfly { public Transform t, wl, wr; public Vector3 pos, target; public float ph, rest; }

        readonly World world;
        readonly List<Butterfly> butterflies = new();
        readonly Mesh[] cloudMeshes = new Mesh[3];
        readonly Mesh mistMesh;
        readonly Vector4[] cbuf = new Vector4[1023];
        readonly MaterialPropertyBlock mpb = new();
        readonly AudioSource river;
        readonly Mesh orbMesh;
        float soundT = 3, leafAcc;

        public Life(Flora f, World w, Transform parent)
        {
            flora = f; world = w;
            var rng = new Rng(21);
            var mb = new MeshBuilder();
            mb.Blob(Vector3.zero, Vector3.one * 0.05f, Col.Hex("#c8ff60"), 0, rng, 0);
            moteMesh = mb.ToMesh("vagalume");
            mb = new MeshBuilder();
            mb.Blob(Vector3.zero, Vector3.one * 0.025f, Col.Hex("#fff0b0"), 0, rng, 0);
            pollenMesh = mb.ToMesh("polen");
            fireflyMat = new Material(Mats.Glow) { name = "vagalumes", enableInstancing = true };
            pollenMat = new Material(Mats.Glow) { name = "polen", enableInstancing = true };

            // pássaros: corpo + duas asas que batem
            var root = new GameObject("Passaros").transform; root.SetParent(parent, false);
            var body = new MeshBuilder();
            body.Blob(Vector3.zero, new Vector3(0.12f, 0.1f, 0.22f), Col.Hex("#3a3430"), 0.05f, rng, 0);
            body.Cylinder(new Vector3(0, 0, 0.2f), 0.1f, 0.04f, 0f, 4, Col.Hex("#d8a040"), 0, 0, 0, rng, false);
            var bodyMesh = body.ToMesh("passaro");
            Mesh Wing(float side)
            {
                var w = new MeshBuilder();
                w.Tri(Vector3.zero, new Vector3(side * 0.45f, 0, -0.1f), new Vector3(0, 0, 0.14f), Col.Hex("#4a423a"));
                w.Tri(Vector3.zero, new Vector3(side * 0.45f, 0, -0.1f), new Vector3(side * 0.2f, 0, -0.2f), Col.Hex("#2e2824"));
                return w.ToMesh("asa");
            }
            var wl = Wing(-1); var wr = Wing(1);

            // borboletas
            Color[] bc = { Col.Hex("#f0a030"), Col.Hex("#f4f0e0"), Col.Hex("#6a9ae8"), Col.Hex("#f0e050") };
            for (int i = 0; i < 14; i++)
            {
                var c = bc[i % bc.Length];
                Mesh BWing(float side)
                {
                    var m = new MeshBuilder();
                    m.Tri(Vector3.zero, new Vector3(side * 0.09f, 0, 0.05f), new Vector3(side * 0.07f, 0, -0.02f), Col.A(c, 0));
                    m.Tri(Vector3.zero, new Vector3(side * 0.07f, 0, -0.02f), new Vector3(side * 0.05f, 0, -0.07f), Col.A(Color.Lerp(c, Color.black, 0.25f), 0));
                    return m.ToMesh("asa_borboleta");
                }
                var bm = new MeshBuilder();
                bm.Box(new Vector3(-0.008f, -0.008f, -0.04f), new Vector3(0.008f, 0.008f, 0.04f), Col.Hex("#2a2420"));
                var bt = Mats.Object("borboleta", bm.ToMesh("borboleta"), Mats.LowPoly, parent, false).transform;
                var b = new Butterfly { t = bt, ph = rng.Range(0, 10) };
                b.wl = Mats.Object("asa", BWing(-1), Mats.LowPoly, bt, false).transform;
                b.wr = Mats.Object("asa", BWing(1), Mats.LowPoly, bt, false).transform;
                butterflies.Add(b);
            }

            // nuvens: aglomerados achatados de bolhas
            for (int k = 0; k < cloudMeshes.Length; k++)
            {
                var cm = new MeshBuilder();
                int puffs = 4 + k * 2;
                for (int i = 0; i < puffs; i++)
                {
                    var c = new Vector3(rng.Range(-14, 14), rng.Range(-1.5f, 3), rng.Range(-8, 8));
                    float s = rng.Range(6, 11) * (1 - Mathf.Abs(c.x) / 30f);
                    cm.Blob(c, new Vector3(s, s * 0.55f, s * 0.8f), Color.white, 0.12f, rng, 1, 0, 0, Col.Hex("#ffffff"));
                    // base mais escura (volume)
                    for (int v = cm.v.Count - 240; v < cm.v.Count; v++) if (v >= 0 && cm.v[v].y < c.y - s * 0.2f) cm.c[v] = new Color(0.82f, 0.85f, 0.9f, 0);
                }
                cloudMeshes[k] = cm.ToMesh("nuvem");
            }
            var mm = new MeshBuilder();
            mm.Blob(Vector3.zero, new Vector3(1, 0.25f, 1), Color.white, 0.15f, rng, 1);
            mistMesh = mm.ToMesh("nevoa");
            river = Audio.Loop3D("river", Vector3.zero, 4, 48);
            var om = new MeshBuilder();
            om.Blob(Vector3.zero, Vector3.one * 0.5f, Color.white, 0.05f, rng, 1);
            orbMesh = om.ToMesh("luz");
            for (int flock = 0; flock < 4; flock++)
            {
                var c = new Vector3(rng.Range(-110, 110), 0, rng.Range(-110, 110));
                float h = rng.Range(22, 34), r = rng.Range(14, 30), sp = rng.Range(0.25f, 0.4f) * (rng.Chance(0.5f) ? 1 : -1);
                int n = rng.Int(4, 7);
                for (int i = 0; i < n; i++)
                {
                    var b = new Bird { center = c + new Vector3(rng.Range(-4, 4), 0, rng.Range(-4, 4)), r = r + rng.Range(-3, 3), h = h + rng.Range(-2, 2), speed = sp, ph = i * 0.18f + rng.Value * 0.1f };
                    b.t = Mats.Object("passaro", bodyMesh, Mats.LowPoly, root, false).transform;
                    b.wl = Mats.Object("asaE", wl, Mats.LowPoly, b.t, false).transform;
                    b.wr = Mats.Object("asaD", wr, Mats.LowPoly, b.t, false).transform;
                    birds.Add(b);
                }
            }
        }

        public void Update(Vector3 cam, float darkness, float time)
        {
            float t = Time.time;
            float dt = Time.deltaTime;
            Butterflies(cam, darkness, t, dt);
            Clouds(cam, darkness, time, t);
            Mist(cam, darkness, time, t);
            FallingLeaves(cam, darkness, dt);
            Sounds(cam, darkness, dt);
            Spirits(cam, darkness, t);
            // som do rio vem do ponto do rio mais perto de você
            if (river) river.transform.position = new Vector3(cam.x, World.WaterLevel, world.RiverZ(cam.x));
            // vagalumes: aparecem do crepúsculo em diante
            float ff = Mathf.Clamp01((darkness - 0.25f) * 2f);
            if (ff > 0.01f)
            {
                fireflyMat.SetColor("_Color", new Color(1, 1, 1, ff * 1.4f));
                int n = 0, idx = 0;
                foreach (var s in flora.FireflySpots)
                {
                    idx++;
                    if ((s - cam).sqrMagnitude > 55 * 55) continue;
                    for (int k = 0; k < 3; k++)
                    {
                        float ph = idx * 1.7f + k * 2.3f;
                        var p = s + new Vector3(Mathf.Sin(t * 0.4f + ph) * 2.2f, Mathf.Sin(t * 0.7f + ph * 1.3f) * 0.8f + 0.4f, Mathf.Cos(t * 0.33f + ph * 0.7f) * 2.2f);
                        // cada vagalume pisca no seu ritmo
                        float blink = Mathf.Clamp01(Mathf.Sin(t * (1.2f + (k * 0.37f)) + ph) * 2f);
                        if (blink <= 0.05f) continue;
                        buf[n++] = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * blink);
                        if (n == buf.Length) { Flush(moteMesh, fireflyMat, n); n = 0; }
                    }
                }
                if (n > 0) Flush(moteMesh, fireflyMat, n);
            }

            // pólen dourado ao redor da câmera durante o dia
            float pl = 1 - darkness;
            if (pl > 0.05f)
            {
                pollenMat.SetColor("_Color", new Color(1, 1, 1, pl * 0.35f));
                int n = 0;
                var origin = new Vector3(Mathf.Floor(cam.x / 24) * 24, 0, Mathf.Floor(cam.z / 24) * 24);
                for (int i = 0; i < 160; i++)
                {
                    float hx = Hash(i * 3.1f), hz = Hash(i * 7.7f + 1), hy = Hash(i * 1.3f + 2);
                    var p = origin + new Vector3(hx * 48 - 12 + Mathf.Sin(t * 0.2f + i) * 1.5f, 0, hz * 48 - 12 + t * 0.3f % 48);
                    p.x = cam.x + Mathf.Repeat(p.x - cam.x + 24, 48) - 24;
                    p.z = cam.z + Mathf.Repeat(p.z - cam.z + 24, 48) - 24;
                    p.y = cam.y - 2 + hy * 6 + Mathf.Sin(t * 0.5f + i) * 0.5f;
                    buf[n++] = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one);
                }
                Flush(pollenMesh, pollenMat, n);
            }

            // pássaros só de dia
            bool day = darkness < 0.6f;
            foreach (var b in birds)
            {
                b.t.gameObject.SetActive(day);
                if (!day) continue;
                float a = t * b.speed + b.ph;
                var p = b.center + new Vector3(Mathf.Cos(a) * b.r, b.h + Mathf.Sin(t * 0.6f + b.ph * 5) * 1.2f, Mathf.Sin(a) * b.r);
                var tangent = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * Mathf.Sign(b.speed);
                b.t.position = p;
                b.t.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0, 0, -15 * Mathf.Sign(b.speed));
                // bate as asas em rajadas e plana
                float flap = Mathf.Sin(t * 14 + b.ph * 20) * (Mathf.Sin(t * 0.8f + b.ph * 3) > 0 ? 45 : 8);
                b.wl.localRotation = Quaternion.Euler(0, 0, -flap);
                b.wr.localRotation = Quaternion.Euler(0, 0, flap);
            }
        }

        void Butterflies(Vector3 cam, float darkness, float t, float dt)
        {
            bool day = darkness < 0.45f;
            foreach (var b in butterflies)
            {
                b.t.gameObject.SetActive(day);
                if (!day) continue;
                // longe demais: reaparece perto de você, num lugar aberto
                if ((b.pos - cam).sqrMagnitude > 45 * 45 || b.pos == Vector3.zero)
                {
                    var d = Random.insideUnitCircle * 30f;
                    b.pos = new Vector3(cam.x + d.x, 0, cam.z + d.y);
                    b.pos.y = world.Ground(b.pos.x, b.pos.z) + 0.8f;
                    b.target = b.pos;
                }
                if (b.rest > 0) b.rest -= dt;
                else if ((b.target - b.pos).sqrMagnitude < 0.05f)
                {
                    // escolhe uma flor/ponto próximo; às vezes pousa um pouco
                    var d = Random.insideUnitCircle * 3f;
                    b.target = b.pos + new Vector3(d.x, 0, d.y);
                    b.target.y = world.Ground(b.target.x, b.target.z) + Random.Range(0.35f, 1.4f);
                    if (Random.value < 0.3f) b.rest = Random.Range(1f, 3f);
                }
                var to = b.target - b.pos;
                var wob = new Vector3(Mathf.Sin(t * 5 + b.ph), Mathf.Sin(t * 7.3f + b.ph) * 1.5f, Mathf.Cos(t * 4.4f + b.ph)) * 0.6f;
                if (b.rest <= 0) b.pos += (to.normalized * 1.1f + wob) * dt + Wind.At(b.pos) * 0.1f * dt;
                b.t.position = b.pos;
                if (to.sqrMagnitude > 0.01f) b.t.rotation = Quaternion.Slerp(b.t.rotation, Quaternion.LookRotation(new Vector3(to.x, 0, to.z) + Vector3.forward * 1e-3f), dt * 4);
                float flap = b.rest > 0 ? 70 + Mathf.Sin(t * 2 + b.ph) * 15 : Mathf.Sin(t * 22 + b.ph) * 65;
                b.wl.localRotation = Quaternion.Euler(0, 0, -flap);
                b.wr.localRotation = Quaternion.Euler(0, 0, flap);
            }
        }

        void Clouds(Vector3 cam, float darkness, float time, float t)
        {
            // cor das nuvens: branca de dia, alaranjada no pôr/nascer do sol, azul-escura à noite
            float sunset = Mathf.Max(Mathf.Clamp01(1 - Mathf.Abs(time - 0.74f) / 0.05f), Mathf.Clamp01(1 - Mathf.Abs(time - 0.26f) / 0.05f));
            var day = Color.Lerp(Col.Hex("#f4f6f8"), Col.Hex("#f0a070"), sunset);
            var col = Color.Lerp(day, Col.Hex("#1c2030"), darkness);
            var c = new Vector4(col.r, col.g, col.b, 0.92f);
            for (int k = 0; k < cloudMeshes.Length; k++)
            {
                int n = 0;
                for (int i = k; i < 21; i += cloudMeshes.Length)
                {
                    // deriva com o vento e "dá a volta" num quadrado de 520 m ao redor da câmera
                    float bx = Hash(i * 3.7f) * 520 + Wind.Dir.x * t * 2.2f, bz = Hash(i * 9.1f) * 520 + Wind.Dir.y * t * 2.2f;
                    float x = cam.x + Wrap(bx - cam.x, 520) - 260, z = cam.z + Wrap(bz - cam.z, 520) - 260;
                    float y = 125 + Hash(i * 1.7f) * 35;
                    float s = 0.9f + Hash(i * 5.3f) * 1.1f;
                    buf[n] = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0, Hash(i * 2.2f) * 360, 0), new Vector3(s, s * 0.8f, s));
                    cbuf[n] = c;
                    n++;
                }
                mpb.Clear(); mpb.SetVectorArray("_Color", cbuf);
                Graphics.DrawMeshInstanced(cloudMeshes[k], 0, Mats.SoftUnlit, buf, n, mpb, UnityEngine.Rendering.ShadowCastingMode.Off, false);
            }
        }

        void Mist(Vector3 cam, float darkness, float time, float t)
        {
            // névoa baixa: forte ao amanhecer, média à noite, some de dia
            float dawn = Mathf.Clamp01(1 - Mathf.Abs(time - 0.27f) / 0.07f);
            float amount = Mathf.Max(dawn, darkness * 0.6f);
            if (amount < 0.02f) return;
            const float C = 7f;
            int cx = Mathf.FloorToInt(cam.x / C), cz = Mathf.FloorToInt(cam.z / C), n = 0;
            for (int z = cz - 9; z <= cz + 9; z++)
                for (int x = cx - 9; x <= cx + 9; x++)
                {
                    float h = Hash(x * 31.7f + z * 17.3f);
                    float wx = x * C + Hash(x * 3.1f + z) * C, wz = z * C + Hash(z * 4.7f + x) * C;
                    float gy = world.Ground(wx, wz);
                    // mais névoa nas partes baixas e perto do rio
                    float low = Mathf.Clamp01(1 - (gy - 0.5f) / 5f) + (world.RiverDist(wx, wz) < 14 ? 0.5f : 0);
                    if (h > 0.25f + low * 0.45f || gy < World.WaterLevel - 0.5f) continue;
                    var drift = new Vector3(Wind.Dir.x, 0, Wind.Dir.y) * Mathf.Sin(t * 0.05f + h * 6) * 2.5f;
                    float s = 3.5f + h * 6f;
                    buf[n] = Matrix4x4.TRS(new Vector3(wx, Mathf.Max(gy, World.WaterLevel) + 0.35f, wz) + drift, Quaternion.Euler(0, h * 360, 0), new Vector3(s, 1.2f + h, s));
                    cbuf[n] = new Vector4(0.82f, 0.86f, 0.92f, amount * (0.1f + h * 0.12f));
                    if (++n == buf.Length) break;
                }
            if (n == 0) return;
            mpb.Clear(); mpb.SetVectorArray("_Color", cbuf);
            Graphics.DrawMeshInstanced(mistMesh, 0, Mats.Soft, buf, n, mpb, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }

        void FallingLeaves(Vector3 cam, float darkness, float dt)
        {
            float dens = world.ForestDensity(cam.x, cam.z);
            if (dens < 0.46f) return;
            leafAcc += dt * (1.5f + Wind.Strength * 2f) * Mathf.InverseLerp(0.46f, 0.6f, dens);
            while (leafAcc > 1)
            {
                leafAcc -= 1;
                var d = Random.insideUnitCircle * 14f;
                var p = new Vector3(cam.x + d.x, 0, cam.z + d.y);
                p.y = world.Ground(p.x, p.z) + Random.Range(5f, 9f);
                int v = Random.value < 0.25f ? Fx.LeafAutumn : Random.value < 0.5f ? Fx.LeafOak : Fx.LeafBirch;
                Fx.Leaves(p, 0.2f, 1, v);
            }
        }

        void Sounds(Vector3 cam, float darkness, float dt)
        {
            soundT -= dt;
            if (soundT > 0) return;
            var d = Random.insideUnitCircle.normalized * Random.Range(14f, 40f);
            var at = new Vector3(cam.x + d.x, 0, cam.z + d.y);
            at.y = world.Ground(at.x, at.z) + Random.Range(1f, 8f);
            bool nearRiver = world.RiverDist(cam.x, cam.z) < 35;
            if (darkness < 0.4f)
            {
                soundT = Random.Range(1.5f, 5f);
                float r = Random.value;
                Audio.PlayAt(r < 0.3f ? "bird1" : r < 0.6f ? "bird2" : r < 0.88f ? "bird3" : "knock", at, Random.Range(0.25f, 0.5f), 0.3f, 60);
            }
            else
            {
                soundT = Random.Range(4f, 11f);
                float r = Random.value;
                if (nearRiver && r < 0.45f) Audio.PlayAt("frog", new Vector3(at.x, World.WaterLevel, world.RiverZ(at.x)), 0.5f, 0.2f, 50);
                else if (r < 0.7f) Audio.PlayAt("owl", at + Vector3.up * 6, 0.45f, 1f, 80);
                else Audio.PlayAt("twig", at, 0.6f, 1f, 45);           // algo pisou num galho...
            }
            if (Wind.Strength > 1.3f && Random.value < 0.25f) Audio.PlayAt("gust", cam + new Vector3(Wind.Dir.x, 2, Wind.Dir.y) * -12, 0.35f, 3f, 60);
        }

        /// <summary>Luzinhas do rio: no lusco-fusco, orbes turquesa flutuam devagar sobre a água (some na noite fechada).</summary>
        void Spirits(Vector3 cam, float darkness, float t)
        {
            float amt = Mathf.Clamp01(1 - Mathf.Abs(darkness - 0.42f) / 0.32f);
            if (amt < 0.02f) return;
            int n = 0;
            for (int i = 0; i < 24; i++)
            {
                float x = cam.x + (Hash(i * 7.3f) - 0.5f) * 90 + Mathf.Sin(t * 0.07f + i) * 6;
                float z = world.RiverZ(x) + (Hash(i * 3.9f) - 0.5f) * 14 + Mathf.Cos(t * 0.09f + i * 2) * 3;
                float y = Mathf.Max(world.Ground(x, z), World.WaterLevel) + 0.8f + Hash(i * 1.1f) * 2.2f + Mathf.Sin(t * 0.6f + i) * 0.35f;
                float pulse = 0.6f + 0.4f * Mathf.Sin(t * 1.3f + i * 1.7f);
                buf[n] = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, Vector3.one * (0.16f + Hash(i * 5.1f) * 0.12f));
                cbuf[n] = new Vector4(0.5f, 0.95f, 1f, amt * pulse * 1.4f);
                n++;
            }
            mpb.Clear(); mpb.SetVectorArray("_Color", cbuf);
            Graphics.DrawMeshInstanced(orbMesh, 0, Mats.SoftAdd, buf, n, mpb, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }

        static float Wrap(float x, float m) => ((x % m) + m) % m;

        void Flush(Mesh m, Material mat, int n) =>
            Graphics.DrawMeshInstanced(m, 0, mat, buf, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);

        static float Hash(float x) { float s = Mathf.Sin(x * 12.9898f) * 43758.5453f; return s - Mathf.Floor(s); }
    }
}
