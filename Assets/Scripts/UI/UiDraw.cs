using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public enum Align { Left, Center, Right }

    /// <summary>
    /// Desenho de interface em "modo imediato" na resolução baixa (pixel perfect):
    /// a cada quadro chama-se Begin(), desenha retângulos/sprites/textos e End().
    /// Coordenadas em pixels da textura, origem no canto superior esquerdo.
    /// </summary>
    public class UiDraw
    {
        readonly List<SpriteRenderer> pool = new();
        readonly Transform root;
        int used, order;
        public int W => PixelCamera.I.RtW;
        public int H => PixelCamera.I.RtH;

        public UiDraw()
        {
            root = new GameObject("UI").transform;
            root.gameObject.layer = Layers.UI;
        }

        public void Begin() { used = 0; order = 0; }

        public void End()
        {
            for (int i = used; i < pool.Count; i++) if (pool[i].enabled) pool[i].enabled = false;
        }

        SpriteRenderer Next()
        {
            SpriteRenderer sr;
            if (used < pool.Count) sr = pool[used];
            else
            {
                sr = new GameObject("ui").AddComponent<SpriteRenderer>();
                sr.gameObject.layer = Layers.UI;
                sr.transform.SetParent(root, false);
                sr.sharedMaterial = Materials.UI;
                pool.Add(sr);
            }
            used++;
            sr.enabled = true;
            sr.sortingOrder = order++;
            sr.flipX = false;
            return sr;
        }

        public void Rect(float x, float y, float w, float h, Color c)
        {
            if (w <= 0 || h <= 0) return;
            var sr = Next();
            sr.sprite = SpriteLib.White;
            sr.color = c;
            sr.transform.position = PixelCamera.I.HudPos(Mathf.Round(x), Mathf.Round(y));
            sr.transform.localScale = new Vector3(Mathf.Round(w), Mathf.Round(h), 1);
        }

        public void Frame(float x, float y, float w, float h, Color fill, Color border)
        {
            Rect(x, y, w, h, border);
            Rect(x + 1, y + 1, w - 2, h - 2, fill);
        }

        /// <summary>Desenha um sprite com o canto superior esquerdo em (x, y).</summary>
        public void Sprite(Sprite s, float x, float y, float scale = 1, Color? color = null, bool flipX = false)
        {
            if (s == null) return;
            var sr = Next();
            sr.sprite = s;
            sr.color = color ?? Color.white;
            sr.flipX = flipX;
            var b = s.bounds;
            float offX = flipX ? -b.max.x : b.min.x;
            var p = PixelCamera.I.HudPos(Mathf.Round(x), Mathf.Round(y));
            sr.transform.position = p - new Vector3(offX, b.max.y, 0) * scale;
            sr.transform.localScale = new Vector3(scale, scale, 1);
        }

        /// <summary>Estica um sprite para cobrir um retângulo (ex.: vinheta).</summary>
        public void Stretch(Sprite s, float x, float y, float w, float h, Color c)
        {
            if (s == null) return;
            var sr = Next();
            sr.sprite = s;
            sr.color = c;
            var b = s.bounds;
            float sx = w / (b.size.x * SpriteLib.PPU), sy = h / (b.size.y * SpriteLib.PPU);
            var p = PixelCamera.I.HudPos(x, y);
            sr.transform.position = p - new Vector3(b.min.x * sx, b.max.y * sy, 0);
            sr.transform.localScale = new Vector3(sx, sy, 1);
        }

        public static int Measure(string text, int scale = 1)
        {
            int cw = SpriteLib.Font != null ? SpriteLib.Font.cellW : 6;
            return string.IsNullOrEmpty(text) ? 0 : (text.Length * cw - 1) * scale;
        }

        public int Text(string text, float x, float y, Color c, int scale = 1, Align align = Align.Left, bool shadow = true)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int w = Measure(text, scale);
            if (align == Align.Center) x -= w / 2f;
            else if (align == Align.Right) x -= w;
            x = Mathf.Round(x); y = Mathf.Round(y);
            int cw = SpriteLib.Font.cellW * scale;
            if (shadow) Glyphs(text, x + scale, y + scale, new Color(0.05f, 0.02f, 0.06f, c.a), scale, cw);
            Glyphs(text, x, y, c, scale, cw);
            return w;
        }

        void Glyphs(string text, float x, float y, Color c, int scale, int cw)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == ' ') continue;
                var g = SpriteLib.Glyph(ch);
                if (g == null) continue;
                var sr = Next();
                sr.sprite = g;
                sr.color = c;
                // a célula tem 2 px de acento acima da letra: sobe para alinhar
                sr.transform.position = PixelCamera.I.HudPos(x + i * cw, y - 2 * scale);
                sr.transform.localScale = new Vector3(scale, scale, 1);
            }
        }
    }
}
