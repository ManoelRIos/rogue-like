using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ecos.EditorTools
{
    /// <summary>
    /// Configura o projeto na primeira abertura:
    ///  - importa os PNGs de Resources/Art como pixel art (Point, sem compressão, sem mipmaps);
    ///  - cria Assets/Scenes/Main.unity e a coloca no Build Settings;
    ///  - ajusta resolução padrão e nome do produto.
    /// Pode ser refeito pelo menu Ecos > Configurar projeto.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string ArtPath = "Assets/Resources/Art";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!SessionState.GetBool("EcosSetupDone", false))
                {
                    SessionState.SetBool("EcosSetupDone", true);
                    Run(false);
                }
            };
        }

        [MenuItem("Ecos/Configurar projeto")]
        public static void RunMenu() => Run(true);

        static void Run(bool verbose)
        {
            int fixedCount = FixTextures();
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                Debug.Log("Ecos: cena criada em " + ScenePath + ". Aperte Play para jogar.");
            }
            var scenes = EditorBuildSettings.scenes;
            bool has = false;
            foreach (var s in scenes) if (s.path == ScenePath) has = true;
            if (!has)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes) { new EditorBuildSettingsScene(ScenePath, true) };
                EditorBuildSettings.scenes = list.ToArray();
            }
            PlayerSettings.productName = "Ecos do Gatilho";
            PlayerSettings.companyName = "ManoelRIos";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            if (SceneManagerSetupNeeded()) EditorSceneManager.OpenScene(ScenePath);
            if (verbose || fixedCount > 0) Debug.Log($"Ecos: projeto configurado ({fixedCount} texturas ajustadas).");
        }

        static bool SceneManagerSetupNeeded()
        {
            var active = EditorSceneManager.GetActiveScene();
            return string.IsNullOrEmpty(active.path) && !active.isDirty;
        }

        public static int FixTextures()
        {
            int n = 0;
            if (!AssetDatabase.IsValidFolder(ArtPath)) return 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti && !IsPixelArt(ti))
                {
                    Apply(ti);
                    ti.SaveAndReimport();
                    n++;
                }
            }
            return n;
        }

        internal static bool IsPixelArt(TextureImporter ti) =>
            ti.textureType == TextureImporterType.Sprite && ti.filterMode == FilterMode.Point && !ti.mipmapEnabled
            && ti.textureCompression == TextureImporterCompression.Uncompressed && ti.npotScale == TextureImporterNPOTScale.None;

        internal static void Apply(TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
        }
    }

    /// <summary>Garante que PNGs novos em Resources/Art já entrem como pixel art.</summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art")) return;
            ProjectSetup.Apply((TextureImporter)assetImporter);
        }
    }
}
