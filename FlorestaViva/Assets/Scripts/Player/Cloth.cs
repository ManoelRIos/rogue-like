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
        const int N = 9;
        const float Seg = 0.075f, Width = 0.075f;
        readonly HumanRig rig;
        readonly Vector3[] p = new Vector3[N], prev = new Vector3[N];
        readonly Mesh mesh;
        readonly Vector3[] verts = new Vector3[N * 2];
        readonly Color[] cols = new Color[N * 2];
        readonly Transform obj;
        bool init;
        Vector3 lastAnchor;

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

        float acc;

        public void Step(float dt, Vector3 bodyVel)
        {
            if (dt <= 0) return;
            var anchor = rig.scarf.position;
            var back = -rig.chest.forward;
            if (!init || (p[0] - anchor).sqrMagnitude > 4)
            {
                for (int i = 0; i < N; i++) { p[i] = prev[i] = anchor + Vector3.down * Seg * i * 0.9f + back * 0.03f * i; }
                init = true; acc = 0; lastAnchor = anchor;
            }
            // passos fixos pequenos: estável mesmo com o jogo a 20 ou a 144 quadros por segundo
            const float H = 1f / 120f;
            acc = Mathf.Min(acc + dt, 0.1f);
            var wind = Wind.At(anchor) * 0.8f;
            var hipTop = rig.pelvis.position + Vector3.up * 0.1f; var neck = rig.neck.position;
            var fwd = rig.chest.forward; fwd.y = 0; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            int steps = Mathf.FloorToInt(acc / H), s = 0;
            var from = lastAnchor;
            while (acc >= H)
            {
                acc -= H; s++;
                // a âncora anda aos poucos entre os passos: com o jogo lento ela não "teleporta" e não chicoteia a fita
                var anc = Vector3.Lerp(from, anchor, s / (float)Mathf.Max(1, steps));
                float t = Time.time;
                for (int i = 1; i < N; i++)
                {
                    float k = i / (float)N;
                    var v = (p[i] - prev[i]) * 0.985f;
                    var rel = wind - bodyVel * 0.35f;
                    var flutter = new Vector3(Mathf.Sin(t * 8f + i * 0.9f), Mathf.Sin(t * 6.3f + i) * 0.4f, Mathf.Cos(t * 7.1f + i * 0.7f)) * 0.9f * k;
                    var a = Vector3.down * 5.5f + (rel * 1.4f - v / H) * 0.9f * k + flutter;          // arrasto do ar: puxa a velocidade para o vento relativo
                    prev[i] = p[i];
                    var step = v + a * H * H;
                    if (step.sqrMagnitude > 0.0016f) step = step.normalized * 0.04f;                 // nunca "explode"
                    p[i] += step;
                }
                p[0] = anc; prev[0] = anc;
                for (int it = 0; it < 3; it++)
                {
                    for (int i = 1; i < N; i++)
                    {
                        var d = p[i] - p[i - 1];
                        float l = d.magnitude;
                        if (l > 1e-5f) p[i] = p[i - 1] + d * (Seg / l);
                    }
                    // só a partir do 3º ponto: o começo fica preso no pescoço sem brigar com o corpo
                    for (int i = 3; i < N; i++) p[i] = OutOfCapsule(p[i], hipTop, neck, 0.17f);
                    for (int i = 1; i < N; i++)
                    {
                        // fica sempre atrás do corpo e nunca sobe acima do ponto anterior (sem "antena" nem enrolar na frente)
                        float f = Vector3.Dot(p[i] - anc, fwd);
                        if (f > -0.02f * i) p[i] -= fwd * (f + 0.02f * i);
                        if (p[i].y > p[i - 1].y + 0.02f) p[i].y = p[i - 1].y + 0.02f;
                    }
                }
            }
            lastAnchor = p[0];
            // fita: largura contínua (sem virar do avesso entre um segmento e outro)
            var right = rig.chest.right;
            var lastW = right;
            for (int i = 0; i < N; i++)
            {
                var seg = (i < N - 1 ? p[i + 1] - p[i] : p[i] - p[i - 1]).normalized;
                var w = right - seg * Vector3.Dot(right, seg);
                if (w.sqrMagnitude < 0.04f) w = Vector3.Cross(seg, Vector3.up);
                if (w.sqrMagnitude < 1e-4f) w = lastW;
                w.Normalize();
                if (Vector3.Dot(w, lastW) < 0) w = -w;
                w = Vector3.Slerp(lastW, w, i == 0 ? 1 : 0.6f).normalized;
                lastW = w;
                var half = w * (Width * (1 - i / (float)N * 0.35f));
                verts[i * 2] = p[i] - half; verts[i * 2 + 1] = p[i] + half;
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
