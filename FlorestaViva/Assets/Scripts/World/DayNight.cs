using System;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Ciclo de dia e noite. De dia: sol quente, céu claro, vento leve, pássaros.
    /// À noite: lua fria, neblina densa, estrelas, vagalumes — e as criaturas saem da mata.
    /// time: 0..1 (0 = meia-noite, 0.25 = amanhecer, 0.5 = meio-dia, 0.75 = anoitecer).
    /// </summary>
    public class DayNight
    {
        public float DayLength = 600f;        // segundos por dia completo
        public float time = 0.3f;
        public int Day = 1;
        public bool IsNight { get; private set; }
        public float Darkness { get; private set; }   // 0 = dia pleno, 1 = noite fechada
        public event Action OnNight, OnDay;

        readonly Light sun, moon;
        readonly Transform sky;
        readonly Material skyMat;

        // cores ao longo do dia (chaves: 0 meia-noite ... 1)
        // paleta "Omno": céus pastel e luminosos, horizonte com névoa quente, noites azul-arroxeadas (escuras, mas com cor)
        // top = alto do céu, hor = horizonte/névoa distante, sun = cor do sol, amb = luz ambiente vinda do céu, fog = névoa perto
        static readonly (float t, Color top, Color hor, Color sun, Color amb, Color fog)[] Keys =
        {
            (0.00f, Col.Hex("#090d28"), Col.Hex("#1a1e46"), Col.Hex("#000000"), Col.Hex("#1c2350"), Col.Hex("#121734")),
            (0.20f, Col.Hex("#121844"), Col.Hex("#362e62"), Col.Hex("#000000"), Col.Hex("#232a56"), Col.Hex("#231f48")),
            (0.27f, Col.Hex("#5a74c8"), Col.Hex("#ffb08a"), Col.Hex("#ffb27a"), Col.Hex("#8a7cae"), Col.Hex("#f0b4a4")),
            (0.35f, Col.Hex("#58a6ea"), Col.Hex("#d4ecf4"), Col.Hex("#fff0d8"), Col.Hex("#9ab8d4"), Col.Hex("#cde6f0")),
            (0.50f, Col.Hex("#479ef0"), Col.Hex("#dcf2f6"), Col.Hex("#fff6e6"), Col.Hex("#a8c6dc"), Col.Hex("#d2eaf2")),
            (0.66f, Col.Hex("#5894e2"), Col.Hex("#ffe0b4"), Col.Hex("#ffd6a4"), Col.Hex("#b4aec4"), Col.Hex("#f6dcc4")),
            (0.73f, Col.Hex("#5856bc"), Col.Hex("#ff9a7c"), Col.Hex("#ff8858"), Col.Hex("#94709c"), Col.Hex("#e88e80")),
            (0.80f, Col.Hex("#181a4a"), Col.Hex("#4a2c62"), Col.Hex("#000000"), Col.Hex("#2a2a5a"), Col.Hex("#261f4a")),
            (1.00f, Col.Hex("#090d28"), Col.Hex("#1a1e46"), Col.Hex("#000000"), Col.Hex("#1c2350"), Col.Hex("#121734")),
        };
        public OmnoPost Post;

        public DayNight()
        {
            var s = new GameObject("Sol");
            sun = s.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.62f;     // sombras claras e coloridas pelo ambiente (estilo Omno)
            var m = new GameObject("Lua");
            moon = m.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = Col.Hex("#7a8cc8");
            moon.shadows = LightShadows.None;

            // cúpula do céu
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.name = "Ceu";
            skyMat = Mats.Sky;
            go.GetComponent<MeshRenderer>().sharedMaterial = skyMat;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.localScale = Vector3.one * 760;   // raio 380 m: dentro do plano distante da câmera (420 m)
            sky = go.transform;

            RenderSettings.fog = false;          // a neblina é feita nos shaders (por altura, com brilho do sol)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            QualitySettings.shadowDistance = 70;
            IsNight = Phase(time) == 1;
        }

        static int Phase(float t) => (t > 0.76f || t < 0.24f) ? 1 : 0;

        static T Sample<T>(float t, Func<(float t, Color top, Color hor, Color sun, Color amb, Color fog), Color> pick, Func<Color, T> conv)
        {
            for (int i = 0; i < Keys.Length - 1; i++)
                if (t >= Keys[i].t && t <= Keys[i + 1].t)
                {
                    float k = Mathf.InverseLerp(Keys[i].t, Keys[i + 1].t, t);
                    return conv(Color.Lerp(pick(Keys[i]), pick(Keys[i + 1]), k));
                }
            return conv(pick(Keys[0]));
        }

        Color C(float t, Func<(float t, Color top, Color hor, Color sun, Color amb, Color fog), Color> pick) => Sample(t, pick, c => c);

        public void Update(float dt, Vector3 cameraPos)
        {
            time = (time + dt / DayLength) % 1f;
            int ph = Phase(time);
            if (ph == 1 && !IsNight) { IsNight = true; OnNight?.Invoke(); }
            if (ph == 0 && IsNight) { IsNight = false; Day++; OnDay?.Invoke(); }

            // escuridão contínua (0 dia .. 1 noite)
            float sunH = Mathf.Sin((time - 0.25f) * Mathf.PI * 2);
            Darkness = Mathf.Clamp01(Mathf.InverseLerp(0.25f, -0.2f, sunH));

            sun.transform.rotation = Quaternion.Euler((time - 0.25f) * 360f, 35f, 0);
            sun.color = C(time, k => k.sun);
            sun.intensity = Mathf.Clamp01(sunH * 2.5f + 0.1f) * 1.25f;
            sun.enabled = sun.intensity > 0.01f;
            moon.transform.rotation = Quaternion.Euler((time + 0.25f) * 360f, 200f, 0);
            moon.intensity = Darkness * 0.22f;
            moon.enabled = moon.intensity > 0.01f;

            // luz ambiente em 3 cores: do céu (azulada), do horizonte e do chão (quente) → sombras coloridas
            var amb = C(time, k => k.amb);
            var fog = C(time, k => k.fog);
            var hor = C(time, k => k.hor);
            RenderSettings.ambientSkyColor = amb;
            RenderSettings.ambientEquatorColor = Color.Lerp(amb, fog, 0.45f);
            RenderSettings.ambientGroundColor = new Color(amb.r * 0.62f, amb.g * 0.56f, amb.b * 0.46f);
            RenderSettings.ambientLight = amb;
            // neblina leve de dia (perspectiva aérea), muito densa à noite (terror)
            float sunset = Mathf.Max(Mathf.Clamp01(1 - Mathf.Abs(time - 0.74f) / 0.06f), Mathf.Clamp01(1 - Mathf.Abs(time - 0.26f) / 0.06f));
            Shader.SetGlobalColor("_FogNear", fog);
            Shader.SetGlobalColor("_FogFar", hor);
            Shader.SetGlobalColor("_FogSun", Color.Lerp(hor, C(time, k => k.sun), 0.6f) * (1 + sunset * 0.25f));
            Shader.SetGlobalVector("_FogParams", new Vector4(Mathf.Lerp(0.0042f, 0.05f, Darkness), Mathf.Lerp(0.035f, 0.02f, Darkness), 0f, Mathf.Lerp(0.85f, 0.2f, Darkness)));
            Shader.SetGlobalColor("_FlorestaRim", Color.Lerp(C(time, k => k.top), Color.white, 0.35f) * Mathf.Lerp(0.55f, 0.12f, Darkness));
            skyMat.SetColor("_Top", C(time, k => k.top));
            skyMat.SetColor("_Horizon", hor);
            skyMat.SetFloat("_Stars", Darkness);
            sky.position = cameraPos;
            if (Camera.main) Camera.main.backgroundColor = hor;
            if (Post) Post.SetMood(Darkness, sunset);

            // luz principal para água, fumaça e névoa (o sol de dia, a lua à noite)
            bool useSun = sun.intensity >= moon.intensity;
            var main = useSun ? sun : moon;
            Shader.SetGlobalVector("_FlorestaSunDir", -main.transform.forward);
            Shader.SetGlobalColor("_FlorestaSunColor", main.color * main.intensity * (useSun ? 1f : 2.2f));
            Shader.SetGlobalColor("_FlorestaSkyColor", Color.Lerp(C(time, k => k.hor), C(time, k => k.top), 0.35f));
            Shader.SetGlobalColor("_FlorestaAmbient", RenderSettings.ambientLight + sun.color * sun.intensity * 0.35f);
        }

        public string Clock()
        {
            float h = time * 24f;
            int hh = Mathf.FloorToInt(h), mm = Mathf.FloorToInt((h - hh) * 60 / 10) * 10;
            return $"{hh:00}:{mm:00}";
        }
    }
}
