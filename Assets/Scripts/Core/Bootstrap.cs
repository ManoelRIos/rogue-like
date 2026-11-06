using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Cria o jogo automaticamente ao dar Play em qualquer cena (inclusive vazia).
    /// Todo o conteúdo é montado por código a partir de Resources/.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            // desliga câmeras/luzes padrão da cena, se houver
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.gameObject.SetActive(false);
            var go = new GameObject("EcosDoGatilho");
            go.AddComponent<GameManager>();
        }
    }
}
