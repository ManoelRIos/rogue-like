using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public class EnemyDef
    {
        public string id, name, ai = "keep", fire;
        public float hp, speed, r = 0.4f, keep = 4, cdMin = 2, cdMax = 3;
        public bool fly;
        public Color color;
        public int coinsMin, coinsMax;
        public float flyHeight = 0.45f;
        public string gun;           // sprite de arma que gira para mirar (opcional)
        public string bullet;        // sprite de bala próprio (opcional)
    }

    public static class EnemyDefs
    {
        static Color C(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static readonly Dictionary<string, EnemyDef> All = new()
        {
            ["cultista"] = new EnemyDef { id = "cultista", name = "Cultista", hp = 20, speed = 1.8f, r = 0.38f, ai = "keep", keep = 5f, cdMin = 1.8f, cdMax = 2.6f, fire = "burst3", color = C("#7a1e24"), coinsMin = 1, coinsMax = 3, gun = "flintlock" },
            ["gosma"] = new EnemyDef { id = "gosma", name = "Gosma", hp = 15, speed = 1.9f, r = 0.4f, ai = "keep", keep = 4, cdMin = 1.7f, cdMax = 2.6f, fire = "single", color = C("#5fbf4a"), coinsMin = 0, coinsMax = 2 },
            ["cogumelo"] = new EnemyDef { id = "cogumelo", name = "Cogumelo", hp = 19, speed = 1.3f, r = 0.42f, ai = "keep", keep = 5, cdMin = 2.4f, cdMax = 3.2f, fire = "spread5", color = C("#c8413d"), coinsMin = 1, coinsMax = 2 },
            ["caveira"] = new EnemyDef { id = "caveira", name = "Caveira", hp = 24, speed = 2.3f, r = 0.38f, ai = "keep", keep = 5.5f, cdMin = 2f, cdMax = 2.8f, fire = "burst3", color = C("#e9e2cf"), coinsMin = 1, coinsMax = 3 },
            ["morcego"] = new EnemyDef { id = "morcego", name = "Morcego", hp = 10, speed = 4.3f, r = 0.36f, ai = "swoop", fly = true, cdMin = 3f, cdMax = 4.2f, fire = "ring6", color = C("#6b4a9a"), coinsMin = 0, coinsMax = 1, flyHeight = 0.7f },
            ["lanterna"] = new EnemyDef { id = "lanterna", name = "Lanterna", hp = 22, speed = 1.4f, r = 0.38f, ai = "keep", keep = 6, fly = true, cdMin = 2.6f, cdMax = 3.4f, fire = "ring10", color = C("#ffbf3b"), coinsMin = 1, coinsMax = 2, flyHeight = 0.55f },
            ["torreta"] = new EnemyDef { id = "torreta", name = "Totem", hp = 38, speed = 0, r = 0.45f, ai = "still", cdMin = 1.4f, cdMax = 2f, fire = "spiral", color = C("#7d7690"), coinsMin = 1, coinsMax = 3 },
            ["cristal"] = new EnemyDef { id = "cristal", name = "Cristal", hp = 26, speed = 0, r = 0.36f, ai = "blink", fly = true, cdMin = 2.6f, cdMax = 3.3f, fire = "lines", color = C("#5ad1e6"), coinsMin = 1, coinsMax = 3, flyHeight = 0.5f },
            ["besouro"] = new EnemyDef { id = "besouro", name = "Besouro", hp = 48, speed = 1.3f, r = 0.48f, ai = "chase", cdMin = 2.4f, cdMax = 3f, fire = "shotgun", color = C("#2d4a8a"), coinsMin = 2, coinsMax = 4 },
        };
    }

    /// <summary>Inimigo comum: IA simples + padrões de tiro com aviso visual (windup).</summary>
    public class Enemy
    {
        public EnemyDef def;
        public Vector2 pos, vel, knock;
        public float r, hp, maxHp, t, flash, cd, windup, spawnT, strafeT, spiralA, blinkT;
        public int strafe = 1, floor;
        public bool dead, active, fly, invulnerable;
        public float bulletSpeed = 1, fireRate = 1;
        public bool Active => active;
        protected readonly List<(float t, System.Action fn)> queue = new();
        protected SpriteView view;
        protected Anim anim;
        protected string sheet;
        protected Transform gunPivot;
        protected SpriteRenderer gunSr;
        protected float gunKick;
        public string bulletSprite;
        protected VoxelRig rig;          // modelo 3D (quando existir)
        protected virtual int DeathStyle => 1;
        protected GameManager G => GameManager.I;
        public virtual bool IsBoss => false;
        public virtual Color Color => def.color;
        public virtual string Name => def.name;

        public Enemy() { }

        public Enemy(string type, Vector2 p, int floor, bool instant = false)
        {
            def = EnemyDefs.All[type];
            Init(p, floor, def.hp * (1 + floor * 0.28f), def.r, def.fly, "enemy_" + type, instant);
            cd = Random.Range(0.9f, 1.7f);
        }

        protected void Init(Vector2 p, int fl, float hp_, float r_, bool fly_, string sheet, bool instant)
        {
            pos = p; floor = fl; maxHp = hp = hp_; r = r_; fly = fly_;
            bulletSpeed = 1 + fl * 0.07f;
            fireRate = 1 - fl * 0.06f;
            spawnT = instant ? 0 : 0.8f;
            active = instant;
            strafe = Random.value < 0.5f ? 1 : -1;
            strafeT = Random.Range(1f, 3f);
            spiralA = Random.value * MathX.TAU;
            t = Random.value * 10;
            this.sheet = sheet;
            anim = SpriteLib.Get(sheet);
            view = new SpriteView(sheet, Materials.Sprite, r * 2.4f);
            if (def != null && def.gun != null)
            {
                gunPivot = new GameObject("arma").transform;
                gunPivot.SetParent(view.Body, false);
                gunSr = new GameObject("s").AddComponent<SpriteRenderer>();
                gunSr.transform.SetParent(gunPivot, false);
                gunSr.sharedMaterial = Materials.Sprite;
                gunSr.sprite = SpriteLib.Weapon(def.gun);
                gunSr.enabled = instant;
            }
            bulletSprite = def?.bullet;
            string model = sheet.Replace("enemy_", "").Replace("boss_", "");
            if (VoxelLib.Has(model))
            {
                rig = new VoxelRig(model);
                if (def != null && def.gun != null) rig.SetGun("arma_" + def.gun, new Vector3(0.45f, 0.55f, 0.3f));
                rig.Visible = false;
            }
            view.SetSprite(anim[0]);
            view.SpriteVisible(instant);
            view.Place(pos, 0);
            if (!instant) G.Fx.Play("fx_spawn", pos, 0.02f, 1.2f, true, new Color(1f, 0.3f, 0.4f), 0.6f);
        }

        public void Damage(float dmg, Vector2 dir)
        {
            if (dead || !active || invulnerable) return;
            hp -= dmg;
            flash = 1;
            if (!IsBoss && dir.sqrMagnitude > 0) knock += dir.normalized * 2.2f;
            Sfx.Play("hit", 0.04f);
            G.Fx.Burst(pos, 0.45f, 3, Color, 2.5f, 0.3f);
            if (hp <= 0) Die();
        }

        public virtual void Die()
        {
            dead = true;
            Sfx.Play("enemyDie", 0.05f);
            G.Fx.Burst(pos, 0.5f, IsBoss ? 60 : 16, Color, IsBoss ? 6 : 3.5f, 0.8f, 2.2f, 3);
            G.Fx.Burst(pos, 0.5f, IsBoss ? 20 : 6, Color.white, 3, 0.4f, 1.4f, 2);
            G.Fx.Glow(pos, 0.5f, IsBoss ? 7 : 2.4f, new Color(1f, 0.85f, 0.65f), 0.25f);
            G.Shake(IsBoss ? 0.8f : 0.12f);
            var deathAnim = SpriteLib.Get(sheet, "death");
            if (deathAnim != null)
            {
                // o corpo fica no chão (animação de morte e depois parado)
                if (gunSr) gunSr.enabled = false;
                view.SetFlash(0);
                view.SetTint(Color.white);
                if (view.Shadow) view.Shadow.enabled = false;
                G.AddCorpse(view, deathAnim, pos, rig, DeathStyle);
            }
            else { view.Destroy(); rig?.Destroy(); }
            G.OnEnemyKilled(this);
        }

        public void Remove() { dead = true; view.Destroy(); rig?.Destroy(); }

        // ------------------------------------------------------------------ tiros
        public void Shot(float angle, float speed, float size = 1, float accel = 0, float curve = 0, Vector2? from = null, float? off = null)
        {
            var o = from ?? pos;
            float d = off ?? (gunSr ? 0.75f : r * 0.8f);
            G.Bullets.Spawn(new Bullet
            {
                pos = o + MathX.Dir(angle) * d, vel = MathX.Dir(angle) * speed * bulletSpeed, enemy = true,
                r = 0.14f * size, size = size, life = 6, accel = accel, curve = curve, sprite = size >= 1.6f ? null : bulletSprite,
            });
            if (gunSr && from == null) { gunKick = 1; G.Fx.Muzzle(o + MathX.Dir(angle) * 0.75f, 0.3f, new Color(1f, 0.5f, 0.3f)); }
        }

        public Player Target() => G.NearestPlayer(pos);
        public float AimAt() { var p = Target(); return p == null ? 0 : MathX.Angle(pos, p.pos); }

        public void Fan(int n, float spread, float speed, float? baseAng = null, float size = 1)
        {
            float b = baseAng ?? AimAt();
            for (int i = 0; i < n; i++) Shot(b + (n > 1 ? (i / (float)(n - 1) - 0.5f) * spread : 0), speed, size);
        }

        public void Ring(int n, float speed, float off = 0, float size = 1)
        {
            for (int i = 0; i < n; i++) Shot(off + i / (float)n * MathX.TAU, speed, size);
        }

        public void Later(float delay, System.Action fn) => queue.Add((delay, fn));

        protected virtual void Fire()
        {
            Sfx.Play("enemyShoot", 0.05f);
            switch (def.fire)
            {
                case "single": Fan(1, 0, 5.5f); if (floor >= 2) Later(0.18f, () => Fan(1, 0, 5.5f)); break;
                case "spread5": Fan(5, 0.9f, 4.8f); break;
                case "burst3": for (int i = 0; i < 3; i++) Later(i * 0.14f, () => Fan(floor >= 3 ? 2 : 1, 0.15f, 6.4f)); break;
                case "ring6": Ring(6, 4, Random.value); break;
                case "ring10": Ring(10 + floor, 4.2f, spiralA); spiralA += 0.3f; break;
                case "spiral":
                    for (int i = 0; i < 18; i++) Later(i * 0.07f, () => { spiralA += 0.38f; Shot(spiralA, 4.4f); Shot(spiralA + Mathf.PI, 4.4f); });
                    break;
                case "lines":
                    { float a = AimAt(); for (int k = 0; k < 3; k++) { int kk = k; Later(k * 0.12f, () => Fan(3, 0.5f, 6.5f - kk * 0.6f, a)); } }
                    break;
                case "shotgun":
                    { float a = AimAt(); for (int i = 0; i < 9; i++) Shot(a + Random.Range(-0.55f, 0.55f), Random.Range(3.6f, 7f)); }
                    break;
            }
        }

        protected void RunQueue(float dt)
        {
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                var q = queue[i];
                q.t -= dt;
                if (q.t <= 0) { queue.RemoveAt(i); if (!dead) q.fn(); }
                else queue[i] = q;
            }
        }

        // ------------------------------------------------------------------ IA
        public virtual void Tick(float dt)
        {
            t += dt;
            flash = Mathf.Max(0, flash - dt * 6);
            if (!active)
            {
                spawnT -= dt;
                if (spawnT <= 0) { active = true; view.SpriteVisible(true); flash = 1; if (gunSr) gunSr.enabled = true; }
                UpdateVisual();
                return;
            }
            RunQueue(dt);

            var P = Target();
            if (P == null) { UpdateVisual(); return; }
            var to = P.pos - pos;
            float d = Mathf.Max(0.001f, to.magnitude);
            var n = to / d;
            Vector2 m = Vector2.zero;
            strafeT -= dt;
            if (strafeT <= 0) { strafe *= -1; strafeT = Random.Range(1.2f, 3f); }

            switch (def.ai)
            {
                case "keep":
                    if (d > def.keep + 1) m = n;
                    else if (d < def.keep - 1.2f) m = -n;
                    else m = new Vector2(-n.y, n.x) * strafe * 0.7f;
                    break;
                case "chase": m = n; break;
                case "swoop": { float s = Mathf.Sin(t * 2.2f) * 0.9f; m = n + new Vector2(-n.y, n.x) * s; break; }
                case "blink":
                    blinkT -= dt;
                    if (blinkT <= 0) { blinkT = Random.Range(2.6f, 3.6f); Blink(); }
                    break;
            }
            foreach (var o in G.Enemies)
            {
                if (o == this || o.dead) continue;
                var dv = pos - o.pos; float od = dv.sqrMagnitude, rr = (r + o.r) * 1.1f;
                if (od < rr * rr && od > 0.0001f) m += dv / Mathf.Sqrt(od) * 0.8f;
            }
            if (m.sqrMagnitude > 1) m.Normalize();
            vel = Vector2.Lerp(vel, m * def.speed, Mathf.Min(1, dt * 6));
            G.Move(ref pos, (vel + knock) * dt, r, fly);
            knock *= Mathf.Pow(0.002f, dt);

            if (P.Vulnerable() && d < r + P.hitR + 0.05f) P.Hurt();

            if (windup > 0) { windup -= dt; if (windup <= 0) Fire(); }
            else
            {
                cd -= dt;
                if (cd <= 0 && d < 16) { windup = 0.3f; cd = Random.Range(def.cdMin, def.cdMax) * fireRate; }
            }
            UpdateVisual();
        }

        public void Blink()
        {
            var room = G.ActiveRoom;
            if (room == null) return;
            for (int i = 0; i < 20; i++)
            {
                var p = new Vector2(room.x0 + 1 + Random.value * (room.w - 2), room.y0 + 1 + Random.value * (room.h - 2));
                if (G.CircleBlocked(p, 0.5f, true)) continue;
                var tp = Target();
                if (tp != null && (p - tp.pos).magnitude < 3.5f) continue;
                G.Fx.Burst(pos, 0.5f, 10, Color, 2, 0.4f);
                pos = p;
                G.Fx.Burst(pos, 0.5f, 10, Color, 2, 0.4f);
                return;
            }
        }

        protected virtual float Height => fly ? def.flyHeight + Mathf.Sin(t * 3) * 0.08f : 0;

        protected virtual void UpdateVisual()
        {
            var P = Target();
            float yawR = PixelCamera.BillboardYaw * Mathf.Deg2Rad;
            float scr = P != null ? -MathX.Angle(pos, P.pos) + yawR : 0;   // ângulo na tela
            bool left = P != null && Mathf.Cos(scr) < 0;
            view.Flip(left);
            // estados: mira (aviso antes do tiro) > andando > parado; folhas antigas usam a 1ª animação
            Anim a = null;
            if (windup > 0) a = SpriteLib.Get(sheet, "aim");
            if (a == null && vel.sqrMagnitude > 0.15f) a = SpriteLib.Get(sheet, "walk");
            if (a == null) a = SpriteLib.Get(sheet, "idle") ?? anim;
            view.SetSprite(a.At(t * (def != null && def.ai == "swoop" ? 1.6f : 1f)));
            float wu = windup > 0 && SpriteLib.Get(sheet, "aim") == null ? 0.35f + Mathf.Sin(t * 40) * 0.25f : 0;
            view.SetFlash(Mathf.Max(flash * 0.9f, wu));
            if (!active) view.SetShadowSize(r * 2.4f * (1 - Mathf.Max(0, spawnT) / 0.8f));
            if (gunSr && P != null)
            {
                float ang = scr * Mathf.Rad2Deg;
                gunKick = Mathf.Max(0, gunKick - Time.deltaTime * 8);
                gunPivot.localPosition = new Vector3(left ? -0.2f : 0.2f, 0.5f, -0.05f);
                gunPivot.localRotation = Quaternion.Euler(0, 0, ang);
                gunSr.transform.localPosition = new Vector3(0.1f - gunKick * 0.1f, 0, 0);
                gunSr.transform.localScale = new Vector3(1, left ? -1 : 1, 1);
            }
            view.Place(pos, Height);
            UpdateRig(new RigPose { walkPhase = t * 9f, walk = Mathf.Clamp01(vel.magnitude / Mathf.Max(0.5f, def != null ? def.speed : 1)), aim = windup > 0 || gunKick > 0 ? 1 : 0.3f, sway = Mathf.Sin(t * 2.5f), kick = gunKick });
        }

        /// <summary>Mostra o modelo 3D no lugar do sprite quando a câmera está em 3D.</summary>
        protected void UpdateRig(RigPose pose)
        {
            if (rig == null) return;
            bool use3D = PixelCamera.I.Blend > 0.5f;
            rig.Visible = use3D && (active || IsBoss);
            if (use3D)
            {
                view.SpriteVisible(false);
                if (gunSr) gunSr.enabled = false;
                var P = Target();
                rig.Pose(pose);
                rig.SetFlash(Mathf.Max(flash * 0.9f, windup > 0 ? 0.25f + Mathf.Sin(t * 40) * 0.2f : 0));
                rig.Place(pos, Height, P != null ? MathX.Angle(pos, P.pos) : 0);
            }
            else if (active || IsBoss) { view.SpriteVisible(true); if (gunSr) gunSr.enabled = true; }
        }
    }
}
