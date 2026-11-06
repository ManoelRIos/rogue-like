using UnityEngine;

namespace Ecos
{
    public enum CameraMode { TwoPointFive, ThreeD }

    /// <summary>
    /// Câmera do jogo.
    ///  - 2.5D (padrão): ortográfica inclinada 45° com projeção "cabinet": o chão aparece
    ///    sem achatamento (1 tile = 16x16 px) e a face frontal das paredes aparece com a
    ///    altura real, como nos roguelikes de visão 3/4. As laterais das paredes ficam de perfil.
    ///  - 3D (preparada para o futuro): perspectiva atrás/acima do jogador. Troca com F2.
    /// Renderiza numa textura de baixa resolução, ampliada sem filtro (pixel perfect).
    /// </summary>
    public class PixelCamera : MonoBehaviour
    {
        public static PixelCamera I;
        public const int TargetHeight = 270;
        static readonly float SQRT2 = Mathf.Sqrt(2f);

        public Camera Cam { get; private set; }
        public Camera HudCam { get; private set; }
        public RenderTexture RT { get; private set; }
        public int Scale { get; private set; } = 1;
        public int RtW { get; private set; }
        public int RtH { get; private set; }
        public CameraMode Mode = CameraMode.TwoPointFive;
        public static float BillboardYaw;   // usado pelos sprites no modo 3D

        public Vector2 Focus;        // ponto seguido (coordenadas de jogo)
        public Vector2 Lead;         // deslocamento extra (mira)
        public float Zoom = 1f;      // 1 = normal; >1 mostra mais área
        public Rect? Bounds;         // limite da sala atual (coordenadas de jogo, y para baixo)
        Vector2 cur;
        float shake;
        float yaw;
        int lastW, lastH;
        Vector3 hudOrigin = new Vector3(5000, 5000, 0);
        public Color Background = new Color(0.04f, 0.02f, 0.05f);

        public static PixelCamera Create()
        {
            var go = new GameObject("PixelCamera");
            var pc = go.AddComponent<PixelCamera>();
            pc.Cam = go.AddComponent<Camera>();
            pc.Cam.clearFlags = CameraClearFlags.SolidColor;
            pc.Cam.backgroundColor = pc.Background;
            pc.Cam.cullingMask = ~(1 << Layers.UI);
            pc.Cam.depth = 0;
            pc.Cam.allowMSAA = false;
            pc.Cam.allowHDR = false;

            var hud = new GameObject("HudCamera");
            pc.HudCam = hud.AddComponent<Camera>();
            pc.HudCam.orthographic = true;
            pc.HudCam.clearFlags = CameraClearFlags.Depth;
            pc.HudCam.cullingMask = 1 << Layers.UI;
            pc.HudCam.depth = 1;
            pc.HudCam.nearClipPlane = 0.1f;
            pc.HudCam.farClipPlane = 100f;
            pc.HudCam.allowMSAA = false;
            hud.transform.position = pc.hudOrigin;

            // câmera "de apresentação": só limpa a tela; a imagem final é desenhada no OnGUI
            var present = new GameObject("PresentCamera").AddComponent<Camera>();
            present.clearFlags = CameraClearFlags.SolidColor;
            present.backgroundColor = Color.black;
            present.cullingMask = 0;
            present.depth = -10;
            I = pc;
            pc.EnsureRT();
            return pc;
        }

        public void AddShake(float a) => shake = Mathf.Min(1.2f, Mathf.Max(shake, a));
        public void SnapTo(Vector2 p) { Focus = p; cur = p; }

        void EnsureRT()
        {
            if (Screen.width == lastW && Screen.height == lastH && RT != null) return;
            lastW = Screen.width; lastH = Screen.height;
            Scale = Mathf.Max(1, Mathf.RoundToInt(Screen.height / (float)TargetHeight));
            RtW = Mathf.CeilToInt(Screen.width / (float)Scale);
            RtH = Mathf.CeilToInt(Screen.height / (float)Scale);
            if (RT != null) { RT.Release(); Destroy(RT); }
            RT = new RenderTexture(RtW, RtH, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "EcosPixelRT" };
            RT.Create();
            Cam.targetTexture = RT;
            HudCam.targetTexture = RT;
            HudCam.orthographicSize = RtH / 2f / SpriteLib.PPU;
        }

        void LateUpdate()
        {
            EnsureRT();
            float dt = Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.F2)) Mode = Mode == CameraMode.TwoPointFive ? CameraMode.ThreeD : CameraMode.TwoPointFive;

            var target = Focus + Lead;
            if (Bounds.HasValue && Mode == CameraMode.TwoPointFive)
            {
                // câmera presa na sala: não mostra o que há além das paredes
                var b = Bounds.Value;
                float hw = RtW / SpriteLib.PPU / 2f, hh = RtH / SpriteLib.PPU / 2f;
                target.x = b.width <= hw * 2 ? b.center.x : Mathf.Clamp(target.x, b.xMin + hw, b.xMax - hw);
                target.y = b.height <= hh * 2 ? b.center.y : Mathf.Clamp(target.y, b.yMin + hh, b.yMax - hh);
            }
            cur = Vector2.Lerp(cur, target, 1f - Mathf.Exp(-dt * 8f));
            Vector2 sh = shake > 0 ? Random.insideUnitCircle * shake * 0.3f : Vector2.zero;
            shake = Mathf.Max(0, shake - dt * 2.5f);

            if (Mode == CameraMode.TwoPointFive)
            {
                Cam.orthographic = true;
                // alinha à grade de pixels para não "tremer" a pixel art
                float px = 1f / SpriteLib.PPU;
                var c = cur + sh;
                c.x = Mathf.Round(c.x / px) * px;
                c.y = Mathf.Round(c.y / px) * px;
                var rot = Quaternion.Euler(45f, 0, 0);
                transform.rotation = rot;
                transform.position = MathX.ToWorld(c) + rot * new Vector3(0, 0, -60f);
                // sem zoom no 2.5D: manter 1 pixel de sprite = 1 pixel da textura
                float w = RtW / SpriteLib.PPU, h = RtH / SpriteLib.PPU;
                Cam.projectionMatrix = Matrix4x4.Ortho(-w / 2, w / 2, -h / 2, h / 2, 1f, 200f) * Matrix4x4.Scale(new Vector3(1, SQRT2, 1));
                BillboardYaw = 0;
                yaw = 0;
            }
            else
            {
                Cam.orthographic = false;
                Cam.ResetProjectionMatrix();
                Cam.fieldOfView = 50f;
                Cam.nearClipPlane = 0.1f; Cam.farClipPlane = 200f;
                // gira a câmera com Q/E segurando Shift (exemplo de controle 3D)
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (Input.GetKey(KeyCode.Q)) yaw -= 90 * dt;
                    if (Input.GetKey(KeyCode.E)) yaw += 90 * dt;
                }
                var rot = Quaternion.Euler(52f, yaw, 0);
                transform.rotation = rot;
                transform.position = MathX.ToWorld(cur + sh, 0.6f) + rot * new Vector3(0, 0, -13f * Zoom);
                BillboardYaw = yaw;
            }
        }

        /// <summary>Retângulo (em pixels de tela) onde a textura é desenhada.</summary>
        public Rect ScreenRect => new Rect(0, 0, RtW * Scale, RtH * Scale);

        /// <summary>Mouse em pixels da textura de baixa resolução (origem no canto superior esquerdo).</summary>
        public Vector2 MouseRT()
        {
            var m = Input.mousePosition;
            return new Vector2(m.x / Scale, (Screen.height - m.y) / Scale);
        }

        /// <summary>Ponto do mouse projetado no plano de altura h (coordenadas de jogo).</summary>
        public Vector2 MouseGame(float h = 0.5f)
        {
            var m = MouseRT();
            float u = m.x / RtW, v = 1f - m.y / RtH;
            var inv = (Cam.projectionMatrix * Cam.worldToCameraMatrix).inverse;
            Vector3 a = inv.MultiplyPoint(new Vector3(u * 2 - 1, v * 2 - 1, -1));
            Vector3 b = inv.MultiplyPoint(new Vector3(u * 2 - 1, v * 2 - 1, 1));
            var dir = b - a;
            if (Mathf.Abs(dir.y) < 1e-5f) return Focus;
            float t = (h - a.y) / dir.y;
            return MathX.FromWorld(a + dir * t);
        }

        /// <summary>Posição de jogo para pixels da textura (para a interface).</summary>
        public Vector2 GameToRT(Vector2 p, float h = 0)
        {
            var vp = Cam.WorldToViewportPoint(MathX.ToWorld(p, h));
            return new Vector2(vp.x * RtW, (1 - vp.y) * RtH);
        }

        /// <summary>Converte pixel da interface (origem no topo esquerdo) para posição local da câmera de HUD.</summary>
        public Vector3 HudPos(float px, float py, float depth = 10f)
        {
            return hudOrigin + new Vector3((px - RtW / 2f) / SpriteLib.PPU, (RtH / 2f - py) / SpriteLib.PPU, depth);
        }

        public void Present()
        {
            if (RT == null || Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);
            // origem do GUI é no topo; a textura cobre a tela inteira (pode sobrar até Scale-1 px)
            GUI.DrawTexture(new Rect(0, 0, RtW * Scale, RtH * Scale), RT, ScaleMode.StretchToFill, false);
        }
    }

    public static class Layers
    {
        public const int Default = 0;
        public const int UI = 5;
    }
}
