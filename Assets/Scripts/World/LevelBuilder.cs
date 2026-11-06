using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos
{
    /// <summary>
    /// Constrói a geometria 3D do andar a partir do Level: chão, paredes com altura
    /// (topo + faces), poços, decalques, adereços, caixas, portões e tochas.
    /// Tudo é separado por ZONA (cada sala e cada corredor), para que só a área
    /// em que o jogador está fique visível — o resto do mapa some na escuridão.
    /// Coordenadas: tile (x, y) ocupa mundo x∈[x,x+1], z∈[-y-1,-y].
    /// </summary>
    public class LevelBuilder
    {
        public const float WallH = 2f;      // parede alta (face frontal = 2 tiles na tela)
        public const float LowH = 0.75f;    // mureta de baixo da sala (não esconde o jogador)
        public const float PillarH = 1.5f;

        public GameObject Root;
        readonly Level L;
        readonly Texture2D atlas;
        readonly Rng rng;
        readonly Color torchColor;
        readonly Dictionary<int, GameObject> crates = new();
        readonly Dictionary<int, float> crateHp = new();
        readonly List<(SpriteView view, Anim anim, float phase)> animated = new();
        Material crateMat, worldMat;

        // zonas
        GameObject[] zoneRoots;
        readonly List<LightingSystem.PointLight>[] zoneLights;
        bool[] zoneVisible;        // zona deve estar visível (alvo)
        float[] zoneFade;          // 0..1 atual (anima)
        List<Renderer>[] zoneRenderers;
        readonly MaterialPropertyBlock mpb = new();
        public const float FadeTime = 0.35f;

        // listas do mesh em construção (uma por zona)
        class MeshData { public List<Vector3> v = new(); public List<Vector2> uv = new(); public List<int> tri = new(); }
        MeshData[] meshes;
        MeshData cur;

        public LevelBuilder(Level level, string tileset, Color torchColor)
        {
            L = level;
            this.torchColor = torchColor;
            atlas = SpriteLib.Tileset(tileset);
            rng = new Rng(level.Seed ^ 0x5bd1e995);
            Root = new GameObject("Level");
            int zc = Mathf.Max(1, L.ZoneCount);
            zoneRoots = new GameObject[zc];
            zoneLights = new List<LightingSystem.PointLight>[zc];
            zoneVisible = new bool[zc];
            zoneFade = new float[zc];
            zoneRenderers = new List<Renderer>[zc];
            meshes = new MeshData[zc];
            for (int i = 0; i < zc; i++)
            {
                zoneRoots[i] = new GameObject("Zona" + i);
                zoneRoots[i].transform.SetParent(Root.transform, false);
                zoneLights[i] = new List<LightingSystem.PointLight>();
                zoneVisible[i] = true;
                meshes[i] = new MeshData();
            }
            worldMat = Materials.World(atlas);
            BuildFloor();
            BuildWalls();
            for (int i = 0; i < zc; i++) Commit(i);
            BuildCrates();
            BuildDoors();
            BuildTorches();
            BuildProps();
            for (int i = 0; i < zc; i++)
            {
                zoneRenderers[i] = new List<Renderer>(zoneRoots[i].GetComponentsInChildren<Renderer>(true));
                zoneVisible[i] = false; zoneFade[i] = 0;
                zoneRoots[i].SetActive(false);
                foreach (var l in zoneLights[i]) l.enabled = false;
            }
        }

        // ------------------------------------------------------------------ zonas
        int ZoneOf(int x, int y)
        {
            if (!L.InBounds(x, y)) return -1;
            return L.zone[y * L.W + x];
        }

        Transform ZoneRoot(int z) => z >= 0 && z < zoneRoots.Length ? zoneRoots[z].transform : Root.transform;

        /// <summary>
        /// Define quais zonas devem aparecer. A troca não é instantânea: a zona nova
        /// surge do escuro e a antiga se apaga em FadeTime segundos (veja UpdateFades).
        /// instant = true pula a animação (ao carregar o andar).
        /// </summary>
        public void ShowOnly(HashSet<int> zones, bool instant = false)
        {
            for (int i = 0; i < zoneRoots.Length; i++)
            {
                bool v = zones.Contains(i);
                zoneVisible[i] = v;
                if (instant) { zoneFade[i] = v ? 1 : 0; ApplyFade(i); }
            }
        }

        void UpdateFades(float dt)
        {
            float step = dt / FadeTime;
            for (int i = 0; i < zoneRoots.Length; i++)
            {
                float target = zoneVisible[i] ? 1 : 0;
                if (Mathf.Approximately(zoneFade[i], target)) continue;
                zoneFade[i] = Mathf.MoveTowards(zoneFade[i], target, step);
                ApplyFade(i);
            }
        }

        void ApplyFade(int i)
        {
            float f = zoneFade[i];
            bool on = f > 0.001f;
            if (zoneRoots[i].activeSelf != on) zoneRoots[i].SetActive(on);
            // curva suave (mais tempo no escuro, entra devagar)
            float k = f * f * (3 - 2 * f);
            foreach (var r in zoneRenderers[i])
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Fade", k);
                r.SetPropertyBlock(mpb);
            }
            foreach (var l in zoneLights[i]) { l.enabled = on; l.fade = k; }
        }

        /// <summary>Quanto a zona está visível agora (0..1).</summary>
        public float ZoneFade(int z) => z >= 0 && z < zoneFade.Length ? zoneFade[z] * zoneFade[z] * (3 - 2 * zoneFade[z]) : 0;

        public bool ZoneVisible(int z) => z >= 0 && z < zoneVisible.Length && zoneVisible[z];

        // ------------------------------------------------------------------ UV do atlas
        static Rect Cell(int col, int row, int rows = 1)
        {
            const float s = 1f / 8f, e = 0.0005f;
            return Rect.MinMaxRect(col * s + e, 1f - (row + rows) * s + e, (col + 1) * s - e, 1f - row * s - e);
        }

        void Use(int x, int y) { int z = ZoneOf(x, y); cur = z >= 0 ? meshes[z] : null; }

        void Quad(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl, Rect r, bool flipU = false)
        {
            if (cur == null) return;
            int i = cur.v.Count;
            cur.v.Add(tl); cur.v.Add(tr); cur.v.Add(br); cur.v.Add(bl);
            float u0 = flipU ? r.xMax : r.xMin, u1 = flipU ? r.xMin : r.xMax;
            cur.uv.Add(new Vector2(u0, r.yMax)); cur.uv.Add(new Vector2(u1, r.yMax));
            cur.uv.Add(new Vector2(u1, r.yMin)); cur.uv.Add(new Vector2(u0, r.yMin));
            cur.tri.Add(i); cur.tri.Add(i + 1); cur.tri.Add(i + 2);
            cur.tri.Add(i); cur.tri.Add(i + 2); cur.tri.Add(i + 3);
        }

        void Commit(int zone)
        {
            var d = meshes[zone];
            if (d.v.Count == 0) return;
            var mesh = new Mesh { name = "Zona" + zone, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(d.v); mesh.SetUVs(0, d.uv); mesh.SetTriangles(d.tri, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject("geometria");
            go.transform.SetParent(zoneRoots[zone].transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = worldMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        bool Walk(int x, int y) => Level.Walkable(L[x, y]);
        bool Solid(int x, int y) { var t = L[x, y]; return t == Tile.Void || (t == Tile.Pillar && !IsProp(x, y)); }
        bool IsProp(int x, int y) => L.InBounds(x, y) && L.propTiles.Contains(y * L.W + x);

        bool IsWallBlock(int x, int y)
        {
            if (!Solid(x, y)) return false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (Walk(x + dx, y + dy) || IsProp(x + dx, y + dy)) return true;
            return false;
        }

        float WallHeight(int x, int y)
        {
            if (!IsWallBlock(x, y)) return 0;
            if (L[x, y] == Tile.Pillar) return PillarH;
            return (Walk(x, y - 1) || IsProp(x, y - 1)) ? LowH : WallH;
        }

        // ------------------------------------------------------------------ chão
        void BuildFloor()
        {
            for (int y = 0; y < L.H; y++)
                for (int x = 0; x < L.W; x++)
                {
                    var t = L[x, y];
                    if (!Level.Walkable(t) && !IsProp(x, y)) continue;
                    Use(x, y);
                    float h = t == Tile.Pit ? -0.02f : 0f;
                    Vector3 tl = new(x, h, -y), tr = new(x + 1, h, -y), br = new(x + 1, h, -y - 1), bl = new(x, h, -y - 1);
                    Rect r;
                    if (t == Tile.Pit) r = L[x, y - 1] == Tile.Pit ? Cell(0, 5) : Cell(1, 5);
                    else
                    {
                        int hsh = (x * 73856093) ^ (y * 19349663);
                        int var = rng.Chance(0.8f) ? ((hsh >> 3) & 3) : 4 + ((hsh >> 5) & 3);
                        r = Cell(var, 0);
                    }
                    Quad(tl, tr, br, bl, r);
                    if (t == Tile.Pit) continue;
                    var up = Vector3.up * 0.003f;
                    if (Solid(x, y - 1)) Quad(tl + up, tr + up, br + up, bl + up, Cell(4, 5));
                    if (Solid(x - 1, y)) Quad(tl + up, tr + up, br + up, bl + up, Cell(5, 5));
                    if (Solid(x + 1, y)) Quad(tl + up, tr + up, br + up, bl + up, Cell(5, 5), true);
                    if (L.roomId[y * L.W + x] >= 0 && t == Tile.Floor && rng.Chance(0.06f))
                    {
                        var u2 = Vector3.up * 0.002f;
                        Quad(tl + u2, tr + u2, br + u2, bl + u2, Cell(rng.Int(0, 7), 6), rng.Chance(0.5f));
                    }
                }
        }

        // ------------------------------------------------------------------ paredes
        void BuildWalls()
        {
            for (int y = 0; y < L.H; y++)
                for (int x = 0; x < L.W; x++)
                {
                    if (!IsWallBlock(x, y)) continue;
                    Use(x, y);
                    float H = WallHeight(x, y);
                    float x0 = x, x1 = x + 1, zN = -y, zS = -y - 1;
                    int mask = 0;
                    if (!IsWallBlock(x, y - 1)) mask |= 1;
                    if (!IsWallBlock(x + 1, y)) mask |= 2;
                    if (!IsWallBlock(x, y + 1)) mask |= 4;
                    if (!IsWallBlock(x - 1, y)) mask |= 8;
                    var top = L[x, y] == Tile.Pillar ? Cell(0, 7) : Cell(mask % 8, 3 + mask / 8);
                    Quad(new Vector3(x0, H, zN), new Vector3(x1, H, zN), new Vector3(x1, H, zS), new Vector3(x0, H, zS), top);

                    float hs = WallHeight(x, y + 1);
                    if (hs < H) Face(new Vector3(x0, 0, zS), new Vector3(x1, 0, zS), hs, H, FaceVariant(x, y));
                    float hn = WallHeight(x, y - 1);
                    if (hn < H) Face(new Vector3(x1, 0, zN), new Vector3(x0, 0, zN), hn, H, 0);
                    float he = WallHeight(x + 1, y);
                    if (he < H) Face(new Vector3(x1, 0, zS), new Vector3(x1, 0, zN), he, H, 1);
                    float hw = WallHeight(x - 1, y);
                    if (hw < H) Face(new Vector3(x0, 0, zN), new Vector3(x0, 0, zS), hw, H, 2);
                }
        }

        int FaceVariant(int x, int y)
        {
            bool roomWall = Walk(x, y + 1) && L.roomId[(y + 1) * L.W + x] >= 0 && L[x, y] == Tile.Void;
            int h = (x * 92837111) ^ (y * 689287499);
            float r = ((h >> 4) & 1023) / 1023f;
            if (roomWall && r < 0.05f) return 5;   // nicho com vela
            if (roomWall && r < 0.10f) return 6;   // grade
            if (roomWall && r < 0.14f) return 7;   // nicho com caveira
            if (r < 0.22f) return 4;               // rachada com sangue
            return (h >> 12) & 3;
        }

        void Face(Vector3 a, Vector3 b, float h0, float h1, int variant)
        {
            var full = Cell(variant, 1, 2);
            float vb = full.yMin, vt = full.yMax;
            float bottom = vt - (h1 - h0) / WallH * (vt - vb);
            var r = Rect.MinMaxRect(full.xMin, Mathf.Max(vb, bottom), full.xMax, vt);
            Quad(a + Vector3.up * h1, b + Vector3.up * h1, b + Vector3.up * h0, a + Vector3.up * h0, r);
        }

        // ------------------------------------------------------------------ caixas
        void BuildCrates()
        {
            var tex = SpriteLib.Frame("crate")?.texture;
            crateMat = tex ? Materials.World(tex) : Materials.Sprite;
            for (int y = 0; y < L.H; y++)
                for (int x = 0; x < L.W; x++)
                {
                    if (L[x, y] != Tile.Crate) continue;
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(go.GetComponent<Collider>());
                    go.name = "Caixa";
                    go.transform.SetParent(ZoneRoot(ZoneOf(x, y)), false);
                    go.transform.position = new Vector3(x + 0.5f, 0.45f, -y - 0.5f);
                    go.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                    go.GetComponent<MeshRenderer>().sharedMaterial = crateMat;
                    crates[y * L.W + x] = go;
                    crateHp[y * L.W + x] = 14;
                }
        }

        public bool IsCrate(int x, int y) => crates.ContainsKey(y * L.W + x);

        public bool DamageCrate(int x, int y, float dmg)
        {
            int k = y * L.W + x;
            if (!crates.TryGetValue(k, out var go)) return false;
            crateHp[k] -= dmg;
            go.transform.localScale = new Vector3(0.9f, 0.86f, 0.9f);
            if (crateHp[k] > 0) return false;
            Object.Destroy(go);
            crates.Remove(k);
            L[x, y] = Tile.Floor;
            return true;
        }

        // ------------------------------------------------------------------ portões
        void BuildDoors()
        {
            foreach (var r in L.rooms)
            {
                if (r.type != RoomType.Combat && r.type != RoomType.Boss) continue;
                foreach (var d in r.doors) d.gate = new GateView(d, ZoneRoot(r.id));
            }
        }

        // ------------------------------------------------------------------ tochas e adereços
        void AddLight(int zone, Vector3 p, float radius, Color c, float intensity)
        {
            var l = LightingSystem.Add(p, radius, c, intensity, -1, 1f);
            if (zone >= 0 && zone < zoneLights.Length) zoneLights[zone].Add(l);
        }

        void BuildTorches()
        {
            var anim = SpriteLib.Get("wall_torch") ?? SpriteLib.Get("torch");
            foreach (var t in L.torches)
            {
                int z = ZoneOf(Mathf.FloorToInt(t.x), Mathf.FloorToInt(t.y) - 1);
                var sv = new SpriteView("Tocha", Materials.SpriteBright, 0, ZoneRoot(z));
                sv.SetSprite(anim[0]);
                sv.Place(new Vector2(t.x, t.y + 0.03f), 0.75f);
                animated.Add((sv, anim, Random.value * 3));
                AddLight(z, new Vector3(t.x, 1.5f, -t.y - 0.7f), 7.5f, torchColor, 1.35f);
            }
        }

        void BuildProps()
        {
            foreach (var p in L.props)
            {
                string kind = p.kind;
                var an = SpriteLib.Get(kind);
                if (an == null) continue;
                var parent = ZoneRoot(p.zone);
                bool decal = kind.StartsWith("decal_");
                bool burning = kind == "prop_candelabra" || kind == "prop_candles";
                var center = new Vector2(p.tile.x + 0.5f, p.tile.y + 0.5f);
                if (decal)
                {
                    // decalque deitado no chão
                    var sr = new GameObject(kind).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(parent, false);
                    sr.sprite = an[0];
                    sr.sharedMaterial = Materials.Sprite;
                    sr.transform.rotation = Quaternion.Euler(90, 0, 0);
                    var pos = kind == "decal_web" ? new Vector2(p.tile.x, p.tile.y) : center + new Vector2(rng.Range(-0.3f, 0.3f), rng.Range(-0.3f, 0.3f));
                    sr.transform.position = MathX.ToWorld(pos, 0.004f);
                    sr.flipX = rng.Chance(0.5f) && kind != "decal_web";
                    continue;
                }
                var sv = new SpriteView(kind, burning ? Materials.SpriteBright : Materials.Sprite, p.solid ? 0.9f : 0, parent);
                sv.SetSprite(an[0]);
                if (p.onWall)
                {
                    // pendurado na face da parede de cima
                    sv.Place(new Vector2(center.x, p.tile.y + 1 + 0.02f), 0.3f);
                }
                else
                {
                    // encostado na parede (caixões/estátuas) ou no centro do tile
                    float y = p.solid && kind != "prop_candles" ? p.tile.y + 0.62f : center.y;
                    sv.Place(new Vector2(center.x, y), 0);
                    sv.Flip(kind != "prop_coffin" && rng.Chance(0.5f));
                }
                if (an.frames.Length > 1) animated.Add((sv, an, Random.value * 3));
                if (burning)
                    AddLight(p.zone, MathX.ToWorld(center, kind == "prop_candelabra" ? 2.0f : 0.8f), kind == "prop_candelabra" ? 6.5f : 4.5f,
                             new Color(1f, 0.72f, 0.38f), kind == "prop_candelabra" ? 1.25f : 0.9f);
            }
        }

        public void Update(float time)
        {
            UpdateFades(Time.unscaledDeltaTime);
            foreach (var (view, anim, phase) in animated) view.SetSprite(anim.At(time + phase));
            foreach (var r in L.rooms) foreach (var d in r.doors) d.gate?.Update(Time.deltaTime);
        }

        public void Destroy() { Object.Destroy(Root); }
    }

    /// <summary>Grade que sobe do chão para trancar uma porta durante o combate.</summary>
    public class GateView
    {
        readonly List<SpriteRenderer> parts = new();
        readonly Transform root;
        float y = -2.2f, target = -2.2f;

        public GateView(Door d, Transform parent)
        {
            root = new GameObject("Portao").transform;
            root.SetParent(parent, false);
            var full = SpriteLib.Frame("gate");
            bool horizontal = d.dir == 'n' || d.dir == 's';
            if (horizontal)
            {
                var a = d.tiles[0];
                var sr = MakePart(full);
                sr.transform.localPosition = new Vector3(a.x + 1.5f, 0, -(a.y + 0.5f));
            }
            else
            {
                var tex = full.texture;
                var piece = Sprite.Create(tex, new Rect(full.rect.x + 16, full.rect.y, 16, 32), new Vector2(0.5f, 0), SpriteLib.PPU);
                foreach (var t in d.tiles)
                {
                    var sr = MakePart(piece);
                    sr.transform.localPosition = new Vector3(t.x + 0.5f, 0, -(t.y + 0.5f));
                }
            }
            root.gameObject.SetActive(false);
        }

        SpriteRenderer MakePart(Sprite s)
        {
            var sr = new GameObject("grade").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(root, false);
            sr.sprite = s;
            sr.sharedMaterial = Materials.Sprite;
            parts.Add(sr);
            return sr;
        }

        public void SetLocked(bool locked)
        {
            target = locked ? 0f : -2.2f;
            root.gameObject.SetActive(true);
        }

        public void Update(float dt)
        {
            if (!root.gameObject.activeSelf) return;
            y += (target - y) * Mathf.Min(1, dt * 12f);
            root.localPosition = new Vector3(0, y, 0);
            foreach (var p in parts) p.transform.localRotation = PixelCamera.Billboard;
            if (target < 0 && y < -2.1f) root.gameObject.SetActive(false);
        }
    }
}
