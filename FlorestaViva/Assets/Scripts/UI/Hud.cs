using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>Interface em IMGUI (sem assets): barras, relógio, inventário, avisos, pausa e morte.</summary>
    public class Hud : MonoBehaviour
    {
        public Game game;
        readonly List<(string text, float t)> popups = new();
        string banner, bannerSub; float bannerT;
        Texture2D white, vignette;
        GUIStyle label, big, small, center;
        public bool Paused;

        public static string Name(Item i) => i switch
        {
            Item.Madeira => "Madeira",
            Item.Pedra => "Pedra",
            Item.Frutinha => "Frutinhas",
            Item.Cogumelo => "Cogumelos",
            Item.Fibra => "Fibra",
            Item.Flor => "Flores",
            Item.SementeCenoura => "Sem. de cenoura",
            Item.SementeAbobora => "Sem. de abóbora",
            Item.Cenoura => "Cenouras",
            _ => "Abóboras",
        };

        static Color ItemCol(Item i) => i switch
        {
            Item.Madeira => Col.Hex("#a0703e"), Item.Pedra => Col.Hex("#9a9a9a"), Item.Frutinha => Col.Hex("#d0304a"), Item.Cogumelo => Col.Hex("#d8c8a8"),
            Item.Fibra => Col.Hex("#8ab060"), Item.Flor => Col.Hex("#e0a0d0"), Item.SementeCenoura => Col.Hex("#e8b060"), Item.SementeAbobora => Col.Hex("#f0e0a0"),
            Item.Cenoura => Col.Hex("#f0802a"), _ => Col.Hex("#e0761e"),
        };

        static readonly string[] ToolNames = { "Machado", "Enxada", "Sementes", "Regador" };

        public void Popup(string s)
        {
            popups.Add((s, 2.2f));
            if (popups.Count > 5) popups.RemoveAt(0);
        }

        public void Banner(string title, string sub, float dur = 5f) { banner = title; bannerSub = sub; bannerT = dur; }

        void Awake()
        {
            white = Texture2D.whiteTexture;
            // vinheta radial gerada em código
            const int S = 128;
            vignette = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S * 2 - 1, dy = (y + 0.5f) / S * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    px[y * S + x] = new Color(0, 0, 0, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.35f, 1f, d)));
                }
            vignette.SetPixels(px); vignette.Apply();
        }

        void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            label.normal.textColor = Col.Hex("#f0e6d0");
            small = new GUIStyle(label) { fontSize = 12, fontStyle = FontStyle.Normal };
            big = new GUIStyle(label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
        }

        void Update()
        {
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i]; p.t -= Time.deltaTime;
                if (p.t <= 0) popups.RemoveAt(i); else popups[i] = p;
            }
            bannerT -= Time.deltaTime;
        }

        void Rect(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = o; }

        void Shadowed(Rect r, string s, GUIStyle st, Color? c = null)
        {
            var col = st.normal.textColor;
            st.normal.textColor = new Color(0, 0, 0, 0.7f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
            st.normal.textColor = c ?? col;
            GUI.Label(r, s, st);
            st.normal.textColor = col;
        }

        void Bar(float x, float y, float w, float v, Color c, string name)
        {
            Rect(new Rect(x - 2, y - 2, w + 4, 16), new Color(0, 0, 0, 0.55f));
            Rect(new Rect(x, y, w * Mathf.Clamp01(v), 12), c);
            Shadowed(new Rect(x + w + 8, y - 4, 120, 20), name, small);
        }

        void OnGUI()
        {
            if (game == null || game.Player == null) return;
            Styles();
            float W = Screen.width, H = Screen.height;
            var p = game.Player; var dn = game.Cycle;

            if (game.Loading)
            {
                Rect(new Rect(0, 0, W, H), Col.Hex("#0b0d0a"));
                Shadowed(new Rect(0, H / 2 - 40, W, 50), "FLORESTA VIVA", big);
                Shadowed(new Rect(0, H / 2 + 10, W, 30), "a floresta está crescendo...", center);
                return;
            }

            // vinheta: escurece as bordas à noite e fica vermelha ao levar dano / com perigo
            float vig = 0.35f + dn.Darkness * 0.45f + game.Director.Danger * 0.2f;
            var o = GUI.color;
            GUI.color = new Color(1, 1, 1, Mathf.Clamp01(vig)); GUI.DrawTexture(new Rect(0, 0, W, H), vignette);
            if (p.HurtFlash > 0) { GUI.color = new Color(0.8f, 0, 0, p.HurtFlash * 0.8f); GUI.DrawTexture(new Rect(0, 0, W, H), vignette); }
            GUI.color = o;

            // barras
            Bar(24, H - 70, 220, p.health / 100f, Col.Hex("#c8424a"), $"Vida {Mathf.CeilToInt(p.health)}");
            Bar(24, H - 46, 220, p.hunger / 100f, Col.Hex("#d89a3a"), $"Fome {Mathf.CeilToInt(p.hunger)}");

            // relógio + dia + fogueira
            Rect(new Rect(W - 210, 18, 192, 78), new Color(0, 0, 0, 0.45f));
            Shadowed(new Rect(W - 198, 22, 180, 24), $"Dia {dn.Day}   {dn.Clock()}", label, dn.IsNight ? Col.Hex("#b0a0ff") : Col.Hex("#ffe6a8"));
            Shadowed(new Rect(W - 198, 46, 180, 20), dn.IsNight ? "NOITE — fique na luz" : "dia — explore e colete", small);
            Rect(new Rect(W - 198, 72, 168, 10), new Color(0, 0, 0, 0.5f));
            Rect(new Rect(W - 198, 72, 168 * game.Fire.Fuel / Campfire.MaxFuel, 10), game.Fire.Fuel < 20 ? Col.Hex("#ff4020") : Col.Hex("#ff9a30"));
            Shadowed(new Rect(W - 198, 80, 180, 18), "fogueira", small);

            // ferramentas (1-4)
            float iw = 92, ix = W / 2 - 4 * iw / 2;
            var smallC = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            for (int i = 0; i < 4; i++)
            {
                bool sel = (int)p.tool == i;
                var r = new Rect(ix + i * iw + 3, H - 66, iw - 6, 54);
                Rect(r, sel ? new Color(0.94f, 0.9f, 0.8f, 0.25f) : new Color(0, 0, 0, 0.5f));
                if (sel) { Rect(new Rect(r.x, r.y, r.width, 3), Col.Hex("#f0a04b")); }
                Shadowed(new Rect(r.x + 6, r.y + 4, 20, 18), (i + 1).ToString(), small, new Color(1, 1, 1, 0.6f));
                Shadowed(new Rect(r.x, r.y + 8, r.width, 22), ToolNames[i], center);
                string sub = i switch
                {
                    2 => $"{p.SeedCrop.name.ToLower()} ×{p.inventory[p.SeedCrop.seed]}",
                    3 => $"água {p.water}/{PlayerController.WaterMax}",
                    _ => i == 0 ? "cortar · lutar" : "arar",
                };
                Shadowed(new Rect(r.x, r.y + 30, r.width, 20), sub, smallC);
            }
            if (p.tool == Tool.Sementes) Shadowed(new Rect(0, H - 88, W, 20), "Q troca a semente", smallC, new Color(1, 1, 1, 0.7f));

            // mochila: só o que você tem
            float iy = H - 120;
            var items = (Item[])System.Enum.GetValues(typeof(Item));
            for (int i = items.Length - 1; i >= 0; i--)
            {
                int n = p.inventory[items[i]];
                if (n <= 0) continue;
                Rect(new Rect(W - 190, iy, 172, 22), new Color(0, 0, 0, 0.42f));
                Rect(new Rect(W - 190, iy, 4, 22), ItemCol(items[i]));
                Shadowed(new Rect(W - 180, iy + 1, 130, 20), Name(items[i]), small);
                Shadowed(new Rect(W - 60, iy + 1, 40, 20), n.ToString(), new GUIStyle(small) { alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold });
                iy -= 24;
            }

            // mira (1ª pessoa) e dica de interação
            if (game.Rig.Blend > 0.5f) Rect(new Rect(W / 2 - 2, H / 2 - 2, 4, 4), new Color(1, 1, 1, 0.8f));
            string prompt = p.TargetLabel;
            if (game.NearFire) prompt = game.Fire.Fuel < Campfire.MaxFuel - 1 ? "E: ALIMENTAR A FOGUEIRA (1 madeira)" : "A fogueira está forte";
            if (prompt != null) Shadowed(new Rect(0, H / 2 + 60, W, 24), prompt, center);
            if (p.TargetInfo != null) Shadowed(new Rect(0, H / 2 + 84, W, 22), p.TargetInfo, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter }, Col.Hex("#d8e8b0"));

            // mensagens de coleta
            for (int i = 0; i < popups.Count; i++)
            {
                var (s, t) = popups[i];
                Shadowed(new Rect(24, H - 110 - (popups.Count - 1 - i) * 22, 300, 22), s, label, new Color(1, 0.95f, 0.8f, Mathf.Clamp01(t)));
            }

            // faixa grande (anoitecer / amanhecer)
            if (bannerT > 0 && banner != null)
            {
                float a = Mathf.Clamp01(bannerT) * Mathf.Clamp01((5 - bannerT) * 2 + 0.2f);
                Rect(new Rect(0, H * 0.22f, W, 96), new Color(0, 0, 0, 0.45f * a));
                Shadowed(new Rect(0, H * 0.22f + 8, W, 50), banner, big, new Color(1, 0.9f, 0.85f, a));
                Shadowed(new Rect(0, H * 0.22f + 60, W, 26), bannerSub, center, new Color(1, 1, 1, a * 0.85f));
            }

            if (!p.alive)
            {
                Rect(new Rect(0, 0, W, H), new Color(0.05f, 0, 0, 0.6f));
                Shadowed(new Rect(0, H / 2 - 40, W, 50), "A FLORESTA TE LEVOU", big, Col.Hex("#e04040"));
                Shadowed(new Rect(0, H / 2 + 14, W, 26), $"voltando para a fogueira em {Mathf.CeilToInt(game.RespawnT)}...", center);
            }

            if (Paused)
            {
                Rect(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.6f));
                Shadowed(new Rect(0, H * 0.18f, W, 50), "PAUSA", big);
                string help =
                    "WASD  andar      SHIFT  correr      ESPAÇO  pular\n" +
                    "MOUSE  olhar      RODA  zoom      V  1ª / 3ª pessoa\n" +
                    "1 machado   2 enxada   3 sementes (Q troca)   4 regador\n" +
                    "CLIQUE  usar a ferramenta: cortar, arar, plantar, regar, colher, atacar\n" +
                    "F  comer      E  alimentar a fogueira      o regador enche no rio\n" +
                    "ESC  voltar ao jogo\n\n" +
                    "De dia a floresta é calma. À noite os Vultos saem da mata.\n" +
                    "Mantenha a fogueira acesa: a luz dela protege a base e a horta.\n" +
                    "As plantas só crescem com a terra molhada.";
                Shadowed(new Rect(0, H * 0.18f + 70, W, 260), help, center);
            }
        }
    }
}
