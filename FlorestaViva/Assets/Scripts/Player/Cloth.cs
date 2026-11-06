using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Cachecol de pano com física (Verlet): uma corrente de pontos presa atrás do pescoço que cai com a
    /// gravidade, é empurrada pelo vento e pelo movimento, e não atravessa o corpo. Desenhada como uma fita
    /// com malha atualizada a cada quadro — é o que dá o ar "esvoaçante" do personagem.
    /// </summary>
    public class ClothRibbon
    {
        const int N = 12;
        const float Seg = 0.1f, Width = 0.085f;
        readonly HumanRig rig;
        readonly Vector3[] p = new Vector3[N], prev = new Vector3[N];
        readonly Mesh mesh;
        readonly Vector3[] verts = new Vector3[N * 2];
        readonly Color[] cols = new Color[N * 2];
        readonly Transform obj;
        bool init;

        public ClothRibbon(HumanRig r, Color baseCol, Color tipCol)
        {
            rig = r;
            mesh = new Mesh { name = "cachecol_pano" };
            var tris = new int[(N - 1) * 6];
            for (int i = 0; i < N - 1; i++)
            {
                int a = i * 2;
                tris[i * 6] = a; tris[i * 6 + 1] = a + 2; tris[i * 6 + 2] = a + 1;
                tris[i * 6 + 3] = a + 1; tris[i * 6 + 4] = a + 2; tris[i * 6 + 5] = a + 3;
            }
            for (int i = 0; i < N; i++)
            {
                var c = Color.Lerp(baseCol, tipCol, i / (float)(N - 1) * 0.6f); c.a = 0;
                cols[i * 2] = c; cols[i * 2 + 1] = Color.Lerp(c, Color.black, 0.08f);
            }
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.triangles = tris;
            var go = Mats.Object("cachecol_pano", mesh, Mats.LowPoly, null, true);
            obj = go.transform;                         // fica no mundo (vértices já em coordenadas do mundo)
        }

        public void SetVisible(bool v) { if (obj) obj.gameObject.SetActive(v); }

        public void Step(float dt, Vector3 bodyVel)
        {
            if (dt <= 0) return;
            var anchor = rig.scarf.position;
            var back = -rig.chest.forward;
            if (!init || (p[0] - anchor).sqrMagnitude > 4)
            {
                for (int i = 0; i < N; i++) { p[i] = prev[i] = anchor + Vector3.down * Seg * i + back * 0.02f * i; }
                init = true;
            }
            float h = Mathf.Min(dt, 1 / 30f);
            var wind = Wind.At(anchor) * 0.9f;
            float t = Time.time;
            // integra (Verlet) com gravidade leve, arrasto do ar e vento com tremor
            for (int i = 1; i < N; i++)
            {
                var v = (p[i] - prev[i]) * 0.965f;
                var rel = wind - bodyVel * 0.25f;
                float k = i / (float)N;
                var flutter = new Vector3(Mathf.Sin(t * 9f + i * 0.8f), Mathf.Sin(t * 7f + i), Mathf.Cos(t * 8f + i * 0.6f)) * 0.6f * k;
                prev[i] = p[i];
                p[i] += v + (Vector3.down * 3.2f + rel * 1.1f + flutter) * h * h;
            }
            p[0] = anchor; prev[0] = anchor;
            // restrições: comprimento dos segmentos + não entrar no corpo (cápsula do tronco)
            var hipTop = rig.pelvis.position; var neck = rig.neck.position;
            for (int it = 0; it < 4; it++)
            {
                for (int i = 1; i < N; i++)
                {
                    var d = p[i] - p[i - 1];
                    float l = d.magnitude;
                    if (l > 1e-5f) p[i] = p[i - 1] + d * (Seg / l);
                }
                for (int i = 1; i < N; i++) p[i] = OutOfCapsule(p[i], hipTop, neck, 0.2f);
            }
            // fita: largura no sentido "direita" do corpo, perpendicular ao segmento
            var right = rig.chest.right;
            for (int i = 0; i < N; i++)
            {
                var seg = i < N - 1 ? p[i + 1] - p[i] : p[i] - p[i - 1];
                var w = Vector3.ProjectOnPlane(right, seg.normalized).normalized * (Width * (1 - i / (float)N * 0.35f));
                verts[i * 2] = p[i] - w; verts[i * 2 + 1] = p[i] + w;
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        static Vector3 OutOfCapsule(Vector3 q, Vector3 a, Vector3 b, float r)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-5f));
            var c = a + ab * t;
            var d = q - c;
            float l = d.magnitude;
            if (l >= r || l < 1e-5f) return q;
            return c + d * (r / l);
        }
    }
}
