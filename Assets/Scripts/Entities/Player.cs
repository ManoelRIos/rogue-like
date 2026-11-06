using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// O Errante. A lógica só depende de PlayerInputState (pronto para várias instâncias
    /// e para rede); o visual fica em SpriteView + arma girando na mão.
    /// </summary>
    public class Player
    {
        public const float RollTime = 0.5f;
        public const float RollIFrames = 0.38f;
        public const float Speed = 5.4f;

        public int Index;                 // 0 = jogador local; >0 reservado para co-op
        public IInputSource Input;
        public PlayerInputState Last;

        public Vector2 pos, vel;
        public float r = 0.3f, hitR = 0.18f;
        public int maxHp = 6, hp = 6, blanks = 2, coins;
        public readonly List<WeaponInstance> weapons = new();
        public int cur;
        public float fireCd, reloadT, rollCd, invuln, flash, kick;
        public bool alive = true;
        public float aim;                 // ângulo da mira (y para baixo)
        public float walkT, animT, deathT;
        public bool moving;
        public RollState roll;
        public Color tint = Color.white;  // diferencia jogadores no co-op

        public class RollState { public float t; public Vector2 dir; }

        GameManager G => GameManager.I;
        readonly SpriteView view;
        readonly Transform gunPivot;
        readonly SpriteRenderer gun;
        readonly MaterialPropertyBlock gunMpb = new();
        LightingSystem.PointLight light;

        public WeaponInstance Weapon => weapons[cur];

        public Player(int index, IInputSource input)
        {
            Index = index; Input = input;
            weapons.Add(new WeaponInstance(Weapons.All["pistola"]));
            view = new SpriteView("Jogador" + index, Materials.Sprite, 0.9f);
            gunPivot = new GameObject("arma").transform;
            gunPivot.SetParent(view.Body, false);
            gun = new GameObject("sprite").AddComponent<SpriteRenderer>();
            gun.transform.SetParent(gunPivot, false);
            gun.sharedMaterial = Materials.Sprite;
            light = LightingSystem.Add(Vector3.zero, 7f, new Color(1f, 0.88f, 0.72f), 0.85f);
            RefreshGun();
        }

        public void RefreshGun()
        {
            gun.sprite = SpriteLib.Weapon(Weapon.Id);
            reloadT = 0;
            G?.Hud?.Dirty();
        }

        public void AddWeapon(string id)
        {
            int have = weapons.FindIndex(w => w.Id == id);
            if (have >= 0) { var w = weapons[have]; if (!w.Infinite) w.ammo = w.def.ammo; cur = have; }
            else { weapons.Add(new WeaponInstance(Weapons.All[id])); cur = weapons.Count - 1; }
            RefreshGun();
        }

        public bool Vulnerable()
        {
            if (G.GodMode || !alive) return false;
            if (roll != null && roll.t < RollIFrames) return false;
            return invuln <= 0;
        }

        public void Hurt()
        {
            if (!Vulnerable()) return;
            hp -= 1;
            invuln = 1.25f;
            flash = 1;
            Sfx.Play("hurt", 0.1f);
            G.Shake(0.35f);
            G.Hitstop(0.09f);
            G.FlashScreen(new Color(1, 0.1f, 0.25f), 0.3f);
            G.Stats.hits++;
            G.Bullets.ClearEnemyNear(pos, 1.6f);
            G.Hud.Dirty();
            if (hp <= 0) { alive = false; deathT = 0; G.OnPlayerDeath(this); }
        }

        public void Heal(int n) { hp = Mathf.Min(maxHp, hp + n); G.Hud.Dirty(); }

        public void SetPosition(Vector2 p) { pos = p; vel = Vector2.zero; roll = null; }

        // ------------------------------------------------------------------ simulação
        public void Tick(float dt)
        {
            if (!alive) { deathT += dt; UpdateVisual(dt); return; }
            var inp = Input.Read(this);
            Last = inp;
            fireCd -= dt; rollCd -= dt; invuln -= dt;
            flash = Mathf.Max(0, flash - dt * 5);
            kick = Mathf.Max(0, kick - dt * 10);
            animT += dt;

            aim = MathX.Angle(pos, inp.aimPoint);

            if (inp.slot >= 0 && inp.slot < weapons.Count && inp.slot != cur) { cur = inp.slot; RefreshGun(); }
            if (inp.cycle != 0 && weapons.Count > 1) { cur = (cur + inp.cycle + weapons.Count) % weapons.Count; RefreshGun(); }

            // cambalhota
            if (roll == null && rollCd <= 0 && inp.roll)
            {
                var dir = inp.move.sqrMagnitude > 0 ? inp.move : MathX.Dir(aim);
                roll = new RollState { t = 0, dir = dir };
                Sfx.Play("roll");
                G.Fx.Dust(pos, 6);
                G.Stats.rolls++;
            }

            if (roll != null)
            {
                roll.t += dt;
                float k = 1 - roll.t / RollTime;
                float spd = 3 + 10.5f * Mathf.Pow(Mathf.Max(0, k), 0.7f);
                G.Move(ref pos, roll.dir * spd * dt, r, false);
                if (Random.value < 0.35f) G.Fx.Dust(pos, 1);
                if (roll.t >= RollTime) { roll = null; rollCd = 0.08f; G.Fx.Dust(pos, 4); }
            }
            else
            {
                var target = inp.move * Speed;
                vel = Vector2.Lerp(vel, target, Mathf.Min(1, dt * 18));
                G.Move(ref pos, vel * dt, r, false);
            }
            moving = inp.move.sqrMagnitude > 0 && roll == null;
            if (moving) walkT += dt;

            // recarga
            var w = Weapon; var d = w.def;
            if (reloadT > 0)
            {
                reloadT -= dt;
                if (reloadT <= 0)
                {
                    int n = Mathf.Min(d.mag - w.mag, w.ammo);
                    w.mag += n; if (!w.Infinite) w.ammo -= n;
                    Sfx.Play("reload");
                    G.Hud.Dirty();
                }
            }
            if (inp.reload && reloadT <= 0 && w.mag < d.mag && w.ammo > 0) StartReload();

            // tiro
            if (roll == null && inp.fire && fireCd <= 0 && reloadT <= 0)
            {
                if (w.mag > 0) Fire();
                else if (w.ammo > 0) StartReload();
                else { Sfx.Play("empty", 0.2f); fireCd = 0.3f; G.Toast("SEM MUNIÇÃO!", "TROQUE DE ARMA (1-9 / RODA)"); }
            }

            if (inp.blank && blanks > 0) { blanks--; G.Blank(pos); G.Hud.Dirty(); }

            UpdateVisual(dt);
        }

        void StartReload() { reloadT = Weapon.def.reload; Sfx.Play("reload"); }

        void Fire()
        {
            var w = Weapon; var d = w.def;
            fireCd = 1f / d.rate;
            w.mag--;
            var muzzle = pos + MathX.Dir(aim) * 0.65f;
            for (int i = 0; i < d.pellets; i++)
            {
                float off = d.fan && d.pellets > 1 ? (i / (float)(d.pellets - 1) - 0.5f) * d.spread : (Random.value - 0.5f) * d.spread;
                float ang = aim + off;
                float spd = d.speed * (1 + (Random.value - 0.5f) * d.speedVar);
                G.Bullets.Spawn(new Bullet
                {
                    pos = muzzle, vel = MathX.Dir(ang) * spd, enemy = false, r = 0.12f * d.size, dmg = d.dmg, life = d.life,
                    pierce = d.pierce, bounce = d.bounce, homing = d.homing, explode = d.explode, color = d.color, size = d.size, sprite = d.bullet,
                    owner = Index,
                });
            }
            G.Fx.Muzzle(muzzle, 0.5f, d.color);
            LightingSystem.Add(MathX.ToWorld(muzzle, 0.6f), 4f, d.color, 1.4f, 0.06f);
            Sfx.Play(d.sfx, 0.02f);
            kick = Mathf.Min(1, 0.4f + d.kick * 0.3f);
            if (d.kick > 1) G.Shake(d.kick * 0.05f);
            vel -= MathX.Dir(aim) * d.kick * 0.8f;
            G.Stats.shots++;
            G.Hud.Dirty();
        }

        // ------------------------------------------------------------------ visual
        public void UpdateVisual(float dt)
        {
            float h = 0;
            // ângulo na tela (y para cima)
            float a = -aim;
            float deg = a * Mathf.Rad2Deg;
            bool left = Mathf.Cos(a) < 0;
            bool up = deg > 50 && deg < 130;
            bool down = deg < -50 && deg > -130;
            string dir = up ? "up" : down ? "down" : "side";

            if (!alive)
            {
                view.SetSprite(SpriteLib.Get("player", "death").At(deathT));
                gun.enabled = false;
                view.SetFlash(0);
                view.Place(pos, 0);
                light.pos = MathX.ToWorld(pos, 1.5f);
                return;
            }

            if (roll != null)
            {
                var an = SpriteLib.Get("player", "roll");
                view.SetSprite(an.At(roll.t));
                view.Flip(roll.dir.x < 0);
                h = Mathf.Sin(roll.t / RollTime * Mathf.PI) * 0.35f;   // salto: sombra fica no chão
                gun.enabled = false;
            }
            else
            {
                var an = SpriteLib.Get("player", (moving ? "run_" : "idle_") + dir);
                view.SetSprite(an.At(moving ? walkT : animT));
                view.Flip(left && dir == "side");
                gun.enabled = true;
            }

            bool blink = invuln > 0 && roll == null && Mathf.FloorToInt(invuln * 14) % 2 == 1;
            view.SpriteVisible(!blink);
            gun.enabled = gun.enabled && !blink;
            view.SetFlash(flash);
            view.SetTint(tint);
            view.Place(pos, h);

            // arma na mão, girando para a mira
            gunPivot.localPosition = new Vector3(left ? -0.14f : 0.14f, 0.42f, up ? 0.05f : -0.05f);
            gunPivot.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
            gun.transform.localPosition = new Vector3(0.12f - kick * 0.1f, 0, 0);
            gun.transform.localScale = new Vector3(1, left ? -1 : 1, 1);

            light.pos = MathX.ToWorld(pos, 1.6f) + new Vector3(0, 0, -0.4f);
        }

        public bool Rolling => roll != null;

        public void RecreateLight() => light = LightingSystem.Add(Vector3.zero, 7f, new Color(1f, 0.88f, 0.72f), 0.85f);

        public void Destroy() { view.Destroy(); LightingSystem.Remove(light); }
    }
}
