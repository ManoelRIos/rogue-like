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
        static readonly (float t, Color top, Color hor, Color sun, Color amb, Color fog)[] Keys =
        {
            (0.00f, Col.Hex("#04050c"), Col.Hex("#0b0d18"), Col.Hex("#000000"), Col.Hex("#10141f"), Col.Hex("#06070d")),
            (0.20f, Col.Hex("#0a0d1e"), Col.Hex("#1a1626"), Col.Hex("#000000"), Col.Hex("#141826"), Col.Hex("#0e0d16")),
            (0.27f, Col.Hex("#3a4f7a"), Col.Hex("#e08a5a"), Col.Hex("#ffb070"), Col.Hex("#6a5a5a"), Col.Hex("#c08a72")),
            (0.35f, Col.Hex("#4f86c6"), Col.Hex("#cfe0e8"), Col.Hex("#fff0d0"), Col.Hex("#8a96a0"), Col.Hex("#b8cfd8")),
            (0.50f, Col.Hex("#3f7fd0"), Col.Hex("#d8ecf2"), Col.Hex("#fff8e8"), Col.Hex("#9aa6ae"), Col.Hex("#c4dae2")),
            (0.66f, Col.Hex("#4a78b8"), Col.Hex("#f0c890"), Col.Hex("#ffd8a0"), Col.Hex("#8a8678"), Col.Hex("#d0b48e")),
            (0.73f, Col.Hex("#2a2a5a"), Col.Hex("#c0503a"), Col.Hex("#ff7040"), Col.Hex("#4a3a44"), Col.Hex("#6a3a36")),
            (0.80f, Col.Hex("#0a0c1c"), Col.Hex("#1c1424"), Col.Hex("#000000"), Col.Hex("#141826"), Col.Hex("#0d0b14")),
            (1.00f, Col.Hex("#04050c"), Col.Hex("#0b0d18"), Col.Hex("#000000"), Col.Hex("#10141f"), Col.Hex("#06070d")),
        };

        public DayNight()
        {
            var s = new GameObject("Sol");
            sun = s.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
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
            go.transform.localScale = Vector3.one * 900;
            sky = go.transform;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
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
            sun.intensity = Mathf.Clamp01(sunH * 2.5f + 0.1f) * 1.15f;
            sun.enabled = sun.intensity > 0.01f;
            moon.transform.rotation = Quaternion.Euler((time + 0.25f) * 360f, 200f, 0);
            moon.intensity = Darkness * 0.22f;
            moon.enabled = moon.intensity > 0.01f;

            RenderSettings.ambientLight = C(time, k => k.amb);
            var fog = C(time, k => k.fog);
            RenderSettings.fogColor = fog;
            // neblina leve de dia, muito densa à noite (terror)
            RenderSettings.fogDensity = Mathf.Lerp(0.0055f, 0.045f, Darkness);
            skyMat.SetColor("_Top", C(time, k => k.top));
            skyMat.SetColor("_Horizon", Color.Lerp(C(time, k => k.hor), fog, Darkness * 0.6f));
            skyMat.SetFloat("_Stars", Darkness);
            sky.position = cameraPos;
            if (Camera.main) Camera.main.backgroundColor = fog;

            Shader.SetGlobalFloat("_FlorestaTime", Time.time);
            Shader.SetGlobalFloat("_FlorestaWind", Mathf.Lerp(0.8f, 1.6f, Mathf.PerlinNoise(Time.time * 0.05f, 0)) * (1 + Darkness * 0.6f));
        }

        public string Clock()
        {
            float h = time * 24f;
            int hh = Mathf.FloorToInt(h), mm = Mathf.FloorToInt((h - hh) * 60 / 10) * 10;
            return $"{hh:00}:{mm:00}";
        }
    }
}
