# Arquitetura

## Coordenadas

- **Jogo**: `Vector2 (x, y)` em tiles, com `y` crescendo para baixo (igual à grade do mapa).
- **Mundo Unity**: `MathX.ToWorld(p, h) = (x, h, -y)`; a altura `h` é o eixo Y do Unity.
- Toda a lógica (colisão, IA, balas) roda em coordenadas de jogo; só o visual converte para o mundo.

## Câmera (Rendering/PixelCamera.cs)

| Modo | Como funciona |
| --- | --- |
| `TwoPointFive` (padrão) | Ortográfica, inclinada 45°, com a projeção multiplicada por `Scale(1, √2, 1)`. Com isso o chão e a altura das paredes aparecem na mesma escala (16 px por tile). Os sprites ficam em pé, de frente para a câmera. |
| `ThreeD` (F2, experimental) | Perspectiva atrás/acima do jogador; os sprites viram billboards que acompanham o giro (`PixelCamera.BillboardYaw`). |

Como as paredes e o chão são geometria 3D real, trocar de câmera não exige refazer o mapa. Para deixar o 3D
"de verdade" depois, os próximos passos são: transição suave entre as câmeras (ex.: Cinemachine Blend),
mira por raycast no chão (já é assim em `MouseGame`) e, se quiser, trocar sprites por modelos low-poly.

## Mapa procedural (World/)

1. `DungeonGen` sorteia 8–13 salas numa grade 7×7 (passeio aleatório a partir do centro, com alguns laços).
2. Por BFS, a sala mais distante vira o **chefe**; outras folhas viram **tesouro** e **loja**.
3. Cada sala de combate usa um **modelo desenhado à mão** (`RoomTemplates`, espelhado aleatoriamente),
   o que dá um mapa com cara de "projetado" mas sempre diferente.
4. Salas vizinhas são ligadas por corredores de 3 tiles; as portas ficam no centro de cada lado.
5. `LevelBuilder` gera um mesh para o chão (com sombras e decorações) e outro para as paredes
   (topo com borda clara + faces), além de caixas destrutíveis, portões e tochas.

## Preparação para co-op online

Já está pronto:

- **Entrada separada da simulação**: `Player` só lê `PlayerInputState` através de `IInputSource`.
  O jogador local usa `LocalInputSource`; um jogador remoto usaria uma fonte que recebe a struct pela rede.
- **Vários jogadores**: `GameManager.Players` é uma lista. Inimigos miram no jogador mais próximo
  (`NearestPlayer`), balas testam colisão com todos, ondas ficam maiores com mais jogadores e a partida só
  termina quando todos caem. `Player.tint` diferencia cada um.
- **Semente por partida**: o mapa é gerado a partir de `runSeed` — basta o host enviar a semente para todos
  gerarem o mesmo andar localmente.
- **Ordem de atualização fixa** (jogadores → salas → inimigos → balas → itens), o que facilita um host autoritativo.

Caminho sugerido (Netcode for GameObjects ou FishNet):

1. Host autoritativo: o host roda `GameManager.Tick` completo; clientes enviam `PlayerInputState` todo tick.
2. O host replica posições/estado de jogadores, inimigos e itens (snapshots a ~20 Hz com interpolação).
3. Balas inimigas: em vez de replicar cada bala, replicar o **evento de ataque** (padrão + semente + tempo),
   e cada cliente simula as balas localmente — é assim que bullet hells online economizam banda.
4. Lobby: Unity Relay/Lobby (ou Steam) para entrar com código, sem abrir portas.
5. Ajustes: câmera dividindo foco entre jogadores, reviver aliado caído, loot por jogador.
