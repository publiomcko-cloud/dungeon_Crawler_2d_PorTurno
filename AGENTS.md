# AGENTS.md

## Project
Unity dungeon crawler / tactical grid RPG.

Current state:
- 2D top-down
- turn-based grid movement
- multiple entities can share the same cell
- combat resolves between groups/cells
- items, equipment, loot on ground
- party-wide shared inventory
- per-entity equipment
- inventory / loot / equip UI already functional

This file is for Codex or any coding agent continuing the project.

---

## Non-negotiable output rules
Always follow these user rules:
- return full `.cs` files, never partial fragments when editing a script
- give step-by-step Unity implementation instructions
- instructions must be short and precise
- mention menu names, object names, components, inspector fields when relevant

Do not switch to pseudocode if the user asked for implementation.

---

## Current design decisions

### 1. Inventory model
The party uses **one shared inventory**.

Implications:
- side selector changes only the **equipment target entity**
- central inventory grid is shared by the whole party
- ground loot is handled through the shared inventory flow
- any party member can equip an item from the shared inventory
- unequipping returns the item to the shared inventory

### 2. Equipment model
Equipment is still **per entity**.

Implications:
- each player entity keeps its own `EquipmentSlots`
- selected player in the side panel defines which entity receives equip / unequip actions
- tooltips compare shared-inventory items against the currently selected entity's equipped item

### 3. UI architecture
`LootWindowUI` is a **controller** and is kept active so the inventory can always be opened.

`LootWindowGridAutoBuilder` is on the **visual window object**.

Important:
- `LootWindowUI` is **not** on the same GameObject as the visual window
- do not assume `GetComponent<LootWindowGridAutoBuilder>()` from the UI controller unless explicitly wired
- prefer serialized references for:
  - `windowRoot`
  - `windowBuilder`
  - `partyInventory`

### 4. First-open bug history
There was a real bug where the first inventory open showed no slots, and only later opens worked.

Root cause:
- controller opened the window before the visual builder had finished building / wiring
- builder could also be on a different object than the UI controller

Rule:
- do not reintroduce implicit initialization ordering bugs
- if changing window setup, preserve explicit build / wire flow

### 5. Auto-open ground loot history
There was a bug where the inventory seemed impossible to close while loot remained on the ground.

Root cause:
- `PlayerItemPickup` reopened the window every frame while standing on the same loot cell

Rule:
- auto-open must only happen on entering a loot cell, not continuously while standing still

---

## Scripts currently important

### Core / Gameplay
- `GridManager`
- `TurnManager`
- `Entity`
- `EnemyAI`
- `PlayerGridMovement`
- `EnemySpawner`

### Stats / progression
- `StatBlock`
- `CharacterStats`
- `Team`

### Items / equipment
- `ItemEnums`
- `ItemData`
- `GeneratedItemInstance`
- `ItemGenerationProfile`
- `ItemGenerator`
- `EquipmentSlots`

### Inventory / loot
- `InventoryItemEntry`
- `GroundItem`
- `PartyInventory` **(official inventory flow for the UI)**
- `PlayerInventory` **(legacy; may still exist in project but should not drive the party UI anymore)**
- `PlayerItemPickup`

### UI
- `LootWindowUI`
- `LootWindowGridAutoBuilder`
- `ItemButtonUI`
- `ItemTooltipUI`
- `StatsPanelUI`
- `StatsPanelAutoBuilder`

---

## Ground truth for the current UI behavior

### Loot window
- `E` opens / closes
- `Esc` closes
- tooltip must disappear when closing the window
- tooltip compares item vs selected entity equipment
- tooltip size follows content size
- drag uses a ghost icon; the source slot should not visually leave its place

### Side selector
- shows only existing alive player entities in the party
- expected range: 1 to 4 buttons
- each button shows:
  - player sprite on top
  - inventory/equipment index number below
- selecting another player changes only equipment target / equipment display
- selecting another player must not swap the shared inventory contents

### Shared inventory grid
- central inventory grid is the shared bag for the whole party
- empty slots must visually look empty immediately, without requiring click activation
- empty slots must still accept valid drops

### Ground loot grid
- ground loot enters the shared inventory
- equipping from ground equips to the currently selected party member

---

## Things that already broke before and should be treated carefully

1. **Mismatched builder/UI versions**
   - different `ConfigureReferences(...)` signatures broke runtime UI creation
   - if you change builder parameters, change both sides together

2. **Controller not on the same object as visual window**
   - assuming same-object lookup caused first-open failures

3. **Slot visuals on empty items**
   - empty slots looked occupied until clicked
   - if editing `ItemButtonUI`, make sure empty state is fully reset in `Setup()`

4. **Tooltip persistence**
   - tooltip could remain open after closing the inventory
   - always hide tooltip on window close / disable / destroy where relevant

5. **Auto-open re-trigger loop**
   - do not reintroduce frame-by-frame reopen behavior in `PlayerItemPickup`

---

## Coding guidance for future edits

### When editing inventory/equipment code
Prefer changes in this order:
- `PartyInventory`
- `LootWindowUI`
- `GroundItem`
- `ItemButtonUI`
- `ItemTooltipUI`

Avoid re-expanding logic into `PlayerInventory` unless explicitly asked.

### When editing UI build code
Keep these responsibilities separated:
- `LootWindowGridAutoBuilder` builds structure and wires references
- `LootWindowUI` owns runtime population and interactions

### When editing interaction flow
Preserve these interaction rules unless the user changes them:
- click ground item -> move to shared inventory
- shift+click ground item -> equip to selected entity
- click inventory item -> equip to selected entity
- click equipped item -> return to shared inventory
- drag inventory <-> inventory
- drag inventory -> equipment
- drag equipment -> inventory
- drag ground -> inventory
- drag ground -> equipment

### When adding a new system
Keep backward compatibility with the current checkpoint unless the user explicitly asks for refactor cleanup.

---

## Unity scene expectations
The project likely expects:
- one always-active controller object containing `LootWindowUI`
- one visual window object containing `LootWindowGridAutoBuilder`
- one shared `PartyInventory` component accessible by the UI controller

If references are missing, prefer inspector-assigned explicit references instead of fragile runtime guessing.

---

## How to respond to the user during future work
When making code changes:
1. explain briefly what will change
2. provide complete `.cs` files
3. provide short Unity setup instructions
4. mention any required inspector wiring
5. mention how to test

Keep explanations concise.

---

## Next likely work areas
These are natural next steps, not confirmed tasks:
- sync status panel with currently selected entity
- explicit party leader / party anchor rules for global interactions
- remove or deprecate old individual-inventory UI flow
- improve party-shared ground logic if the party splits across cells
- continued UI polish and equipment feedback

