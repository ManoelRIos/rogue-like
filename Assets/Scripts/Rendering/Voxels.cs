using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos
{
    // ----- formato de Assets/Resources/Voxels/*.json (gerado por Tools/ArtGen/models3d.py)
    [Serializable] public class VoxPartData { public string name; public string parent; public float[] pivot; public int[] vox; public int[] glow; }
    [Serializable] public class VoxModelData { public string[] palette; public VoxPartData[] parts; }

    /// <summary>Carrega modelos voxel e gera as malhas (só faces visíveis, sombreamento por face nas cores).</summary>
    public static class VoxelLib
    {
        public const float Scale = 1f / 16f;   // 1 voxel = 1 pixel da arte
        public class PartMesh { public string name, parent; public Vector3 pivot; public Mesh mesh; }
        static readonly Dictionary<string, List<PartMesh>> cache = new();
        static readonly Dictionary<string, bool> missing = new();
        public static Material Lit, Bright;

        static void InitMaterials()
        {
            if (Lit != null) return;
            var sh = Resources.Load<Shader>("Shaders/PixelLit");
            Lit = new Material(sh) { mainTexture = Texture2D.whiteTexture, name = "EcosVoxel" };
            Bright = new Material(sh) { mainTexture = Texture2D.whiteTexture, name = "EcosVoxelBright" };
            Bright.SetFloat("_Lit", 0);
        }

        public static bool Has(string model)
        {
            if (cache.ContainsKey(model)) return true;
            if (missing.ContainsKey(model)) return false;
            return Get(model) != null;
        }

        public static List<PartMesh> Get(string model)
        {
            if (cache.TryGetValue(model, out var c)) return c;
            if (missing.ContainsKey(model)) return null;
            InitMaterials();
            var ta = Resources.Load<TextAsset>("Voxels/" + model);
            if (ta == null) { missing[model] = true; return null; }
            var data = JsonUtility.FromJson<VoxModelData>(ta.text);
            var pal = new Color[data.palette.Length];
            for (int i = 0; i < pal.Length; i++) ColorUtility.TryParseHtmlString(data.palette[i], out pal[i]);
            var list = new List<PartMesh>();
            foreach (var p in data.parts)
            {
                var pivot = new Vector3(p.pivot[0], p.pivot[1], p.pivot[2]);
                list.Add(new PartMesh { name = p.name, parent = p.parent, pivot = pivot, mesh = Build(p, pal, pivot, model + "_" + p.name) });
            }
            cache[model] = list;
            return list;
        }

        // faces: direção, 4 cantos (unidades de voxel) e o fator de sombra
        static readonly Vector3Int[] Dirs = { new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1) };
        static readonly float[] Shade = { 0.82f, 0.72f, 1.0f, 0.5f, 0.9f, 0.62f };
        static readonly Vector3[][] Corners =
        {
            new[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) },
            new[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) },
            new[] { new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) },
            new[] { new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1) },
            new[] { new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1) },
            new[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) },
        };

        static Mesh Build(VoxPartData p, Color[] pal, Vector3 pivot, string name)
        {
            var occ = new HashSet<Vector3Int>();
            void Collect(int[] a) { if (a == null) return; for (int i = 0; i + 3 < a.Length; i += 4) occ.Add(new Vector3Int(a[i], a[i + 1], a[i + 2])); }
            Collect(p.vox); Collect(p.glow);
            var verts = new List<Vector3>(); var cols = new List<Color>(); var uvs = new List<Vector2>();
            var tris = new[] { new List<int>(), new List<int>() };
            void Emit(int[] a, int sub)
            {
                if (a == null) return;
                for (int i = 0; i + 3 < a.Length; i += 4)
                {
                    var v = new Vector3Int(a[i], a[i + 1], a[i + 2]);
                    var baseC = pal[Mathf.Clamp(a[i + 3], 0, pal.Length - 1)];
                    for (int f = 0; f < 6; f++)
                    {
                        if (occ.Contains(v + Dirs[f])) continue;
                        int s = verts.Count;
                        float k = sub == 1 ? 1f : Shade[f];
                        var c = new Color(baseC.r * k, baseC.g * k, baseC.b * k, 1);
                        foreach (var cn in Corners[f])
                        {
                            verts.Add((new Vector3(v.x, v.y, v.z) + cn - pivot) * Scale);
                            cols.Add(c);
                            uvs.Add(new Vector2(0.5f, 0.5f));
                        }
                        tris[sub].Add(s); tris[sub].Add(s + 1); tris[sub].Add(s + 2);
                        tris[sub].Add(s); tris[sub].Add(s + 2); tris[sub].Add(s + 3);
                    }
                }
            }
            Emit(p.vox, 0); Emit(p.glow, 1);
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(tris[0], 0); mesh.SetTriangles(tris[1], 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Cria um GameObject com as malhas das partes (hierarquia pelos pivôs).</summary>
        public static Dictionary<string, Transform> Instantiate(string model, Transform root)
        {
            var parts = Get(model);
            var map = new Dictionary<string, Transform>();
            if (parts == null) return map;
            var pivots = new Dictionary<string, Vector3>();
            foreach (var p in parts)
            {
                var go = new GameObject(p.name);
                go.AddComponent<MeshFilter>().sharedMesh = p.mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { Lit, Bright };
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                map[p.name] = go.transform;
                pivots[p.name] = p.pivot;
            }
            foreach (var p in parts)
            {
                var t = map[p.name];
                bool hasParent = !string.IsNullOrEmpty(p.parent) && map.ContainsKey(p.parent);
                t.SetParent(hasParent ? map[p.parent] : root, false);
                var parentPivot = hasParent ? pivots[p.parent] : Vector3.zero;
                t.localPosition = (p.pivot - parentPivot) * Scale;
            }
            return map;
        }
    }

    /// <summary>Pose desejada de um modelo neste quadro (o rig interpola as rotações).</summary>
    public struct RigPose
    {
        public float walkPhase, walk;       // ciclo e intensidade da caminhada
        public float tuck;                  // 0..1 encolhe as pernas (salto)
        public float aim;                   // 0..1 braço direito apontando para frente
        public float armsUp, armsForward;   // chefe: braços para cima / para frente
        public float sway;                  // balanço lateral (túnicas)
        public float death;                 // 0..1 progresso da morte
        public int deathStyle;              // 0 cai de costas, 1 desaba, 2 derrete
        public bool phase2;
        public float kick;
    }

    /// <summary>Modelo voxel articulado e animado por código (sem ossos).</summary>
    public class VoxelRig
    {
        public readonly GameObject Root;
        readonly Transform body;
        readonly Dictionary<string, Transform> parts;
        readonly Dictionary<string, Vector3> basePos = new();
        readonly List<Renderer> renderers = new();
        readonly MaterialPropertyBlock mpb = new();
        Transform gun;
        float fade = 1, flash;
        public float Yaw;

        public VoxelRig(string model, Transform parent = null)
        {
            Root = new GameObject("voxel_" + model);
            if (parent) Root.transform.SetParent(parent, false);
            body = new GameObject("pose").transform;
            body.SetParent(Root.transform, false);
            parts = VoxelLib.Instantiate(model, body);
            foreach (var kv in parts) basePos[kv.Key] = kv.Value.localPosition;
            renderers.AddRange(Root.GetComponentsInChildren<Renderer>(true));
        }

        public bool Valid => parts.Count > 0;
        public Transform Part(string n) => parts.TryGetValue(n, out var t) ? t : null;

        /// <summary>Arma voxel presa na mão direita (aponta para frente do personagem).</summary>
        public void SetGun(string weaponModel, Vector3 localPos)
        {
            if (gun) UnityEngine.Object.Destroy(gun.gameObject);
            gun = null;
            if (!VoxelLib.Has(weaponModel)) return;
            gun = new GameObject("arma").transform;
            gun.SetParent(body, false);
            VoxelLib.Instantiate(weaponModel, gun);
            gun.localPosition = localPos;
            gun.localRotation = Quaternion.Euler(0, -90, 0);   // +x do sprite → +z (frente)
            renderers.Clear();
            renderers.AddRange(Root.GetComponentsInChildren<Renderer>(true));
            ApplyBlock();
        }

        public bool Visible { get => Root.activeSelf; set { if (Root.activeSelf != value) Root.SetActive(value); } }

        public void SetFade(float f) { f = Mathf.Clamp01(f); if (Mathf.Approximately(f, fade)) return; fade = f; ApplyBlock(); }
        public void SetFlash(float f) { if (Mathf.Approximately(f, flash)) return; flash = f; ApplyBlock(); }

        void ApplyBlock()
        {
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Fade", fade);
                mpb.SetFloat("_Flash", flash);
                r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>Posição de jogo, altura e direção (ângulo de jogo, y para baixo) para onde o modelo olha.</summary>
        public void Place(Vector2 p, float h, float facingAngle)
        {
            Root.transform.position = MathX.ToWorld(p, h);
            // frente do modelo (+z) apontando para o ângulo de jogo
            float yaw = Mathf.Atan2(Mathf.Cos(facingAngle), -Mathf.Sin(facingAngle)) * Mathf.Rad2Deg;
            Yaw = Mathf.LerpAngle(Yaw, yaw, 0.35f);
            Root.transform.rotation = Quaternion.Euler(0, Yaw, 0);
        }

        static Quaternion R(float x, float y = 0, float z = 0) => Quaternion.Euler(x, y, z);

        void Rot(string n, Quaternion q, float lerp = 0.4f)
        {
            var t = Part(n);
            if (t) t.localRotation = Quaternion.Slerp(t.localRotation, q, lerp);
        }

        public void Pose(RigPose p)
        {
            float s = Mathf.Sin(p.walkPhase) * p.walk;
            float bob = Mathf.Abs(Mathf.Cos(p.walkPhase)) * p.walk * 0.05f;
            // pernas e braços balançando; no salto as pernas encolhem
            Rot("pernaE", R(s * 38 - p.tuck * 70));
            Rot("pernaD", R(-s * 38 - p.tuck * 60));
            Rot("bracoE", R(-s * 30 - p.tuck * 40 - p.armsForward * 75, 0, p.armsUp * -120));
            Rot("bracoD", R(s * 30 * (1 - p.aim) - p.aim * 80 - p.armsForward * 75 + p.kick * 12, 0, p.armsUp * 120));
            Rot("corpo", R(p.walk * 6 + p.tuck * 18, 0, p.sway * 4));
            Rot("cabeca", R(-p.walk * 4 - p.tuck * 10));
            var c2 = Part("cabeca2");
            var c1 = Part("cabeca");
            if (c2 && c1) { c2.gameObject.SetActive(p.phase2); c1.gameObject.SetActive(!p.phase2); }

            // morte
            var pos = new Vector3(0, bob, 0);
            var rot = Quaternion.identity;
            var scale = Vector3.one;
            float d = Mathf.Clamp01(p.death);
            if (d > 0)
            {
                switch (p.deathStyle)
                {
                    case 0:   // cai de costas, chapéu voa
                        rot = R(-90 * Mathf.SmoothStep(0, 1, Mathf.Min(1, d * 1.6f)));
                        pos.y = 0.15f * d;
                        var hat = Part("chapeu");
                        if (hat) hat.localPosition = basePos["chapeu"] + new Vector3(0.4f * d, 0.3f * Mathf.Sin(d * Mathf.PI), -0.8f * d);
                        break;
                    case 1:   // desaba (túnica vira um monte)
                        scale = new Vector3(1 + d * 0.3f, Mathf.Lerp(1, 0.25f, d), 1 + d * 0.3f);
                        rot = R(-20 * d, 0, 12 * d);
                        break;
                    case 2:   // derrete no chão
                        scale = new Vector3(1 + d * 0.5f, Mathf.Lerp(1, 0.08f, d), 1 + d * 0.5f);
                        break;
                }
            }
            body.localPosition = pos;
            body.localRotation = rot;
            body.localScale = scale;
            if (gun) gun.gameObject.SetActive(d <= 0);
        }

        public void Destroy() { if (Root) UnityEngine.Object.Destroy(Root); }
    }
}
