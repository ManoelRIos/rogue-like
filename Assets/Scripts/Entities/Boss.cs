using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public class BossDef
    {
        public string id, name, minion;
        public float hp, r, speed;
        public bool fly;
        public Color color;
        public string[] attacks;
    }

    public static class BossDefs
    {
        static Color C(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static readonly Dictionary<string, BossDef> All = new()
        {
            ["reiGosma"] = new BossDef { id = "reiGosma", name = "Rei Gosma", hp = 560, r = 1.0f, speed = 1.4f, minion = "gosma", color = C("#62c44a"), attacks = new[] { "fan", "ring", "hop", "summon", "hop", "fan" } },
            ["sinoFantasma"] = new BossDef { id = "sinoFantasma", name = "Sino Fantasma", hp = 700, r = 0.95f, speed = 1.6f, fly = true, minion = "lanterna", color = C("#c9bfe8"), attacks = new[] { "spiral", "bell", "teleport", "wave", "bell" } },
            ["golem"] = new BossDef { id = "golem", name = "Golem da Forja", hp = 880, r = 1.05f, speed = 1.2f, color = C("#7c7266"), attacks = new[] { "charge", "shotgun", "slam", "bigshot", "charge" } },
            ["aranha"] = new BossDef { id = "aranha", name = "Aranha Relojoeira", hp = 900, r = 1.1f, speed = 2.0f, minion = "morcego", color = C("#c99a3b"), attacks = new[] { "clock", "spray", "summon", "fan", "wave", "clock" } },
            ["sacerdote"] = new BossDef { id = "sacerdote", name = "Sacerdote do Último Tiro", hp = 1150, r = 0.9f, speed = 1.6f, fly = true, minion = "caveira", color = C("#7a1f34"), attacks = new[] { "cross", "rain", "teleport", "bell", "spiral", "clock", "summon", "wave" } },
        };
    }

    /// <summary>Chefe: ciclo de ataques próprios e segunda fase abaixo de 50% de vida.</summary>
    public class Boss : Enemy
    {
        public BossDef bdef;
        public override bool IsBoss => true;
        public override Color Color => bdef.color;
        public override string Name => bdef.name;
        public bool Phase2 => hp < maxHp * 0.5f;

        float intro = 1.6f, idle = 1f, alpha = 1;
        int attackIdx;
        readonly List<string> order;
        AttackState atk;

        public class AttackState
        {
            public string name; public float t, a, a1, hopY; public int n, snd = -1;
            public bool lockMove, windup;
            public Vector2 start, target;
            public readonly Dictionary<string, float> timers = new();
        }

        public Boss(string type, Vector2 p, int floor)
        {
            bdef = BossDefs.All[type];
            Init(p, floor, bdef.hp * (1 + floor * 0.12f), bdef.r, bdef.fly, "boss_" + type, true);
            bulletSpeed = 1 + floor * 0.05f;
            order = new List<string>(bdef.attacks);
        }

        public override void Tick(float dt)
        {
            t += dt;
            flash = Mathf.Max(0, flash - dt * 6);
            if (intro > 0) { intro -= dt; UpdateVisual(); return; }
            RunQueue(dt);
            var P = Target();
            if (P == null) { UpdateVisual(); return; }

            if (atk == null || !atk.lockMove)
            {
                var to = P.pos - pos; float d = Mathf.Max(0.001f, to.magnitude);
                Vector2 m;
                if (d > 6.5f) m = to / d; else if (d < 4f) m = -to / d; else m = new Vector2(-to.y, to.x) / d * strafe * 0.5f;
                strafeT -= dt;
                if (strafeT <= 0) { strafe *= -1; strafeT = Random.Range(2f, 4f); }
                G.Move(ref pos, m * bdef.speed * (Phase2 ? 1.25f : 1f) * dt, r, fly);
            }
            if (P.Vulnerable() && (P.pos - pos).magnitude < r + P.hitR) P.Hurt();

            if (atk != null)
            {
                atk.t += dt;
                if (Run(atk, dt))
                {
                    atk = null;
                    idle = (Phase2 ? Random.Range(0.4f, 0.8f) : Random.Range(0.8f, 1.3f)) - floor * 0.05f;
                }
            }
            else
            {
                idle -= dt;
                if (idle <= 0)
                {
                    string name = order[attackIdx % order.Count];
                    attackIdx++;
                    if (attackIdx % order.Count == 0)
                        for (int i = order.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (order[i], order[j]) = (order[j], order[i]); }
                    atk = new AttackState { name = name };
                }
            }
            UpdateVisual();
        }

        protected override float Height => (fly ? 0.35f + Mathf.Sin(t * 2) * 0.12f : 0) + (atk?.hopY ?? 0);

        protected override void UpdateVisual()
        {
            base.UpdateVisual();
            view.SpriteVisible(alpha > 0.5f || Mathf.FloorToInt(t * 20) % 2 == 0);
            view.SetTint(Phase2 ? new Color(1f, 0.82f, 0.82f) : Color.white);
            if (atk != null && atk.windup) view.SetFlash(Mathf.Max(flash, 0.3f + Mathf.Sin(t * 40) * 0.25f));
            // sombra encolhe no pulo (noção de altura)
            float hop = atk?.hopY ?? 0;
            view.SetShadowSize(r * 2.4f * (1 - Mathf.Clamp01(hop / 3f) * 0.5f));
        }

        // ------------------------------------------------------------------ ataques
        bool Every(AttackState s, string key, float interval, float dt, System.Action fn)
        {
            if (!s.timers.TryGetValue(key, out float acc)) acc = interval;
            acc += dt;
            bool fired = false;
            while (acc >= interval) { acc -= interval; fn(); fired = true; }
            s.timers[key] = acc;
            return fired;
        }

        void Snd(AttackState s, float rate)
        {
            int k = Mathf.FloorToInt(s.t * rate);
            if (k != s.snd) { s.snd = k; Sfx.Play("enemyShoot"); }
        }

        bool Run(AttackState s, float dt)
        {
            switch (s.name)
            {
                case "fan":
                    {
                        int waves = Phase2 ? 4 : 3;
                        Every(s, "f", 0.38f, dt, () => { if (s.n < waves) { Fan(Phase2 ? 9 : 7, 1.1f, 6); Sfx.Play("enemyShoot"); s.n++; } });
                        return s.n >= waves && s.t > waves * 0.38f + 0.2f;
                    }
                case "ring":
                    Every(s, "r", 0.45f, dt, () => { if (s.n < 3) { Ring(Phase2 ? 24 : 18, 4.6f, s.n * 0.17f); Sfx.Play("enemyShoot"); s.n++; } });
                    return s.n >= 3;
                case "hop":
                    {
                        s.lockMove = true;
                        var P = Target();
                        if (s.n == 0) { s.n = 1; s.start = pos; s.target = P != null ? P.pos : pos; s.windup = true; }
                        if (s.t < 0.35f) return false;
                        s.windup = false;
                        float k = Mathf.Min(1, (s.t - 0.35f) / 0.8f);
                        var np = Vector2.Lerp(s.start, s.target, k);
                        G.Move(ref pos, np - pos, r, false);
                        s.hopY = Mathf.Sin(k * Mathf.PI) * 2.6f;
                        invulnerable = k < 0.85f;
                        if (k >= 1)
                        {
                            s.hopY = 0; invulnerable = false;
                            Ring(Phase2 ? 26 : 20, 4.2f, Random.value);
                            Later(0.25f, () => Ring(14, 3, Random.value));
                            G.Shake(0.5f); Sfx.Play("explode");
                            G.Fx.Burst(pos, 0.1f, 30, Color, 5, 0.6f, 2f);
                            G.Fx.Dust(pos, 10);
                            return true;
                        }
                        return false;
                    }
                case "summon":
                    if (s.n == 0)
                    {
                        s.n = 1;
                        int alive = 0; foreach (var e in G.Enemies) if (!e.dead && !e.IsBoss) alive++;
                        int count = Mathf.Max(0, Mathf.Min(2 + (Phase2 ? 1 : 0), 5 - alive));
                        for (int i = 0; i < count; i++) G.SpawnEnemyNear(bdef.minion, pos);
                        Ring(12, 3.5f, 0);
                    }
                    return s.t > 0.6f;
                case "spiral":
                    {
                        int arms = Phase2 ? 4 : 3;
                        Every(s, "s", 0.075f, dt, () => { s.a += 0.21f; for (int i = 0; i < arms; i++) Shot(s.a + i / (float)arms * MathX.TAU, 4.3f); });
                        Snd(s, 8);
                        return s.t > 3;
                    }
                case "bell":
                    Every(s, "b", 0.6f, dt, () =>
                    {
                        if (s.n >= 3) return;
                        int n = 30; float gap = AimAt() + Random.Range(-0.3f, 0.3f);
                        for (int i = 0; i < n; i++)
                        {
                            float a = i / (float)n * MathX.TAU;
                            if (Mathf.Abs(MathX.WrapAngle(a - gap)) < 0.42f) continue;
                            Shot(a, 3.8f);
                        }
                        Sfx.Play("enemyShoot"); s.n++;
                    });
                    return s.n >= 3 && s.t > 2;
                case "teleport":
                    s.lockMove = true;
                    if (s.n == 0) { s.n = 1; alpha = 0; }
                    if (s.n == 1 && s.t > 0.45f)
                    {
                        s.n = 2; Blink(); alpha = 1;
                        Fan(Phase2 ? 11 : 9, 1.4f, 5.5f);
                        Later(0.3f, () => Fan(Phase2 ? 10 : 8, 1.3f, 4.5f));
                        Sfx.Play("enemyShoot");
                    }
                    return s.n == 2 && s.t > 1.1f;
                case "wave":
                    Every(s, "w", 0.16f, dt, () =>
                    {
                        if (s.n >= 12) return;
                        float a = AimAt(); float sg = s.n % 2 == 1 ? 1 : -1;
                        Shot(a + sg * 0.5f, 5, 1, 0, -sg * 0.9f);
                        Shot(a - sg * 0.5f, 5, 1, 0, sg * 0.9f);
                        if (Phase2) Shot(a, 6);
                        s.n++;
                    });
                    return s.n >= 12;
                case "charge":
                    {
                        s.lockMove = true;
                        if (s.n == 0) { s.n = 1; s.windup = true; }
                        if (s.t < 0.6f) { s.a = AimAt(); return false; }
                        s.windup = false;
                        float sp = Phase2 ? 15 : 12.5f;
                        var before = pos;
                        G.Move(ref pos, MathX.Dir(s.a) * sp * dt, r, false);
                        if (Random.value < 0.5f) G.Fx.Dust(pos, 1);
                        if ((pos - before).magnitude < sp * dt * 0.5f || s.t > 2)
                        {
                            G.Shake(0.7f); Sfx.Play("explode");
                            Ring(Phase2 ? 22 : 16, 4, Random.value);
                            for (int i = 0; i < 8; i++) Shot(s.a + Mathf.PI + Random.Range(-0.8f, 0.8f), Random.Range(3f, 6f));
                            G.Fx.Burst(pos, 0.3f, 25, new Color(0.65f, 0.6f, 0.55f), 5, 0.6f, 2.4f);
                            return true;
                        }
                        return false;
                    }
                case "shotgun":
                    Every(s, "s", 0.55f, dt, () =>
                    {
                        if (s.n >= 3) return;
                        float a = AimAt();
                        for (int i = 0; i < (Phase2 ? 18 : 13); i++) Shot(a + Random.Range(-0.7f, 0.7f), Random.Range(3.5f, 7.5f));
                        Sfx.Play("shotgun"); s.n++;
                    });
                    return s.n >= 3 && s.t > 1.5f;
                case "slam":
                    if (s.t < 0.5f) { s.windup = true; return false; }
                    s.windup = false;
                    Every(s, "m", 0.28f, dt, () =>
                    {
                        if (s.n >= 4) return;
                        Ring(24, 3.6f + s.n * 0.4f, (s.n % 2) * (Mathf.PI / 24));
                        G.Shake(0.3f); Sfx.Play("explode", 0.1f); s.n++;
                    });
                    return s.n >= 4;
                case "bigshot":
                    Every(s, "g", 0.5f, dt, () =>
                    {
                        if (s.n >= 4) return;
                        Shot(AimAt(), 3.6f, 2.4f, 0.6f);
                        if (Phase2) { Shot(AimAt() + 0.6f, 3.4f, 1.8f); Shot(AimAt() - 0.6f, 3.4f, 1.8f); }
                        Sfx.Play("heavy"); s.n++;
                    });
                    return s.n >= 4 && s.t > 2.2f;
                case "clock":
                    {
                        s.lockMove = true;
                        int hands = Phase2 ? 3 : 2;
                        if (s.t <= dt) s.a1 = Random.value * MathX.TAU;
                        Every(s, "c", 0.07f, dt, () =>
                        {
                            s.a1 += 0.11f;
                            for (int i = 0; i < hands; i++) Shot(s.a1 + i / (float)hands * MathX.TAU, 5.2f);
                            Shot(-s.a1 * 0.7f, 3.4f);
                        });
                        Snd(s, 6);
                        return s.t > 3.4f;
                    }
                case "spray":
                    Every(s, "p", Phase2 ? 0.03f : 0.045f, dt, () => Shot(AimAt() + Random.Range(-1f, 1f), Random.Range(4f, 7f)));
                    Snd(s, 8);
                    return s.t > 1.8f;
                case "cross":
                    {
                        int arms = Phase2 ? 8 : 4;
                        Every(s, "x", 0.09f, dt, () => { s.a += Phase2 ? 0.09f : 0.07f; for (int i = 0; i < arms; i++) Shot(s.a + i / (float)arms * MathX.TAU, 4.8f); });
                        Snd(s, 6);
                        return s.t > 3.2f;
                    }
                case "rain":
                    {
                        var room = G.ActiveRoom;
                        if (room == null) return true;
                        Every(s, "r", Phase2 ? 0.06f : 0.09f, dt, () =>
                        {
                            float x = room.x0 + 0.5f + Random.value * (room.w - 1);
                            Shot(Mathf.PI / 2 + Random.Range(-0.15f, 0.15f), Random.Range(3.6f, 5.2f), 1, 0, 0, new Vector2(x, room.y0 + 0.4f), 0);
                        });
                        return s.t > 2.6f;
                    }
            }
            return true;
        }
    }
}
