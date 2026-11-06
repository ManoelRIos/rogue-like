using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Visual de uma entidade: sprite em pé (billboard) + sombra no chão.
    /// A altura (h) separa o sprite da sombra, dando a noção de pulo/flutuação.
    /// </summary>
    public class SpriteView
    {
        public readonly GameObject Root;
        public readonly Transform Body;          // gira para a câmera e carrega o sprite
        public readonly SpriteRenderer Sr;
        public readonly SpriteRenderer Shadow;
        readonly MaterialPropertyBlock mpb = new();
        float flash;
        Color tint = Color.white;

        public SpriteView(string name, Material mat = null, float shadowSize = 0.8f, Transform parent = null)
        {
            Root = new GameObject(name);
            if (parent) Root.transform.SetParent(parent, false);
            Body = new GameObject("body").transform;
            Body.SetParent(Root.transform, false);
            Sr = new GameObject("sprite").AddComponent<SpriteRenderer>();
            Sr.transform.SetParent(Body, false);
            Sr.sharedMaterial = mat ? mat : Materials.Sprite;
            if (shadowSize > 0)
            {
                Shadow = new GameObject("shadow").AddComponent<SpriteRenderer>();
                Shadow.transform.SetParent(Root.transform, false);
                Shadow.sprite = SpriteLib.Frame("shadow");
                Shadow.sharedMaterial = Materials.Blend;
                Shadow.color = new Color(0, 0, 0, 0.45f);
                Shadow.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Shadow.transform.localPosition = new Vector3(0, 0.01f, 0);
                SetShadowSize(shadowSize);
            }
        }

        public void SetShadowSize(float s)
        {
            if (Shadow) Shadow.transform.localScale = new Vector3(s, s, 1);
        }

        /// <summary>Posição de jogo + altura do sprite acima do chão.</summary>
        public void Place(Vector2 p, float h = 0)
        {
            const float px = 1f / SpriteLib.PPU;
            float x = Mathf.Round(p.x / px) * px, y = Mathf.Round(p.y / px) * px;
            Root.transform.position = MathX.ToWorld(new Vector2(x, y));
            Body.localPosition = new Vector3(0, Mathf.Round(h / px) * px, 0);
            Body.localRotation = Quaternion.Euler(0, PixelCamera.BillboardYaw, 0);
        }

        public void SetSprite(Sprite s) { if (Sr.sprite != s) Sr.sprite = s; }
        public void Flip(bool left) => Sr.flipX = left;
        public bool Visible { get => Root.activeSelf; set { if (Root.activeSelf != value) Root.SetActive(value); } }
        public void SpriteVisible(bool v) => Sr.enabled = v;

        public void SetFlash(float f)
        {
            if (Mathf.Approximately(f, flash)) return;
            flash = f;
            Sr.GetPropertyBlock(mpb);
            mpb.SetFloat("_Flash", f);
            Sr.SetPropertyBlock(mpb);
        }

        public void SetTint(Color c)
        {
            if (c == tint) return;
            tint = c;
            Sr.color = c;
        }

        public void Destroy() { if (Root) Object.Destroy(Root); }
    }
}
