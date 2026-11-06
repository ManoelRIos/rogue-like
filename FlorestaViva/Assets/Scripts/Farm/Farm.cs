using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>Uma cultura que dá para plantar.</summary>
    public class CropDef
    {
        public string name;
        public Item seed, produce;
        public float growSeconds;     // tempo molhado necessário para ficar madura
        public int yieldMin, yieldMax;
        public Mesh[] stages;         // 0 broto .. 3 madura
    }

    /// <summary>Um canteiro arado na clareira (pode ter ou não uma planta).</summary>
    public class Plot
    {
        public Vector2Int cell;
        public Vector3 pos;
        public Transform soil, plant;
        public Renderer soilR;
        public CropDef crop;
        public float growth;          // 0..1
        public float wet;             // 0..1 (seca com o tempo)
        public float hp = 30;         // a planta pode ser comida à noite
        public int Stage => crop == null ? -1 : growth >= 1 ? 3 : Mathf.Min(2, Mathf.FloorToInt(growth * 3));
        public bool Ripe => crop != null && growth >= 1;
    }

    /// <summary>
    /// A fazenda: arar com a enxada, plantar sementes, regar com água do rio e colher.
    /// As plantas só crescem enquanto a terra está molhada. À noite os Vultos vêm comer
    /// o que estiver fora da luz da fogueira.
    /// </summary>
    public class Farm
    {
        public const float CellSize = 1.6f;
        public static CropDef Cenoura, Abobora;
        public readonly Dictionary<Vector2Int, Plot> plots = new();
        readonly World world;
        readonly Campfire fire;
        readonly Transform root;
        readonly Mesh soilMesh;
        readonly Transform marker;
        readonly Renderer markerR;
        readonly MaterialPropertyBlock mpb = new();
        static readonly Color Dry = Col.Hex("#7a5434"), Wet = Col.Hex("#3e2818");

        public Farm(World w, Campfire f, Transform parent)
        {
            world = w; fire = f;
            root = new GameObject("Fazenda").transform; root.SetParent(parent, false);
            var rng = new Rng(404);
            var mb = new MeshBuilder();
            // canteiro: monte de terra com sulcos
            mb.Box(new Vector3(-0.72f, -0.1f, -0.72f), new Vector3(0.72f, 0.1f, 0.72f), Color.white);
            for (int i = -1; i <= 1; i++)
                mb.Box(new Vector3(i * 0.45f - 0.12f, 0.1f, -0.66f), new Vector3(i * 0.45f + 0.12f, 0.17f, 0.66f), new Color(0.88f, 0.88f, 0.88f));
            soilMesh = mb.ToMesh("canteiro");
            Cenoura ??= new CropDef { name = "Cenoura", seed = Item.SementeCenoura, produce = Item.Cenoura, growSeconds = 380, yieldMin = 2, yieldMax = 4, stages = CarrotMeshes(rng) };
            Abobora ??= new CropDef { name = "Abóbora", seed = Item.SementeAbobora, produce = Item.Abobora, growSeconds = 820, yieldMin = 1, yieldMax = 2, stages = PumpkinMeshes(rng) };

            // marcador do canteiro mirado
            mb = new MeshBuilder();
            float a = CellSize * 0.5f - 0.05f, t = 0.06f;
            mb.Box(new Vector3(-a, 0, -a), new Vector3(a, 0.04f, -a + t), Color.white);
            mb.Box(new Vector3(-a, 0, a - t), new Vector3(a, 0.04f, a), Color.white);
            mb.Box(new Vector3(-a, 0, -a), new Vector3(-a + t, 0.04f, a), Color.white);
            mb.Box(new Vector3(a - t, 0, -a), new Vector3(a, 0.04f, a), Color.white);
            var mg = Mats.Object("marcador", mb.ToMesh("marcador"), Mats.LowPoly, root, false);
            marker = mg.transform; markerR = mg.GetComponent<Renderer>();
            mg.SetActive(false);
        }

        // ------------------------------------------------------------------ malhas das plantas
        static Mesh[] CarrotMeshes(Rng r)
        {
            var leaf = Col.Hex("#5aa83a"); var leafL = Col.Hex("#8ed04e"); var orange = Col.Hex("#f0802a");
            var m = new Mesh[4];
            for (int s = 0; s < 4; s++)
            {
                var mb = new MeshBuilder();
                for (int k = -1; k <= 1; k++)       // três pés por canteiro (um em cada sulco)
                {
                    var c = new Vector3(k * 0.45f, 0.15f, 0);
                    int leaves = 2 + s * 2; float h = 0.1f + s * 0.12f;
                    for (int i = 0; i < leaves; i++)
                    {
                        float ang = i / (float)leaves * 6.28f + k;
                        var tip = c + new Vector3(Mathf.Cos(ang) * h * 0.5f, h + r.Range(0, 0.08f), Mathf.Sin(ang) * h * 0.5f);
                        var side = new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang)) * 0.035f;
                        mb.Tri(c - side, tip, c + side, Col.A(leaf, 0), Col.A(leafL, 0.6f), Col.A(leaf, 0));
                    }
                    if (s == 3) mb.Cylinder(c + Vector3.down * 0.05f, 0.1f, 0.07f, 0.08f, 6, orange, 0, 0, 0, r);   // ombro laranja aparecendo
                }
                m[s] = mb.ToMesh("cenoura_" + s);
            }
            return m;
        }

        static Mesh[] PumpkinMeshes(Rng r)
        {
            var leaf = Col.Hex("#4a8a34"); var leafL = Col.Hex("#7ab84a"); var green = Col.Hex("#6a9a3a"); var orange = Col.Hex("#e0761e"); var orangeL = Col.Hex("#f4a03c"); var stem = Col.Hex("#5a4a26");
            var m = new Mesh[4];
            for (int s = 0; s < 4; s++)
            {
                var mb = new MeshBuilder();
                int leaves = 2 + s * 2;
                for (int i = 0; i < leaves; i++)    // folhas largas espalhadas pelo canteiro
                {
                    float ang = i / (float)leaves * 6.28f;
                    var c = new Vector3(Mathf.Cos(ang) * (0.15f + s * 0.12f), 0.16f + s * 0.03f, Mathf.Sin(ang) * (0.15f + s * 0.12f));
                    mb.Blob(c, new Vector3(0.14f + s * 0.04f, 0.03f, 0.14f + s * 0.04f), leaf, 0.2f, r, 0, 0.3f, 0.5f, leafL);
                }
                mb.Cylinder(new Vector3(0, 0.15f, 0), 0.1f + s * 0.06f, 0.03f, 0.02f, 4, leaf, 0, 0.3f, 0, r);
                if (s >= 2)
                {
                    float big = s == 3 ? 0.42f : 0.2f;
                    var col = s == 3 ? orange : green;
                    // abóbora com gomos: várias bolhas achatadas lado a lado
                    for (int g = 0; g < 5; g++)
                    {
                        float ga = g / 5f * 6.28f;
                        mb.Blob(new Vector3(0.18f + Mathf.Cos(ga) * big * 0.35f, 0.17f + big * 0.55f, Mathf.Sin(ga) * big * 0.35f), new Vector3(big * 0.55f, big * 0.5f, big * 0.55f), col, 0.08f, r, 1, 0, 0, s == 3 ? orangeL : col);
                    }
                    mb.Cylinder(new Vector3(0.18f, 0.17f + big * 1.0f, 0), 0.1f, 0.04f, 0.03f, 5, stem, 0, 0, 0.4f, r);
                }
                m[s] = mb.ToMesh("abobora_" + s);
            }
            return m;
        }

        // ------------------------------------------------------------------ grade
        public Vector2Int CellOf(Vector3 p) => new(Mathf.RoundToInt((p.x - World.BaseCenter.x) / CellSize), Mathf.RoundToInt((p.z - World.BaseCenter.z) / CellSize));
        public Vector3 CellPos(Vector2Int c)
        {
            float x = World.BaseCenter.x + c.x * CellSize, z = World.BaseCenter.z + c.y * CellSize;
            return new Vector3(x, world.Height(x, z), z);
        }

        public Plot At(Vector2Int c) => plots.TryGetValue(c, out var p) ? p : null;

        /// <summary>Dá para arar aqui? (dentro da clareira, longe do fogo, sem objetos em cima)</summary>
        public bool CanTill(Vector2Int c, out string why)
        {
            why = null;
            if (plots.ContainsKey(c)) { why = "já está arado"; return false; }
            var p = CellPos(c);
            if (!world.InBase(p) || (p - World.BaseCenter).magnitude > World.BaseRadius - 2.5f) { why = "só dá para plantar dentro da clareira"; return false; }
            if ((new Vector3(p.x, 0, p.z) - new Vector3(fire.Pos.x, 0, fire.Pos.z)).magnitude < 3.2f) { why = "muito perto do fogo"; return false; }
            if (world.Normal(p.x, p.z).y < 0.9f) { why = "chão inclinado demais"; return false; }
            foreach (var col in Physics.OverlapSphere(p + Vector3.up * 0.5f, 0.65f, 1 << Layers.Default))
                if (!(col is CharacterController)) { why = "tem algo no caminho"; return false; }
            return true;
        }

        public bool Till(Vector2Int c)
        {
            if (!CanTill(c, out _)) return false;
            var p = CellPos(c);
            var go = Mats.Object("canteiro", soilMesh, Mats.LowPoly, root);
            go.transform.position = p;
            var plot = new Plot { cell = c, pos = p, soil = go.transform, soilR = go.GetComponent<Renderer>() };
            plots[c] = plot;
            Tint(plot);
            Audio.Play("rustle", 0.6f);
            return true;
        }

        public bool Plant(Vector2Int c, CropDef crop)
        {
            var p = At(c);
            if (p == null || p.crop != null) return false;
            p.crop = crop; p.growth = 0; p.hp = 30;
            var go = Mats.Object(crop.name, crop.stages[0], Mats.LowPoly, p.soil);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 4) * 90, 0);
            p.plant = go.transform;
            Audio.Play("pickup", 0.3f);
            return true;
        }

        public bool Water(Vector2Int c)
        {
            var p = At(c);
            if (p == null || p.wet > 0.95f) return false;
            p.wet = 1;
            Tint(p);
            Audio.Play("rustle", 0.4f);
            return true;
        }

        /// <summary>Colhe uma planta madura; devolve (produto, quantidade, sementes).</summary>
        public (Item item, int n, int seeds) Harvest(Vector2Int c)
        {
            var p = At(c);
            if (p == null || !p.Ripe) return (Item.Madeira, 0, 0);
            var crop = p.crop;
            int n = Random.Range(crop.yieldMin, crop.yieldMax + 1), seeds = Random.Range(1, 3);
            Clear(p);
            Audio.Play("eat", 0.4f);
            return (crop.produce, n, seeds);
        }

        void Clear(Plot p)
        {
            if (p.plant) Object.Destroy(p.plant.gameObject);
            p.plant = null; p.crop = null; p.growth = 0;
        }

        /// <summary>Uma criatura comendo a planta. Devolve true se acabou com ela.</summary>
        public bool Eat(Plot p, float amount)
        {
            if (p.crop == null) return true;
            p.hp -= amount;
            if (p.plant) p.plant.localScale = new Vector3(1, Mathf.Clamp01(p.hp / 30f) * 0.7f + 0.3f, 1);
            if (p.hp > 0) return false;
            Clear(p);
            Audio.Play("rustle", 0.8f);
            return true;
        }

        /// <summary>Plantas fora da luz da fogueira (alvos para os Vultos).</summary>
        public Plot UnprotectedCrop(Vector3 from)
        {
            Plot best = null; float bd = float.MaxValue;
            foreach (var p in plots.Values)
            {
                if (p.crop == null || p.growth < 0.15f) continue;
                if (fire.Lit && (p.pos - fire.Pos).magnitude < fire.SafeRadius + 0.5f) continue;
                float d = (p.pos - from).sqrMagnitude;
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }

        void Tint(Plot p)
        {
            mpb.SetColor("_Color", Color.Lerp(Dry, Wet, p.wet));
            p.soilR.SetPropertyBlock(mpb);
        }

        public void Update(float dt, float dayLength)
        {
            foreach (var p in plots.Values)
            {
                float before = p.wet;
                p.wet = Mathf.Max(0, p.wet - dt / (dayLength * 0.55f));      // seca em pouco mais de meio dia
                if ((int)(before * 20) != (int)(p.wet * 20)) Tint(p);
                if (p.crop == null || p.wet <= 0 || p.growth >= 1) continue;
                int st = p.Stage;
                p.growth = Mathf.Min(1, p.growth + dt / p.crop.growSeconds);
                p.hp = Mathf.Min(30, p.hp + dt * 0.5f);
                if (p.Stage != st && p.plant) p.plant.GetComponent<MeshFilter>().sharedMesh = p.crop.stages[p.Stage];
                // planta balança de leve ao crescer
                if (p.plant) p.plant.localScale = Vector3.Lerp(p.plant.localScale, Vector3.one, dt * 2);
            }
        }

        /// <summary>Mostra o quadrado no chão onde a ação vai acontecer.</summary>
        public void ShowMarker(Vector2Int? c, bool ok)
        {
            if (c == null) { marker.gameObject.SetActive(false); return; }
            marker.gameObject.SetActive(true);
            var p = CellPos(c.Value);
            var plot = At(c.Value);
            marker.position = p + Vector3.up * (plot != null ? 0.2f : 0.03f);
            mpb.SetColor("_Color", ok ? Col.Hex("#f4ecc8") : Col.Hex("#d8604a"));
            markerR.SetPropertyBlock(mpb);
        }

        public string Describe(Plot p)
        {
            if (p == null) return null;
            string w = p.wet > 0 ? "molhado" : "seco — precisa de água";
            if (p.crop == null) return $"canteiro vazio · {w}";
            if (p.Ripe) return $"{p.crop.name} pronta para colher!";
            return $"{p.crop.name} · {Mathf.FloorToInt(p.growth * 100)}% · {w}";
        }
    }
}
