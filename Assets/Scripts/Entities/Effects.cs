using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>Partículas e efeitos animados (faíscas, poeira, clarões, explosões).</summary>
    public class Effects
    {
        class P
        {
            public GameObject go; public SpriteRenderer sr;
            public Vector2 pos, vel; public float h, vh, grav, life, max, size;
            public Anim anim; public bool flat, additive, shrink = true; public float spin;
        }

        readonly List<P> live = new();
        readonly Stack<P> poolLit = new(), poolAdd = new();
        readonly Transform root;
        readonly Sprite dot, glow;

        public Effects()
        {
            root = new GameObject("Efeitos").transform;
            var wt = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            wt.SetPixel(0, 0, Color.white); wt.Apply();
            dot = Sprite.Create(wt, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16);
            glow = MakeGlow();
        }

        static Sprite MakeGlow()
        {
            var t = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16, 16)) / 16f;
                    float a = Mathf.Floor(Mathf.Clamp01(1 - d) * 4) / 4f;
                    t.SetPixel(x, y, new Color(a, a, a, a));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16);
        }

        P Get(bool additive)
        {
            var pool = additive ? poolAdd : poolLit;
            P p;
            if (pool.Count > 0) { p = pool.Pop(); p.go.SetActive(true); }
            else
            {
                p = new P { go = new GameObject("fx") };
                p.go.transform.SetParent(root, false);
                p.sr = p.go.AddComponent<SpriteRenderer>();
                p.sr.sharedMaterial = additive ? Materials.Additive : Materials.SpriteBright;
                p.additive = additive;
            }
            p.anim = null; p.flat = false; p.spin = 0; p.shrink = true; p.sr.flipX = false;
            live.Add(p);
            if (live.Count > 1400) Kill(0);
            return p;
        }

        void Kill(int i)
        {
            var p = live[i];
            live.RemoveAt(i);
            p.go.SetActive(false);
            (p.additive ? poolAdd : poolLit).Push(p);
        }

        public void Particle(Vector2 pos, float h, Vector2 vel, float vh, Color c, float life, float size, float grav = 9f)
        {
            var p = Get(false);
            p.sr.sprite = dot; p.sr.color = c;
            p.pos = pos; p.vel = vel; p.h = h; p.vh = vh; p.grav = grav; p.life = p.max = life; p.size = size;
        }

        public void Burst(Vector2 pos, float h, int n, Color c, float speed = 3, float life = 0.4f, float size = 1.5f, float up = 2)
        {
            for (int i = 0; i < n; i++)
            {
                var v = MathX.Dir(Random.value * MathX.TAU) * speed * Random.Range(0.4f, 1.2f);
                Particle(pos, h, v, Random.value * up + 0.5f, c, life * Random.Range(0.6f, 1.2f), size * Random.Range(0.7f, 1.4f));
            }
        }

        public void Sparks(Vector2 pos, int n, Color c) => Burst(pos, BulletSystem.Height, n, c, 2.4f, 0.25f, 1.2f, 1.5f);

        public void Dust(Vector2 pos, int n)
        {
            var an = SpriteLib.Get("fx_dust");
            for (int i = 0; i < n; i++)
            {
                var p = Get(false);
                p.anim = an; p.sr.sprite = an[0]; p.sr.color = new Color(0.75f, 0.68f, 0.6f);
                p.pos = pos + Random.insideUnitCircle * 0.25f; p.vel = Random.insideUnitCircle * 0.8f;
                p.h = 0.08f; p.vh = Random.Range(0.2f, 0.7f); p.grav = 0; p.life = p.max = an.Duration; p.size = 1; p.shrink = false;
            }
        }

        public void Play(string sheet, Vector2 pos, float h, float scale = 1, bool flat = false, Color? color = null, float speed = 1)
        {
            var an = SpriteLib.Get(sheet);
            if (an == null) return;
            var p = Get(false);
            p.anim = an; p.sr.sprite = an[0]; p.sr.color = color ?? Color.white;
            p.pos = pos; p.vel = Vector2.zero; p.h = h; p.vh = 0; p.grav = 0;
            p.life = p.max = an.Duration / speed; p.size = scale; p.flat = flat; p.shrink = false;
        }

        public void Glow(Vector2 pos, float h, float size, Color c, float life)
        {
            var p = Get(true);
            p.sr.sprite = glow; p.sr.color = c;
            p.pos = pos; p.vel = Vector2.zero; p.h = h; p.vh = 0; p.grav = 0; p.life = p.max = life; p.size = size;
        }

        public void Muzzle(Vector2 pos, float size, Color c)
        {
            Play("fx_muzzle", pos, BulletSystem.Height, 0.9f + size * 0.3f, false, Color.Lerp(c, Color.white, 0.5f), 1.6f);
            Glow(pos, BulletSystem.Height, 1.2f + size, c * 0.8f, 0.08f);
        }

        /// <summary>Cápsula ejetada da arma: quica no chão e fica um tempo lá.</summary>
        public void Casing(Vector2 pos, float aim)
        {
            var sp = SpriteLib.Frame("bullet_casing");
            if (sp == null) return;
            var p = Get(false);
            p.sr.sprite = sp; p.sr.color = Color.white;
            p.pos = pos; p.vel = MathX.Dir(aim + Mathf.PI * 0.5f + Random.Range(-0.5f, 0.5f)) * Random.Range(1.2f, 2.4f);
            p.h = 0.55f; p.vh = Random.Range(1.8f, 2.8f); p.grav = 12f; p.life = p.max = 6f; p.size = 1; p.shrink = false;
            p.spin = Random.Range(0, 360f);
        }

        /// <summary>Brasas e poeira flutuando no ar (atmosfera).</summary>
        public void Ambient(Vector2 center, float dt, Color ember)
        {
            if (Random.value < dt * 6f)
            {
                var pos = center + new Vector2(Random.Range(-14f, 14f), Random.Range(-8f, 9f));
                var p = Get(true);
                p.sr.sprite = glow; p.sr.color = ember;
                p.pos = pos; p.vel = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.25f, -0.05f));
                p.h = Random.Range(0.1f, 1.5f); p.vh = Random.Range(0.15f, 0.4f); p.grav = 0; p.life = p.max = Random.Range(2.5f, 5f); p.size = Random.Range(0.12f, 0.22f);
            }
            if (Random.value < dt * 4f)
            {
                var pos = center + new Vector2(Random.Range(-14f, 14f), Random.Range(-8f, 9f));
                Particle(pos, Random.Range(0.2f, 1.8f), new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.1f)), 0.05f,
                         new Color(0.45f, 0.4f, 0.42f), Random.Range(3f, 6f), 1f, 0);
            }
        }

        public void Explosion(Vector2 pos, float r)
        {
            Play("fx_explosion", pos, 0.5f, r * 0.7f);
            Glow(pos, 0.5f, r * 3f, new Color(1f, 0.6f, 0.25f), 0.3f);
            Burst(pos, 0.4f, 22, new Color(1f, 0.55f, 0.15f), 5, 0.5f, 1.8f, 3);
            Burst(pos, 0.4f, 10, new Color(0.35f, 0.32f, 0.32f), 2.5f, 0.8f, 2.4f, 2);
        }

        public void Update(float dt)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.life -= dt;
                if (p.life <= 0) { Kill(i); continue; }
                p.vh -= p.grav * dt;
                p.pos += p.vel * dt; p.h += p.vh * dt;
                if (p.h < 0.02f && p.grav > 0) { p.h = 0.02f; p.vh *= -0.35f; p.vel *= 0.55f; if (p.spin != 0) p.spin += 90; }
                if (p.spin != 0 && p.h > 0.03f) p.spin += dt * 900f;
                float k = p.shrink ? Mathf.Min(1, p.life / p.max * 2) : 1;
                if (!p.shrink && p.anim == null && !p.additive && p.life < 0.6f) k = p.life / 0.6f;
                if (p.anim != null) p.sr.sprite = p.anim.At(p.max - p.life);
                float s = p.size * k;
                p.go.transform.localScale = new Vector3(s, s, 1);
                p.go.transform.position = MathX.ToWorld(p.pos, p.h) + (p.additive ? new Vector3(0, 0, 0.05f) : Vector3.zero);
                p.go.transform.rotation = p.flat ? Quaternion.Euler(90, 0, 0) : PixelCamera.Billboard * Quaternion.Euler(0, 0, p.spin);
                if (p.additive) { var c = p.sr.color; c.a = p.life / p.max; p.sr.color = c; }
            }
        }

        public void Reset() { for (int i = live.Count - 1; i >= 0; i--) Kill(i); }
    }
}
