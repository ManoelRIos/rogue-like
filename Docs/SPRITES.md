# Sprites e como trocar a arte

Toda a arte atual é original e gerada por `Tools/ArtGen/build.py` (Python + Pillow). O jogo lê tudo de
`Assets/Resources/Art/atlas.json`, então dá para trocar qualquer folha sem mexer no código.

## Escala

- **16 pixels = 1 tile = 1 unidade** do Unity. O jogador tem 24×26 px; inimigos comuns 16–24 px; chefes 48×48 px.
- Pixel art sem anti-aliasing, de preferência com contorno escuro de 1 px.

## Formato do atlas.json

```json
{
  "sheets": [
    {
      "name": "player", "file": "Art/Sheets/player",
      "fw": 24, "fh": 26,            // tamanho de cada quadro
      "pivotX": 12, "pivotY": 1,     // pivô em pixels (x da esquerda, y de baixo) = ponto no chão
      "anims": [
        { "name": "idle_down", "row": 0, "count": 4, "fps": 6, "loop": true },
        { "name": "run_side",  "row": 4, "count": 6, "fps": 12, "loop": true }
      ]
    }
  ],
  "weapons":  [{ "name": "pistola", "file": "Art/Weapons/pistola", "gripX": 4, "gripY": 5 }],
  "tilesets": [{ "name": "masmorra", "file": "Art/Tiles/masmorra" }]
}
```

- Cada linha (`row`) da folha é uma animação; os quadros ficam da esquerda para a direita.
- `file` é o caminho dentro de `Resources`, sem extensão.
- Armas: `gripX/gripY` é o pixel da empunhadura (a arma gira em volta dele), com a arma apontando para a direita.

## Animações que o código usa

| Folha | Animações |
| --- | --- |
| `player` | `idle_down`, `idle_side`, `idle_up`, `run_down`, `run_side`, `run_up`, `roll`, `death` (lado = virado para a direita; o jogo espelha) |
| `enemy_<id>` | primeira animação da folha (movimento) — ids: gosma, cogumelo, caveira, morcego, lanterna, torreta, cristal, besouro |
| `boss_<id>` | primeira animação — ids: reiGosma, sinoFantasma, golem, aranha, sacerdote |
| itens/efeitos | heart, heart_half, heart_empty, blank, coin, ammo, chest, chest_rare, pedestal, torch, hatch, crate, gate, shadow, fx_*, bullet_* |

## Tileset (128×128, células de 16 px)

| Linha | Conteúdo |
| --- | --- |
| 0 | 8 variações de chão |
| 1–2 | 8 faces de parede (16×32): 0–3 tijolos, 4 rachada, 5 estandarte, 6 grade, 7 nicho |
| 3–4 | topo da parede por máscara de vizinhos (bit 1 = norte, 2 = leste, 4 = sul, 8 = oeste) |
| 5 | poço, borda do poço, tapete, tapete 2, sombra pontilhada, sombra lateral, lava, borda da lava |
| 6 | decorações espalhadas no chão (com transparência) |

## Usando seus próprios spritesheets

1. Coloque os PNGs em `Assets/Resources/Art/` (o importador já aplica filtro Point e sem compressão).
2. Edite o `atlas.json` apontando `file`, tamanho do quadro, pivô e linhas das animações.
3. Mantenha os nomes das animações da tabela acima (ou ajuste em `Player.cs` / `Enemies.cs`).

**Direitos autorais:** este repositório é público. Use arte sua, encomendada ou de pacotes com licença que
permita redistribuição (ex.: itch.io / OpenGameArt com licença compatível). Sprites extraídos de jogos
comerciais não devem ser publicados aqui.
