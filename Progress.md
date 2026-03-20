# Documento de Continuidade do Projeto
## Dungeon Crawler 2D Turn-Based (Unity)

Checkpoint consolidado em 2026-03-19 com base no estado atual do codigo.

---

# 1. Estado geral

O projeto ja possui um slice jogavel com:
- exploracao em grid por turnos
- multiplas entidades por celula
- combate em grupo/celula
- XP, level e atributos
- itens fixos e procedurais
- equipamentos por entidade
- loot no chao
- inventario compartilhado da party
- UI funcional de inventario, loot e equipamentos
- persistencia basica entre exploracao, combate e troca de cenas
- portais entre cenas
- NPCs de recrutamento, quest e comercio
- moeda compartilhada da party

---

# 2. Decisoes de design vigentes

## Inventario compartilhado da party

A decisao atual de game design e:
- a party usa uma unica mochila compartilhada
- o painel lateral troca apenas qual entidade esta sendo equipada
- os slots equipados continuam individuais por entidade
- o inventario central e o ground loot sao compartilhados no fluxo da party

## Arquitetura derivada

- `PartyInventory` = mochila compartilhada
- `Entity + EquipmentSlots` = equipamento individual
- `LootWindowUI` = controller sempre ativo
- `LootWindowGridAutoBuilder` = builder no objeto visual da janela

Importante:
- `LootWindowUI` nao fica no mesmo GameObject da janela visual
- o builder e a janela visual ficam separados do controller
- evitar depender de ordem implicita de inicializacao

---

# 3. O que ja esta fechado

## Loot / Inventario / UI principal

- drag and drop entre mochila, equipados e chao
- drag com ghost, sem o slot sair do lugar
- tooltip com comparacao contra item equipado
- tooltip com tamanho automatico pelo conteudo
- cores por raridade e delta positivo/negativo
- correcao do tooltip ficar aberto ao fechar a janela
- seletor lateral de personagem para trocar apenas o alvo de equipamento
- separacao correta entre controller e builder da janela
- correcao de primeira abertura sem slots por ordem de inicializacao
- correcao visual dos slots vazios
- correcao do auto-open reabrindo continuamente ao ficar sobre loot
- migracao da UI principal para inventario unico da party

## Exploracao / Combate

- movimento em grid por teclado
- combate ao tentar entrar em celula hostil
- inimigos podem ficar estaticos na exploracao
- chance configuravel de combate ao ficar adjacente a inimigos
- combate e retorno preservando dados relevantes da exploracao
- transicao entre cenas por `ScenePortal`

## NPCs

- `NpcActor` com 3 tipos:
  - `Recruit`
  - `Quest`
  - `Merchant`
- recrutamento por custo em dinheiro
- comercio com compra e venda
- quests de exterminio
- filtragem de quests pelo nivel do lider
- quests por inimigo especifico
- quests com nivel minimo do inimigo alvo

## Economia e progressao

- `PartyCurrency` para dinheiro compartilhado
- recompensa de quest em dinheiro
- compra de item usando dinheiro da party
- venda de item da mochila pelo `Value`
- XP e level por personagem
- distribuicao de atributos via sistema atual de stats

## Spawner / exploracao

- `EnemySpawner` com multiplas areas de spawn
- cada area pode definir:
  - lista de inimigos
  - area de spawn
  - grupos
  - configuracao de quantidade
- suporte a gizmos de area no editor

---

# 4. Estado atual da UI de NPC

## Janela de interacao

- a janela ocupa proporcionalmente 80% da tela
- ha modos separados para recrutamento, comercio e quest
- existe barra de acao no rodape
- comerciante mostra:
  - lado da loja
  - lado da mochila da party
- compra e venda usam confirmacao
- a mochila da party no comerciante possui rolagem vertical

## Observacoes

- o item comprado deve sumir do lado da loja
- o item comprado deve aparecer imediatamente no lado da mochila da party
- o inventario `E` continua refletindo a mesma mochila compartilhada

---

# 5. Scripts mais relevantes no estado atual

## Core / Gameplay

- `GridManager`
- `TurnManager`
- `Entity`
- `EnemyAI`
- `PlayerGridMovement`
- `EnemySpawner`
- `ScenePortal`

## Progressao / Stats

- `StatBlock`
- `CharacterStats`
- `Team`

## Itens / Equipamentos

- `ItemEnums`
- `ItemData`
- `GeneratedItemInstance`
- `ItemGenerationProfile`
- `ItemGenerator`
- `EquipmentSlots`

## Loot / Inventario

- `GroundItem`
- `InventoryItemEntry`
- `PlayerInventory` (legado)
- `PartyInventory` (oficial para a UI da party)
- `PlayerItemPickup`

## NPC / Quests / Economia

- `NpcActor`
- `NpcRecruitmentPersistence`
- `NpcInteractionUI`
- `QuestDefinition`
- `QuestTracker`
- `PartyCurrency`

## UI

- `LootWindowUI`
- `LootWindowGridAutoBuilder`
- `ItemButtonUI`
- `ItemTooltipUI`
- `StatsPanelUI`
- `StatsPanelAutoBuilder`

## Persistencia entre cenas / combate

- `CombatSessionData`
- `CombatExplorationReturnData`
- `CombatGridSceneManager`
- `CombatTurnManager`
- `ExplorationScenePersistenceData`
- `ExplorationSceneBootstrap`
- `ExplorationReturnApplier`

---

# 6. Regras de implementacao definidas pelo usuario

Seguir sempre:
- passar arquivo `.cs` completo quando editar script
- explicar passo a passo de implementacao na Unity
- instrucoes curtas e precisas
- citar nomes de menus, objetos e componentes quando relevante

---

# 7. O que ainda nao esta fechado

Estas requisicoes do escopo original ainda nao estao totalmente comprovadas no projeto:

- movimento por duplo clique para deslocamento multiplo
- sistema de bau principal / baus garantidos por dungeon
- boss de dungeon
- boss final
- campanha fechada com 4 dungeons prontas
- regras completas de drop garantido por dungeon para baus
- uso de item em combate

Observacao:
- parte visual isometrica depende das cenas e nao fica comprovada apenas pelos scripts

---

# 8. Riscos / cuidados de continuidade

- nao reintroduzir bugs de ordem de inicializacao entre UI controller e builder
- nao reintroduzir reopen infinito da janela ao ficar parado sobre loot
- manter `PartyInventory` como fonte oficial da mochila compartilhada
- evitar expandir logica nova em `PlayerInventory` legado
- preservar o fluxo de tooltip e estado visual de slots vazios
- tratar com cuidado persistencia entre exploracao, combate e retorno

---

# 9. Proximos passos recomendados

## Prioridade alta

1. Implementar sistema de bau
- bau interagivel no mapa
- lista/configuracao de drop garantido
- tier por dungeon

2. Implementar boss de dungeon
- prefab/config por boss
- encontro fixo no mapa
- recompensa / progressao de saida

3. Implementar movimento por duplo clique
- path simples em grid
- respeitando paredes, ocupacao e interacoes

## Prioridade media

4. Revisar e polir UI de NPC
- acabamento visual final
- revisar fluxo de compra/venda/quest em runtime

5. Sincronizar melhor painel de status com personagem selecionado

6. Definir fluxo explicito de `party leader` para interacoes globais

## Prioridade de conteudo

7. Montar estrutura concreta das dungeons
- dungeon 1 jogavel completa
- depois escalar para 2, 3 e 4

8. Planejar tabela de progressao
- tiers por dungeon
- inimigos por mapa
- drops por mapa

---

# 10. Resumo rapido

O nucleo sistemico do projeto esta forte:
- exploracao
- combate
- inventario compartilhado
- equipamentos
- NPCs
- quests
- comercio
- moeda

O que mais falta agora e fechar conteudo e sistemas de progressao de mapa:
- baus
- bosses
- movimento por duplo clique
- estrutura final das dungeons
