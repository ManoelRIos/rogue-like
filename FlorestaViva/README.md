# Floresta Viva

Sobrevivência cooperativa em 3D low-poly. **De dia** a floresta é aconchegante: sol quente, pássaros, pólen no ar, rio, flores, frutinhas. **À noite** a neblina fecha, os vagalumes acendem, olhos aparecem entre as árvores — e os **Vultos** saem da mata. A fogueira no meio da clareira é a base: enquanto ela queima forte, a luz espanta as criaturas. Se apagar, ninguém está seguro.

> Projeto Unity separado, dentro da pasta `FlorestaViva/` do repositório. O rogue-like (*Ecos do Gatilho*) continua na raiz.

## Como abrir

1. Unity Hub → **Add project from disk** → escolha a pasta `FlorestaViva` (Unity **6000.0.85f1** ou outro Unity 6).
2. Na primeira abertura o script `Assets/Editor/FlorestaSetup.cs` cria a cena `Assets/Scenes/Floresta.unity`, configura camadas, sombras e build. (Se precisar repetir: menu **Floresta → Configurar projeto**.)
3. Aperte **Play**. Tudo — terreno, árvores, personagens, sons — é gerado por código, não há assets para importar.

## Controles

| Tecla | Ação |
|---|---|
| WASD / Shift / Espaço | andar / correr / pular |
| Mouse / roda | olhar / zoom da câmera |
| **V** | alterna 3ª pessoa ↔ 1ª pessoa (transição suave) |
| Clique (segurar) | cortar árvore, quebrar pedra, colher arbusto, pegar cogumelo, atacar Vulto |
| **F** | comer (frutinhas; cogumelos enchem mais mas tiram um pouco de vida) |
| **E** (perto da fogueira) | alimentar a fogueira com 1 madeira |
| Esc | pausa + lista de controles |
| **N** | *teste:* pula direto para o anoitecer / amanhecer |

## O que já existe (marco 1 — Mundo + dia/noite)

- **Mundo** 320 × 320 m gerado por semente: colinas, montanhas na borda, rio sinuoso com margem de areia/lama, clareira plana da base.
- **Floresta viva**: pinheiros, carvalhos (alguns de outono), bétulas, árvores mortas, arbustos com frutinhas, pedras com musgo, cogumelos (alguns brilham), grama e flores balançando ao vento (shader), pássaros em bando, pólen, vagalumes perto do rio e da mata fechada.
- **Coleta**: árvores tombam e voltam a crescer depois de um tempo; pedras e arbustos se regeneram.
- **Dia e noite** (10 min por dia): sol e lua com sombras, céu em gradiente com estrelas, neblina leve de dia e densa à noite, vento mais forte à noite, ambientes sonoros diferentes.
- **Terror noturno**: Vultos (altos, braços longos, olhos vermelhos) saem das bordas da floresta, mais a cada noite. Eles espreitam na neblina, **se aproximam quando você não está olhando**, avançam e atacam. Atravessam árvores. Fogem da luz da fogueira, mas **sugam a chama** se ficarem na borda dela. Sussurros, batimento cardíaco quando estão perto, "olhos no escuro" que somem quando você se aproxima. Ao amanhecer, se desfazem.
- **Base**: fogueira com luz tremeluzente, faíscas, barraca, troncos para sentar, pilha de lenha, estacas marcando a clareira.
- **Jogador** (Andarilho): vida, fome, inventário; ao morrer volta para a fogueira perdendo metade do que carregava.

## Próximos marcos

Veja [Docs/ROTEIRO.md](Docs/ROTEIRO.md): fazenda (plantio + defesa da plantação), modo construção, veículo, aventuras pelo mapa (ruínas, baús, missões) e **co-op online** (até 4, host + código de convite).

## Estrutura

```
Assets/
  Editor/FlorestaSetup.cs      configura projeto e cena
  Resources/Shaders/           LowPoly (vento, neblina, sombras), Water, Sky, Glow
  Scripts/Core/                Game (bootstrap e laço), Util (ruído, MeshBuilder, materiais)
  Scripts/World/               World (terreno/rio), Flora (plantas e coleta), DayNight, Life (pássaros, vagalumes)
  Scripts/Night/               Campfire (base), Creature (Vulto), NightDirector (ondas e "olhos no escuro")
  Scripts/Player/              Characters (modelos low-poly), PlayerController, CameraRig
  Scripts/UI/Hud.cs            interface
  Scripts/Audio/Audio.cs       sons sintetizados
```
