using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Câmera: terceira pessoa (orbita atrás do ombro, desvia do terreno e das árvores)
    /// ou primeira pessoa (nos olhos). V alterna com transição suave; a roda do mouse aproxima.
    /// </summary>
    public class CameraRig
    {
        public readonly Camera cam;
        public float Yaw, Pitch = 12;
        public bool FirstPerson;
        public float Blend;                 // 0 = 3ª pessoa, 1 = 1ª pessoa (suavizado)
        float dist = 5.5f, shake;
        Vector3 smoothPivot;
        const float EyeHeight = 1.62f;

        public CameraRig()
        {
            var go = new GameObject("Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 420;
            cam.fieldOfView = 62;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.depthTextureMode |= DepthTextureMode.Depth;     // a água usa a profundidade (cor e espuma na margem)
            go.AddComponent<AudioListener>();
        }

        public void Shake(float s) => shake = Mathf.Max(shake, s);

        public void Look(Vector2 mouse, float scroll)
        {
            Yaw += mouse.x;
            Pitch = Mathf.Clamp(Pitch - mouse.y, -70, 80);
            dist = Mathf.Clamp(dist - scroll * 2f, 2.5f, 9f);
        }

        public void Update(Transform target, float dt)
        {
            Blend = Mathf.MoveTowards(Blend, FirstPerson ? 1 : 0, dt * 2.8f);
            float b = Blend * Blend * (3 - 2 * Blend);
            var rot = Quaternion.Euler(Pitch, Yaw, 0);

            var pivotTarget = target.position + Vector3.up * 1.55f;
            smoothPivot = smoothPivot == Vector3.zero ? pivotTarget : Vector3.Lerp(smoothPivot, pivotTarget, 1 - Mathf.Exp(-dt * 14));

            // 3ª pessoa: atrás e um pouco à direita (por cima do ombro)
            var shoulder = rot * new Vector3(0.55f, 0.15f, 0);
            var desired = smoothPivot + shoulder - rot * Vector3.forward * dist;
            var from = smoothPivot + shoulder * 0.5f;
            var dir = desired - from;
            int mask = (1 << Layers.Ground) | (1 << Layers.Gather);
            if (Physics.SphereCast(from, 0.25f, dir.normalized, out var hit, dir.magnitude, mask, QueryTriggerInteraction.Ignore))
                desired = from + dir.normalized * Mathf.Max(0.6f, hit.distance);

            var eye = target.position + Vector3.up * EyeHeight + rot * Vector3.forward * 0.12f;
            var pos = Vector3.Lerp(desired, eye, b);
            if (shake > 0)
            {
                pos += Random.insideUnitSphere * shake * 0.15f;
                shake = Mathf.MoveTowards(shake, 0, dt * 3);
            }
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = Mathf.Lerp(62, 74, b);
        }
    }
}
