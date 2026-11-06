using UnityEngine;

namespace Ecos
{
    public interface IInteractable
    {
        Vector2 Pos { get; }
        float Radius { get; }
        string Prompt { get; }   // null = não interativo agora
        void Use(Player p);
        void Remove();
        void SetFade(float f);
    }

    public enum PickupType { Heart, Half, Ammo, Coin, Blank, Weapon }

    /// <summary>Item no chão (salta ao surgir; moedas são atraídas pelo jogador).</summary>
    public class Pickup
    {
        public PickupType type;
        public string weaponId;
        public Vector2 pos, vel;
        public float h, vh, t, age;
        public bool dead, display;
        readonly SpriteView view;
        readonly Anim anim;
        GameManager G => GameManager.I;

        public Pickup(PickupType type, Vector2 p, string weaponId = null, bool pop = true, Vector2? v = null)
        {
            this.type = type; this.weaponId = weaponId; pos = p;
            if (pop) { h = 0.5f; vh = 3.2f; vel = v ?? Random.insideUnitCircle * 2f; }
            t = Random.value * 6;
            view = new SpriteView("item", Materials.Sprite, type == PickupType.Weapon ? 0.9f : 0.5f);
            if (type == PickupType.Weapon) view.SetSprite(SpriteLib.Weapon(weaponId));
            else
            {
                string sheet = type switch
                {
                    PickupType.Heart => "heart", PickupType.Half => "heart_half", PickupType.Ammo => "ammo",
                    PickupType.Coin => "coin", PickupType.Blank => "blank", _ => "coin",
                };
                anim = SpriteLib.Get(sheet);
                view.SetSprite(anim[0]);
            }
            view.Place(pos, h);
        }

        public void Tick(float dt)
        {
            t += dt; age += dt;
            if (h > 0 || vh > 0)
            {
                vh -= 14 * dt; h += vh * dt;
                if (h <= 0) { h = 0; vh = Mathf.Abs(vh) > 1.5f ? -vh * 0.4f : 0; }
                G.Move(ref pos, vel * dt, 0.15f, false);
                vel *= Mathf.Pow(0.05f, dt);
            }
            if (type == PickupType.Coin && age > 0.4f)
            {
                var P = G.NearestPlayer(pos);
                if (P != null)
                {
                    var d = P.pos - pos; float dist = d.magnitude;
                    if (dist < 2.8f && dist > 0.01f) pos += d / dist * (3 - dist) * 4 * dt;
                }
            }
            if (anim != null && anim.frames.Length > 1) view.SetSprite(anim.At(t));
            float bob = display ? 0.55f + Mathf.Sin(t * 3) * 0.05f
                : type == PickupType.Weapon ? 0.25f + Mathf.Sin(t * 3) * 0.06f : Mathf.Abs(Mathf.Sin(t * 3)) * 0.05f;
            view.SetFlash(type == PickupType.Weapon ? 0.15f + Mathf.Sin(t * 5) * 0.12f : 0);
            if (display && view.Shadow) view.Shadow.enabled = false;
            view.Place(pos, h + bob + 0.05f);
        }

        public void Remove() { dead = true; view.Destroy(); }
        public void SetFade(float f) { if (!dead) view.SetFade(f); }
    }

    public class Chest : IInteractable
    {
        public Vector2 Pos { get; }
        public float Radius => 0.7f;
        public bool rare, opened;
        readonly SpriteView view;
        readonly Anim anim;

        public Chest(Vector2 p, bool rare)
        {
            Pos = p; this.rare = rare;
            anim = SpriteLib.Get(rare ? "chest_rare" : "chest");
            view = new SpriteView("bau", Materials.Sprite, 1.3f);
            view.SetSprite(anim[0]);
            view.Place(p, 0);
        }

        public string Prompt => opened ? null : rare ? "E  ABRIR BAÚ RARO" : "E  ABRIR BAÚ";
        public void Use(Player p)
        {
            if (opened) return;
            opened = true;
            view.SetSprite(anim[1]);
            GameManager.I.OpenChest(this);
        }
        public void Remove() => view.Destroy();
        public void SetFade(float f) => view.SetFade(f);
    }

    public class ShopItem : IInteractable
    {
        public Vector2 Pos { get; }
        public float Radius => 0.6f;
        public PickupType type;
        public string weaponId;
        public int price;
        public bool sold;
        public Pickup display;
        readonly SpriteView ped;

        public ShopItem(Vector2 p, PickupType type, int price, string weaponId = null)
        {
            Pos = p; this.type = type; this.price = price; this.weaponId = weaponId;
            ped = new SpriteView("pedestal", Materials.Sprite, 0);
            ped.SetSprite(SpriteLib.Frame("pedestal"));
            ped.Place(p, 0);
            display = new Pickup(type, p + new Vector2(0, 0.01f), weaponId, false) { display = true };
        }

        public string Label => type switch
        {
            PickupType.Weapon => Weapons.All[weaponId].name,
            PickupType.Heart => "CORAÇÃO",
            PickupType.Ammo => "MUNIÇÃO",
            PickupType.Blank => "BLANK",
            _ => "ITEM",
        };
        public string Prompt => sold ? null : $"E  COMPRAR {Label.ToUpper()} - {price} MOEDAS";
        public void Use(Player p) => GameManager.I.Buy(this, p);
        public void Remove() { ped.Destroy(); if (!display.dead) display.Remove(); }
        public void SetFade(float f) { ped.SetFade(f); display.SetFade(f); }
    }

    public class Hatch : IInteractable
    {
        public Vector2 Pos { get; }
        public float Radius => 1.1f;
        readonly SpriteRenderer sr;

        public Hatch(Vector2 p)
        {
            Pos = p;
            sr = new GameObject("alcapao").AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLib.Frame("hatch");
            sr.sharedMaterial = Materials.Sprite;
            sr.transform.rotation = Quaternion.Euler(90, 0, 0);
            sr.transform.position = MathX.ToWorld(p, 0.006f);
        }

        public string Prompt => "E  DESCER PARA O PRÓXIMO ANDAR";
        public void Use(Player p) => GameManager.I.Descend();
        public void Remove() => Object.Destroy(sr.gameObject);
        readonly MaterialPropertyBlock mpb = new();
        public void SetFade(float f)
        {
            sr.gameObject.SetActive(f > 0.01f);
            sr.GetPropertyBlock(mpb); mpb.SetFloat("_Fade", f); sr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>Arma caída no chão (pegar com E).</summary>
    public class WeaponDrop : IInteractable
    {
        public readonly Pickup pickup;
        public Vector2 Pos => pickup.pos;
        public float Radius => 0.6f;
        public WeaponDrop(Pickup p) { pickup = p; }
        public string Prompt => pickup.dead ? null : "E  PEGAR " + Weapons.All[pickup.weaponId].name.ToUpper();
        public void Use(Player p)
        {
            if (pickup.dead) return;
            pickup.Remove();
            p.AddWeapon(pickup.weaponId);
            Sfx.Play("pickup");
            GameManager.I.Toast(Weapons.All[pickup.weaponId].name.ToUpper(), "NOVA ARMA! (1-9 PARA TROCAR)");
        }
        public void Remove() { if (!pickup.dead) pickup.Remove(); }
        public void SetFade(float f) => pickup.SetFade(f);
    }
}
