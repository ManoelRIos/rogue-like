using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public class WeaponDef
    {
        public string id, name, bullet = "player", sfx = "shoot";
        public int tier;
        public float dmg, rate, reload, spread, speed, life = 1.2f, size = 1f, kick = 0.5f, speedVar;
        public int mag, ammo, pellets = 1, pierce, bounce;
        public float homing, explode;
        public bool fan, infinite;
        public Color color = Color.white;
    }

    /// <summary>Arma no inventário (estado mutável).</summary>
    public class WeaponInstance
    {
        public WeaponDef def;
        public int mag, ammo;
        public string Id => def.id;
        public WeaponInstance(WeaponDef d) { def = d; mag = d.mag; ammo = d.infinite ? int.MaxValue : d.ammo - d.mag; }
        public bool Infinite => def.infinite;
    }

    public static class Weapons
    {
        public static readonly Dictionary<string, WeaponDef> All = new();
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static Weapons()
        {
            Add(new WeaponDef { id = "pistola", name = "Pistola Velha", tier = 0, dmg = 5, rate = 4.5f, mag = 8, infinite = true, reload = 0.85f, spread = 0.06f, speed = 16, color = C("#fff0b0"), kick = 0.6f });
            Add(new WeaponDef { id = "escopeta", name = "Escopeta Serrada", tier = 1, dmg = 4.2f, rate = 1.5f, mag = 4, ammo = 64, reload = 1.25f, spread = 0.5f, pellets = 6, speed = 14, life = 0.5f, color = C("#ffb070"), size = 0.9f, sfx = "shotgun", kick = 2.2f, speedVar = 0.25f });
            Add(new WeaponDef { id = "metralhadora", name = "Metralhadora Ferrugem", tier = 1, dmg = 3.3f, rate = 12, mag = 32, ammo = 340, reload = 1.4f, spread = 0.15f, speed = 17, life = 1f, color = C("#ffe080"), size = 0.8f, kick = 0.3f });
            Add(new WeaponDef { id = "rifle", name = "Rifle Longo", tier = 2, dmg = 24, rate = 1.3f, mag = 5, ammo = 60, reload = 1.6f, spread = 0.01f, speed = 32, color = C("#fff6dd"), size = 1.1f, sfx = "heavy", pierce = 4, kick = 1.5f, bullet = "laser" });
            Add(new WeaponDef { id = "bolhas", name = "Soprador de Bolhas", tier = 1, dmg = 6, rate = 5, mag = 20, ammo = 240, reload = 1.1f, spread = 0.3f, speed = 7, life = 2.6f, color = C("#9ff3ff"), size = 1.4f, sfx = "bubble", bounce = 3, kick = 0.2f, speedVar = 0.3f, bullet = "bubble" });
            Add(new WeaponDef { id = "raio", name = "Lança-Raios", tier = 2, dmg = 2.2f, rate = 22, mag = 70, ammo = 700, reload = 1.5f, spread = 0.03f, speed = 34, life = 0.55f, color = C("#6ff0ff"), size = 0.9f, sfx = "laser", pierce = 99, kick = 0.05f, bullet = "laser" });
            Add(new WeaponDef { id = "foguete", name = "Canhão de Fogos", tier = 2, dmg = 16, rate = 1.1f, mag = 3, ammo = 30, reload = 1.8f, spread = 0.02f, speed = 11, life = 2f, color = C("#ff7a3a"), size = 1f, sfx = "heavy", explode = 2.1f, kick = 2f, bullet = "rocket" });
            Add(new WeaponDef { id = "besta", name = "Besta Tripla", tier = 1, dmg = 9, rate = 2.1f, mag = 6, ammo = 96, reload = 1.3f, spread = 0.32f, pellets = 3, fan = true, speed = 22, life = 1f, color = C("#d8ffb0"), size = 1f, pierce = 1, kick = 0.6f, bullet = "bolt" });
            Add(new WeaponDef { id = "revolver", name = "Revólver Dourado", tier = 2, dmg = 13, rate = 3.6f, mag = 6, ammo = 120, reload = 1.2f, spread = 0.03f, speed = 21, life = 1.4f, color = C("#ffd35a"), size = 1.1f, sfx = "heavy", bounce = 2, kick = 1f });
            Add(new WeaponDef { id = "estrelas", name = "Lança-Estrelas", tier = 2, dmg = 7, rate = 6, mag = 24, ammo = 260, reload = 1.2f, spread = 0.35f, speed = 12, life = 2.2f, color = C("#ffe94d"), size = 1.1f, sfx = "laser", homing = 5, kick = 0.2f, bullet = "star" });
        }

        static void Add(WeaponDef d) => All[d.id] = d;

        public static string Random(Rng rng, int floor, ICollection<string> exclude)
        {
            var entries = new List<(string, float)>();
            foreach (var d in All.Values)
            {
                if (d.id == "pistola" || exclude.Contains(d.id)) continue;
                float w = d.tier == 1 ? 4 - floor * 0.4f : 1 + floor * 0.7f;
                entries.Add((d.id, Mathf.Max(0.3f, w)));
            }
            return entries.Count == 0 ? null : rng.Weighted(entries);
        }
    }

    public class Theme
    {
        public string name, tileset, boss;
        public Color ambient, torch, background;
        public (string id, float w)[] enemies;
    }

    public static class Themes
    {
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public static readonly Theme[] All =
        {
            new Theme { name = "Masmorra Ferrugem", tileset = "masmorra", ambient = C("#8a7468"), torch = C("#ffa050"), background = C("#120a0c"), boss = "reiGosma",
                enemies = new[] { ("gosma", 5f), ("cogumelo", 3f), ("caveira", 2f), ("morcego", 2f) } },
            new Theme { name = "Catacumbas de Osso", tileset = "catacumbas", ambient = C("#727a94"), torch = C("#8ab4ff"), background = C("#0a0b12"), boss = "sinoFantasma",
                enemies = new[] { ("caveira", 5f), ("morcego", 3f), ("lanterna", 3f), ("gosma", 2f), ("cogumelo", 2f) } },
            new Theme { name = "Forja Rubra", tileset = "forja", ambient = C("#8a6260"), torch = C("#ff7a30"), background = C("#140806"), boss = "golem",
                enemies = new[] { ("torreta", 3f), ("besouro", 3f), ("caveira", 3f), ("lanterna", 3f), ("morcego", 2f) } },
            new Theme { name = "Cripta Esmeralda", tileset = "cripta", ambient = C("#628a74"), torch = C("#7dffb0"), background = C("#06100a"), boss = "aranha",
                enemies = new[] { ("cristal", 4f), ("besouro", 3f), ("torreta", 3f), ("cogumelo", 3f), ("lanterna", 2f) } },
            new Theme { name = "Altar do Último Tiro", tileset = "altar", ambient = C("#7a6890"), torch = C("#d08aff"), background = C("#0c0714"), boss = "sacerdote",
                enemies = new[] { ("cristal", 4f), ("besouro", 3f), ("torreta", 3f), ("caveira", 3f), ("lanterna", 3f), ("morcego", 2f) } },
        };
    }
}
