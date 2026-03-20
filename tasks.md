# Tasks do Projeto
## Dungeon Crawler 2D Turn-Based

Documento de planejamento baseado no `escopo.md` e no estado atual do projeto.

Legenda de status:
- `[feito]` implementado
- `[parcial]` implementado em parte ou precisa de refinamento
- `[pendente]` ainda nao implementado

---

# 0. Estado atual resumido

0.1 Sistemas ja existentes
- `[feito]` exploracao em grid por teclado
- `[feito]` combate em grid por grupo/celula
- `[feito]` XP, level e atributos
- `[feito]` itens, equipamentos e raridade
- `[feito]` loot no chao
- `[feito]` inventario compartilhado da party
- `[feito]` NPCs de recrutamento, quest e comercio
- `[feito]` moeda da party
- `[feito]` portais entre cenas

0.2 Sistemas faltando do escopo principal
- `[pendente]` movimento por duplo clique
- `[pendente]` sistema de bau principal / bau garantido
- `[pendente]` boss de dungeon
- `[pendente]` boss final
- `[pendente]` estrutura fechada das 4 dungeons
- `[pendente]` regras fechadas de recompensa por dungeon
- `[pendente]` uso de item em combate

---

# 1. Implementar sistema de bau

1.1 Objetivo
- criar um interativo de mapa que permita abrir bau e receber recompensa configurada

1.2 Dependencias
- sistema de interacao no grid
- inventario da party
- geracao ou selecao de item
- persistencia de estado do mapa

1.3 Tarefas principais
1.3.1 criar script base de bau
- detectar interacao ao tentar entrar na celula
- bloquear reabertura infinita
- expor configuracoes no Inspector

1.3.2 definir tipos de recompensa
- item fixo
- item gerado
- dinheiro
- lista de recompensas

1.3.3 definir comportamento de abertura
- abrir uma vez
- trocar sprite/estado visual ao abrir
- nao permitir loot duplicado

1.3.4 integrar com a UI existente
- enviar item para a mochila compartilhada
- se a mochila estiver cheia, manter feedback claro
- opcionalmente usar a janela de loot atual

1.3.5 persistencia
- registrar bau aberto por cena
- nao respawnar bau aberto ao voltar da batalha ou trocar de cena

1.4 Tarefas intermediarias recomendadas
1.4.1 criar `ChestActor`
1.4.2 criar `ChestPersistence`
1.4.3 integrar com `PartyInventory`
1.4.4 integrar com `ItemGenerator`
1.4.5 adicionar feedback visual e sonoro

1.5 Criterio de conclusao
- bau pode ser colocado como prefab
- abre apenas uma vez
- entrega recompensa corretamente
- persiste entre retorno de combate e troca de cena

---

# 2. Implementar boss de dungeon

2.1 Objetivo
- criar encontro especial por mapa com identidade, dificuldade e recompensa propria

2.2 Dependencias
- combate atual funcionando
- sistema de inimigos
- transicao de cena e/ou progressao por portal
- sistema de recompensa

2.3 Tarefas principais
2.3.1 definir estrutura de boss
- prefab de boss
- metadados de boss
- recompensa ao derrotar

2.3.2 integracao com mapa
- posicionar boss em sala final
- iniciar combate ao interagir
- impedir bypass involuntario

2.3.3 recompensa
- dinheiro
- item garantido
- desbloqueio de saida da dungeon

2.3.4 estado persistente
- boss derrotado nao reaparece
- mapa reflete estado pos-vitoria

2.4 Tarefas intermediarias recomendadas
2.4.1 criar `BossActor` ou usar `Entity` com flags de boss
2.4.2 criar `BossRewardDefinition`
2.4.3 criar estado de dungeon concluida
2.4.4 adicionar feedback de encontro especial

2.5 Criterio de conclusao
- cada dungeon pode ter 1 boss configuravel
- derrotar o boss conclui a dungeon ou libera sua saida
- recompensa especial e persistente

---

# 3. Implementar movimento por duplo clique

3.1 Objetivo
- permitir selecionar uma celula e mover automaticamente por varias celulas validas

3.2 Dependencias
- grid manager
- validacao de celula caminhavel
- controle de turno do player

3.3 Tarefas principais
3.3.1 detectar clique no grid
- raycast ou conversao de tela para celula
- diferenciar clique simples de duplo clique

3.3.2 pathfinding simples
- BFS ou A*
- respeitar paredes
- respeitar celulas bloqueadas

3.3.3 execucao do movimento
- mover passo a passo
- interromper se houver combate
- interromper em interacao de NPC, bau ou portal

3.3.4 feedback visual
- mostrar rota
- destacar destino

3.4 Tarefas intermediarias recomendadas
3.4.1 criar helper de pathfinding
3.4.2 criar visual de path
3.4.3 integrar com `PlayerGridMovement`
3.4.4 revisar convivio com input de teclado

3.5 Criterio de conclusao
- duplo clique move o player ate o destino valido
- rota para ao encontrar combate ou interacao
- teclado continua funcionando normalmente

---

# 4. Implementar uso de item em combate

4.1 Objetivo
- permitir consumir item ou acionar item de suporte durante batalha

4.2 Dependencias
- inventario compartilhado
- UI de combate
- sistema de turno

4.3 Tarefas principais
4.3.1 definir categoria de item usavel
- cura
- buff
- revival opcional

4.3.2 criar fluxo de uso
- abrir lista de usaveis
- escolher alvo
- consumir item

4.3.3 integrar com turno
- custo de acao
- validar se pode usar naquele turno

4.4 Tarefas intermediarias recomendadas
4.4.1 extender `ItemData`
4.4.2 criar metodo de uso em combate
4.4.3 integrar com UI de combate

4.5 Criterio de conclusao
- item de cura pode ser usado em combate
- efeito aplica no alvo correto
- item sai da mochila compartilhada

---

# 5. Fechar sistema de dungeons

5.1 Objetivo
- transformar o prototipo sistemico em progressao jogavel por capitulos

5.2 Dependencias
- portais entre cenas
- bau
- boss
- tabela de drops

5.3 Tarefas principais
5.3.1 definir Dungeon 1 como vertical slice completa
- inicio
- salas
- inimigos
- 1 bau principal
- 1 boss
- saida

5.3.2 definir progressao por mapa
- dungeon 1 facil
- dungeon 2 media
- dungeon 3 dificil
- dungeon 4 final

5.3.3 definir tiers predominantes por dungeon
- comum
- comum/raro
- raro/epico
- epico/lendario

5.3.4 definir recompensas garantidas e drops
- bau por dungeon
- drop pool por dungeon
- recompensas de boss

5.4 Tarefas intermediarias recomendadas
5.4.1 criar documento de tabela de conteudo
5.4.2 criar prefab set por dungeon
5.4.3 validar pacing e dificuldade

5.5 Criterio de conclusao
- existe ao menos 1 dungeon totalmente fechada e jogavel
- a estrutura para replicar as demais 3 esta pronta

---

# 6. Polimento da exploracao e combate

6.1 Objetivo
- melhorar leitura e satisfacao do loop principal

6.2 Tarefas principais
6.2.1 intencao inimiga
- mostrar quem o inimigo vai atacar
- indicar risco no proximo turno

6.2.2 highlights no grid
- movimento
- alvo
- interacao
- perigo

6.2.3 feedback audiovisual
- dano
- critico
- morte
- coleta
- abertura de bau

6.2.4 log de combate
- dano causado
- critico
- morte
- ganho de XP

6.3 Criterio de conclusao
- combate fica mais legivel sem aumentar complexidade

---

# 7. Refinar itens e builds

7.1 Objetivo
- reforcar o foco do jogo em build por item

7.2 Tarefas principais
7.2.1 criar afixos simples
- bonus de stat
- efeito curto
- sinergia facil de entender

7.2.2 ampliar diversidade util
- armas
- armaduras
- acessorios

7.2.3 revisar tiers
- comum
- raro
- epico
- lendario

7.2.4 criar sinergias curtas
- exemplo: veneno + acerto extra
- exemplo: AP + critico

7.3 Criterio de conclusao
- itens mudam decisao do jogador, nao apenas numero

---

# 8. Refinar NPCs e economia

8.1 Objetivo
- consolidar NPCs como loop secundario importante

8.2 Tarefas principais
8.2.1 polir UI dos NPCs
- consistencia visual
- revisar estados de compra/venda/quest/recruit

8.2.2 ampliar quests
- mais objetivos de exterminio
- recompensas melhores por progressao

8.2.3 ampliar comercio
- estoque por dungeon
- itens melhores em mapas avancados

8.2.4 revisar recrutamento
- custo por progressao
- NPCs recrutaveis especiais

8.3 Criterio de conclusao
- NPCs passam a enriquecer progressao e build, nao apenas servir de teste de sistema

---

# 9. Qualidade de vida

9.1 Tarefas principais
9.1.1 salvar e carregar
9.1.2 filtros e ordenacao no inventario
9.1.3 remapeamento de teclas
9.1.4 tutorial contextual curto
9.1.5 codex de inimigos e itens

9.2 Criterio de conclusao
- jogar, testar e iterar fica mais rapido e confiavel

---

# 10. Ordem sugerida de execucao

10.1 Fase 1
- implementar bau
- implementar boss de dungeon
- fechar dungeon 1

10.2 Fase 2
- implementar movimento por duplo clique
- implementar highlights no grid
- implementar intencao inimiga

10.3 Fase 3
- implementar uso de item em combate
- refinar itens e sinergias
- refinar loot por dungeon

10.4 Fase 4
- polir NPCs, economia e quests
- adicionar eventos de sala
- revisar progressao geral

10.5 Fase 5
- salvar/carregar
- codex
- qol final
- expansao para dungeons 2, 3 e 4

---

# 11. Proxima entrega recomendada

11.1 Proxima implementacao ideal
- sistema de bau

11.2 Motivo
- fecha parte central do escopo
- conversa diretamente com inventario, loot, dungeon e recompensa
- prepara terreno para boss e progressao de mapa

11.3 Considerar concluido quando
- houver um prefab de bau configuravel
- o bau abrir uma vez
- entregar item ou dinheiro
- persistir corretamente no mapa
