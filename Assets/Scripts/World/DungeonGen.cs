using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public enum Tile : byte { Void = 0, Floor = 1, Pillar = 2, Crate = 3, Pit = 4 }
    public enum RoomType { Start, Combat, Treasure, Shop, Boss }

    public class Door
    {
        public List<Vector2Int> tiles = new();
        public char dir;          // n, s, e, w (lado da sala)
        public GateView gate;
        public bool locked;
    }

    public class Room
    {
        public int id, gx, gy;
        public int x0, y0, w, h;      // interior em tiles (y para baixo)
        public int cx, cy;            // centro da célula (linha das portas)
        public RoomType type = RoomType.Combat;
        public List<int> neighbors = new();
        public List<Door> doors = new();
        public bool cleared, visited, discovered, active;
        public int wavesLeft;
        public float waveDelay;
        public int dist;
        public Vector2 Center => new Vector2(x0 + w / 2f, y0 + h / 2f);
        public bool Contains(Vector2 p, float margin = 0) =>
            p.x > x0 + margin && p.x < x0 + w - margin && p.y > y0 + margin && p.y < y0 + h - margin;
    }

    public class Corridor { public int x0, y0, x1, y1; public int a, b; public int zone; }

    /// <summary>Adereço decorativo (castiçal, caixão, estandarte...). Sólidos bloqueiam o tile.</summary>
    public class PropSpot { public Vector2Int tile; public string kind; public bool solid, onWall; public int zone; }

    public class Level
    {
        public int W, H, Floor, Seed;
        public Tile[] tiles;
        public short[] roomId;
        public List<Room> rooms = new();
        public List<Corridor> corridors = new();
        public Room start, boss;
        public List<Vector2> torches = new();
        public List<PropSpot> props = new();
        public HashSet<int> propTiles = new();   // tiles sólidos desenhados como adereço (sem bloco)
        public short[] zone;                      // zona de visibilidade (sala ou corredor) por tile
        public int ZoneCount;

        public int ZoneAt(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            return InBounds(x, y) ? zone[y * W + x] : -1;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
        public Tile this[int x, int y]
        {
            get => InBounds(x, y) ? tiles[y * W + x] : Tile.Void;
            set { if (InBounds(x, y)) tiles[y * W + x] = value; }
        }
        public Room RoomAt(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            if (!InBounds(x, y)) return null;
            int id = roomId[y * W + x];
            return id >= 0 ? rooms[id] : null;
        }
        public static bool Walkable(Tile t) => t == Tile.Floor || t == Tile.Crate || t == Tile.Pit;
    }

    /// <summary>Gera um andar: salas numa grade 7x7 ligadas por corredores de 3 tiles.</summary>
    public static class DungeonGen
    {
        public const int CELL = 26;
        const int G = 7;
        static readonly Vector2Int[] DIRS = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        public static Level Generate(int floor, int seed)
        {
            var rng = new Rng(seed);
            for (int attempt = 0; attempt < 100; attempt++)
            {
                var l = TryGenerate(floor, rng);
                if (l != null) { l.Seed = seed; return l; }
            }
            throw new System.Exception("Falha ao gerar o andar " + floor);
        }

        static Level TryGenerate(int floor, Rng rng)
        {
            int target = 8 + floor + rng.Int(0, 1);
            var cells = new Dictionary<Vector2Int, Room>();
            var lvl = new Level { Floor = floor };
            Room AddRoom(int gx, int gy)
            {
                var r = new Room { id = lvl.rooms.Count, gx = gx, gy = gy };
                lvl.rooms.Add(r); cells[new Vector2Int(gx, gy)] = r; return r;
            }
            void Link(Room a, Room b) { a.neighbors.Add(b.id); b.neighbors.Add(a.id); }

            var start = AddRoom(3, 3);
            int guard = 0;
            while (lvl.rooms.Count < target && guard++ < 3000)
            {
                var from = rng.Pick(lvl.rooms);
                var d = rng.Pick(DIRS);
                var n = new Vector2Int(from.gx + d.x, from.gy + d.y);
                if (n.x < 0 || n.y < 0 || n.x >= G || n.y >= G || cells.ContainsKey(n)) continue;
                int occ = 0;
                foreach (var a in DIRS) if (cells.ContainsKey(n + a)) occ++;
                if (occ > 1 && !rng.Chance(0.15f)) continue;
                Link(from, AddRoom(n.x, n.y));
            }
            // alguns laços extras
            foreach (var r in lvl.rooms)
                foreach (var d in DIRS)
                    if (cells.TryGetValue(new Vector2Int(r.gx + d.x, r.gy + d.y), out var o) && !r.neighbors.Contains(o.id)
                        && r != start && o != start && rng.Chance(0.08f)) Link(r, o);

            // distâncias (BFS) e tipos especiais
            var dist = new int[lvl.rooms.Count];
            for (int i = 0; i < dist.Length; i++) dist[i] = int.MaxValue;
            dist[start.id] = 0;
            var q = new Queue<int>(); q.Enqueue(start.id);
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                foreach (var nb in lvl.rooms[c].neighbors)
                    if (dist[nb] == int.MaxValue) { dist[nb] = dist[c] + 1; q.Enqueue(nb); }
            }
            var leaves = lvl.rooms.FindAll(r => r.neighbors.Count == 1 && r != start);
            if (leaves.Count < 3) return null;
            leaves.Sort((a, b) => dist[b.id].CompareTo(dist[a.id]));
            var boss = leaves[0];
            if (dist[boss.id] < 3) return null;
            boss.type = RoomType.Boss;
            var rest = leaves.GetRange(1, leaves.Count - 1);
            rng.Shuffle(rest);
            rest[0].type = RoomType.Treasure;
            rest[1].type = RoomType.Shop;
            start.type = RoomType.Start;
            foreach (var r in lvl.rooms) r.dist = dist[r.id];

            // ---- grade de tiles
            lvl.W = G * CELL; lvl.H = G * CELL;
            lvl.tiles = new Tile[lvl.W * lvl.H];
            lvl.roomId = new short[lvl.W * lvl.H];
            for (int i = 0; i < lvl.roomId.Length; i++) lvl.roomId[i] = -1;

            foreach (var r in lvl.rooms)
            {
                string[] tpl = r.type switch
                {
                    RoomType.Boss => RoomTemplates.Boss,
                    RoomType.Start => RoomTemplates.Start,
                    RoomType.Treasure => RoomTemplates.Treasure,
                    RoomType.Shop => RoomTemplates.Shop,
                    _ => rng.Pick(RoomTemplates.Combat),
                };
                bool mirrorX = rng.Chance(0.5f), mirrorY = rng.Chance(0.5f);
                r.h = tpl.Length; r.w = tpl[0].Length;
                r.cx = r.gx * CELL + CELL / 2; r.cy = r.gy * CELL + CELL / 2;
                r.x0 = r.cx - r.w / 2; r.y0 = r.cy - r.h / 2;
                for (int y = 0; y < r.h; y++)
                    for (int x = 0; x < r.w; x++)
                    {
                        int sx = mirrorX ? r.w - 1 - x : x, sy = mirrorY ? r.h - 1 - y : y;
                        char ch = sx < tpl[sy].Length ? tpl[sy][sx] : '.';
                        var t = ch switch { '#' => Tile.Pillar, 'c' => Tile.Crate, 'o' => Tile.Pit, '~' => Tile.Pit, _ => Tile.Floor };
                        int gx = r.x0 + x, gy = r.y0 + y;
                        lvl[gx, gy] = t;
                        lvl.roomId[gy * lvl.W + gx] = (short)r.id;
                    }
                // passagens das portas sempre livres (2 tiles para dentro)
                for (int k = -1; k <= 1; k++)
                    for (int s = 0; s < 2; s++)
                    {
                        Clear(lvl, r.cx + k, r.y0 + s); Clear(lvl, r.cx + k, r.y0 + r.h - 1 - s);
                        Clear(lvl, r.x0 + s, r.cy + k); Clear(lvl, r.x0 + r.w - 1 - s, r.cy + k);
                    }
                r.cleared = r.type != RoomType.Combat && r.type != RoomType.Boss;
            }

            // ---- corredores
            foreach (var a in lvl.rooms)
                foreach (var nid in a.neighbors)
                {
                    if (nid < a.id) continue;
                    var b = lvl.rooms[nid];
                    Room A = a, B = b;
                    if (a.gx == b.gx)
                    {
                        if (A.gy > B.gy) (A, B) = (B, A);
                        int ys = A.y0 + A.h, ye = B.y0 - 1;
                        for (int y = ys; y <= ye; y++) for (int x = A.cx - 1; x <= A.cx + 1; x++) lvl[x, y] = Tile.Floor;
                        A.doors.Add(MakeDoor('s', A.cx - 1, ys, 1, 0));
                        B.doors.Add(MakeDoor('n', B.cx - 1, ye, 1, 0));
                        lvl.corridors.Add(new Corridor { x0 = A.cx - 1, y0 = ys, x1 = A.cx + 1, y1 = ye, a = A.id, b = B.id });
                    }
                    else
                    {
                        if (A.gx > B.gx) (A, B) = (B, A);
                        int xs = A.x0 + A.w, xe = B.x0 - 1;
                        for (int x = xs; x <= xe; x++) for (int y = A.cy - 1; y <= A.cy + 1; y++) lvl[x, y] = Tile.Floor;
                        A.doors.Add(MakeDoor('e', xs, A.cy - 1, 0, 1));
                        B.doors.Add(MakeDoor('w', xe, B.cy - 1, 0, 1));
                        lvl.corridors.Add(new Corridor { x0 = xs, y0 = A.cy - 1, x1 = xe, y1 = A.cy + 1, a = A.id, b = B.id });
                    }
                }

            // ---- tochas na parede de cima das salas
            foreach (var r in lvl.rooms)
                for (int x = r.x0 + 2; x < r.x0 + r.w - 2; x += 4)
                    if (lvl[x, r.y0 - 1] == Tile.Void && Mathf.Abs(x - r.cx) > 2)
                        lvl.torches.Add(new Vector2(x + 0.5f, r.y0));

            AssignZones(lvl);
            PlaceProps(lvl, rng);
            lvl.start = start; lvl.boss = boss;
            return lvl;
        }

        /// <summary>Cada sala (com seu anel de paredes) e cada corredor viram uma zona de visibilidade.</summary>
        static void AssignZones(Level l)
        {
            l.zone = new short[l.W * l.H];
            for (int i = 0; i < l.zone.Length; i++) l.zone[i] = -1;
            foreach (var r in l.rooms)
                for (int y = r.y0 - 1; y <= r.y0 + r.h; y++)
                    for (int x = r.x0 - 1; x <= r.x0 + r.w; x++)
                        if (l.InBounds(x, y)) l.zone[y * l.W + x] = (short)r.id;
            for (int i = 0; i < l.corridors.Count; i++)
            {
                var c = l.corridors[i];
                c.zone = l.rooms.Count + i;
                for (int y = c.y0 - 1; y <= c.y1 + 1; y++)
                    for (int x = c.x0 - 1; x <= c.x1 + 1; x++)
                        if (l.InBounds(x, y) && l.zone[y * l.W + x] < 0) l.zone[y * l.W + x] = (short)c.zone;
            }
            l.ZoneCount = l.rooms.Count + l.corridors.Count;
        }

        /// <summary>Castiçais nos cantos, caixões/estátuas junto à parede de cima, estandartes e correntes na parede.</summary>
        static void PlaceProps(Level l, Rng rng)
        {
            foreach (var r in l.rooms)
            {
                bool start = r.type == RoomType.Start;
                void Solid(int x, int y, string kind)
                {
                    if (l[x, y] != Tile.Floor || Mathf.Abs(x - r.cx) <= 2 || Mathf.Abs(y - r.cy) <= 2) return;
                    l[x, y] = Tile.Pillar;
                    l.propTiles.Add(y * l.W + x);
                    l.props.Add(new PropSpot { tile = new Vector2Int(x, y), kind = kind, solid = true, zone = r.id });
                }
                // castiçais nos cantos de cima
                Solid(r.x0, r.y0, "prop_candelabra");
                Solid(r.x0 + r.w - 1, r.y0, "prop_candelabra");
                if (!start && r.type != RoomType.Shop)
                {
                    // um ou dois caixões / estátuas / gaiolas encostados na parede de cima
                    int n = rng.Int(1, 2);
                    for (int k = 0; k < n; k++)
                    {
                        int x = rng.Int(r.x0 + 2, r.x0 + r.w - 3);
                        Solid(x, r.y0, rng.Pick(new[] { "prop_coffin", "prop_statue", "prop_cage", "prop_coffin" }));
                    }
                    // castiçais embaixo também nas salas grandes
                    if (r.w * r.h > 170) { Solid(r.x0, r.y0 + r.h - 1, "prop_candles"); Solid(r.x0 + r.w - 1, r.y0 + r.h - 1, "prop_candles"); }
                }
                // decoração na face da parede de cima (não bloqueia)
                for (int x = r.x0 + 1; x < r.x0 + r.w - 1; x++)
                {
                    if (l[x, r.y0 - 1] != Tile.Void || Mathf.Abs(x - r.cx) <= 2) continue;
                    bool nearTorch = false;
                    foreach (var t in l.torches) if (Mathf.Abs(t.x - 0.5f - x) < 1.5f && Mathf.Abs(t.y - r.y0) < 0.1f) nearTorch = true;
                    if (nearTorch || !rng.Chance(0.16f)) continue;
                    l.props.Add(new PropSpot { tile = new Vector2Int(x, r.y0 - 1), kind = rng.Chance(0.6f) ? "prop_banner" : "prop_chains", onWall = true, zone = r.id });
                    x += 2;
                }
                // decalques no chão
                int dec = start ? 2 : rng.Int(3, 7);
                for (int k = 0; k < dec; k++)
                {
                    int x = rng.Int(r.x0, r.x0 + r.w - 1), y = rng.Int(r.y0, r.y0 + r.h - 1);
                    if (l[x, y] != Tile.Floor) continue;
                    string kind = rng.Chance(0.5f) ? "decal_blood" + rng.Int(0, 2) : "prop_bones";
                    l.props.Add(new PropSpot { tile = new Vector2Int(x, y), kind = kind, zone = r.id });
                }
                l.props.Add(new PropSpot { tile = new Vector2Int(r.x0, r.y0), kind = "decal_web", zone = r.id });
            }
        }

        static void Clear(Level l, int x, int y) { if (l[x, y] != Tile.Floor && l.roomId[y * l.W + x] >= 0) l[x, y] = Tile.Floor; }

        static Door MakeDoor(char dir, int x, int y, int dx, int dy)
        {
            var d = new Door { dir = dir };
            for (int i = 0; i < 3; i++) d.tiles.Add(new Vector2Int(x + dx * i, y + dy * i));
            return d;
        }
    }
}
