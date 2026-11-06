# Roteiro — Floresta Viva

## Marco 1 — Mundo + dia/noite ✅ (primeira versão)
Floresta gerada, coleta, ciclo dia/noite, Vultos à noite, fogueira como base, jogador em 1ª/3ª pessoa.

## Marco 2 — Fazenda
- Enxada (fibra + madeira) para arar a terra dentro da clareira.
- Sementes vindas de frutinhas/flores; canteiros que crescem ao longo dos dias e precisam de água (balde no rio).
- À noite os Vultos **atacam a plantação** além da fogueira — a defesa ganha um segundo alvo.
- Cozinhar na fogueira (comida melhor que frutinha crua).

## Marco 3 — Modo construção
- Tecla B abre o modo: grade de encaixe, peças de madeira e pedra (piso, parede, porta, telhado, cerca, tocha).
- Tochas e lanternas criam novas áreas de luz (raio seguro menor que o da fogueira, gastam combustível).
- Paredes bloqueiam o caminho dos Vultos? Não totalmente — eles atravessam árvores; paredes só os **atrasam** (mantém o terror).

## Marco 4 — Veículo
- Carroça/jipe velho encontrado quebrado numa ruína; consertar com peças das aventuras.
- Física com WheelCollider (módulo `vehicles` já está no manifest), farol que funciona como luz segura móvel, combustível.
- Até 4 lugares (motorista + passageiros) para o co-op.

## Marco 5 — Aventuras pelo mapa
- Pontos de interesse gerados: ruínas de pedra, cabana abandonada, caverna, torre de vigia, ponte caída.
- Baús com itens raros, notas que contam a história da floresta, chave para áreas fechadas.
- Missões simples do "diário": "ache a lanterna do guarda", "acenda os três totens antes da lua cheia".
- Noites especiais (lua de sangue) com um Vulto maior — chefe.

## Marco 6 — Co-op online (até 4)
- **Netcode for GameObjects + Unity Relay + Lobby**: um jogador hospeda, os outros entram com um código de convite (sem abrir portas no roteador).
- Já preparado: `PlayerInput` (struct com os comandos do quadro) separa entrada de simulação; `Game.Players` é uma lista; o Diretor da noite escolhe o jogador mais próximo como alvo; a cor do manto muda por jogador.
- Host é a autoridade: mundo (semente), ciclo dia/noite, criaturas, fogueira e coleta. Clientes enviam `PlayerInput`, recebem estado.
- Mundo é determinístico pela semente → só o que muda (árvores cortadas, construções, plantações) precisa ser sincronizado.
