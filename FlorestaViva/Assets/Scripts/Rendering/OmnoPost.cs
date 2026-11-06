using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Pós-processamento na câmera (Built-in): bloom em cascata + correção de cor + vinheta.
    /// Os valores mudam com a hora do dia (DayNight chama SetMood).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OmnoPost : MonoBehaviour
    {
        public float threshold = 0.82f, knee = 0.5f, intensity = 0.65f;
        public float saturation = 1.12f, contrast = 1.06f, exposure = 1.0f, vignette = 0.28f;
        public Color tint = Color.white, lift = new(0.015f, 0.02f, 0.05f, 0);
        Material mat;
        readonly RenderTexture[] chain = new RenderTexture[6];

        void OnEnable()
        {
            var sh = Shader.Find("Hidden/Floresta/Post");
            if (sh == null) sh = Resources.Load<Shader>("Shaders/Post");
            if (sh != null && sh.isSupported) mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        }

        public void SetMood(float darkness, float sunset)
        {
            // dia: quente e saturado; pôr do sol: mais bloom e tom dourado; noite: azul, menos saturado, bloom forte (fogo e olhos brilham)
            tint = Color.Lerp(Color.Lerp(new Color(1.02f, 1f, 0.97f), new Color(1.08f, 0.98f, 0.9f), sunset), new Color(0.86f, 0.92f, 1.08f), darkness);
            saturation = Mathf.Lerp(1.14f, 0.9f, darkness);
            intensity = Mathf.Lerp(0.6f, 0.95f, Mathf.Max(darkness, sunset * 0.6f));
            threshold = Mathf.Lerp(0.85f, 0.55f, darkness);
            lift = Color.Lerp(new Color(0.012f, 0.018f, 0.045f), new Color(0.01f, 0.012f, 0.04f), darkness);
            vignette = Mathf.Lerp(0.26f, 0.42f, darkness);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null) { Graphics.Blit(src, dst); return; }
            mat.SetVector("_Threshold", new Vector4(threshold, knee, intensity, 0));
            mat.SetVector("_Grade", new Vector4(saturation, contrast, exposure, vignette));
            mat.SetColor("_Tint", tint);
            mat.SetColor("_Lift", lift);

            int w = src.width / 2, h = src.height / 2;
            var fmt = src.format;
            int n = 0;
            var cur = chain[n++] = RenderTexture.GetTemporary(w, h, 0, fmt);
            Graphics.Blit(src, cur, mat, 0);
            for (; n < chain.Length; n++)
            {
                w /= 2; h /= 2;
                if (w < 4 || h < 4) break;
                chain[n] = RenderTexture.GetTemporary(w, h, 0, fmt);
                Graphics.Blit(cur, chain[n], mat, 1);
                cur = chain[n];
            }
            for (int i = n - 2; i >= 0; i--)
            {
                Graphics.Blit(cur, chain[i], mat, 2);
                cur = chain[i];
            }
            mat.SetTexture("_Bloom", cur);
            Graphics.Blit(src, dst, mat, 3);
            for (int i = 0; i < n; i++) { if (chain[i]) RenderTexture.ReleaseTemporary(chain[i]); chain[i] = null; }
        }
    }
}
