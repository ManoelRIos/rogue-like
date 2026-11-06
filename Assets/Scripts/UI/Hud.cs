using UnityEngine;

namespace Ecos
{
    /// <summary>HUD, minimapa, avisos e telas (título, pausa, fim), tudo em pixel art.</summary>
    public class Hud
    {
        static readonly Color Ink = new(0.96f, 0.91f, 0.85f);
        static readonly Color Dim = new(0.72f, 0.65f, 0.55f);
        static readonly Color Accent = new(1f, 0.70f, 0.28f);
        static readonly Color Danger = new(1f, 0.30f, 0.37f);
        static readonly Color Panel = new(0.07f, 0.05f, 0.08f, 0.88f);
        static readonly Color Edge = new(0.23f, 0.16f, 0.21f, 1f);
        static readonly Color BlankC = new(0.5f, 0.84f, 1f);

        readonly UiDraw ui = new();
        GameManager G => GameManager.I;
        string toastTitle, toastSub, prompt;
        float toastT, flashT, flashMax;
        Color flashColor;
        string bannerTop, bannerName;
        float bannerT;
        public string BossName;
        Sprite vignette;

        Sprite Vignette()
        {
            if (vignette != null) return vignette;
            const int W = 96, H = 54;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x + 0.5f) / W * 2 - 1, dy = (y + 0.5f) / H * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                    float a = Mathf.Clamp01((d - 0.55f) / 0.6f);
                    t.SetPixel(x, y, new Color(0.02f, 0.0f, 0.02f, a * a * 0.85f));
                }
            t.Apply();
            vignette = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16);
            return vignette;
        }
        public float BossFrac;

        public void Dirty() { }  // o HUD é redesenhado todo quadro

        public void Toast(string title, string sub, float time) { toastTitle = title; toastSub = sub; toastT = time; }
        public void Flash(Color c, float a) { flashColor = c; flashT = flashMax = 0.35f; flashColor.a = a; }
        public void SetPrompt(string p) => prompt = p;
        public void Banner(string top, string name) { bannerTop = top; bannerName = name; bannerT = 2.8f; }

        // botões clicáveis das telas
        struct Button { public float x, y, w, h; public string label; }

        bool DrawButton(string label, float cx, float y, bool primary)
        {
            int w = UiDraw.Measure(label, 1) + 20, h = 15;
            float x = Mathf.Round(cx - w / 2f);
            var m = PixelCamera.I.MouseRT();
            bool hover = m.x >= x && m.x <= x + w && m.y >= y && m.y <= y + h;
            Color fill = primary ? (hover ? new Color(1f, 0.8f, 0.4f) : Accent) : (hover ? new Color(0.25f, 0.18f, 0.24f) : Panel);
            ui.Rect(x, y + 2, w, h, new Color(0, 0, 0, 0.8f));
            ui.Frame(x, y, w, h, fill, primary ? new Color(0.55f, 0.29f, 0.08f) : Dim);
            ui.Text(label, cx, y + 4, primary ? new Color(0.1f, 0.06f, 0.04f) : Ink, 1, Align.Center, !primary);
            return hover && Input.GetMouseButtonDown(0);
        }

        public void Draw(float dt)
        {
            ui.Begin();
            int W = ui.W, H = ui.H;
            var st = G.State;

            // vinheta escura nas bordas (clima de terror)
            ui.Stretch(Vignette(), 0, 0, W, H, Color.white);
            if (st == GameState.Playing || st == GameState.Paused || st == GameState.Dying) DrawGameplay(dt, W, H);

            if (st == GameState.Title) DrawTitle(W, H);
            else if (st == GameState.Paused) DrawPause(W, H);
            else if (st == GameState.Over || st == GameState.Victory) DrawEnd(W, H);

            // banner do andar
            if (bannerT > 0)
            {
                bannerT -= dt;
                float a = Mathf.Clamp01(Mathf.Min(bannerT, 2.8f - bannerT) * 3f);
                ui.Text(bannerTop, W / 2f, H * 0.28f, new Color(Dim.r, Dim.g, Dim.b, a), 1, Align.Center);
                ui.Text(bannerName, W / 2f, H * 0.28f + 12, new Color(Accent.r, Accent.g, Accent.b, a), 2, Align.Center);
            }
            // clarão de tela
            if (flashT > 0)
            {
                flashT -= dt;
                var c = flashColor; c.a *= flashT / flashMax;
                ui.Rect(0, 0, W, H, c);
            }
            // mira
            if (st == GameState.Playing)
            {
                var m = PixelCamera.I.MouseRT();
                float x = Mathf.Round(m.x), y = Mathf.Round(m.y);
                var dark = new Color(0.05f, 0.02f, 0.05f);
                ui.Rect(x - 5, y - 1, 3, 3, dark); ui.Rect(x + 3, y - 1, 3, 3, dark); ui.Rect(x - 1, y - 5, 3, 3, dark); ui.Rect(x - 1, y + 3, 3, 3, dark);
                ui.Rect(x - 4, y, 2, 1, Ink); ui.Rect(x + 3, y, 2, 1, Ink); ui.Rect(x, y - 4, 1, 2, Ink); ui.Rect(x, y + 3, 1, 2, Ink);
                ui.Rect(x, y, 1, 1, Danger);
            }
            ui.End();
        }

        void DrawGameplay(float dt, int W, int H)
        {
            var P = G.Players.Count > 0 ? G.Players[0] : null;
            if (P == null) return;

            // ---- vida, blanks e moedas
            var heart = SpriteLib.Frame("heart"); var half = SpriteLib.Frame("heart_half"); var empty = SpriteLib.Frame("heart_empty");
            for (int i = 0; i < P.maxHp / 2; i++)
            {
                int v = P.hp - i * 2;
                ui.Sprite(v >= 2 ? heart : v == 1 ? half : empty, 6 + i * 12, 6);
            }
            var bl = SpriteLib.Frame("blank");
            for (int i = 0; i < P.blanks; i++) ui.Sprite(bl, 6 + i * 12, 19);
            ui.Sprite(SpriteLib.Frame("coin"), 6, 32);
            ui.Text(P.coins.ToString(), 18, 34, new Color(1f, 0.83f, 0.35f));

            // ---- arma (canto inferior direito)
            var w = P.Weapon; var d = w.def;
            int pw = Mathf.Max(112, 52 + UiDraw.Measure(d.name.ToUpper())), ph = 40, px = W - pw - 6, py = H - ph - 6;
            ui.Frame(px, py, pw, ph, Panel, Edge);
            var ws = SpriteLib.Weapon(w.Id);
            if (ws != null)
            {
                float sc = Mathf.Max(1, Mathf.Floor(Mathf.Min(34f / ws.rect.width, 22f / ws.rect.height)));
                ui.Sprite(ws, px + 6 + (34 - ws.rect.width * sc) / 2, py + 5 + (22 - ws.rect.height * sc) / 2, sc);
            }
            ui.Text(d.name.ToUpper(), px + 44, py + 5, Accent);
            string ammo = w.Infinite ? $"{w.mag}/∞" : $"{w.mag}/{w.ammo}";
            ui.Text(ammo, px + 44, py + 16, Ink);
            // pentes como "balinhas"
            int pips = Mathf.Min(d.mag, 20);
            float pipW = Mathf.Max(1, Mathf.Floor(64f / pips) - 1);
            for (int i = 0; i < pips; i++)
            {
                bool full = i < Mathf.CeilToInt(w.mag / (float)d.mag * pips);
                ui.Rect(px + 44 + i * (pipW + 1), py + 27, pipW, 4, full ? new Color(1f, 0.8f, 0.4f) : new Color(0.25f, 0.18f, 0.2f));
            }
            for (int i = 0; i < P.weapons.Count; i++)
            {
                bool on = i == P.cur;
                ui.Rect(px + 6 + i * 6, py + ph - 6, 4, 3, on ? Accent : Edge);
            }

            // ---- recarga sobre a cabeça
            if (P.reloadT > 0 && P.alive)
            {
                var sp = PixelCamera.I.GameToRT(P.pos, 2f);
                float k = 1 - P.reloadT / d.reload;
                ui.Frame(sp.x - 12, sp.y - 4, 24, 5, new Color(0.1f, 0.06f, 0.08f), new Color(0.05f, 0.02f, 0.05f));
                ui.Rect(sp.x - 11, sp.y - 3, 22 * k, 3, Ink);
            }

            // ---- minimapa
            DrawMinimap(W - 78, 6, 72, 52);
            ui.Text($"ANDAR {G.FloorIndex + 1}", W - 6, 62, Dim, 1, Align.Right);

            // ---- chefe
            if (BossName != null)
            {
                int bw = Mathf.Min(220, W - 160), bx = (W - bw) / 2, by = H - 18;
                ui.Text(BossName.ToUpper(), W / 2f, by - 11, Danger, 1, Align.Center);
                ui.Frame(bx, by, bw, 8, new Color(0.15f, 0.04f, 0.07f), new Color(0.03f, 0.01f, 0.02f));
                ui.Rect(bx + 1, by + 1, (bw - 2) * Mathf.Clamp01(BossFrac), 6, Danger);
                ui.Rect(bx + 1, by + 1, (bw - 2) * Mathf.Clamp01(BossFrac), 2, new Color(1f, 0.6f, 0.55f));
            }

            // ---- prompt de interação
            if (!string.IsNullOrEmpty(prompt) && P.alive)
            {
                int tw = UiDraw.Measure(prompt) + 12;
                float y = H * 0.66f;
                ui.Frame(W / 2f - tw / 2f, y, tw, 15, Panel, Edge);
                ui.Text(prompt, W / 2f, y + 4, Ink, 1, Align.Center);
                ui.Text("E", W / 2f - UiDraw.Measure(prompt) / 2f, y + 4, Accent, 1, Align.Left);
            }

            // ---- aviso
            if (toastT > 0)
            {
                toastT -= dt;
                float a = Mathf.Clamp01(toastT * 3);
                ui.Text(toastTitle, W / 2f, H * 0.16f, new Color(Accent.r, Accent.g, Accent.b, a), 2, Align.Center);
                if (!string.IsNullOrEmpty(toastSub)) ui.Text(toastSub, W / 2f, H * 0.16f + 20, new Color(Ink.r, Ink.g, Ink.b, a), 1, Align.Center);
            }
        }

        void DrawMinimap(float x, float y, float w, float h)
        {
            var L = G.Level;
            if (L == null) return;
            ui.Frame(x, y, w, h, Panel, Edge);
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (var r in L.rooms) { x0 = Mathf.Min(x0, r.x0); y0 = Mathf.Min(y0, r.y0); x1 = Mathf.Max(x1, r.x0 + r.w); y1 = Mathf.Max(y1, r.y0 + r.h); }
            float s = Mathf.Min((w - 6) / (x1 - x0), (h - 6) / (y1 - y0));
            float ox = x + (w - (x1 - x0) * s) / 2 - x0 * s, oy = y + (h - (y1 - y0) * s) / 2 - y0 * s;
            var corr = new Color(0.36f, 0.29f, 0.32f);
            foreach (var c in L.corridors)
            {
                if (!L.rooms[c.a].visited && !L.rooms[c.b].visited) continue;
                ui.Rect(ox + c.x0 * s, oy + c.y0 * s, Mathf.Max(1, (c.x1 - c.x0 + 1) * s), Mathf.Max(1, (c.y1 - c.y0 + 1) * s), corr);
            }
            foreach (var r in L.rooms)
            {
                if (!r.discovered) continue;
                bool cur = G.CurrentRoom == r;
                Color c = cur ? new Color(0.79f, 0.64f, 0.42f) : r.visited ? new Color(0.43f, 0.35f, 0.38f) : new Color(0.2f, 0.15f, 0.18f);
                ui.Rect(ox + r.x0 * s, oy + r.y0 * s, Mathf.Max(2, r.w * s), Mathf.Max(2, r.h * s), c);
                Color? icon = r.type switch
                {
                    RoomType.Boss => Danger,
                    RoomType.Treasure => new Color(1f, 0.83f, 0.35f),
                    RoomType.Shop => new Color(0.5f, 1f, 0.7f),
                    _ => null,
                };
                if (icon.HasValue) ui.Rect(ox + (r.x0 + r.w / 2f) * s - 1, oy + (r.y0 + r.h / 2f) * s - 1, 3, 3, icon.Value);
            }
            foreach (var p in G.Players)
                ui.Rect(ox + p.pos.x * s - 1, oy + p.pos.y * s - 1, 2, 2, Color.white);
        }

        // ------------------------------------------------------------------ telas
        void Shade(int W, int H, float a = 0.82f) => ui.Rect(0, 0, W, H, new Color(0.04f, 0.02f, 0.05f, a));

        void DrawTitle(int W, int H)
        {
            Shade(W, H, 0.55f);
            float y = H * 0.14f;
            ui.Text("ECOS", W / 2f, y, Accent, 4, Align.Center);
            ui.Text("DO GATILHO", W / 2f, y + 36, Ink, 2, Align.Center);
            ui.Text("ROGUELIKE DE BALAS - 5 ANDARES ATÉ O ALTAR DO ÚLTIMO TIRO", W / 2f, y + 62, Dim, 1, Align.Center);
            if (DrawButton("COMEÇAR DESCIDA", W / 2f, y + 80, true) || Input.GetKeyDown(KeyCode.Return)) { Sfx.Play("select"); G.StartRun(); }

            string[] lines =
            {
                "WASD MOVER          MOUSE MIRAR / CLIQUE ATIRAR",
                "ESPAÇO CAMBALHOTA   R RECARREGAR   Q BLANK",
                "E INTERAGIR         1-9 / RODA TROCAR ARMA",
                "ESC PAUSAR          F2 CÂMERA 3D (EXPERIMENTAL)",
            };
            float ly = y + 106;
            int bw = 0; foreach (var l in lines) bw = Mathf.Max(bw, UiDraw.Measure(l));
            ui.Frame(W / 2f - bw / 2f - 8, ly - 6, bw + 16, lines.Length * 11 + 10, Panel, Edge);
            foreach (var l in lines) { ui.Text(l, W / 2f - bw / 2f, ly, Ink); ly += 11; }
            ui.Text("DICA: NO COMEÇO DA CAMBALHOTA VOCÊ É INVULNERÁVEL!", W / 2f, ly + 10, BlankC, 1, Align.Center);
        }

        void DrawPause(int W, int H)
        {
            Shade(W, H);
            ui.Text("PAUSADO", W / 2f, H * 0.3f, Accent, 3, Align.Center);
            if (DrawButton("CONTINUAR", W / 2f, H * 0.3f + 34, true)) G.SetPaused(false);
            if (DrawButton("RECOMEÇAR", W / 2f, H * 0.3f + 56, false)) G.StartRun();
        }

        void DrawEnd(int W, int H)
        {
            Shade(W, H);
            bool win = G.State == GameState.Victory;
            float y = H * 0.2f;
            ui.Text(win ? "VITÓRIA!" : "VOCÊ CAIU...", W / 2f, y, win ? Accent : Danger, 3, Align.Center);
            if (win) ui.Text("O ÚLTIMO TIRO FOI SEU.", W / 2f, y + 26, Ink, 1, Align.Center);
            var s = G.Stats;
            int t = Mathf.FloorToInt(s.time);
            string[] lines =
            {
                $"ANDAR ALCANÇADO: {(win ? 5 : G.FloorIndex + 1)} / 5",
                $"INIMIGOS DERROTADOS: {s.kills}",
                $"CAMBALHOTAS: {s.rolls}   DANO SOFRIDO: {s.hits}",
                $"MOEDAS COLETADAS: {s.coins}",
                $"TEMPO: {t / 60}M {t % 60:00}S",
            };
            float ly = y + 44;
            foreach (var l in lines) { ui.Text(l, W / 2f, ly, Dim, 1, Align.Center); ly += 11; }
            if (DrawButton("TENTAR DE NOVO", W / 2f, ly + 10, true) || Input.GetKeyDown(KeyCode.Return)) { Sfx.Play("select"); G.StartRun(); }
        }
    }
}
