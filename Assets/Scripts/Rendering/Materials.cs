using UnityEngine;

namespace Ecos
{
    /// <summary>Materiais compartilhados criados a partir dos shaders em Resources/Shaders.</summary>
    public static class Materials
    {
        public static Material Sprite;       // sprites iluminados (personagens, itens)
        public static Material SpriteBright; // sprites sem iluminação (balas, efeitos, UI no mundo)
        public static Material Additive;     // brilhos
        public static Material UI;           // interface
        public static Material Blend;        // sombras e transparências
        static Shader lit;

        public static void Init()
        {
            if (Sprite != null) return;
            lit = Resources.Load<Shader>("Shaders/PixelLit");
            var add = Resources.Load<Shader>("Shaders/PixelAdditive");
            var ui = Resources.Load<Shader>("Shaders/PixelUI");
            if (lit == null) Debug.LogError("Ecos: shader PixelLit não encontrado.");
            Sprite = new Material(lit) { name = "EcosSprite" };
            SpriteBright = new Material(lit) { name = "EcosSpriteBright" };
            SpriteBright.SetFloat("_Lit", 0);
            Additive = new Material(add) { name = "EcosAdditive" };
            UI = new Material(ui) { name = "EcosUI" };
            Blend = new Material(Resources.Load<Shader>("Shaders/PixelBlend")) { name = "EcosBlend" };
        }

        public static Material World(Texture tex)
        {
            var m = new Material(lit) { mainTexture = tex, name = "EcosWorld" };
            return m;
        }
    }

    /// <summary>Luzes pontuais estilizadas enviadas ao shader (até 16 por quadro).</summary>
    public static class LightingSystem
    {
        public class PointLight
        {
            public Vector3 pos;
            public float radius = 6;
            public Color color = Color.white;
            public float intensity = 1;
            public float life = -1;     // >0 = luz temporária
            public float flicker;       // 0..1
            public float phase;
            public bool enabled = true;
            internal float baseIntensity;
        }

        const int MAX = 16;
        static readonly System.Collections.Generic.List<PointLight> lights = new();
        static readonly Vector4[] pos = new Vector4[MAX];
        static readonly Vector4[] col = new Vector4[MAX];
        static readonly System.Collections.Generic.List<(PointLight l, float d)> sorted = new();
        public static Color Ambient = new Color(0.55f, 0.5f, 0.55f);

        public static PointLight Add(Vector3 p, float radius, Color c, float intensity, float life = -1, float flicker = 0)
        {
            var l = new PointLight { pos = p, radius = radius, color = c, intensity = intensity, life = life, flicker = flicker, phase = Random.value * 10f };
            l.baseIntensity = intensity;
            lights.Add(l);
            return l;
        }

        public static void Remove(PointLight l) => lights.Remove(l);
        public static void Clear() => lights.Clear();

        public static void Update(float dt, Vector3 center)
        {
            float t = Time.time;
            sorted.Clear();
            for (int i = lights.Count - 1; i >= 0; i--)
            {
                var l = lights[i];
                if (!l.enabled) continue;
                if (l.life > 0)
                {
                    l.life -= dt;
                    if (l.life <= 0) { lights.RemoveAt(i); continue; }
                }
                if (l.flicker > 0)
                    l.intensity = l.baseIntensity * (1 + (Mathf.Sin(t * 9 + l.phase) * 0.08f + Mathf.Sin(t * 23 + l.phase * 3) * 0.05f) * l.flicker * 2f);
                sorted.Add((l, (l.pos - center).sqrMagnitude));
            }
            sorted.Sort((a, b) => a.d.CompareTo(b.d));
            int n = Mathf.Min(MAX, sorted.Count);
            for (int i = 0; i < MAX; i++)
            {
                if (i < n)
                {
                    var l = sorted[i].l;
                    float k = l.life > 0 ? Mathf.Clamp01(l.life * 4f) : 1f;
                    pos[i] = new Vector4(l.pos.x, l.pos.y, l.pos.z, l.radius);
                    col[i] = new Vector4(l.color.r, l.color.g, l.color.b, l.intensity * k);
                }
                else { pos[i] = Vector4.zero; col[i] = Vector4.zero; }
            }
            Shader.SetGlobalVectorArray("_EcosLightPos", pos);
            Shader.SetGlobalVectorArray("_EcosLightCol", col);
            Shader.SetGlobalFloat("_EcosLightCount", n);
            Shader.SetGlobalVector("_EcosAmbient", Ambient);
            Shader.SetGlobalFloat("_EcosSteps", 6f);
        }
    }
}
