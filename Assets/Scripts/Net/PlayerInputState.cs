using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Tudo o que um jogador "pede" em um quadro. A simulação do jogador consome
    /// apenas esta struct — nunca lê o teclado direto. No co-op online, o cliente
    /// envia esta struct ao host (ou a recebe de outros jogadores) e a mesma
    /// simulação roda dos dois lados. Veja Docs/ARQUITETURA.md.
    /// </summary>
    [System.Serializable]
    public struct PlayerInputState
    {
        public Vector2 move;          // direção normalizada (x, y para baixo)
        public Vector2 aimPoint;      // ponto mirado em coordenadas de jogo
        public bool fire;             // gatilho segurado
        public bool roll;             // apertou cambalhota neste quadro
        public bool reload;
        public bool blank;
        public bool interact;
        public int slot;              // -1 = nenhum; 0..8 = trocar para a arma
        public int cycle;             // -1/0/1 roda do mouse
    }

    public interface IInputSource
    {
        PlayerInputState Read(Player p);
    }

    /// <summary>Teclado + mouse (e um controle, se houver) da máquina local.</summary>
    public class LocalInputSource : IInputSource
    {
        public PlayerInputState Read(Player p)
        {
            var s = new PlayerInputState { slot = -1 };
            Vector2 m = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) m.y -= 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) m.y += 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) m.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) m.x += 1;
            s.move = m.sqrMagnitude > 0 ? m.normalized : Vector2.zero;
            s.aimPoint = PixelCamera.I.MouseGame(0.5f);
            s.fire = Input.GetMouseButton(0);
            s.roll = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1);
            s.reload = Input.GetKeyDown(KeyCode.R);
            s.blank = Input.GetKeyDown(KeyCode.Q) && !Input.GetKey(KeyCode.LeftShift);
            s.interact = Input.GetKeyDown(KeyCode.E) && !Input.GetKey(KeyCode.LeftShift);
            for (int i = 0; i < 9; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) s.slot = i;
            float wheel = Input.mouseScrollDelta.y;
            s.cycle = wheel > 0.1f ? -1 : wheel < -0.1f ? 1 : 0;
            return s;
        }
    }
}
