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

## Visual e animação (atualização)

- **Andarilho com esqueleto completo** (quadril, coluna, peito, pescoço, cabeça; ombro → cotovelo → mão; coxa → joelho → pé) e proporções de adulto.
- **Animação procedural realista**, sem clipes prontos:
  - pés posicionados por **IK em cima do chão de verdade** (raycast): não patinam, sobem e descem morro, o pé acompanha a inclinação e "empurra" o chão no fim do passo;
  - cadência e passada calculadas pela velocidade; o quadril sobe/desce e balança para o lado da perna de apoio; correr agacha um pouco;
  - **inércia**: o corpo acelera e freia, inclina para frente ao arrancar e para trás ao frear, **inclina nas curvas**, tem limite de velocidade de giro e faz meia-volta mais lenta;
  - parado: respira, troca o peso de perna, olha em volta de vez em quando; ao girar no lugar **dá passinhos** para acompanhar;
  - pulo e **aterrissagem com amortecimento** (o quadril afunda conforme a altura da queda, poeira e som);
  - a **cabeça olha para onde a câmera aponta** e compensa o balanço do corpo;
  - golpes com **antecipação, impacto e recuperação**, diferentes para machado, enxada, sementes (jogar a mão) e regador (inclinar e derramar, com gotas);
  - **molas** na aba do casaco, mochila e cachecol (balançam ao correr, pular e parar);
  - passos com som posicional, respingo na água e poeira ao correr.
- **Vulto** com movimento de molas soltas: braços pendurados que arrastam, corpo curvado que se inclina ao avançar, **tiques bruscos de cabeça**; quando você olha, ele congela com a cabeça torta.
- **Vento de verdade**: rajadas em faixas que atravessam o campo a favor do vento (a grama ondula quando uma passa), direção que muda ao longo do dia, tremidinha nas pontas; **a grama abre quando você ou um Vulto passa**; folhas e grama têm luz translúcida (sem lado preto).
- **Mais vegetação**: grama com lâminas curvas (escuras na base, claras na ponta), capim alto com espiga nos campos e no rio, **samambaias** na mata fechada; a grama some aos poucos ao longe em vez de "piscar"; desenho por células (só perto da câmera).
- **Água nova**: ondas em facetas, cor pela profundidade real, **espuma na margem** e listras que descem com a **correnteza do rio**, reflexo do céu nos ângulos rasos e brilhos do sol/lua; malha só onde existe água; som de rio que vem do ponto mais próximo.
- **Fogueira nova**: oito línguas de fogo que tremem, esticam e deitam com o vento; brasas que pulsam (e continuam vermelhas quando o fogo apaga); halo de calor; **fumaça** que sobe e vai com o vento; faíscas contínuas e **estouros com estalo**; luz que dança e **sombras que se mexem** à noite; som de fogo crepitando no lugar da fogueira.
- **Árvores caindo com física**: cada golpe solta lascas e folhas e a copa balança; no último, a árvore **range**, tomba devagar e acelera (corpo rígido girando na base), **bate no chão, quica**, levanta poeira e uma chuva de folhas, faz a câmera tremer de quem está perto e afunda. Fica o **toco**; depois uma **muda nasce e cresce**. Pedras se quebram em pedaços; arbustos soltam folhas e frutinhas.
- **Ambientação**: borboletas nas flores, folhas caindo na mata, **névoa rasteira** no amanhecer e à noite (mais nas baixadas e no rio), **nuvens** que andam com o vento (alaranjadas no pôr do sol), disco do sol e da lua no céu, e sons com posição: vários pássaros e pica-pau de dia; coruja, sapos no rio e **galhos estalando no escuro** à noite; rajadas de vento.

## Estética estilo Omno + correções (atualização)

- **Nada flutua mais**: o chão tem uma função de altura exata (`World.Ground`) que lê o triângulo desenhado; árvores, pedras, arbustos, grama, flores, base, horta, partículas e criaturas usam ela. Troncos e pedras assentam no ponto mais baixo embaixo deles e entram um pouco no chão.
- **Movimento**: o Andarilho gruda no chão ao descer morro (antes descolava por um quadro e as pernas encolhiam como se fosse pular), tolerância curta de "no chão" para pulo e animação.
- **Luz pintada**: passagem suave entre sombra e luz, folhas e grama brilham contra o sol (translúcidas), contorno claro com a cor do céu, **normais suaves** nas copas, arbustos, pinheiros e morros (degradê em vez de quinas duras).
- **Sombras coloridas**: luz ambiente em 3 cores (céu azulado, horizonte, chão quente).
- **Neblina por altura** com perspectiva aérea: mais densa nas baixadas, morros e copas aparecem acima; ao longe vira o tom do horizonte e **esquenta na direção do sol**. À noite fica densa e azul-arroxeada.
- **Paleta pastel saturada**: céus luminosos, pores do sol rosa/laranja, pinheiros turquesa, **cerejeiras rosa** (com pétalas caindo), pedras lilases, água turquesa.
- **Pós-processamento** (`Rendering/OmnoPost.cs`): bloom suave (sol, fogo, vagalumes, olhos), correção de cor que muda com a hora (quente de dia, dourada no pôr do sol, azul à noite) e vinheta.
- **Luzes do rio**: orbes turquesa flutuando sobre a água no lusco-fusco.

## Peregrino e atmosfera (atualização)

- **Novo personagem (original, no estilo suave e estilizado)**: o Peregrino — túnica clara até acima do joelho, manto/capuz macio com ponta caída na cor do jogador, faixa coral, mangas largas, bolsa na cintura e cajado nas costas com **cristal que acende à noite** (luz própria para andar no escuro; não espanta os Vultos).
- **Saia da túnica contínua e deformável**: 10 setores com mola abrem quando a perna passa, ao correr (vento relativo), ao cair e com o vento — sem vãos nem "placas" soltas (era o bug visual das abas do casaco).
- **Cachecol de pano com física** (`Player/Cloth.cs`): corrente de pontos com gravidade, vento, inércia do corpo e colisão com o tronco; esvoaça ao correr e pular.
- **Rastros de vento**: fitas brancas que serpenteiam a favor do vento perto de você (mais quando venta forte).
- **Feixes de sol** atravessando a mata quando o sol está baixo (manhã e fim de tarde).
- Chão com **manchas largas e suaves** (dourado/turquesa) e quase sem ruído de triângulo; morros ainda mais macios.

## Próximos marcos

Veja [Docs/ROTEIRO.md](Docs/ROTEIRO.md): modo construção, veículo, aventuras pelo mapa (ruínas, baús, missões) e **co-op online** (até 4, host + código de convite).

## Estrutura

```
Assets/
  Editor/FlorestaSetup.cs      configura projeto e cena
  Resources/Shaders/           LowPoly (vento, rajadas, grama empurrada, folhas translúcidas), Water, Fire, Soft (fumaça/névoa/nuvens), Sky, Glow
  Scripts/Core/                Game (bootstrap e laço), Util (ruído, MeshBuilder, materiais), Motion (molas, curvas), Fx (partículas)
  Scripts/World/               World (terreno/rio/água), Flora (plantas, coleta, queda das árvores), DayNight, Wind, Life (bichos, névoa, nuvens, sons)
  Scripts/Night/               Campfire (base), Creature (Vulto), NightDirector (ondas e "olhos no escuro")
  Scripts/Farm/Farm.cs         canteiros, culturas (cenoura, abóbora), rega, crescimento e ataque noturno
  Scripts/Player/              Human (esqueleto + animação procedural), Characters (Vulto, ferramentas), PlayerController, CameraRig
  Scripts/UI/Hud.cs            interface
  Scripts/Audio/Audio.cs       sons sintetizados
```
