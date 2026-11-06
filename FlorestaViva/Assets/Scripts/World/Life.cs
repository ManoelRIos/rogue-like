using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Vida da floresta: vagalumes (anoitecer e noite, perto do rio e da mata fechada),
    /// pólen flutuando de dia e bandos de pássaros circulando no céu.
    /// Tudo instanciado/leve — nada disso tem física.
    /// </summary>
    public class Life
    {
        readonly Flora flora;
        readonly Mesh moteMesh, pollenMesh;
        readonly Material fireflyMat, pollenMat;
        readonly Matrix4x4[] buf = new Matrix4x4[1023];
        readonly List<Bird> birds = new();

        class Bird { public Transform t, wl, wr; public Vector3 center; public float r, h, speed, ph; }

        public Life(Flora f, Transform parent)
        {
            flora = f;
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

        void Flush(Mesh m, Material mat, int n) =>
            Graphics.DrawMeshInstanced(m, 0, mat, buf, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);

        static float Hash(float x) { float s = Mathf.Sin(x * 12.9898f) * 43758.5453f; return s - Mathf.Floor(s); }
    }
}
