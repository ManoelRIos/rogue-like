# Ecos do Gatilho

Roguelike *bullet hell* em **2.5D** inspirado em Enter the Gungeon, feito com **Three.js** + **Vite**.
Desça 5 andares gerados proceduralmente, desvie das balas com cambalhotas e derrote o guardião de cada andar.

## Como jogar

```bash
npm install
npm run dev      # abre em http://localhost:5173
npm run build    # gera a versão final em dist/
```

### Controles

| Ação | Tecla |
| --- | --- |
| Mover | `W A S D` / setas |
| Mirar / atirar | Mouse / clique esquerdo (segure para tiro automático) |
| Cambalhota (invulnerável no início) | `Espaço` ou botão direito |
| Recarregar | `R` |
| Blank (apaga todas as balas inimigas) | `Q` |
| Interagir (baús, loja, armas, alçapão) | `E` |
| Trocar arma | `1–9` ou roda do mouse |
| Pausar | `Esc` / `P` |

## O que tem no jogo

- **5 andares** com temas próprios: Masmorra Ferrugem, Catacumbas de Osso, Forja Rubra, Cripta Esmeralda e Altar do Último Tiro.
- **Mapa procedural**: salas numa grade ligadas por corredores, com sala inicial, salas de combate (com pilares, caixas destrutíveis e poços), sala do tesouro, loja e sala do chefe no ponto mais distante. Cada andar é diferente a cada partida.
- **Cambalhota** com quadros de invulnerabilidade para atravessar as balas, e **blanks** para limpar a tela em emergências.
- **10 armas** (pistola, escopeta, metralhadora, rifle perfurante, bolhas que ricocheteiam, lança-raios, canhão de fogos explosivo, besta tripla, revólver dourado com ricochete, lança-estrelas teleguiado).
- **8 inimigos** com padrões de tiro diferentes (tiro mirado, leque, rajada, anéis, espiral, teleporte, escopeta) que avisam antes de atirar.
- **5 chefes** com múltiplos ataques e segunda fase abaixo de 50% de vida: Rei Gosma, Sino Fantasma, Golem da Forja, Aranha Relojoeira e Sacerdote do Último Tiro.
- Portas que trancam durante o combate, ondas de inimigos, moedas, loja, baús, minimapa, HUD, sons sintetizados e tremor de tela.

## Visual 2.5D

O cenário é 3D (chão, paredes com altura, pilares, caixas e portões) visto por uma câmera inclinada em perspectiva,
enquanto personagens, inimigos e itens são sprites de pixel art em pé (billboards). Tudo é renderizado em baixa
resolução e ampliado sem suavização para manter o aspecto pixelado, com luz dinâmica de tochas e do jogador.
Todos os sprites são desenhados por código (`src/sprites.js` e `src/weapons.js`) — não há arquivos de imagem.

## Estrutura

```
src/
  main.js        ponto de entrada
  game.js        loop, estados, salas, colisão, câmera, recompensas
  dungeon.js     geração procedural dos andares
  world.js       geometria 3D do andar (chão, paredes, portões, tochas, luzes)
  player.js      jogador, cambalhota, armas, blanks
  enemies.js     inimigos comuns, chefes e padrões de balas
  bullets.js     sistema de projéteis (InstancedMesh)
  particles.js   partículas e clarões
  items.js       itens, baús, loja e alçapão
  weapons.js     definição e sprites das armas
  sprites.js     pixel art procedural dos personagens e itens
  hud.js         interface e minimapa
  themes.js      temas visuais e inimigos de cada andar
  audio.js       efeitos sonoros com WebAudio
```

### Parâmetros de depuração (URL)

- `?seed=123` — semente fixa para o mapa
- `?floor=3` — começa no andar indicado
- `?god` — invencível (para testes)

## Publicar no GitHub Pages

O workflow em `.github/workflows/deploy.yml` faz o build e publica a cada push na `main`.
Basta ativar uma vez em **Settings → Pages → Source: GitHub Actions**.
