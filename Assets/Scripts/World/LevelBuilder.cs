using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos
{
    /// <summary>
    /// Constrói a geometria 3D do andar a partir do Level: chão, paredes com altura
    /// (topo + faces), poços, decorações, caixas, portões e tochas.
    /// Coordenadas: tile (x, y) ocupa mundo x∈[x,x+1], z∈[-y-1,-y].
    /// </summary>
    public class LevelBuilder
    {
        public const float WallH = 2f;      // parede alta (face frontal = 2 tiles na tela)
        public const float LowH = 0.75f;    // parede de baixo da sala (não esconde o jogador)
        public const float PillarH = 1.5f;

        public GameObject Root;
        readonly Level L;
        readonly Texture2D atlas;
        readonly Rng rng;
        readonly Dictionary<int, GameObject> crates = new();
        readonly Dictionary<int, float> crateHp = new();
        readonly List<SpriteView> torchViews = new();
        Material crateMat;

        // listas do mesh em construção
        List<Vector3> v; List<Vector2> uv; List<int> tri;

        readonly Color torchColor;

        public LevelBuilder(Level level, string tileset, Color torchColor)
        {
            L = level;
            this.torchColor = torchColor;
            atlas = SpriteLib.Tileset(tileset);
            rng = new Rng(level.Seed ^ 0x5bd1e995);
            Root = new GameObject("Level");
            BuildFloor();
            BuildWalls();
            BuildCrates();
            BuildDoors();
            BuildTorches();
        }

        // ------------------------------------------------------------------ UV do atlas
        static Rect Cell(int col, int row, int rows = 1)
        {
            const float s = 1f / 8f, e = 0.0005f;
            return Rect.MinMaxRect(col * s + e, 1f - (row + rows) * s + e, (col + 1) * s - e, 1f - row * s - e);
        }

        void Begin() { v = new List<Vector3>(); uv = new List<Vector2>(); tri = new List<int>(); }

        void Quad(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl, Rect r, bool flipU = false)
        {
            int i = v.Count;
            v.Add(tl); v.Add(tr); v.Add(br); v.Add(bl);
            float u0 = flipU ? r.xMax : r.xMin, u1 = flipU ? r.xMin : r.xMax;
            uv.Add(new Vector2(u0, r.yMax)); uv.Add(new Vector2(u1, r.yMax));
            uv.Add(new Vector2(u1, r.yMin)); uv.Add(new Vector2(u0, r.yMin));
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
            tri.Add(i); tri.Add(i + 2); tri.Add(i + 3);
        }

        GameObject Commit(string name, Material mat)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(Root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        bool Walk(int x, int y) => Level.Walkable(L[x, y]);
        bool Solid(int x, int y) { var t = L[x, y]; return t == Tile.Void || t == Tile.Pillar; }

        bool IsWallBlock(int x, int y)
        {
            if (!Solid(x, y)) return false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (Walk(x + dx, y + dy)) return true;
            return false;
        }

        float WallHeight(int x, int y)
        {
            if (!IsWallBlock(x, y)) return 0;
            if (L[x, y] == Tile.Pillar) return PillarH;
            return Walk(x, y - 1) ? LowH : WallH;
        }

        // ------------------------------------------------------------------ chão
        void BuildFloor()
        {
            Begin();
            for (int y = 0; y < L.H; y++)
                for (int x = 0; x < L.W; x++)
                {
                    var t = L[x, y];
                    if (!Level.Walkable(t)) continue;
                    float h = t == Tile.Pit ? -0.02f : 0f;
                    Vector3 tl = new(x, h, -y), tr = new(x + 1, h, -y), br = new(x + 1, h, -y - 1), bl = new(x, h, -y - 1);
                    Rect r;
                    if (t == Tile.Pit) r = L[x, y - 1] == Tile.Pit ? Cell(0, 5) : Cell(1, 5);
                    else
                    {
                        int hsh = (x * 73856093) ^ (y * 19349663);
                        int var = rng.Chance(0.82f) ? ((hsh >> 3) & 3) : 4 + ((hsh >> 5) & 3);
                        r = Cell(var, 0);
                    }
                    Quad(tl, tr, br, bl, r);

                    if (t == Tile.Pit) continue;
                    // sombra pontilhada na base das paredes de cima e laterais
                    if (Solid(x, y - 1)) Quad(tl + Vector3.up * 0.003f, tr + Vector3.up * 0.003f, br + Vector3.up * 0.003f, bl + Vector3.up * 0.003f, Cell(4, 5));
                    if (Solid(x - 1, y)) Quad(tl + Vector3.up * 0.004f, tr + Vector3.up * 0.004f, br + Vector3.up * 0.004f, bl + Vector3.up * 0.004f, Cell(5, 5));
                    if (Solid(x + 1, y)) Quad(tl + Vector3.up * 0.004f, tr + Vector3.up * 0.004f, br + Vector3.up * 0.004f, bl + Vector3.up * 0.004f, Cell(5, 5), true);
                    // decorações espalhadas dentro das salas
                    if (L.roomId[y * L.W + x] >= 0 && t == Tile.Floor && rng.Chance(0.07f))
                    {
                        var up = Vector3.up * 0.002f;
                        Quad(tl + up, tr + up, br + up, bl + up, Cell(rng.Int(0, 7), 6), rng.Chance(0.5f));
                    }
                }
            // tapete na loja e círculos no início/chefe ficam por conta das decorações
            Commit("Floor", Materials.World(atlas));
        }

        // ------------------------------------------------------------------ paredes
        void BuildWalls()
        {
            Begin();
            for (int y = 0; y < L.H; y++)
                for (int x = 0; x < L.W; x++)
                {
                    if (!IsWallBlock(x, y)) continue;
                    float H = WallHeight(x, y);
                    float x0 = x, x1 = x + 1, zN = -y, zS = -y - 1;

                    // topo com borda clara onde não há outra parede
                    int mask = 0;
                    if (!IsWallBlock(x, y - 1)) mask |= 1;
                    if (!IsWallBlock(x + 1, y)) mask |= 2;
                    if (!IsWallBlock(x, y + 1)) mask |= 4;
                    if (!IsWallBlock(x - 1, y)) mask |= 8;
                    Quad(new Vector3(x0, H, zN), new Vector3(x1, H, zN), new Vector3(x1, H, zS), new Vector3(x0, H, zS), Cell(mask % 8, 3 + mask / 8));

                    // face sul (a que a câmera vê): do topo do vizinho até o topo desta parede
                    float hs = WallHeight(x, y + 1);
                    if (hs < H)
                    {
                        int var = FaceVariant(x, y);
                        Face(new Vector3(x0, 0, zS), new Vector3(x1, 0, zS), hs, H, var, false);
                    }
                    // demais faces (só aparecem no modo de câmera 3D)
                    float hn = WallHeight(x, y - 1);
                    if (hn < H) Face(new Vector3(x1, 0, zN), new Vector3(x0, 0, zN), hn, H, 0, false);
                    float he = WallHeight(x + 1, y);
                    if (he < H) Face(new Vector3(x1, 0, zS), new Vector3(x1, 0, zN), he, H, 1, false);
                    float hw = WallHeight(x - 1, y);
                    if (hw < H) Face(new Vector3(x0, 0, zN), new Vector3(x0, 0, zS), hw, H, 2, false);
                }
            Commit("Walls", Materials.World(atlas));
        }

        int FaceVariant(int x, int y)
        {
            bool roomWall = Walk(x, y + 1) && L.roomId[(y + 1) * L.W + x] >= 0 && L[x, y] == Tile.Void;
            int h = (x * 92837111) ^ (y * 689287499);
            float r = ((h >> 4) & 1023) / 1023f;
            if (roomWall && r < 0.05f) return 5;   // estandarte
            if (roomWall && r < 0.09f) return 6;   // grade
            if (roomWall && r < 0.12f) return 7;   // nicho
            if (r < 0.2f) return 4;                // rachada
            return (h >> 12) & 3;
        }

        /// <summary>Face vertical de a→b (de baixo) entre as alturas h0 e h1; textura de 2 tiles de altura.</summary>
        void Face(Vector3 a, Vector3 b, float h0, float h1, int variant, bool flip)
        {
            var full = Cell(variant, 1, 2); // 16x32
            float vb = full.yMin, vt = full.yMax;
            // o topo da textura (borda clara) fica sempre no topo da parede
            float top = vt;
            float bottom = top - (h1 - h0) / WallH * (vt - vb);
            var r = Rect.MinMaxRect(full.xMin, Mathf.Max(vb, bottom), full.xMax, top);
            Quad(a + Vector3.up * h1, b + Vector3.up * h1, b + Vector3.up * h0, a + Vector3.up * h0, r, flip);
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
                    go.transform.SetParent(Root.transform, false);
                    go.transform.position = new Vector3(x + 0.5f, 0.45f, -y - 0.5f);
                    go.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                    go.GetComponent<MeshRenderer>().sharedMaterial = crateMat;
                    crates[y * L.W + x] = go;
                    crateHp[y * L.W + x] = 14;
                }
        }

        public bool IsCrate(int x, int y) => crates.ContainsKey(y * L.W + x);

        /// <summary>Aplica dano numa caixa; retorna true se ela quebrou.</summary>
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
                foreach (var d in r.doors) d.gate = new GateView(d, Root.transform);
            }
        }

        // ------------------------------------------------------------------ tochas
        void BuildTorches()
        {
            var anim = SpriteLib.Get("torch");
            foreach (var t in L.torches)
            {
                var sv = new SpriteView("Tocha", Materials.SpriteBright, 0, Root.transform);
                sv.SetSprite(anim[0]);
                sv.Place(new Vector2(t.x, t.y + 0.03f), 0.85f);
                torchViews.Add(sv);
                LightingSystem.Add(new Vector3(t.x, 1.4f, -t.y - 0.6f), 6.5f, torchColor, 1.1f, -1, 1f);
            }
        }

        public void Update(float time)
        {
            var anim = SpriteLib.Get("torch");
            for (int i = 0; i < torchViews.Count; i++) torchViews[i].SetSprite(anim.At(time + i * 0.37f));
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
                // fica na linha da parede, centrado nos 3 tiles
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
            foreach (var p in parts) p.transform.localRotation = Quaternion.Euler(0, PixelCamera.BillboardYaw, 0);
            if (target < 0 && y < -2.1f) root.gameObject.SetActive(false);
        }
    }
}
