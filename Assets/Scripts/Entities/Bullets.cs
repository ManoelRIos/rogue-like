using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public class Bullet
    {
        public Vector2 pos, vel;
        public float r = 0.14f, life = 6, dmg = 1, size = 1, accel, curve, homing, explode, age;
        public int pierce, bounce, owner = -1;
        public bool enemy = true, dead;
        public Color color = Color.white;
        public string sprite;
        public HashSet<Enemy> hit;
        internal BulletView view;
    }

    internal class BulletView
    {
        public GameObject go;
        public SpriteRenderer sr, glow;
    }

    /// <summary>Projéteis do jogador e dos inimigos, com pool de objetos.</summary>
    public class BulletSystem
    {
        public const float Height = 0.45f;
        public readonly List<Bullet> list = new();
        readonly Stack<BulletView> pool = new();
        readonly Transform root;
        readonly Sprite glowSprite;
        GameManager G => GameManager.I;

        public BulletSystem()
        {
            root = new GameObject("Balas").transform;
            glowSprite = MakeGlow();
        }

        static Sprite MakeGlow()
        {
            var t = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16, 16)) / 16f;
                    float a = Mathf.Clamp01(1 - d);
                    a = Mathf.Floor(a * a * 5) / 5f; // degraus (pixelado)
                    t.SetPixel(x, y, new Color(a, a, a, a));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16);
        }

        public int EnemyCount { get { int n = 0; foreach (var b in list) if (b.enemy && !b.dead) n++; return n; } }

        public Bullet Spawn(Bullet b)
        {
            if (b.enemy && list.Count > 2600) return null;
            if (!b.enemy) b.hit = new HashSet<Enemy>();
            b.view = Acquire();
            string sp = b.sprite ?? (b.enemy ? (b.size >= 1.6f ? "enemyBig" : "enemy") : "player");
            b.view.sr.sprite = SpriteLib.Frame("bullet_" + sp);
            b.view.sr.color = b.enemy ? Color.white : b.color;
            float s = b.enemy ? (sp == "enemyBig" ? b.size / 1.8f : b.size) : 1f;
            b.view.sr.transform.localScale = new Vector3(s, s, 1);
            b.view.glow.enabled = true;
            b.view.glow.color = b.enemy ? new Color(1f, 0.25f, 0.4f, 0.9f) : b.color * 0.55f;
            float gs = (b.enemy ? 1.4f : 0.9f) * b.size;
            b.view.glow.transform.localScale = new Vector3(gs, gs, 1);
            list.Add(b);
            Render(b);
            return b;
        }

        BulletView Acquire()
        {
            if (pool.Count > 0) { var v = pool.Pop(); v.go.SetActive(true); return v; }
            var go = new GameObject("bala");
            go.transform.SetParent(root, false);
            var sr = new GameObject("s").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(go.transform, false);
            sr.sharedMaterial = Materials.SpriteBright;
            var glow = new GameObject("g").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0, 0, 0.03f);
            glow.sprite = glowSprite;
            glow.sharedMaterial = Materials.Additive;
            return new BulletView { go = go, sr = sr, glow = glow };
        }

        void Release(Bullet b)
        {
            if (b.view == null) return;
            b.view.go.SetActive(false);
            pool.Push(b.view);
            b.view = null;
        }

        public void ClearEnemy(bool fx = true)
        {
            foreach (var b in list)
                if (b.enemy && !b.dead)
                {
                    b.dead = true;
                    if (fx) G.Fx.Sparks(b.pos, 2, new Color(1f, 0.55f, 0.7f));
                }
        }

        public void ClearEnemyNear(Vector2 p, float radius)
        {
            foreach (var b in list) if (b.enemy && !b.dead && (b.pos - p).sqrMagnitude < radius * radius) b.dead = true;
        }

        public void Update(float dt)
        {
            var players = G.Players;
            foreach (var b in list)
            {
                if (b.dead) continue;
                b.age += dt;
                b.life -= dt;
                if (b.accel != 0) b.vel *= 1 + b.accel * dt;
                if (b.curve != 0) { float a = b.curve * dt; b.vel = new Vector2(b.vel.x * Mathf.Cos(a) - b.vel.y * Mathf.Sin(a), b.vel.x * Mathf.Sin(a) + b.vel.y * Mathf.Cos(a)); }
                if (b.homing > 0 && !b.enemy)
                {
                    var t = G.NearestEnemy(b.pos, 9);
                    if (t != null)
                    {
                        float sp = b.vel.magnitude, want = MathX.Angle(b.pos, t.pos), cur = MathX.Angle(b.vel);
                        float d = MathX.WrapAngle(want - cur);
                        float na = cur + Mathf.Clamp(d, -b.homing * dt, b.homing * dt);
                        b.vel = MathX.Dir(na) * sp;
                    }
                }
                if (b.life <= 0) { Kill(b, false); continue; }

                int steps = Mathf.Max(1, Mathf.CeilToInt(b.vel.magnitude * dt / 0.3f));
                float sdt = dt / steps;
                for (int s = 0; s < steps && !b.dead; s++)
                {
                    var np = b.pos + b.vel * sdt;
                    if (G.BlocksBullet(np))
                    {
                        int tx = Mathf.FloorToInt(np.x), ty = Mathf.FloorToInt(np.y);
                        if (G.Builder.IsCrate(tx, ty)) G.HitCrate(tx, ty, b.enemy ? 4 : b.dmg);
                        if (b.bounce > 0)
                        {
                            b.bounce--;
                            bool bx = G.BlocksBullet(new Vector2(np.x, b.pos.y)), by = G.BlocksBullet(new Vector2(b.pos.x, np.y));
                            if (bx) b.vel.x = -b.vel.x;
                            if (by) b.vel.y = -b.vel.y;
                            if (!bx && !by) b.vel = -b.vel;
                            G.Fx.Sparks(b.pos, 3, b.color);
                        }
                        else Kill(b, true);
                        break;
                    }
                    b.pos = np;

                    if (b.enemy)
                    {
                        foreach (var p in players)
                            if (p.Vulnerable() && (b.pos - p.pos).sqrMagnitude < (b.r + p.hitR) * (b.r + p.hitR))
                            {
                                p.Hurt();
                                Kill(b, true);
                                break;
                            }
                    }
                    else
                    {
                        foreach (var e in G.Enemies)
                        {
                            if (!e.Active || e.dead || b.hit.Contains(e)) continue;
                            if ((b.pos - e.pos).sqrMagnitude < (b.r + e.r) * (b.r + e.r))
                            {
                                e.Damage(b.dmg, b.vel);
                                b.hit.Add(e);
                                if (b.explode > 0) { Kill(b, true); break; }
                                if (b.pierce > 0) b.pierce--; else { Kill(b, true); break; }
                            }
                        }
                    }
                }
                if (!b.dead) Render(b);
            }
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].dead) { Release(list[i]); list.RemoveAt(i); }
        }

        void Render(Bullet b)
        {
            var v = b.view;
            const float px = 1f / 16f;
            float x = Mathf.Round(b.pos.x / px) * px, y = Mathf.Round(b.pos.y / px) * px;
            v.go.transform.position = MathX.ToWorld(new Vector2(x, y), Height);
            // sprites direcionais (raio, flecha, foguete) apontam para a velocidade
            float ang = (b.sprite == "laser" || b.sprite == "bolt" || b.sprite == "rocket") ? -MathX.Angle(b.vel) * Mathf.Rad2Deg : 0;
            if (b.sprite == "star") ang = b.age * 600f;
            v.go.transform.rotation = Quaternion.Euler(0, PixelCamera.BillboardYaw, 0) * Quaternion.Euler(0, 0, ang);
        }

        public void Kill(Bullet b, bool impact)
        {
            if (b.dead) return;
            b.dead = true;
            if (b.explode > 0) G.Explosion(b.pos, b.explode, b.dmg);
            else if (impact) G.Fx.Sparks(b.pos, b.enemy ? 3 : 4, b.enemy ? new Color(1f, 0.45f, 0.6f) : b.color);
        }

        public void Reset()
        {
            foreach (var b in list) Release(b);
            list.Clear();
        }
    }
}
