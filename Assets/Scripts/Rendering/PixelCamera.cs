using UnityEngine;

namespace Ecos
{
    public enum CameraMode { TwoPointFive, ThreeD, FirstPerson }

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

        // ------------------------------------------------------------------ troca 2.5D ↔ 3D
        public const float TransitionTime = 1.1f;
        public static Quaternion Billboard = Quaternion.identity;   // rotação dos sprites (encara a câmera)
        public static float BillboardPitch;
        float blend;            // 0 = 2.5D, 1 = 3D (animado)
        float blendTarget;
        float fps, fpsTarget;   // 0 = terceira pessoa, 1 = primeira pessoa (animado)
        public float Blend => blend;
        public float FpsBlend => fps;
        public static float FirstPersonBlend;            // usado por balas e sprites
        public bool FirstPersonActive => fps > 0.5f;
        public bool Transitioning => !Mathf.Approximately(blend, blendTarget) || !Mathf.Approximately(fps, fpsTarget);
        public float EyeHeight = 1.45f;   // altura dos olhos (o jogo soma o salto)
        public bool AllowLook;            // mouse controla a visão (só jogando)
        public float LookYaw, LookPitch;  // visão em primeira pessoa (graus)
        public float MouseSensitivity = 2.2f;

        /// <summary>Alterna a visão: 2.5D → 3D (terceira pessoa) → primeira pessoa → 2.5D, sempre animado.</summary>
        public void ToggleMode()
        {
            if (Mode == CameraMode.TwoPointFive) { Mode = CameraMode.ThreeD; blendTarget = 1; fpsTarget = 0; }
            else if (Mode == CameraMode.ThreeD) { Mode = CameraMode.FirstPerson; blendTarget = 1; fpsTarget = 1; LookYaw = yaw; LookPitch = 8; }
            else { Mode = CameraMode.TwoPointFive; blendTarget = 0; fpsTarget = 0; yaw = LookYaw; }
        }

        static float Smooth(float x) => x * x * x * (x * (x * 6 - 15) + 10);   // smootherstep

        void LateUpdate()
        {
            EnsureRT();
            float dt = Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.V)) ToggleMode();
            // ordem das transições: subir 2.5D→3D antes de entrar na cabeça; sair da cabeça antes de voltar ao 2.5D
            if (fpsTarget < fps || blend >= 0.999f) fps = Mathf.MoveTowards(fps, fpsTarget, dt / 0.8f);
            if (fps <= 0.001f || blendTarget > blend) blend = Mathf.MoveTowards(blend, blendTarget, dt / TransitionTime);
            float e = Smooth(blend);
            float f1 = Smooth(fps);
            FirstPersonBlend = f1;

            // olhar com o mouse na primeira pessoa
            if (fps > 0.5f && AllowLook)
            {
                LookYaw += Input.GetAxis("Mouse X") * MouseSensitivity;
                LookPitch = Mathf.Clamp(LookPitch - Input.GetAxis("Mouse Y") * MouseSensitivity, -35f, 40f);
                yaw = LookYaw;
            }

            // giro da câmera no 3D (Z / C)
            if (blendTarget > 0.5f && fpsTarget < 0.5f)
            {
                if (Input.GetKey(KeyCode.Z)) yaw -= 100 * dt;
                if (Input.GetKey(KeyCode.C)) yaw += 100 * dt;
            }
            else yaw = Mathf.MoveTowardsAngle(yaw, 0, 180 * dt * 2);
            float curYaw = Mathf.LerpAngle(0, yaw, e);

            var target = Focus + Lead;
            if (Bounds.HasValue)
            {
                // câmera presa na sala no 2.5D (vai soltando conforme entra no 3D)
                var b = Bounds.Value;
                float hw = RtW / SpriteLib.PPU / 2f, hh = RtH / SpriteLib.PPU / 2f;
                var cl = target;
                cl.x = b.width <= hw * 2 ? b.center.x : Mathf.Clamp(cl.x, b.xMin + hw, b.xMax - hw);
                cl.y = b.height <= hh * 2 ? b.center.y : Mathf.Clamp(cl.y, b.yMin + hh, b.yMax - hh);
                target = Vector2.Lerp(cl, Focus + Lead * 0.4f, e);
            }
            cur = Vector2.Lerp(cur, target, 1f - Mathf.Exp(-dt * 8f));
            Vector2 sh = shake > 0 ? Random.insideUnitCircle * shake * 0.3f : Vector2.zero;
            shake = Mathf.Max(0, shake - dt * 2.5f);

            float aspect = RtW / (float)RtH;
            if (e <= 0.0001f)
            {
                // ---- 2.5D puro: ortográfica com projeção cabinet, alinhada aos pixels
                Cam.orthographic = true;
                float px = 1f / SpriteLib.PPU;
                var c = cur + sh;
                c.x = Mathf.Round(c.x / px) * px;
                c.y = Mathf.Round(c.y / px) * px;
                var rot = Quaternion.Euler(45f, 0, 0);
                transform.rotation = rot;
                transform.position = MathX.ToWorld(c) + rot * new Vector3(0, 0, -60f);
                float w = RtW / SpriteLib.PPU, h = RtH / SpriteLib.PPU;
                Cam.projectionMatrix = Matrix4x4.Ortho(-w / 2, w / 2, -h / 2, h / 2, 1f, 200f) * Matrix4x4.Scale(new Vector3(1, SQRT2, 1));
            }
            else
            {
                // ---- transição / 3D: perspectiva cujo campo de visão abre de ~2° até 50°
                // enquanto a câmera se aproxima (dolly zoom). Em 2° ela é quase ortográfica,
                // então o começo da transição casa com o 2.5D sem "pulo".
                Cam.orthographic = false;
                float fov = 2f * Mathf.Pow(25f, e);
                float pitch = Mathf.Lerp(45f, 52f, e);
                float viewH = Mathf.Lerp(RtH / SpriteLib.PPU, 12.5f, e);
                float yScale = Mathf.Lerp(SQRT2, 1f, e);
                float dist = viewH / (2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
                var rot = Quaternion.Euler(pitch, curYaw, 0);
                transform.rotation = rot;
                transform.position = MathX.ToWorld(cur + sh, Mathf.Lerp(0, 0.6f, e)) + rot * new Vector3(0, 0, -dist);
                float near = Mathf.Max(0.1f, dist - 45f), far = dist + 60f;
                var pos3 = transform.position;
                if (f1 > 0.0001f)
                {
                    // ---- primeira pessoa: a câmera mergulha até os olhos do jogador
                    var eye = MathX.ToWorld(Focus, EyeHeight);
                    var rotF = Quaternion.Euler(LookPitch, LookYaw, 0);
                    transform.position = Vector3.Lerp(pos3, eye, f1);
                    transform.rotation = Quaternion.Slerp(rot, rotF, f1);
                    fov = Mathf.Lerp(fov, 72f, f1);
                    near = Mathf.Lerp(near, 0.03f, f1);
                    far = Mathf.Lerp(far, 60f, f1);
                    curYaw = Mathf.LerpAngle(curYaw, LookYaw, f1);
                }
                Cam.projectionMatrix = Matrix4x4.Perspective(fov, aspect, near, far) * Matrix4x4.Scale(new Vector3(1, yScale, 1));
            }
            BillboardYaw = curYaw;
            BillboardPitch = 28f * e * (1 - f1);   // em primeira pessoa os sprites ficam em pé
            Billboard = Quaternion.Euler(BillboardPitch, BillboardYaw, 0);
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
