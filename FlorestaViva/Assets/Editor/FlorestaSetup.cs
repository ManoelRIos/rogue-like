#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Floresta.EditorTools
{
    /// <summary>
    /// Configura o projeto na primeira abertura: cena, build settings, nomes das camadas,
    /// espaço de cor e sombras. Também disponível no menu Floresta → Configurar projeto.
    /// </summary>
    [InitializeOnLoad]
    public static class FlorestaSetup
    {
        const string ScenePath = "Assets/Scenes/Floresta.unity";
        const string Flag = "FlorestaViva.SetupDone.v1";

        static FlorestaSetup()
        {
            if (EditorPrefs.GetBool(Flag + Application.dataPath, false)) return;
            EditorApplication.delayCall += () => { Setup(); EditorPrefs.SetBool(Flag + Application.dataPath, true); };
        }

        [MenuItem("Floresta/Configurar projeto")]
        public static void Setup()
        {
            PlayerSettings.productName = "Floresta Viva";
            PlayerSettings.companyName = "ManoelRIos";
            // cores dos vértices foram pensadas em Gamma (como os hex do código)
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            // camadas usadas pelo código (World.Layers)
            var tmAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tmAssets != null && tmAssets.Length > 0)
            {
                var tm = new SerializedObject(tmAssets[0]);
                var layers = tm.FindProperty("layers");
                void Name(int i, string n) { var p = layers.GetArrayElementAtIndex(i); if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = n; }
                if (layers != null) { Name(8, "Chao"); Name(9, "Coleta"); Name(10, "Criatura"); tm.ApplyModifiedProperties(); }
            }

            // o jogo usa o Input clássico: garante "Both" se o Input System estiver ativo
            var psAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (psAssets != null && psAssets.Length > 0)
            {
                var ps = new SerializedObject(psAssets[0]);
                var ih = ps.FindProperty("activeInputHandler");
                if (ih != null && ih.intValue == 1) { ih.intValue = 2; ps.ApplyModifiedProperties(); }
            }

            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 70;
            QualitySettings.pixelLightCount = 6;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("FlorestaViva").AddComponent<Game>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (EditorSceneManager.GetActiveScene().path != ScenePath && !EditorApplication.isPlaying)
                EditorSceneManager.OpenScene(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Floresta Viva] Projeto configurado. Aperte Play.");
        }
    }
}
#endif
