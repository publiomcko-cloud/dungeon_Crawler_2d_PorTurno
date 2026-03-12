# Documento de Continuidade do Projeto
## Dungeon Crawler 2D Turn-Based (Unity)

Este documento resume o estado atual real do projeto com base no `Progress.md` antigo, no `fullcode.md` enviado e em tudo que foi desenvolvido nesta conversa.

Ele deve ser usado como checkpoint para continuar o desenvolvimento em uma nova conversa sem perder contexto.

---

# 1. VISÃO GERAL DO PROJETO

## Objetivo
Criar um dungeon crawler / roguelike 2D em Unity, baseado em grid, com combate por turnos, progressão de personagens, loot, equipamentos e inventário.

## Direção de design atual
O projeto evoluiu além do protótipo inicial e hoje já possui:

- grid com múltiplas entidades por célula
- combate corpo a corpo por célula
- turnos de player e inimigos
- progressão de personagem por atributos e nível
- equipamentos e itens gerados aleatoriamente
- loot no chão
- inventário funcional em runtime
- UI de status e UI de inventário/loot

## Estado do projeto
O projeto não está mais no estágio “só movimento em grid”.
Atualmente já existe um **slice jogável de combate + progressão + loot + inventário**.

---

# 2. MUDANÇA DE ESCOPO IMPORTANTE

O `Progress.md` antigo dizia que:

- combate ainda não existia
- dano ainda não existia
- morte ainda não existia
- multi-entidade ainda não estava implementado por completo

Isso não é mais verdade.

## O que foi implementado desde então

### Sistema multi-entidade por célula
Cada célula do grid suporta **até 4 entidades**.

### Combate em célula
Quando um grupo tenta entrar em uma célula ocupada por inimigos:

- a invasão vira ataque
- todos os atacantes da célula atacam em sequência lógica
- o dano total é somado
- o dano é distribuído entre os defensores da célula

### Progressão
Player e inimigos passaram a usar a mesma base de atributos:

- HP
- ATK
- DEF
- AP
- CRIT
- level
- XP
- pontos distribuíveis

### Itens e equipamentos
Foi implementado:

- itens fixos por `ScriptableObject`
- itens gerados proceduralmente por perfil/range
- slots de equipamento
- aplicação de bônus nos atributos

### Loot e inventário
Foi implementado:

- drop de item no chão ao morrer
- item ocupando a célula do inimigo morto
- inventário do player
- UI de inventário/loot/equipados
- coleta via click e shift+click

---

# 3. ESTADO ATUAL FUNCIONAL

## Sistemas já funcionando

### Core / Grid / Turnos
- grid manager singleton
- turn manager singleton
- player turn → enemy turn
- movimentação por grid
- inimigos executam turno em coroutine
- suporte a paredes (`wallLayer`)
- line of sight para inimigos
- alcance máximo de visão para inimigos

### Entidades
- registro automático no grid
- posição lógica em `Vector2Int`
- posição visual separada da posição lógica
- smooth movement entre células
- ataque com lunge
- morte e remoção do grid

### Multi-entidade por célula
- até 4 entidades por célula
- célula de player pode conter grupo
- célula de inimigo pode conter grupo
- movimento em grupo
- ataque em grupo por célula

### Combate
- ataque ao tentar invadir célula inimiga
- dano total baseado em ATK
- redução por DEF do defensor
- dano mínimo 1
- chance de crítico preparada
- HP funcionando
- morte funcionando
- XP por morte funcionando

### Feedback visual
- health bar
- smooth movement
- attack lunge
- damage numbers
- damage flash
- posicionamento central da célula com offset por slot interno

### Progressão
- level
- XP
- XP para próximo nível
- pontos por level up
- bônus automáticos por nível
- bônus por pontos distribuídos manualmente

### Equipamentos e itens
- item fixo (`ItemData`)
- item procedural (`GeneratedItemInstance`)
- perfis de geração (`ItemGenerationProfile`)
- slots:
  - Weapon
  - Armor
  - Accessory
- aplicação de bônus do equipamento nos atributos finais

### Loot e inventário
- inimigo pode ter tabela de drop
- item dropa na célula da morte
- inventário do player com 20 slots
- equipar pela mochila
- desequipar para a mochila
- abrir UI com tecla `E`
- fechar UI com `E` ou `Esc`
- mostrar itens do chão na célula atual do player
- click no chão → envia para mochila
- shift+click no chão → tenta equipar
- click na mochila → tenta equipar
- click no equipado → manda para mochila

### UI
- painel de status separado
- loot/inventory window separada
- builders automáticos de UI
- slots quadrados na UI de inventário
- tooltip com nome + stats não zero
- slots mostram só ícone do item

---

# 4. ARQUITETURA ATUAL

A arquitetura deixou de ser o protótipo simples original e hoje se organiza em blocos.

## Núcleo de gameplay
- `GridManager`
- `TurnManager`
- `Entity`
- `EnemyAI`
- `PlayerGridMovement`
- `EnemySpawner`

## Stats / progressão
- `StatBlock`
- `CharacterStats`
- `Team`

## Equipamentos / itens
- `ItemEnums`
- `ItemData`
- `GeneratedItemInstance`
- `ItemGenerationProfile`
- `ItemGenerator`
- `EquipmentSlots`

## Loot / inventário
- `LootDropEntry`
- `LootDropper`
- `GroundItem`
- `InventoryItemEntry`
- `PlayerInventory`
- `PlayerItemPickup`

## UI
- `StatsPanelUI`
- `StatsPanelAutoBuilder`
- `LootWindowUI`
- `LootWindowGridAutoBuilder`
- `ItemButtonUI`
- `ItemTooltipUI`

## Feedback de dano
- `DamageNumber`
- `DamageNumberManager`
- `DamageNumberReceiver`
- (em versões anteriores também apareceu `DamageNumberSpawner`)

## Debug / utilitários
- `GridDebug`
- `EquipmentDebugWatcher`

---

# 5. GRID SYSTEM ATUAL

## Estrutura do grid
O grid não usa mais `Dictionary<Vector2Int, Entity>`.

Hoje ele usa:

- `Dictionary<Vector2Int, List<Entity>>`

Cada célula contém uma lista de entidades.

## Limite por célula
- `maxEntitiesPerCell = 4`

## Responsabilidades do `GridManager`
- registrar entidades
- remover entidades
- consultar entidades por célula
- consultar células ocupadas por time
- movimentar grupos
- detectar paredes
- detectar line of sight
- resolver combate célula vs célula
- posicionar visualmente entidades dentro da célula

## Sistema visual por subslot
As entidades não ficam exatamente uma em cima da outra.
Cada célula usa subposições visuais com `slotOffset`, normalmente formando um arranjo 2x2.

## Coordenadas de mundo
Centro da célula:
- `x + 0.5`
- `y + 0.5`

Isso continua sendo a base do alinhamento visual.

---

# 6. ENTITY / CHARACTER STATS

## `Entity`
A entidade é hoje um wrapper de gameplay + eventos, apoiado em `CharacterStats`.

### Responsabilidades
- time (`Player` / `Enemy`)
- posição lógica no grid
- smooth movement visual
- attack lunge
- eventos de dano/vida/morte/xp/level
- integração com grid
- integração com equipamentos

### Propriedades importantes
- `GridPosition`
- `CurrentHP`
- `maxHP`
- `attackDamage`
- `defense`
- `actionPoints`
- `critChance`
- `Level`
- `CurrentXP`
- `UnspentStatPoints`
- `IsDead`

## `CharacterStats`
Centraliza a progressão e cálculo final dos atributos.

### Camadas de stat
- `BaseStats`
- `LevelBonus`
- `PointBonus`
- `ItemBonus` (calculado a partir de `EquipmentSlots`)

### Stats finais
Usados no gameplay:
- HP máximo
- ATK
- DEF
- AP
- CRIT

### Sistema de level
- XP acumulado
- XP para próximo nível
- bônus automáticos por nível
- pontos distribuíveis por level up

### Gasto manual de pontos
Já existem métodos para investir em:
- HP
- ATK
- DEF
- AP
- CRIT

---

# 7. COMBATE ATUAL

## Regra principal
Quando um grupo tenta entrar numa célula ocupada por inimigos, a ação vira ataque.

## Fluxo do ataque
1. identifica célula atacante
2. identifica célula defensora
3. todos os atacantes válidos da célula participam
4. toca animação de lunge dos atacantes
5. soma ATK dos atacantes
6. divide o dano entre os defensores
7. cada defensor aplica redução por DEF
8. aplica dano
9. se HP chega a zero, morre

## Observações
- dano mínimo = 1
- crítico está preparado no sistema, mas o feedback visual ainda é simples
- não existe ainda ordem individual de ataque por personagem com skill selection
- o combate é resolvido por célula/grupo

---

# 8. IA INIMIGA

## `EnemyAI`
A IA atual funciona em turnos.

### Comportamento
- busca células ocupadas pelo player
- escolhe a célula visível mais próxima
- respeita `maxVisionRange`
- respeita `requireLineOfSight`
- tenta se mover em direção ao player
- se adjacente, ataca

### Observação de design
O inimigo continua com foco em perseguição direta por eixo dominante.
Ainda não existe pathfinding completo.

---

# 9. SPAWN DE INIMIGOS

Existe spawner com grupos de inimigos.

## Estado atual
- grupos podem nascer com quantidade aleatória
- número de inimigos por célula varia
- spawner tenta evitar célula do player
- spawner respeita capacidade máxima da célula

## Observação
Ainda é um spawner simples, não um diretor procedural completo de dungeon.

---

# 10. LOOT E ITENS

## `ItemData`
Item fixo via ScriptableObject.

Campos relevantes:
- nome
- descrição
- ícone
- tipo de slot
- raridade
- required level
- valor
- stat bonus

## `GeneratedItemInstance`
Versão runtime de item procedural.

Campos relevantes:
- nome
- descrição
- ícone
- slot type
- rarity
- required level
- value
- stat bonus

## `ItemGenerationProfile`
Perfil de geração procedural.

Define ranges para:
- HP
- ATK
- DEF
- AP
- CRIT

## `ItemGenerator`
Gera um `GeneratedItemInstance` a partir do profile.

## `EquipmentSlots`
Slots atuais:
- Weapon
- Armor
- Accessory

Aceita tanto item fixo quanto item gerado.

## Nota importante
Foi corrigido um bug em que itens gerados “fantasma” apareciam nos slots. Hoje os slots gerados são normalizados para evitar instâncias vazias contaminando stats.

---

# 11. LOOT DROP NO CHÃO

## `LootDropper`
Fica no inimigo.

### Responsabilidades
- ouvir morte do inimigo
- consultar tabela de drop
- rolar chance
- gerar item fixo ou procedural
- spawnar `GroundItem` na célula do inimigo morto

## `GroundItem`
Representa o item no chão.

### Estado atual
- tem visual simples por cor/sprite
- armazena item fixo ou gerado
- pode converter para `InventoryItemEntry`
- pode enviar para inventário
- pode tentar equipar direto
- só é destruído depois de confirmar sucesso

Isso foi ajustado porque antes havia caso em que o item sumia sem ir para mochila/equipamento.

---

# 12. INVENTÁRIO

## `PlayerInventory`
Inventário do player com tamanho fixo.

### Tamanho atual
- 20 slots

### Operações já implementadas
- adicionar item fixo
- adicionar item gerado
- remover item
- mover item de slot
- equipar a partir da mochila
- desequipar para mochila
- equipar direto um item vindo do chão

### Comportamento importante atual
Se o player equipa um item e já existe outro naquele slot:
- o item antigo tenta voltar para a mochila
- se não houver espaço, a troca pode falhar dependendo da operação

### Observação
Ainda não existe stack de itens, peso ou inventário persistente fora de runtime.

---

# 13. UI DE STATUS

## `StatsPanelUI`
Painel separado do loot/inventário.

### Mostra
- nome
- level
- XP
- pontos disponíveis
- HP
- ATK
- DEF
- AP
- CRIT

### Já foi usado também para depuração
Em alguns momentos a UI mostrou breakdown de Base/Level/Point/Item bonus para localizar origem dos stats.

## `StatsPanelAutoBuilder`
Monta automaticamente o painel de status quando necessário.

---

# 14. UI DE INVENTÁRIO / LOOT

## Estrutura atual
A UI foi redesenhada para grade.

### Layout desejado/atual
- coluna esquerda: 3 slots equipáveis, um abaixo do outro
- coluna central: inventário 5x4 = 20 slots
- coluna direita: chão também em grade 5x4 visualmente

## `LootWindowUI`
Janela principal do inventário/loot.

### Comportamento atual
- tecla `E` abre e fecha
- `Esc` fecha
- abre mesmo sem item no chão
- se houver item na célula do player, ele aparece na coluna `Ground Loot`

### Interações atuais
- click no chão → manda para mochila
- shift+click no chão → tenta equipar direto
- click na mochila → tenta equipar
- click no equipado → manda para mochila

## `LootWindowGridAutoBuilder`
Builder mais recente da UI, usado para montar layout em grade com slots quadrados.

## `ItemButtonUI`
Slot visual reutilizado para:
- equipados
- inventário
- itens no chão

Atualmente o slot já suporta:
- click
- shift+click
- hover
- base parcial para drag

### Estado do drag and drop
A base do componente existe, mas o drag and drop real ainda não está concluído no fluxo do inventário.

---

# 15. TOOLTIP

## `ItemTooltipUI`
Tooltip funcional ao passar o mouse sobre o slot.

### Estado atual
- mostra nome do item
- mostra apenas stats diferentes de zero
- não intercepta raycast do mouse
- fundo escuro
- texto branco
- foi corrigido bug de flicker causado pelo tooltip ficar na frente do slot

### Visual atual
- slot mostra só o ícone
- tooltip mostra o detalhe textual

---

# 16. PROBLEMAS IMPORTANTES JÁ RESOLVIDOS NESTA CONVERSA

## Core / Grid
- erro de registro em dicionário do grid
- entidades desalinhadas entre células
- correção para centro da célula (`.5`)
- paredes não sendo respeitadas
- visão inimiga sem bloqueio por parede

## Feedback / combate
- barra de vida não funcionando
- reconstrução da health bar do zero
- smooth movement quebrado
- damage number não aparecendo
- damage flash não funcionando

## Stats / equipamentos
- `StatBlock` contaminando bônus com valores default
- bônus de item aparecendo mesmo sem item equipado
- instâncias fantasmas de `GeneratedItemInstance`
- necessidade de normalização dos slots gerados

## UI
- painel de status não abria porque o controlador era desligado junto com a janela
- loot window não abria pelo mesmo motivo
- layout empilhado/centralizado sem organização
- builder da UI perdendo referência de prefab
- tooltip piscando/branco por raycast indevido

## Loot / inventário
- item do chão sumindo sem equipar nem ir para mochila
- dificuldade de trocar item equipado quando já existe outro item no slot

---

# 17. CONFIGURAÇÃO DE CENA / UNITY

## Objetos importantes na cena

### Managers
- `GridManager`
- `TurnManager`
- `DamageNumberManager`
- `PlayerItemPickup`

### UI
Dentro do mesmo `Canvas`:
- `StatsUIController`
- `StatsPanel`
- `LootWindowController`
- `LootWindow`
- `ItemTooltip`

## Player
Componentes esperados:
- `Entity`
- `CharacterStats`
- `EquipmentSlots`
- `PlayerInventory`
- `PlayerGridMovement`
- feedbacks visuais relevantes

## Enemy
Componentes esperados:
- `Entity`
- `CharacterStats`
- `EquipmentSlots` (quando necessário)
- `EnemyAI`
- `LootDropper`

## Ground item prefab
Deve ter:
- `GroundItem`
- `SpriteRenderer`
- `Collider2D` trigger

---

# 18. O QUE AINDA NÃO ESTÁ PRONTO

## Gameplay
- habilidades ativas
- buff/debuff
- seleção de skill antes do ataque
- AP tático real por ação no turno
- ordem de turnos avançada
- pathfinding inimigo completo

## Loot / inventário
- drag and drop real entre slots
- comparação de item melhor/pior
- inventário persistente/salvamento
- stacks de item
- descarte de item
- re-drop do item antigo no chão

## UI / UX
- ícones/arte final de todos os itens
- destaque visual por raridade
- comparação lado a lado no tooltip
- sons/UI polish

## Roguelike loop
- geração procedural de dungeon robusta
- salas/corredores completos
- escadas/andar seguinte
- loop de run completo

---

# 19. PRÓXIMOS PASSOS RECOMENDADOS

## Próximo passo mais lógico imediato
Finalizar o **drag and drop do inventário**.

### Objetivo
Permitir fluxo visual natural entre:
- chão → mochila
- mochila → equipado
- equipado → mochila
- possivelmente mochila ↔ mochila

## Depois disso
1. comparação de item atual vs item novo
2. cor/tooltip por raridade
3. AP real por ação
4. habilidades
5. procedural dungeon / loop roguelike

---

# 20. RESUMO EXECUTIVO

O projeto saiu de um protótipo de grid turn-based simples e hoje já possui:

- multi-entidade por célula
- combate funcional
- dano, morte e XP
- sistema de stats unificado
- progressão por nível e pontos
- itens fixos e procedurais
- equipamento funcional
- drop no chão
- inventário funcional
- UI de status
- UI de inventário/loot com tooltip

O próximo gargalo principal não é mais o combate básico.
Agora o foco mais natural é **polimento do inventário/loot UI e aprofundamento do sistema tático**.

---

# 21. OBSERVAÇÃO FINAL

Use este documento como a nova referência de continuidade.

Ele substitui o checkpoint antigo para qualquer continuação futura do projeto.
