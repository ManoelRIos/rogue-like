# Floresta Viva

Sobrevivência cooperativa em 3D low-poly. **De dia** a floresta é aconchegante: sol quente, pássaros, pólen no ar, rio, flores, frutinhas. **À noite** a neblina fecha, os vagalumes acendem, olhos aparecem entre as árvores — e os **Vultos** saem da mata. A fogueira no meio da clareira é a base: enquanto ela queima forte, a luz espanta as criaturas. Se apagar, ninguém está seguro.

> Projeto Unity separado, dentro da pasta `FlorestaViva/` do repositório. O rogue-like (*Ecos do Gatilho*) continua na raiz.

## Como abrir

1. Unity Hub → **Add project from disk** → escolha a pasta `FlorestaViva` (Unity **6000.0.85f1** ou outro Unity 6).
2. Na primeira abertura o script `Assets/Editor/FlorestaSetup.cs` cria a cena `Assets/Scenes/Floresta.unity`, configura camadas, sombras e build. (Se precisar repetir: menu **Floresta → Configurar projeto**.)
3. Aperte **Play**. Tudo — terreno, árvores, personagens, sons — é gerado por código, não há assets para importar.

## Roteiro de teste no Unity

1. Abra a pasta no Hub e espere a importação. No console deve aparecer **"[Floresta Viva] Projeto configurado. Aperte Play."** e a cena `Floresta` fica aberta.
   - Se o Unity perguntar sobre reiniciar por causa do *Input System*, aceite (o jogo usa o Input clássico).
2. **Play.** Tela "a floresta está crescendo…" por alguns segundos, depois você aparece ao lado da fogueira de manhã.
3. **Mundo:** ande até a borda da clareira, olhe as árvores balançando, siga a trilha para o norte até o rio.
4. **Coleta (tecla 1):** segure o clique numa árvore até ela tombar; colha um arbusto (frutinhas, às vezes sementes); quebre uma pedra.
5. **Fazenda:** 2 enxada → clique no chão da clareira; 3 sementes → clique no canteiro (Q troca); 4 regador → clique no canteiro; vá ao rio e clique para encher. Segure **T** para ver crescer; colha com clique quando ficar madura.
6. **Noite:** aperte **N**. Neblina, vagalumes, olhos no escuro; Vultos saem da mata. Afaste-se da fogueira para eles virem; use o machado. Deixe uma planta longe do fogo e veja o aviso "A PLANTAÇÃO!".
7. **Fogueira:** com madeira, **E** perto do fogo. Deixe apagar para ver a base ficar sem proteção.
8. **V** alterna 1ª/3ª pessoa; **Esc** pausa e mostra os controles.

Se algo der erro, copie a **primeira** mensagem vermelha do Console (as outras costumam ser consequência dela).

## Controles

| Tecla | Ação |
|---|---|
| WASD / Shift / Espaço | andar / correr / pular |
| Mouse / roda | olhar / zoom da câmera |
| **V** | alterna 3ª pessoa ↔ 1ª pessoa (transição suave) |
| **1 – 4** | ferramenta: machado, enxada, sementes, regador |
| Clique (segurar) | usar a ferramenta: cortar, quebrar pedra, colher arbusto, arar, plantar, regar, colher a horta, atacar Vulto |
| **Q** | troca a semente (cenoura ↔ abóbora) |
| **F** | comer (escolhe a comida que não desperdiça: frutinha, cenoura, abóbora, cogumelo) |
| **E** (perto da fogueira) | alimentar a fogueira com 1 madeira |
| Esc | pausa + lista de controles |
| **N** | *teste:* pula direto para o anoitecer / amanhecer |
| **T** (segurar) | *teste:* acelera o mundo ×10 (dia, fogueira, plantas) |

## O que já existe (marco 1 — Mundo + dia/noite)

- **Mundo** 320 × 320 m gerado por semente: colinas, montanhas na borda, rio sinuoso com margem de areia/lama, clareira plana da base.
- **Floresta viva**: pinheiros, carvalhos (alguns de outono), bétulas, árvores mortas, arbustos com frutinhas, pedras com musgo, cogumelos (alguns brilham), grama e flores balançando ao vento (shader), pássaros em bando, pólen, vagalumes perto do rio e da mata fechada.
- **Coleta**: árvores tombam e voltam a crescer depois de um tempo; pedras e arbustos se regeneram.
- **Dia e noite** (10 min por dia): sol e lua com sombras, céu em gradiente com estrelas, neblina leve de dia e densa à noite, vento mais forte à noite, ambientes sonoros diferentes.
- **Terror noturno**: Vultos (altos, braços longos, olhos vermelhos) saem das bordas da floresta, mais a cada noite. Eles espreitam na neblina, **se aproximam quando você não está olhando**, avançam e atacam. Atravessam árvores. Fogem da luz da fogueira, mas **sugam a chama** se ficarem na borda dela. Sussurros, batimento cardíaco quando estão perto, "olhos no escuro" que somem quando você se aproxima. Ao amanhecer, se desfazem.
- **Base**: fogueira com luz tremeluzente, faíscas, barraca, troncos para sentar, pilha de lenha, estacas marcando a clareira.
- **Jogador** (Andarilho): vida, fome, inventário; ao morrer volta para a fogueira perdendo metade do que carregava.

## Marco 2 — Fazenda

- **Enxada** ara um canteiro na clareira (grade de 1,6 m, com marcador no chão mostrando onde vai arar; não deixa arar colado no fogo nem em cima da barraca).
- **Sementes** de cenoura (cresce em ~2/3 de dia) e de abóbora (~1,5 dia). Você começa com 6 + 3; colher devolve 1–2 sementes e os arbustos às vezes soltam sementes silvestres.
- **Regador** com 6 cargas, enche no rio (a trilha da clareira leva até lá). **A planta só cresce com a terra molhada**, e a terra seca em pouco mais de meio dia — regar vira a rotina da manhã.
- 4 estágios visuais por planta (broto → folhas → fruto verde → madura). Canteiro escurece quando está molhado.
- **À noite os Vultos atacam a horta**: parte deles vai direto nas plantas que estão **fora da luz da fogueira** e come até destruir. Aparece o aviso "A PLANTAÇÃO!". Para proteger: plante perto do fogo, mantenha a fogueira alta (o raio de luz cresce com a lenha) ou vá lá espantar com o machado.
- O Andarilho troca a ferramenta na mão (machado, enxada, saco de sementes, regador).

## Próximos marcos

Veja [Docs/ROTEIRO.md](Docs/ROTEIRO.md): modo construção, veículo, aventuras pelo mapa (ruínas, baús, missões) e **co-op online** (até 4, host + código de convite).

## Estrutura

```
Assets/
  Editor/FlorestaSetup.cs      configura projeto e cena
  Resources/Shaders/           LowPoly (vento, neblina, sombras), Water, Sky, Glow
  Scripts/Core/                Game (bootstrap e laço), Util (ruído, MeshBuilder, materiais)
  Scripts/World/               World (terreno/rio), Flora (plantas e coleta), DayNight, Life (pássaros, vagalumes)
  Scripts/Night/               Campfire (base), Creature (Vulto), NightDirector (ondas e "olhos no escuro")
  Scripts/Farm/Farm.cs         canteiros, culturas (cenoura, abóbora), rega, crescimento e ataque noturno
  Scripts/Player/              Characters (modelos low-poly), PlayerController, CameraRig
  Scripts/UI/Hud.cs            interface
  Scripts/Audio/Audio.cs       sons sintetizados
```
