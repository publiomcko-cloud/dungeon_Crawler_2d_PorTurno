# Documento de Continuidade do Projeto
## Dungeon Crawler 2D Turn-Based (Unity)

Checkpoint consolidado com base no `Progress.md` anterior, no `fullcode.md` e em tudo que foi implementado nesta conversa.

---

# 1. Estado geral

O projeto já possui um slice jogável com:
- grid por turnos
- múltiplas entidades por célula
- combate em grupo/célula
- level, XP e atributos
- itens fixos e procedurais
- equipamentos
- loot no chão
- UI de inventário/loot/equipados

---

# 2. Mudança de design mais recente

## Inventário compartilhado da party
A decisão atual de game design é:
- a party usa **uma única mochila compartilhada**
- o painel lateral troca apenas **qual entidade está sendo equipada**
- os slots equipados continuam individuais por entidade
- o inventário central e o ground loot são compartilhados no fluxo da party

## Arquitetura derivada
- `PartyInventory` = mochila compartilhada
- `Entity + EquipmentSlots` = equipamento individual
- `LootWindowUI` = controller sempre ativo
- `LootWindowGridAutoBuilder` = builder no objeto visual da janela

Esse detalhe é importante: o `LootWindowUI` **não fica no mesmo GameObject da janela**. O builder e a janela visual ficam separados do controller.

---

# 3. O que foi fechado nesta conversa

## Loot / Inventário / UI
- drag and drop real entre mochila, equipados e chão
- drag com ghost, sem o slot sair do lugar
- tooltip com comparação de item equipado
- tooltip com tamanho automático baseado no texto
- cores por raridade e delta positivo/negativo
- correção do tooltip ficar aberto ao fechar a janela
- seletor lateral de personagem para trocar apenas os equipamentos mostrados
- separação correta entre controller e builder da janela
- correção de primeira abertura sem slots por ordem de inicialização
- correção do visual dos slots vazios vindo como se tivessem item
- correção do auto-open reabrindo a janela quando ainda havia loot no chão
- migração da UI principal para **inventário único da party**

## Comportamento atual da janela
- `E` abre/fecha
- `Esc` fecha
- o seletor lateral muda só o alvo de equipamento
- mochila central é compartilhada
- ground loot segue o fluxo compartilhado
- qualquer entidade da party pode equipar itens da mochila compartilhada
- desequipar devolve para a mochila compartilhada

---

# 4. Scripts mais relevantes no estado atual

## Core / Gameplay
- `GridManager`
- `TurnManager`
- `Entity`
- `EnemyAI`
- `PlayerGridMovement`
- `EnemySpawner`

## Progressão / Stats
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

## Loot / Inventário
- `LootDropEntry`
- `LootDropper`
- `GroundItem`
- `InventoryItemEntry`
- `PlayerInventory` (legado; ainda existe no projeto)
- `PartyInventory` (novo e oficial para a UI da party)
- `PlayerItemPickup`

## UI
- `LootWindowUI`
- `LootWindowGridAutoBuilder`
- `ItemButtonUI`
- `ItemTooltipUI`
- `StatsPanelUI`
- `StatsPanelAutoBuilder`

---

# 5. Regras de implementação definidas pelo usuário

Seguir sempre:
- passar **arquivo `.cs` completo**
- explicar **passo a passo de implementação na Unity**
- instruções **curtas e precisas**
- citar nomes de menus, objetos e componentes

---

# 6. Observações importantes para continuidade

- a party pode ter até 4 entidades do jogador
- o seletor lateral mostra apenas os players existentes
- cada botão do seletor mostra sprite do player e número
- o tooltip e a UI de loot foram alvo de várias correções de estado visual
- o objeto com `LootWindowUI` é um controller separado do objeto visual da janela
- o `fullcode.md` foi atualizado com um bloco final chamado **LATEST CHECKPOINT OVERRIDES (March 13 2026)** contendo as versões mais recentes adicionadas nesta conversa

---

# 7. Próximos passos naturais

Os próximos refinamentos mais lógicos agora são:
- sincronizar o painel de status com o personagem selecionado no seletor lateral
- definir um `party leader` explícito para interações globais e loot ground
- aposentar gradualmente o fluxo antigo baseado em `PlayerInventory` individual
- refinar o comportamento do chão compartilhado quando a party se separar no grid
