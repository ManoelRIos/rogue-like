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
    }

    public static class EnemyDefs
    {
        static Color C(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static readonly Dictionary<string, EnemyDef> All = new()
        {
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
            anim = SpriteLib.Get(sheet);
            view = new SpriteView(sheet, Materials.Sprite, r * 2.4f);
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
            view.Destroy();
            G.OnEnemyKilled(this);
        }

        public void Remove() { dead = true; view.Destroy(); }

        // ------------------------------------------------------------------ tiros
        public void Shot(float angle, float speed, float size = 1, float accel = 0, float curve = 0, Vector2? from = null, float? off = null)
        {
            var o = from ?? pos;
            float d = off ?? r * 0.8f;
            G.Bullets.Spawn(new Bullet
            {
                pos = o + MathX.Dir(angle) * d, vel = MathX.Dir(angle) * speed * bulletSpeed, enemy = true,
                r = 0.14f * size, size = size, life = 6, accel = accel, curve = curve,
            });
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
                if (spawnT <= 0) { active = true; view.SpriteVisible(true); flash = 1; }
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
            view.Flip(P != null && P.pos.x < pos.x);
            view.SetSprite(anim.At(t * (def != null && def.ai == "swoop" ? 1.6f : 1f)));
            float wu = windup > 0 ? 0.35f + Mathf.Sin(t * 40) * 0.25f : 0;
            view.SetFlash(Mathf.Max(flash * 0.9f, wu));
            if (!active) view.SetShadowSize(r * 2.4f * (1 - Mathf.Max(0, spawnT) / 0.8f));
            view.Place(pos, Height);
        }
    }
}
