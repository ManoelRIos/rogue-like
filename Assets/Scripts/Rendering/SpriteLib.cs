using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    // ----- formato do Assets/Resources/Art/atlas.json (veja Docs/SPRITES.md)
    [Serializable] public class AnimData { public string name; public int row; public int count; public float fps; public bool loop; }
    [Serializable] public class SheetData { public string name; public string file; public int fw; public int fh; public float pivotX; public float pivotY; public AnimData[] anims; }
    [Serializable] public class WeaponData { public string name; public string file; public int gripX; public int gripY; public int w; public int h; }
    [Serializable] public class TilesetData { public string name; public string file; }
    [Serializable] public class FontData { public string file; public int cellW; public int cellH; public int cols; public string chars; }
    [Serializable] public class AtlasData { public SheetData[] sheets; public WeaponData[] weapons; public TilesetData[] tilesets; public FontData font; }

    /// <summary>Uma animação: quadros + velocidade.</summary>
    public class Anim
    {
        public Sprite[] frames;
        public float fps = 8;
        public bool loop = true;
        public Sprite this[int i] => frames[Mathf.Clamp(i, 0, frames.Length - 1)];
        public float Duration => frames.Length / Mathf.Max(0.01f, fps);

        public Sprite At(float t)
        {
            int i = Mathf.FloorToInt(t * fps);
            i = loop ? ((i % frames.Length) + frames.Length) % frames.Length : Mathf.Min(i, frames.Length - 1);
            return frames[i];
        }
    }

    /// <summary>Carrega e fatia os spritesheets descritos no atlas.json.</summary>
    public static class SpriteLib
    {
        public const float PPU = 16f;
        static readonly Dictionary<string, Dictionary<string, Anim>> sheets = new();
        static readonly Dictionary<string, Sprite> weapons = new();
        static readonly Dictionary<string, Texture2D> tilesets = new();
        static readonly Dictionary<char, Sprite> glyphs = new();
        public static FontData Font;
        public static Sprite White;
        static bool ready;

        public static void Init()
        {
            if (ready) return;
            ready = true;
            var json = Resources.Load<TextAsset>("Art/atlas");
            if (json == null) { Debug.LogError("Ecos: Art/atlas.json não encontrado em Resources."); return; }
            var data = JsonUtility.FromJson<AtlasData>(json.text);

            foreach (var s in data.sheets)
            {
                var tex = LoadTex(s.file);
                if (tex == null) continue;
                var dict = new Dictionary<string, Anim>();
                foreach (var a in s.anims)
                {
                    var frames = new Sprite[a.count];
                    for (int i = 0; i < a.count; i++)
                    {
                        var rect = new Rect(i * s.fw, tex.height - (a.row + 1) * s.fh, s.fw, s.fh);
                        frames[i] = Sprite.Create(tex, rect, new Vector2(s.pivotX / s.fw, s.pivotY / s.fh), PPU, 0, SpriteMeshType.FullRect);
                        frames[i].name = $"{s.name}_{a.name}_{i}";
                    }
                    dict[a.name] = new Anim { frames = frames, fps = a.fps, loop = a.loop };
                }
                sheets[s.name] = dict;
            }

            foreach (var w in data.weapons)
            {
                var tex = LoadTex(w.file);
                if (tex == null) continue;
                // pivô na empunhadura (coordenadas da imagem com y para baixo)
                var pivot = new Vector2((w.gripX + 0.5f) / tex.width, 1f - (w.gripY + 0.5f) / tex.height);
                weapons[w.name] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, PPU, 0, SpriteMeshType.FullRect);
            }

            foreach (var t in data.tilesets)
            {
                var tex = LoadTex(t.file);
                if (tex != null) tilesets[t.name] = tex;
            }

            Font = data.font;
            var ftex = LoadTex(Font.file);
            if (ftex != null)
            {
                for (int i = 0; i < Font.chars.Length; i++)
                {
                    int cx = i % Font.cols, cy = i / Font.cols;
                    var rect = new Rect(cx * Font.cellW, ftex.height - (cy + 1) * Font.cellH, Font.cellW, Font.cellH);
                    glyphs[Font.chars[i]] = Sprite.Create(ftex, rect, new Vector2(0, 1), PPU, 0, SpriteMeshType.FullRect);
                }
            }

            var wt = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            wt.SetPixel(0, 0, Color.white);
            wt.Apply();
            White = Sprite.Create(wt, new Rect(0, 0, 1, 1), new Vector2(0, 1), PPU);
        }

        static Texture2D LoadTex(string file)
        {
            var tex = Resources.Load<Texture2D>(file);
            if (tex == null) { Debug.LogError("Ecos: textura não encontrada: " + file); return null; }
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        public static Anim Get(string sheet, string anim = null)
        {
            if (!sheets.TryGetValue(sheet, out var d)) { Debug.LogWarning("Ecos: sheet ausente " + sheet); return null; }
            if (anim == null) { foreach (var a in d.Values) return a; }
            return d.TryGetValue(anim, out var r) ? r : null;
        }

        public static Sprite Frame(string sheet, int i = 0) => Get(sheet)?[i];
        public static Sprite Weapon(string name) => weapons.TryGetValue(name, out var s) ? s : null;
        public static Texture2D Tileset(string name) => tilesets.TryGetValue(name, out var t) ? t : null;

        public static Sprite Glyph(char c)
        {
            c = char.ToUpperInvariant(c);
            if (glyphs.TryGetValue(c, out var s)) return s;
            // equivalências simples
            switch (c)
            {
                case 'Ü': return Glyph('U');
                case 'Ò': return Glyph('O');
                case 'È': return Glyph('E');
                case 'Ì': return Glyph('I');
                case 'Ù': return Glyph('U');
            }
            return glyphs.TryGetValue('?', out var q) ? q : null;
        }
    }
}
