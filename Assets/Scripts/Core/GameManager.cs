using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public enum GameState { Title, Playing, Paused, Dying, Over, Victory }

    public class RunStats { public int kills, shots, hits, rolls, coins; public float time; }

    /// <summary>
    /// Orquestra a partida: andares, salas, combate, recompensas, colisão e estados.
    /// A ordem de atualização é determinística (jogadores → salas → inimigos → balas),
    /// o que facilita uma futura versão online com host autoritativo.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;
        public const int Floors = 5;

        public GameState State { get; private set; } = GameState.Title;
        public Level Level { get; private set; }
        public LevelBuilder Builder { get; private set; }
        public Theme Theme { get; private set; }
        public int FloorIndex { get; private set; }
        public readonly List<Player> Players = new();
        public List<Enemy> Enemies = new();
        public BulletSystem Bullets { get; private set; }
        public Effects Fx { get; private set; }
        public Hud Hud { get; private set; }
        public RunStats Stats = new();
        public Room CurrentRoom, ActiveRoom;
        public bool GodMode;
        public Rng Rng;

        readonly List<Pickup> pickups = new();
        class Corpse { public SpriteView view; public Anim anim; public float t; public int zone; public VoxelRig rig; public int style; }
        readonly List<Corpse> corpses = new();
        readonly HashSet<int> visibleZones = new();
        int curZone = -1, prevZone = -1;
        readonly List<IInteractable> interactables = new();
        readonly HashSet<int> lockedTiles = new();
        Boss boss;
        float stopT, deathT, victoryT = -1;
        bool descending;
        int runSeed;
        PixelCamera cam;

        // ------------------------------------------------------------------ inicialização
        void Awake()
        {
            I = this;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            SpriteLib.Init();
            Materials.Init();
            Sfx.Init(gameObject);
            cam = PixelCamera.Create();
            Bullets = new BulletSystem();
            Fx = new Effects();
            Hud = new Hud();
#if UNITY_EDITOR
            GodMode = false;
#endif
            NewRun(true);
        }

        public void StartRun()
        {
            Time.timeScale = 1;
            NewRun(false);
            State = GameState.Playing;
        }

        void NewRun(bool attract)
        {
            Stats = new RunStats();
            victoryT = -1; descending = false;
            foreach (var p in Players) p.Destroy();
            Players.Clear();
            // jogador 0 = local. No co-op, outros entram com outra IInputSource (rede).
            Players.Add(new Player(0, new LocalInputSource()));
            runSeed = Random.Range(1, int.MaxValue);
            LoadFloor(0, attract);
            State = attract ? GameState.Title : GameState.Playing;
        }

        void ClearLevel()
        {
            foreach (var e in Enemies) if (!e.dead) e.Remove();
            foreach (var p in pickups) if (!p.dead) p.Remove();
            foreach (var i in interactables) i.Remove();
            foreach (var c in corpses) { c.view.Destroy(); c.rig?.Destroy(); }
            corpses.Clear();
            curZone = prevZone = -1;
            Enemies.Clear(); pickups.Clear(); interactables.Clear();
            Bullets.Reset(); Fx.Reset();
            lockedTiles.Clear();
            LightingSystem.Clear();
            Builder?.Destroy();
            boss = null; ActiveRoom = null; CurrentRoom = null;
            Hud.BossName = null;
        }

        void LoadFloor(int i, bool attract = false)
        {
            ClearLevel();
            FloorIndex = i;
            Theme = Themes.All[i];
            int seed = runSeed + i * 7919;
            Rng = new Rng(seed);
            Level = DungeonGen.Generate(i, seed);
            Builder = new LevelBuilder(Level, Theme.tileset, Theme.torch);
            LightingSystem.Ambient = Theme.ambient;
            cam.Background = Theme.background;
            cam.Cam.backgroundColor = Theme.background;

            var s = Level.start;
            foreach (var p in Players)
            {
                p.SetPosition(s.Center + new Vector2(p.Index * 1.2f, 1));
                p.blanks = Mathf.Max(p.blanks, 2);
            }
            ReattachPlayerLights();
            cam.SnapTo(Players[0].pos);
            UpdateVisibility(true);

            foreach (var r in Level.rooms)
            {
                var c = r.Center;
                if (r.type == RoomType.Treasure) interactables.Add(new Chest(c, Rng.Chance(0.25f + i * 0.05f)));
                if (r.type == RoomType.Shop) MakeShop(r);
            }
            if (!attract) Hud.Banner($"ANDAR {i + 1} DE {Floors}", Theme.name.ToUpper());
        }

        void ReattachPlayerLights()
        {
            // LightingSystem.Clear() apaga todas as luzes; a do jogador é recriada
            foreach (var p in Players) p.RecreateLight();
        }

        void MakeShop(Room r)
        {
            int f = FloorIndex;
            var c = r.Center;
            var owned = new List<string>(); foreach (var w in Players[0].weapons) owned.Add(w.Id);
            string wid = Weapons.Random(Rng, f, owned) ?? "escopeta";
            var others = new List<(PickupType t, int price)> { (PickupType.Heart, 14 + f * 3), (PickupType.Ammo, 11 + f * 2), (PickupType.Blank, 10 + f * 2) };
            Rng.Shuffle(others);
            interactables.Add(new ShopItem(c + new Vector2(-3, -0.5f), others[0].t, others[0].price));
            interactables.Add(new ShopItem(c + new Vector2(0, -0.5f), PickupType.Weapon, 38 + f * 9, wid));
            interactables.Add(new ShopItem(c + new Vector2(3, -0.5f), others[1].t, others[1].price));
        }

        // ------------------------------------------------------------------ colisão
        public bool TileBlocksMove(int x, int y, bool fly)
        {
            if (!Level.InBounds(x, y)) return true;
            var t = Level[x, y];
            if (t == Tile.Void || t == Tile.Pillar || t == Tile.Crate) return true;
            if (t == Tile.Pit && !fly) return true;
            return lockedTiles.Contains(y * Level.W + x);
        }

        public bool BlocksBullet(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            if (!Level.InBounds(x, y)) return true;
            var t = Level[x, y];
            return t == Tile.Void || t == Tile.Pillar || t == Tile.Crate || lockedTiles.Contains(y * Level.W + x);
        }

        public bool CircleBlocked(Vector2 p, float r, bool fly)
        {
            for (int ty = Mathf.FloorToInt(p.y - r); ty <= Mathf.FloorToInt(p.y + r); ty++)
                for (int tx = Mathf.FloorToInt(p.x - r); tx <= Mathf.FloorToInt(p.x + r); tx++)
                {
                    if (!TileBlocksMove(tx, ty, fly)) continue;
                    float cx = Mathf.Clamp(p.x, tx, tx + 1), cy = Mathf.Clamp(p.y, ty, ty + 1);
                    if ((p.x - cx) * (p.x - cx) + (p.y - cy) * (p.y - cy) < r * r) return true;
                }
            return false;
        }

        /// <summary>Move um círculo separando os eixos (desliza nas paredes).</summary>
        public void Move(ref Vector2 pos, Vector2 delta, float r, bool fly)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) / 0.15f));
            var s = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                if (s.x != 0 && !CircleBlocked(new Vector2(pos.x + s.x, pos.y), r, fly)) pos.x += s.x;
                if (s.y != 0 && !CircleBlocked(new Vector2(pos.x, pos.y + s.y), r, fly)) pos.y += s.y;
            }
        }

        public Player NearestPlayer(Vector2 p)
        {
            Player best = null; float bd = float.MaxValue;
            foreach (var pl in Players)
            {
                if (!pl.alive) continue;
                float d = (pl.pos - p).sqrMagnitude;
                if (d < bd) { bd = d; best = pl; }
            }
            return best ?? (Players.Count > 0 ? Players[0] : null);
        }

        public Enemy NearestEnemy(Vector2 p, float maxD)
        {
            Enemy best = null; float bd = maxD * maxD;
            foreach (var e in Enemies)
            {
                if (e.dead || !e.Active) continue;
                float d = (e.pos - p).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        // ------------------------------------------------------------------ salas
        void UpdateRooms(float dt)
        {
            var P = Players[0];
            var room = Level.RoomAt(P.pos);
            CurrentRoom = room;
            if (room != null && !room.visited)
            {
                room.visited = room.discovered = true;
                foreach (var n in room.neighbors) Level.rooms[n].discovered = true;
                if (room.type == RoomType.Treasure) Toast("SALA DO TESOURO");
                if (room.type == RoomType.Shop) Toast("LOJA DO VELHO ARMEIRO", "GASTE SUAS MOEDAS");
            }
            if (room != null && !room.cleared && !room.active && room.Contains(P.pos, 0.75f)) StartRoom(room);

            var ar = ActiveRoom;
            if (ar != null && ar.type != RoomType.Boss)
            {
                bool alive = false; foreach (var e in Enemies) if (!e.dead) { alive = true; break; }
                if (!alive)
                {
                    if (ar.wavesLeft > 0)
                    {
                        ar.waveDelay -= dt;
                        if (ar.waveDelay <= 0) { ar.waveDelay = 0.7f; SpawnWave(ar); }
                    }
                    else ClearRoom(ar);
                }
            }
        }

        void StartRoom(Room room)
        {
            room.active = true;
            ActiveRoom = room;
            foreach (var d in room.doors)
            {
                d.gate?.SetLocked(true);
                foreach (var t in d.tiles) lockedTiles.Add(t.y * Level.W + t.x);
            }
            Sfx.Play("door");
            if (room.type == RoomType.Boss)
            {
                var b = new Boss(Theme.boss, new Vector2(room.x0 + room.w / 2f, room.y0 + room.h * 0.36f), FloorIndex);
                Enemies.Add(b);
                boss = b;
                Hud.BossName = b.Name;
                Toast(b.Name.ToUpper(), $"GUARDIÃO DO ANDAR {FloorIndex + 1}", 2.5f);
                Shake(0.5f);
                Sfx.Play("boss");
            }
            else
            {
                int area = room.w * room.h;
                room.wavesLeft = 1 + (area > 170 ? 1 : 0) + (FloorIndex >= 2 && Rng.Chance(0.4f) ? 1 : 0);
                room.waveDelay = 0.7f;
                SpawnWave(room);
            }
        }

        void SpawnWave(Room room)
        {
            room.wavesLeft--;
            int f = FloorIndex;
            int n = 3 + Mathf.FloorToInt(f * 0.8f) + Rng.Int(0, 2) + (room.w * room.h > 170 ? 1 : 0) + (Players.Count - 1) * 2;
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < n; i++)
            {
                string type = Rng.Weighted(Theme.enemies);
                counts.TryGetValue(type, out int c);
                if ((type == "torreta" || type == "besouro" || type == "cristal") && c >= 2) type = Theme.enemies[0].id;
                counts[type] = (counts.TryGetValue(type, out int c2) ? c2 : 0) + 1;
                var pos = FindSpawn(room, EnemyDefs.All[type].fly);
                if (pos.HasValue) Enemies.Add(new Enemy(type, pos.Value, f));
            }
        }

        Vector2? FindSpawn(Room room, bool fly)
        {
            for (int k = 0; k < 60; k++)
            {
                var p = new Vector2(room.x0 + 1 + Rng.Range(0, room.w - 2), room.y0 + 1 + Rng.Range(0, room.h - 2));
                if (CircleBlocked(p, 0.5f, fly)) continue;
                bool near = false;
                foreach (var pl in Players) if ((pl.pos - p).magnitude < 4) near = true;
                foreach (var e in Enemies) if (!e.dead && (e.pos - p).magnitude < 1.3f) near = true;
                if (near) continue;
                return p;
            }
            return null;
        }

        public void SpawnEnemyNear(string type, Vector2 p)
        {
            var room = ActiveRoom;
            for (int k = 0; k < 20; k++)
            {
                var np = p + MathX.Dir(Random.value * MathX.TAU) * (1.5f + Random.value * 2.5f);
                if (room != null && Level.RoomAt(np) != room) continue;
                if (CircleBlocked(np, 0.5f, EnemyDefs.All[type].fly)) continue;
                Enemies.Add(new Enemy(type, np, FloorIndex));
                return;
            }
        }

        void Unlock(Room room)
        {
            room.active = false;
            ActiveRoom = null;
            foreach (var d in room.doors) d.gate?.SetLocked(false);
            lockedTiles.Clear();
            Sfx.Play("door");
        }

        void ClearRoom(Room room)
        {
            room.cleared = true;
            Unlock(room);
            Enemies.RemoveAll(e => e.dead);
            var c = room.Center;
            if (Rng.Chance(0.4f))
            {
                var w = Players[0].Weapon;
                bool low = !w.Infinite && w.ammo < w.def.ammo * 0.3f;
                PickupType t = low && Rng.Chance(0.6f) ? PickupType.Ammo : Rng.Weighted(new List<(PickupType, float)>
                    { (PickupType.Ammo, 3), (PickupType.Half, 3), (PickupType.Heart, 1), (PickupType.Blank, 2), (PickupType.Coin, 3) });
                int n = t == PickupType.Coin ? 4 : 1;
                for (int i = 0; i < n; i++) pickups.Add(new Pickup(t, c));
            }
        }

        public void AddCorpse(SpriteView view, Anim death, Vector2 pos, VoxelRig rig = null, int style = 1)
        {
            corpses.Add(new Corpse { view = view, anim = death, zone = Level.ZoneAt(pos), rig = rig, style = style });
            if (corpses.Count > 40) { corpses[0].view.Destroy(); corpses[0].rig?.Destroy(); corpses.RemoveAt(0); }
        }

        /// <summary>
        /// Só a sala (ou corredor) onde o jogador está fica visível; ao atravessar uma porta,
        /// a zona anterior continua aparecendo até o jogador entrar de vez. A câmera fica presa na sala.
        /// </summary>
        void UpdateVisibility(bool force = false)
        {
            var P = Players[0];
            int z = Level.ZoneAt(P.pos);
            if (z < 0) z = curZone;
            if (z != curZone || force)
            {
                if (z != curZone && curZone >= 0) prevZone = curZone;
                curZone = z;
            }
            visibleZones.Clear();
            if (curZone >= 0) visibleZones.Add(curZone);
            var room = curZone >= 0 && curZone < Level.rooms.Count ? Level.rooms[curZone] : null;
            bool deep = room != null && room.Contains(P.pos, 1.2f);
            if (!deep && prevZone >= 0) visibleZones.Add(prevZone);
            Builder.ShowOnly(visibleZones, force);

            if (room != null)
                cam.Bounds = Rect.MinMaxRect(room.x0 - 1.5f, room.y0 - 3.5f, room.x0 + room.w + 1.5f, room.y0 + room.h + 1.8f);
            else cam.Bounds = null;
        }

        public float ZoneFadeAt(Vector2 p) => Builder.ZoneFade(Level.ZoneAt(p));

        public void OnEnemyKilled(Enemy e)
        {
            Stats.kills++;
            if (e.IsBoss) { OnBossKilled((Boss)e); return; }
            int n = Rng.Int(e.def.coinsMin, e.def.coinsMax);
            for (int i = 0; i < n; i++) pickups.Add(new Pickup(PickupType.Coin, e.pos));
            if (Rng.Chance(0.035f)) pickups.Add(new Pickup(PickupType.Half, e.pos));
            else if (Rng.Chance(0.05f)) pickups.Add(new Pickup(PickupType.Ammo, e.pos));
        }

        void OnBossKilled(Boss b)
        {
            var room = ActiveRoom;
            foreach (var e in Enemies.ToArray()) if (!e.dead && e != b) e.Die();
            Bullets.ClearEnemy();
            Hud.BossName = null;
            boss = null;
            Hitstop(0.35f);
            FlashScreen(Color.white, 0.7f);
            Sfx.Play("explode");
            if (room != null) { room.cleared = true; Unlock(room); }
            foreach (var p in Players) { p.maxHp = Mathf.Min(p.maxHp + 2, 16); p.hp = p.maxHp; }
            for (int i = 0; i < 10 + FloorIndex * 3; i++) pickups.Add(new Pickup(PickupType.Coin, b.pos));
            if (FloorIndex >= Floors - 1)
            {
                Toast("VITÓRIA!", "O ALTAR DO ÚLTIMO TIRO SILENCIOU", 4);
                victoryT = 3;
                return;
            }
            Toast($"{b.Name.ToUpper()} DERROTADO!", "+1 CORAÇÃO MÁXIMO - VIDA RESTAURADA", 3);
            var c = room != null ? room.Center : b.pos;
            interactables.Add(new Chest(c + new Vector2(-2.5f, 1.5f), true));
            interactables.Add(new Hatch(c + new Vector2(2f, 1.5f)));
        }

        // ------------------------------------------------------------------ efeitos de jogo
        public void Blank(Vector2 p)
        {
            Sfx.Play("blank");
            Bullets.ClearEnemy(true);
            FlashScreen(new Color(0.62f, 0.9f, 1f), 0.55f);
            Shake(0.4f);
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * MathX.TAU;
                Fx.Particle(p, 0.4f, MathX.Dir(a) * 9, 0, new Color(0.62f, 0.9f, 1f), 0.45f, 2.2f, 0);
            }
            Fx.Glow(p, 0.5f, 9, new Color(0.5f, 0.84f, 1f), 0.4f);
            LightingSystem.Add(MathX.ToWorld(p, 1f), 10, new Color(0.5f, 0.84f, 1f), 2.5f, 0.4f);
            foreach (var e in Enemies)
            {
                if (e.dead || !e.Active) continue;
                var d = e.pos - p;
                if (d.magnitude < 7) { e.Damage(e.IsBoss ? 18 : 8, d); if (!e.IsBoss) e.knock += d.normalized * 6; }
            }
        }

        public void Explosion(Vector2 p, float r, float dmg)
        {
            Sfx.Play("explode", 0.05f);
            Shake(0.4f);
            Fx.Explosion(p, r);
            LightingSystem.Add(MathX.ToWorld(p, 1f), r * 4, new Color(1f, 0.6f, 0.25f), 2.5f, 0.3f);
            foreach (var e in Enemies)
            {
                if (e.dead || !e.Active) continue;
                float d = (e.pos - p).magnitude;
                if (d < r + e.r) e.Damage(dmg * (1 - d / (r + e.r) * 0.5f), e.pos - p);
            }
            for (int ty = Mathf.FloorToInt(p.y - r); ty <= Mathf.FloorToInt(p.y + r); ty++)
                for (int tx = Mathf.FloorToInt(p.x - r); tx <= Mathf.FloorToInt(p.x + r); tx++)
                    if (Vector2.Distance(new Vector2(tx + 0.5f, ty + 0.5f), p) < r) HitCrate(tx, ty, 50);
        }

        public void HitCrate(int tx, int ty, float dmg)
        {
            if (!Builder.IsCrate(tx, ty)) return;
            var c = new Vector2(tx + 0.5f, ty + 0.5f);
            Fx.Burst(c, 0.5f, 2, new Color(0.6f, 0.42f, 0.21f), 2, 0.3f);
            if (Builder.DamageCrate(tx, ty, dmg))
            {
                Sfx.Play("crate", 0.05f);
                Fx.Burst(c, 0.4f, 14, new Color(0.6f, 0.42f, 0.21f), 3.5f, 0.7f, 2.2f, 3);
                if (Rng.Chance(0.18f)) pickups.Add(new Pickup(PickupType.Coin, c));
                else if (Rng.Chance(0.04f)) pickups.Add(new Pickup(PickupType.Ammo, c));
            }
        }

        public void OpenChest(Chest chest)
        {
            Sfx.Play("chest");
            Fx.Burst(chest.Pos, 0.6f, 24, chest.rare ? new Color(0.5f, 0.84f, 1f) : new Color(1f, 0.83f, 0.35f), 3.5f, 0.7f, 1.6f, 4);
            var owned = new List<string>(); foreach (var w in Players[0].weapons) owned.Add(w.Id);
            string wid = Weapons.Random(Rng, FloorIndex + (chest.rare ? 2 : 0), owned);
            var drop = chest.Pos + new Vector2(0, 0.4f);
            if (wid != null)
            {
                var p = new Pickup(PickupType.Weapon, drop, wid, true, new Vector2(0, 2.5f));
                pickups.Add(p);
                interactables.Add(new WeaponDrop(p));
            }
            else for (int i = 0; i < 3; i++) pickups.Add(new Pickup(PickupType.Ammo, drop));
            if (chest.rare) { pickups.Add(new Pickup(PickupType.Blank, drop)); pickups.Add(new Pickup(PickupType.Heart, drop)); }
            for (int i = 0; i < 3; i++) pickups.Add(new Pickup(PickupType.Coin, drop));
        }

        public void Buy(ShopItem item, Player P)
        {
            if (item.sold) return;
            if (P.coins < item.price) { Sfx.Play("deny"); Toast("MOEDAS INSUFICIENTES", $"FALTAM {item.price - P.coins}"); return; }
            if (item.type == PickupType.Heart && P.hp >= P.maxHp) { Sfx.Play("deny"); Toast("SUA VIDA JÁ ESTÁ CHEIA"); return; }
            P.coins -= item.price;
            item.sold = true;
            item.display.Remove();
            Sfx.Play("buy");
            if (item.type == PickupType.Weapon) { P.AddWeapon(item.weaponId); Toast(Weapons.All[item.weaponId].name.ToUpper(), "NOVA ARMA!"); }
            else Apply(item.type, P, true);
        }

        bool Apply(PickupType t, Player P, bool force = false)
        {
            switch (t)
            {
                case PickupType.Heart: if (P.hp >= P.maxHp && !force) return false; P.Heal(2); Sfx.Play("heal"); break;
                case PickupType.Half: if (P.hp >= P.maxHp && !force) return false; P.Heal(1); Sfx.Play("heal"); break;
                case PickupType.Blank: P.blanks = Mathf.Min(P.blanks + 1, 9); Sfx.Play("pickup"); break;
                case PickupType.Coin: P.coins++; Stats.coins++; Sfx.Play("coin", 0.02f); break;
                case PickupType.Ammo:
                    foreach (var w in P.weapons)
                        if (!w.Infinite) w.ammo = Mathf.Min(w.def.ammo, w.ammo + Mathf.CeilToInt(w.def.ammo * (w == P.Weapon ? 1f : 0.5f)));
                    Sfx.Play("pickup"); Toast("MUNIÇÃO REABASTECIDA");
                    break;
                default: return false;
            }
            return true;
        }

        public void Descend()
        {
            if (descending) return;
            descending = true;
            Sfx.Play("stairs");
            FlashScreen(Color.black, 1);
            LoadFloor(FloorIndex + 1);
            descending = false;
        }

        public void OnPlayerDeath(Player p)
        {
            bool anyAlive = false; foreach (var pl in Players) if (pl.alive) anyAlive = true;
            if (anyAlive) return;
            State = GameState.Dying;
            deathT = 1.8f;
            FlashScreen(new Color(1, 0, 0.2f), 0.6f);
            Fx.Burst(p.pos, 0.5f, 40, new Color(1f, 0.3f, 0.37f), 4, 1, 2, 4);
        }

        public void SetPaused(bool p)
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            State = p ? GameState.Paused : GameState.Playing;
            Time.timeScale = p ? 0 : 1;
        }

        public void Shake(float a) => cam.AddShake(a);
        public void Hitstop(float t) => stopT = Mathf.Max(stopT, t);
        public void FlashScreen(Color c, float a) => Hud.Flash(c, a);
        public void Toast(string t, string s = "", float time = 2.2f) => Hud.Toast(t, s, time);

        // ------------------------------------------------------------------ loop
        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            Cursor.visible = State != GameState.Playing;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                if (State == GameState.Playing || State == GameState.Paused) SetPaused(State == GameState.Playing);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F1)) { GodMode = !GodMode; Toast(GodMode ? "MODO DEUS LIGADO" : "MODO DEUS DESLIGADO"); }
            if (Input.GetKeyDown(KeyCode.F3) && State == GameState.Playing && FloorIndex < Floors - 1) LoadFloor(FloorIndex + 1);
#endif
            if (State == GameState.Playing || State == GameState.Dying)
            {
                if (stopT > 0) { stopT -= dt; dt = 0; }
                if (State == GameState.Dying)
                {
                    deathT -= dt;
                    dt *= 0.35f;
                    if (deathT <= 0) { State = GameState.Over; }
                }
                Tick(dt);
            }
            else if (State == GameState.Title)
            {
                Builder.Update(Time.time);
                Fx.Update(dt);
                foreach (var p in Players) p.UpdateVisual(dt);
                var c = Level.start.Center;
                cam.Focus = c + new Vector2(Mathf.Sin(Time.time * 0.25f) * 3, Mathf.Cos(Time.time * 0.2f) * 2);
            }

            cam.AllowLook = State == GameState.Playing;
            bool lockCursor = State == GameState.Playing && cam.FpsBlend > 0.5f;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            if (State == GameState.Playing || State == GameState.Dying)
            {
                var P = Players[0];
                cam.EyeHeight = (P.alive ? 1.45f : 0.35f) + P.Height;
                cam.Focus = P.pos;
                var lead = (P.Last.aimPoint - P.pos) * 0.22f;
                cam.Lead = Vector2.ClampMagnitude(lead, 2.6f);
            }
            else cam.Lead = Vector2.zero;

            LightingSystem.Update(dt, MathX.ToWorld(cam.Focus));
            Hud.Draw(Mathf.Min(0.05f, Time.unscaledDeltaTime));
        }

        void Tick(float dt)
        {
            Stats.time += dt;
            foreach (var p in Players) p.Tick(dt);
            UpdateRooms(dt);
            UpdateVisibility();
            bool use3D = cam.Blend > 0.5f;
            foreach (var c in corpses)
            {
                c.t += dt;
                float f = Builder.ZoneFade(c.zone);
                c.view.SetSprite(c.anim.At(c.t));
                c.view.SetFade(f);
                c.view.SpriteVisible(!(use3D && c.rig != null));
                if (c.rig != null)
                {
                    c.rig.Visible = use3D && f > 0.01f;
                    if (c.rig.Visible) { c.rig.Pose(new RigPose { death = c.t * 1.4f, deathStyle = c.style }); c.rig.SetFade(f); c.rig.SetFlash(0); }
                }
            }
            Fx.Ambient(cam.Focus, dt, new Color(1f, 0.55f, 0.25f, 0.8f));
            for (int i = 0; i < Enemies.Count; i++) if (!Enemies[i].dead) Enemies[i].Tick(dt);
            if (Enemies.Count > 40) Enemies.RemoveAll(e => e.dead);
            Bullets.Update(dt);
            Fx.Update(dt);
            Builder.Update(Time.time);

            // itens
            foreach (var pk in pickups)
            {
                if (pk.dead) continue;
                pk.Tick(dt);
                if (pk.type == PickupType.Weapon || pk.display) continue;
                foreach (var P in Players)
                    if (P.alive && pk.age > 0.35f && (pk.pos - P.pos).magnitude < 0.6f && Apply(pk.type, P)) { pk.Remove(); break; }
            }
            pickups.RemoveAll(p => p.dead);
            foreach (var it in interactables) if (it is ShopItem si && !si.display.dead) si.display.Tick(dt);
            foreach (var pk in pickups) if (!pk.dead) pk.SetFade(ZoneFadeAt(pk.pos));
            foreach (var it in interactables) it.SetFade(ZoneFadeAt(it.Pos));

            // interação
            var P0 = Players[0];
            IInteractable best = null; float bd = float.MaxValue;
            foreach (var it in interactables)
            {
                if (it.Prompt == null) continue;
                float d = (it.Pos - P0.pos).magnitude;
                if (d < it.Radius + 0.7f && d < bd) { bd = d; best = it; }
            }
            Hud.SetPrompt(best != null && P0.alive ? best.Prompt : null);
            if (best != null && P0.alive && P0.Last.interact) best.Use(P0);
            interactables.RemoveAll(it => it is WeaponDrop wd && wd.pickup.dead);

            if (boss != null && !boss.dead) Hud.BossFrac = boss.hp / boss.maxHp;
            if (victoryT > 0) { victoryT -= dt; if (victoryT <= 0) { State = GameState.Victory; Sfx.Play("victory"); } }
        }

        void OnGUI() => cam?.Present();
    }
}
